using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurneraJardin.Api.Data;
using TurneraJardin.Api.Dtos;
using TurneraJardin.Api.Models;
using TurneraJardin.Api.Models.Enums;

namespace TurneraJardin.Api.Controllers;

/// <summary>
/// Gestión de accesos al panel (usuarios de dirección y de docentes). Solo la dirección
/// (Admin) puede crear logins, restablecer contraseñas y activar/desactivar accesos.
/// </summary>
[ApiController]
[Route("api/admin/usuarios")]
[Authorize(Roles = nameof(RolUsuario.Admin))]
public class AdminUsuariosController : ControllerBase
{
    private const string CaracteresPassword = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%";

    private readonly AppDbContext _db;

    public AdminUsuariosController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<UsuarioAdminDto>>> Listar()
    {
        var usuarios = await _db.Usuarios
            .Include(u => u.Docente)
            .OrderBy(u => u.Rol).ThenBy(u => u.NombreCompleto)
            .Select(u => new UsuarioAdminDto(
                u.Id, u.Email, u.NombreCompleto, u.Rol, u.DocenteId, u.Docente!.NombreCompleto, u.Activo))
            .ToListAsync();

        return Ok(usuarios);
    }

    /// <summary>
    /// Crea un acceso nuevo. Con DocenteId, crea el login individual de ese docente
    /// (para que solo pueda ver y gestionar sus propios turnos). Sin DocenteId, crea
    /// otro usuario de dirección (Admin).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UsuarioCredencialesDto>> Crear(UsuarioCreateDto dto)
    {
        string email;
        string nombreCompleto;
        RolUsuario rol;
        Docente? docente = null;

        if (dto.DocenteId is not null)
        {
            docente = await _db.Docentes.FindAsync(dto.DocenteId.Value);
            if (docente is null)
            {
                return NotFound(new { mensaje = "Docente no encontrado." });
            }

            var yaTieneUsuario = await _db.Usuarios.AnyAsync(u => u.DocenteId == docente.Id);
            if (yaTieneUsuario)
            {
                return Conflict(new { mensaje = "Ese docente ya tiene un usuario asignado. Si necesita otra contraseña, restablecela en vez de crear un usuario nuevo." });
            }

            email = string.IsNullOrWhiteSpace(dto.Email) ? docente.Email : dto.Email.Trim();
            nombreCompleto = docente.NombreCompleto;
            rol = RolUsuario.Docente;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.NombreCompleto))
            {
                return BadRequest(new { mensaje = "Para crear un usuario de dirección hacen falta el email y el nombre completo." });
            }

            if (dto.Rol == RolUsuario.Docente)
            {
                return BadRequest(new { mensaje = "Para crear un usuario Docente hace falta indicar DocenteId." });
            }

            email = dto.Email.Trim();
            nombreCompleto = dto.NombreCompleto.Trim();
            rol = dto.Rol ?? RolUsuario.Admin;
        }

        var emailEnUso = await _db.Usuarios.AnyAsync(u => u.Email.ToLower() == email.ToLower());
        if (emailEnUso)
        {
            return Conflict(new { mensaje = "Ya existe un usuario con ese email." });
        }

        var passwordTemporal = string.IsNullOrWhiteSpace(dto.Password) ? GenerarPasswordTemporal() : dto.Password.Trim();

        var usuario = new Usuario
        {
            Email = email,
            NombreCompleto = nombreCompleto,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(passwordTemporal),
            Rol = rol,
            DocenteId = docente?.Id,
            Activo = true
        };

        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Listar), new UsuarioCredencialesDto(usuario.Id, usuario.Email, usuario.NombreCompleto, usuario.Rol.ToString(), passwordTemporal));
    }

    /// <summary>Restablece la contraseña de un usuario. Sin NuevaPassword, se genera una temporal aleatoria.</summary>
    [HttpPost("{id:int}/restablecer-password")]
    public async Task<ActionResult<UsuarioCredencialesDto>> RestablecerPassword(int id, RestablecerPasswordDto dto)
    {
        var usuario = await _db.Usuarios.FindAsync(id);
        if (usuario is null)
        {
            return NotFound();
        }

        var passwordTemporal = string.IsNullOrWhiteSpace(dto.NuevaPassword) ? GenerarPasswordTemporal() : dto.NuevaPassword.Trim();
        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(passwordTemporal);
        await _db.SaveChangesAsync();

        return Ok(new UsuarioCredencialesDto(usuario.Id, usuario.Email, usuario.NombreCompleto, usuario.Rol.ToString(), passwordTemporal));
    }

    [HttpPut("{id:int}/activar")]
    public async Task<IActionResult> Activar(int id)
    {
        var usuario = await _db.Usuarios.FindAsync(id);
        if (usuario is null)
        {
            return NotFound();
        }

        usuario.Activo = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Desactiva un acceso (no lo borra, para no perder trazabilidad). No se puede desactivar al último Admin activo.</summary>
    [HttpPost("{id:int}/desactivar")]
    public async Task<IActionResult> Desactivar(int id)
    {
        var usuario = await _db.Usuarios.FindAsync(id);
        if (usuario is null)
        {
            return NotFound();
        }

        if (usuario.Rol == RolUsuario.Admin)
        {
            var otrosAdminsActivos = await _db.Usuarios
                .CountAsync(u => u.Rol == RolUsuario.Admin && u.Activo && u.Id != id);

            if (otrosAdminsActivos == 0)
            {
                return BadRequest(new { mensaje = "No se puede desactivar el último usuario de dirección activo." });
            }
        }

        usuario.Activo = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static string GenerarPasswordTemporal()
    {
        var bytes = RandomNumberGenerator.GetBytes(12);
        return new string(bytes.Select(b => CaracteresPassword[b % CaracteresPassword.Length]).ToArray());
    }
}
