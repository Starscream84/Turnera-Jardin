using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurneraJardin.Api.Data;
using TurneraJardin.Api.Dtos;
using TurneraJardin.Api.Models;
using TurneraJardin.Api.Models.Enums;
using TurneraJardin.Api.Services;

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
                u.Id, u.Email, u.FullName, u.Role, u.TeacherId, u.Teacher!.FirstName + " " + u.Teacher!.LastName, u.IsActive, u.PhotoUrl, u.MustChangePassword))
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
                return this.ApiError(StatusCodes.Status404NotFound, ErrorCodes.TeacherNotFound, "Docente no encontrado.");
            }

            var alreadyHasUser = await _db.Users.AnyAsync(u => u.TeacherId == teacher.Id);
            if (alreadyHasUser)
            {
                return this.ApiError(StatusCodes.Status409Conflict, ErrorCodes.TeacherHasUser, "Ese docente ya tiene un usuario asignado. Si necesita otra contraseña, restablecela en vez de crear un usuario nuevo.");
            }

            email = string.IsNullOrWhiteSpace(dto.Email) ? teacher.Email : dto.Email.Trim();
            fullName = teacher.FullName;
            role = UserRole.Teacher;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.FullName))
            {
                return this.ApiError(StatusCodes.Status400BadRequest, ErrorCodes.ValidationError, "Para crear un usuario de dirección hacen falta el email y el nombre completo.");
            }

            if (dto.Role == UserRole.Teacher)
            {
                return this.ApiError(StatusCodes.Status400BadRequest, ErrorCodes.ValidationError, "Para crear un usuario Docente hace falta indicar el docente.");
            }

            email = dto.Email.Trim();
            fullName = dto.FullName.Trim();
            role = dto.Role ?? UserRole.Admin;
        }

        // Direct comparison: the Email column uses MySQL's case-insensitive collation (and its unique index).
        var emailInUse = await _db.Users.AnyAsync(u => u.Email == email);
        if (emailInUse)
        {
            return this.ApiError(StatusCodes.Status409Conflict, ErrorCodes.UserEmailInUse, "Ya existe un usuario con ese email.");
        }

        var temporaryPassword = string.IsNullOrWhiteSpace(dto.Password) ? GenerateTemporaryPassword() : dto.Password.Trim();

        var user = new User
        {
            Email = email,
            FullName = fullName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword),
            // Temporary password: the first login forces a new one (H30).
            MustChangePassword = true,
            Role = role,
            TeacherId = teacher?.Id,
            IsActive = true
        };

        _db.Users.Add(user);
        // Two simultaneous requests for the same teacher/email hit the unique indexes → 409 (ApiExceptionHandler).
        await _db.SaveChangesAsync();

        return Created($"/api/admin/users/{user.Id}", new UserCredentialsDto(user.Id, user.Email, user.FullName, user.Role.ToString(), temporaryPassword));
    }

    /// <summary>Resets a user's password. Without NewPassword, a random temporary one is generated.</summary>
    [HttpPost("{id:int}/reset-password")]
    public async Task<ActionResult<UserCredentialsDto>> ResetPassword(int id, ResetPasswordDto dto)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null)
        {
            return this.ApiError(StatusCodes.Status404NotFound, ErrorCodes.UserNotFound, "Usuario no encontrado.");
        }

        var temporaryPassword = string.IsNullOrWhiteSpace(dto.NewPassword) ? GenerateTemporaryPassword() : dto.NewPassword.Trim();
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword);
        user.MustChangePassword = true;
        await _db.SaveChangesAsync();

        return Ok(new UserCredentialsDto(user.Id, user.Email, user.FullName, user.Role.ToString(), temporaryPassword));
    }

    [HttpPut("{id:int}/activate")]
    public async Task<IActionResult> Activate(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user is null)
        {
            return this.ApiError(StatusCodes.Status404NotFound, ErrorCodes.UserNotFound, "Usuario no encontrado.");
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
            return this.ApiError(StatusCodes.Status404NotFound, ErrorCodes.UserNotFound, "Usuario no encontrado.");
        }

        if (user.Role == UserRole.Admin)
        {
            var otherActiveAdmins = await _db.Users
                .CountAsync(u => u.Role == UserRole.Admin && u.IsActive && u.Id != id);

            if (otherActiveAdmins == 0)
            {
                return this.ApiError(StatusCodes.Status400BadRequest, ErrorCodes.LastAdmin, "No se puede desactivar el último usuario de dirección activo.");
            }
        }

        user.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// 12 random characters. GetInt32 picks each one uniformly (the old "byte % length" made the first
    /// characters of the alphabet more likely).
    /// </summary>
    private static string GenerateTemporaryPassword() =>
        new(Enumerable.Range(0, 12)
            .Select(_ => PasswordCharacters[RandomNumberGenerator.GetInt32(PasswordCharacters.Length)])
            .ToArray());
}
