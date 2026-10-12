using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TurneraJardin.Api.Data;
using TurneraJardin.Api.Dtos;
using TurneraJardin.Api.Models;
using TurneraJardin.Api.Auth;
using TurneraJardin.Api.Services;

namespace TurneraJardin.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    // Se verifica contra este hash cuando el email no existe, para que la respuesta tarde lo mismo
    // y no se pueda deducir qué emails están registrados midiendo tiempos.
    private static readonly string HashFicticio = BCrypt.Net.BCrypt.HashPassword("usuario-inexistente");

    private readonly AppDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IAuditoriaService _auditoria;

    public AuthController(AppDbContext context, IJwtService jwtService, IAuditoriaService auditoria)
    {
        _context = context;
        _jwtService = jwtService;
        _auditoria = auditoria;
    }

    /// <summary>Inicia sesión. Limitado por IP para frenar intentos de adivinar contraseñas.</summary>
    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new { mensaje = "Email y contraseña son obligatorios." });
        }

        var email = dto.Email.Trim();
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

        var passwordOk = BCrypt.Net.BCrypt.Verify(dto.Password, usuario?.PasswordHash ?? HashFicticio);

        if (usuario is null || !passwordOk)
        {
            await _auditoria.RegistrarAsync("LoginFallido", "Usuario", usuario?.Id.ToString(),
                $"Email: {email}");
            return Unauthorized(new { mensaje = "Credenciales inválidas." });
        }

        // Recién con la contraseña correcta se informa el estado de la cuenta.
        if (!usuario.Activo)
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                new { mensaje = "La cuenta está suspendida. Consultá con la dirección." });
        }

        if (usuario.DebeCambiarPassword && usuario.PasswordTemporalVenceUtc < DateTime.UtcNow)
        {
            return Unauthorized(new { mensaje = "La contraseña temporal venció. Pedile a la dirección que genere una nueva." });
        }

        return Ok(ConstruirRespuesta(usuario));
    }

    /// <summary>
    /// Cambia la contraseña propia. Es la única acción permitida con una contraseña temporal.
    /// Devuelve un token nuevo, porque el anterior queda invalidado al cambiar la versión de sesión.
    /// </summary>
    [HttpPost("cambiar-password")]
    [Authorize(Policy = Politicas.Personal)]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<AuthResponseDto>> CambiarPassword([FromBody] CambiarPasswordDto dto)
    {
        var usuarioId = User.GetUsuarioId();
        if (usuarioId == 0)
        {
            return Unauthorized();
        }

        var usuario = await _context.Usuarios.FindAsync(usuarioId);

        if (usuario is null)
        {
            return Unauthorized();
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.PasswordActual, usuario.PasswordHash))
        {
            return BadRequest(new { mensaje = "La contraseña actual no es correcta." });
        }

        if (!Contrasenas.EsValida(dto.PasswordNueva))
        {
            return BadRequest(new { mensaje = "La contraseña debe tener entre 10 y 72 caracteres, con letras y números." });
        }

        if (BCrypt.Net.BCrypt.Verify(dto.PasswordNueva, usuario.PasswordHash))
        {
            return BadRequest(new { mensaje = "La contraseña nueva tiene que ser distinta de la actual." });
        }

        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.PasswordNueva);
        usuario.DebeCambiarPassword = false;
        usuario.PasswordTemporalVenceUtc = null;
        usuario.VersionSesion++;

        await _auditoria.RegistrarAsync("CambioPassword", "Usuario", usuario.Id.ToString(), "Usuario cambió su contraseña");

        return Ok(ConstruirRespuesta(usuario));
    }

    private AuthResponseDto ConstruirRespuesta(Usuario usuario)
    {
        var tokenResult = _jwtService.GenerarToken(usuario);

        return new AuthResponseDto(
            tokenResult.Token,
            tokenResult.ExpiraUtc,
            usuario.Id,
            usuario.NombreCompleto,
            usuario.Email,
            usuario.Rol.ToString(),
            usuario.DocenteId,
            usuario.DebeCambiarPassword);
    }
}
