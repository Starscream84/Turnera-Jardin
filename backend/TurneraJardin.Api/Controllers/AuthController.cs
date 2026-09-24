using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurneraJardin.Api.Data;
using TurneraJardin.Api.Dtos;
using TurneraJardin.Api.Services;

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

        return Ok(new LoginResponseDto(token, expiraUtc, usuario.NombreCompleto, usuario.Rol.ToString(), usuario.DocenteId));
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
}
