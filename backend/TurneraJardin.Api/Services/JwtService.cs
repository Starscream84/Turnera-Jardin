using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TurneraJardin.Api.Models;

namespace TurneraJardin.Api.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _config;

    public JwtService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerarToken(Usuario usuario)
    {
        var secretKey = _config["Jwt:SecretKey"] ?? throw new InvalidOperationException("Jwt:SecretKey no está configurada.");
        var issuer = _config["Jwt:Issuer"] ?? "TurneraJardinApi";
        var audience = _config["Jwt:Audience"] ?? "TurneraJardinClient";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Name, usuario.NombreCompleto),
            new Claim(ClaimTypes.Role, usuario.Rol.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    (string Token, DateTime ExpiraUtc) IJwtService.GenerarToken(Usuario usuario)
    {
        throw new NotImplementedException();
    }
}