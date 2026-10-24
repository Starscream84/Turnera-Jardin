using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurneraJardin.Api.Data;
using TurneraJardin.Api.Dtos;
using TurneraJardin.Api.Services;
using System.Linq;
using Microsoft.AspNetCore.RateLimiting;

namespace TurneraJardin.Api.Controllers;

/// <summary>Login for the admin panel (management and teachers).</summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IJwtService _jwtService;

    public AuthController(AppDbContext db, IJwtService jwtService)
    {
        _db = db;
        _jwtService = jwtService;
    }

    [EnableRateLimiting("login")]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.IsActive);

        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Email o contraseña incorrectos." });
        }

        var (token, expiresAtUtc) = _jwtService.GenerateToken(user);

        return Ok(new LoginResponseDto(token, expiresAtUtc, user.FullName, user.Role.ToString(), user.TeacherId, user.PhotoUrl));
    }

    /// <summary>Lets any logged-in user (management or teacher) change their own password.</summary>
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangeOwnPasswordDto dto)
    {
        var idClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (idClaim is null || !int.TryParse(idClaim, out var userId))
        {
            return Unauthorized();
        }

        var user = await _db.Users.FindAsync(userId);
        if (user is null)
        {
            return Unauthorized();
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
        {
            return BadRequest(new { message = "La contraseña actual no es correcta." });
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>Uploads (or replaces) the logged-in user's profile photo. Accepts JPG, PNG or WEBP, up to 2 MB.</summary>
    [Authorize]
    [HttpPost("my-photo")]
    public async Task<ActionResult<object>> UploadPhoto(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "No se recibió ningún archivo." });
        }

        var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!validExtensions.Contains(extension))
        {
            return BadRequest(new { message = "Solo se aceptan imágenes JPG, PNG o WEBP." });
        }

        const long maxBytes = 2 * 1024 * 1024; // 2 MB
        if (file.Length > maxBytes)
        {
            return BadRequest(new { message = "La imagen no puede superar los 2 MB." });
        }

        var idClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (idClaim is null || !int.TryParse(idClaim, out var userId))
        {
            return Unauthorized();
        }

        var user = await _db.Users.FindAsync(userId);
        if (user is null)
        {
            return Unauthorized();
        }

        // The folder keeps its Spanish name: it is part of the public URL already stored in Users.PhotoUrl.
        var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fotos-perfil");
        Directory.CreateDirectory(folder);

        // If the user already had a photo, delete it so orphan files don't pile up.
        if (!string.IsNullOrEmpty(user.PhotoUrl))
        {
            var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.PhotoUrl.TrimStart('/'));
            if (System.IO.File.Exists(oldPath))
            {
                System.IO.File.Delete(oldPath);
            }
        }

        var fileName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(folder, fileName);
        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        user.PhotoUrl = $"/fotos-perfil/{fileName}";
        await _db.SaveChangesAsync();

        return Ok(new { photoUrl = user.PhotoUrl });
    }
}
