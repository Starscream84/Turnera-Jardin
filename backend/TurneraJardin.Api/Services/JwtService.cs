using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TurneraJardin.Api.Models;

namespace TurneraJardin.Api.Services;

public class JwtService : IJwtService
{
    /// <summary>Name of the custom claim with the teacher id, for users with the Teacher role.</summary>
    public const string TeacherIdClaim = "teacherId";

    private readonly IConfiguration _config;

    public JwtService(IConfiguration config)
    {
        _config = config;
    }

    public (string Token, DateTime ExpiresAtUtc) GenerateToken(User user)
    {
        var jwtConfig = _config.GetSection("Jwt");
        var key = jwtConfig["Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured in appsettings.json");
        var expirationHours = double.TryParse(jwtConfig["ExpirationHours"], out var h) ? h : 8;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        if (user.TeacherId is not null)
        {
            claims.Add(new Claim(TeacherIdClaim, user.TeacherId.Value.ToString()));
        }

        var expiresAtUtc = DateTime.UtcNow.AddHours(expirationHours);

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtConfig["Issuer"],
            audience: jwtConfig["Audience"],
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }
}
