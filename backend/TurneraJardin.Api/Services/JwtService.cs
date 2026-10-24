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

    public (string Token, DateTime ExpiraUtc) GenerarToken(Usuario usuario)
    {
        var jwtConfig = _config.GetSection("Jwt");
        var key = jwtConfig["Key"]
            ?? throw new InvalidOperationException("Falta configurar Jwt:Key en appsettings.json");
        var horasExpiracion = double.TryParse(jwtConfig["HorasExpiracion"], out var h) ? h : 8;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Email),
            new(ClaimTypes.Name, usuario.NombreCompleto),
            new(ClaimTypes.Role, usuario.Rol.ToString())
        };

        if (usuario.DocenteId is not null)
        {
            claims.Add(new Claim("docenteId", usuario.DocenteId.Value.ToString()));
        }

        var expiraUtc = DateTime.UtcNow.AddHours(horasExpiracion);

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtConfig["Issuer"],
            audience: jwtConfig["Audience"],
            claims: claims,
            expires: expiraUtc,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiraUtc);
    }
}
