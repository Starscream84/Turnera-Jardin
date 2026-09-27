using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurneraJardin.Api.Data;
using TurneraJardin.Api.Dtos;
using TurneraJardin.Api.Services;

namespace TurneraJardin.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IJwtService _jwtService;

    public AuthController(AppDbContext context, IJwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    /// <summary>
    /// Inicia sesión para docentes o personal administrativo.
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new { mensaje = "Email y contraseña son obligatorios." });
        }

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

        if (usuario == null || !BCrypt.Net.BCrypt.Verify(dto.Password, usuario.PasswordHash))
        {
            return Unauthorized(new { mensaje = "Credenciales inválidas." });
        }

        // Generar el token JWT
        var resultadoToken = _jwtService.GenerarToken(usuario);

        return Ok(new AuthResponseDto(
            resultadoToken.Token,
            usuario.Id,
            usuario.NombreCompleto,
            usuario.Email,
            usuario.Rol.ToString()
        ));
    }
}