using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TurneraJardin.Api.Data;
using TurneraJardin.Api.Dtos;
using TurneraJardin.Api.Services;

namespace TurneraJardin.Api.Controllers;

/// <summary>Login for the admin panel (management and teachers).</summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    // Hash of a random password, used when the email does not exist so the answer takes the same time
    // as a wrong password (otherwise the response time reveals which emails have an account).
    private static readonly string DummyHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString());

    private readonly AppDbContext _db;
    private readonly IJwtService _jwtService;
    private readonly LoginAttemptTracker _attempts;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AppDbContext db, IJwtService jwtService, LoginAttemptTracker attempts, ILogger<AuthController> logger)
    {
        _db = db;
        _jwtService = jwtService;
        _attempts = attempts;
        _logger = logger;
    }

    [EnableRateLimiting(RateLimitPolicies.Login)]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        if (_attempts.IsLocked(email))
        {
            return this.ApiError(StatusCodes.Status429TooManyRequests, ErrorCodes.AccountLocked, "Demasiados intentos fallidos con este usuario. Esperá 15 minutos o pedí que te restablezcan la contraseña.");
        }

        // Direct comparison: the Email column uses MySQL's case-insensitive collation, so this uses the unique index.
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

        var passwordOk = BCrypt.Net.BCrypt.Verify(dto.Password, user?.PasswordHash ?? DummyHash);
        if (user is null || !passwordOk)
        {
            _attempts.RegisterFailure(email);
            return this.ApiError(StatusCodes.Status401Unauthorized, ErrorCodes.InvalidCredentials, "Email o contraseña incorrectos.");
        }

        _attempts.Reset(email);
        return Ok(CreateSession(user));
    }

    /// <summary>
    /// Lets any logged-in user (management or teacher) change their own password. Every token issued
    /// before stops working (other devices are logged out); the response carries a new session for this one.
    /// </summary>
    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult<LoginResponseDto>> ChangePassword(ChangeOwnPasswordDto dto)
    {
        var user = await CurrentUserAsync();
        if (user is null)
        {
            return this.ApiError(StatusCodes.Status401Unauthorized, ErrorCodes.SessionExpired, "Tu sesión venció. Volvé a iniciar sesión.");
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
        {
            return this.ApiError(StatusCodes.Status400BadRequest, ErrorCodes.WrongCurrentPassword, "La contraseña actual no es correcta.");
        }

        if (dto.NewPassword == dto.CurrentPassword)
        {
            return this.ApiError(StatusCodes.Status400BadRequest, ErrorCodes.ValidationError, "La contraseña nueva tiene que ser distinta de la actual.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.MustChangePassword = false;
        await _db.SaveChangesAsync();
        _logger.LogInformation("User {UserId} changed their password", user.Id);

        return Ok(CreateSession(user));
    }

    /// <summary>Uploads (or replaces) the logged-in user's profile photo. Accepts JPG, PNG or WEBP, up to 2 MB.</summary>
    [Authorize]
    [HttpPost("my-photo")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<ActionResult<object>> UploadPhoto(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            return this.ApiError(StatusCodes.Status400BadRequest, ErrorCodes.InvalidImage, "No se recibió ningún archivo.");
        }

        const long maxBytes = 2 * 1024 * 1024; // 2 MB
        if (file.Length > maxBytes)
        {
            return this.ApiError(StatusCodes.Status400BadRequest, ErrorCodes.InvalidImage, "La imagen no puede superar los 2 MB.");
        }

        // The file is rebuilt keeping only the image itself: no EXIF/GPS or other metadata, nothing
        // hidden after the end of the image, and a size limit in pixels (ImageSanitizer, H12).
        // The type comes from the real content, never from the file name.
        byte[] original;
        await using (var input = file.OpenReadStream())
        using (var buffer = new MemoryStream((int)file.Length))
        {
            await input.CopyToAsync(buffer);
            original = buffer.ToArray();
        }

        var clean = ImageSanitizer.Sanitize(original, out var imageError);
        if (clean is null)
        {
            return this.ApiError(StatusCodes.Status400BadRequest, ErrorCodes.InvalidImage, imageError ?? "Solo se aceptan imágenes JPG, PNG o WEBP.");
        }

        var user = await CurrentUserAsync();
        if (user is null)
        {
            return this.ApiError(StatusCodes.Status401Unauthorized, ErrorCodes.SessionExpired, "Tu sesión venció. Volvé a iniciar sesión.");
        }

        // The folder keeps its Spanish name: it is part of the public URL already stored in Users.PhotoUrl.
        // In Railway it is a persistent volume mounted on /app/wwwroot/fotos-perfil, so photos survive deploys.
        var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fotos-perfil");
        Directory.CreateDirectory(folder);

        // If the user already had a photo, delete it so orphan files don't pile up.
        if (!string.IsNullOrEmpty(user.PhotoUrl))
        {
            var oldName = Path.GetFileName(user.PhotoUrl); // never a path: avoids deleting anything outside the folder
            var oldPath = Path.Combine(folder, oldName);
            if (System.IO.File.Exists(oldPath))
            {
                System.IO.File.Delete(oldPath);
            }
        }

        var fileName = $"{Guid.NewGuid()}{clean.Extension}";
        var fullPath = Path.Combine(folder, fileName);
        await System.IO.File.WriteAllBytesAsync(fullPath, clean.Data);

        user.PhotoUrl = $"/fotos-perfil/{fileName}";
        await _db.SaveChangesAsync();

        return Ok(new { photoUrl = user.PhotoUrl });
    }

    private LoginResponseDto CreateSession(Models.User user)
    {
        var (token, expiresAtUtc) = _jwtService.GenerateToken(user);
        return new LoginResponseDto(token, expiresAtUtc, user.FullName, user.Role.ToString(), user.TeacherId, user.PhotoUrl, user.MustChangePassword);
    }

    private async Task<Models.User?> CurrentUserAsync()
    {
        var idClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return idClaim is not null && int.TryParse(idClaim, out var userId)
            ? await _db.Users.FindAsync(userId)
            : null;
    }
}
