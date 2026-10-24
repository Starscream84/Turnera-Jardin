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
/// Management of panel logins (management and teacher users). Only management
/// (Admin) can create logins, reset passwords and activate/deactivate access.
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = nameof(UserRole.Admin))]
public class AdminUsersController : ControllerBase
{
    private const string PasswordCharacters = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%";

    private readonly AppDbContext _db;

    public AdminUsersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserAdminDto>>> List()
    {
        var users = await _db.Users
            .Include(u => u.Teacher)
            .OrderBy(u => u.Role).ThenBy(u => u.FullName)
            .Select(u => new UserAdminDto(
                u.Id, u.Email, u.FullName, u.Role, u.TeacherId, u.Teacher!.FullName, u.IsActive, u.PhotoUrl))
            .ToListAsync();

        return Ok(users);
    }

    /// <summary>
    /// Creates a new login. With TeacherId, it creates that teacher's individual login
    /// (so they can only see and manage their own appointments). Without TeacherId, it creates
    /// another management user (Admin by default).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserCredentialsDto>> Create(UserCreateDto dto)
    {
        string email;
        string fullName;
        UserRole role;
        Teacher? teacher = null;

        if (dto.TeacherId is not null)
        {
            teacher = await _db.Teachers.FindAsync(dto.TeacherId.Value);
            if (teacher is null)
            {
                return NotFound(new { message = "Docente no encontrado." });
            }

            var alreadyHasUser = await _db.Users.AnyAsync(u => u.TeacherId == teacher.Id);
            if (alreadyHasUser)
            {
                return Conflict(new { message = "Ese docente ya tiene un usuario asignado. Si necesita otra contraseña, restablecela en vez de crear un usuario nuevo." });
            }

            email = string.IsNullOrWhiteSpace(dto.Email) ? teacher.Email : dto.Email.Trim();
            fullName = teacher.FullName;
            role = UserRole.Teacher;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.FullName))
            {
                return BadRequest(new { message = "Para crear un usuario de dirección hacen falta el email y el nombre completo." });
            }

            if (dto.Role == UserRole.Teacher)
            {
                return BadRequest(new { message = "Para crear un usuario Docente hace falta indicar el docente." });
            }

            email = dto.Email.Trim();
            fullName = dto.FullName.Trim();
            role = dto.Role ?? UserRole.Admin;
        }

        var emailInUse = await _db.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower());
        if (emailInUse)
        {
            return Conflict(new { message = "Ya existe un usuario con ese email." });
        }

        var temporaryPassword = string.IsNullOrWhiteSpace(dto.Password) ? GenerateTemporaryPassword() : dto.Password.Trim();

        var user = new User
        {
            Email = email,
            FullName = fullName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword),
            Role = role,
            TeacherId = teacher?.Id,
            IsActive = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(List), new UserCredentialsDto(user.Id, user.Email, user.FullName, user.Role.ToString(), temporaryPassword));
    }

    /// <summary>Resets a user's password. Without NewPassword, a random temporary one is generated.</summary>
    [HttpPost("{id:int}/reset-password")]
    public async Task<ActionResult<UserCredentialsDto>> ResetPassword(int id, ResetPasswordDto dto)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var temporaryPassword = string.IsNullOrWhiteSpace(dto.NewPassword) ? GenerateTemporaryPassword() : dto.NewPassword.Trim();
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword);
        await _db.SaveChangesAsync();

        return Ok(new UserCredentialsDto(user.Id, user.Email, user.FullName, user.Role.ToString(), temporaryPassword));
    }

    [HttpPut("{id:int}/activate")]
    public async Task<IActionResult> Activate(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        user.IsActive = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Deactivates a login (does not delete it, to keep traceability). The last active Admin cannot be deactivated.</summary>
    [HttpPost("{id:int}/deactivate")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        if (user.Role == UserRole.Admin)
        {
            var otherActiveAdmins = await _db.Users
                .CountAsync(u => u.Role == UserRole.Admin && u.IsActive && u.Id != id);

            if (otherActiveAdmins == 0)
            {
                return BadRequest(new { message = "No se puede desactivar el último usuario de dirección activo." });
            }
        }

        user.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static string GenerateTemporaryPassword()
    {
        var bytes = RandomNumberGenerator.GetBytes(12);
        return new string(bytes.Select(b => PasswordCharacters[b % PasswordCharacters.Length]).ToArray());
    }
}
