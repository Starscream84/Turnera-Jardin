using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurneraJardin.Api.Data;
using TurneraJardin.Api.Dtos;
using TurneraJardin.Api.Services;
using System.Linq;

namespace TurneraJardin.Api.Controllers;

/// <summary>Login para el panel de administración (directivos y docentes).</summary>
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

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.Activo);

        if (usuario is null || !BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash))
        {
            return Unauthorized(new { mensaje = "Email o contraseña incorrectos." });
        }

        var (token, expiraUtc) = _jwtService.GenerarToken(usuario);

        return Ok(new LoginResponseDto(token, expiraUtc, usuario.NombreCompleto, usuario.Rol.ToString(), usuario.DocenteId, usuario.FotoUrl));
    }

    /// <summary>Permite a cualquier usuario logueado (dirección o docente) cambiar su propia contraseña.</summary>
    [Authorize]
    [HttpPost("cambiar-password")]
    public async Task<IActionResult> CambiarPassword(CambiarPasswordPropiaDto dto)
    {
        var idClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (idClaim is null || !int.TryParse(idClaim, out var usuarioId))
        {
            return Unauthorized();
        }

        var usuario = await _db.Usuarios.FindAsync(usuarioId);
        if (usuario is null)
        {
            return Unauthorized();
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.PasswordActual, usuario.PasswordHash))
        {
            return BadRequest(new { mensaje = "La contraseña actual no es correcta." });
        }

        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.PasswordNueva);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>Sube (o reemplaza) la foto de perfil del usuario logueado. Acepta JPG, PNG o WEBP, hasta 2 MB.</summary>
    [Authorize]
    [HttpPost("mi-foto")]
    public async Task<ActionResult<object>> SubirFoto(IFormFile archivo)
    {
        if (archivo is null || archivo.Length == 0)
        {
            return BadRequest(new { mensaje = "No se recibió ningún archivo." });
        }

        var extensionesValidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!extensionesValidas.Contains(extension))
        {
            return BadRequest(new { mensaje = "Solo se aceptan imágenes JPG, PNG o WEBP." });
        }

        const long maxBytes = 2 * 1024 * 1024; // 2 MB
        if (archivo.Length > maxBytes)
        {
            return BadRequest(new { mensaje = "La imagen no puede superar los 2 MB." });
        }

        var idClaim = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (idClaim is null || !int.TryParse(idClaim, out var usuarioId))
        {
            return Unauthorized();
        }

        var usuario = await _db.Usuarios.FindAsync(usuarioId);
        if (usuario is null)
        {
            return Unauthorized();
        }

        var carpeta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fotos-perfil");
        Directory.CreateDirectory(carpeta);

        // Si ya tenía una foto propia, la borramos para no acumular archivos huérfanos.
        if (!string.IsNullOrEmpty(usuario.FotoUrl))
        {
            var rutaVieja = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", usuario.FotoUrl.TrimStart('/'));
            if (System.IO.File.Exists(rutaVieja))
            {
                System.IO.File.Delete(rutaVieja);
            }
        }

        var nombreArchivo = $"{Guid.NewGuid()}{extension}";
        var rutaCompleta = Path.Combine(carpeta, nombreArchivo);
        using (var stream = new FileStream(rutaCompleta, FileMode.Create))
        {
            await archivo.CopyToAsync(stream);
        }

        usuario.FotoUrl = $"/fotos-perfil/{nombreArchivo}";
        await _db.SaveChangesAsync();

        return Ok(new { fotoUrl = usuario.FotoUrl });
    }
}
