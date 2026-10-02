using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using TurneraJardin.Api.Models;
using TurneraJardin.Api.Models.Enums;
using TurneraJardin.Api.Options;

namespace TurneraJardin.Api.Services;

/// <summary>
/// Representa el resultado de la generación de un token JWT.
/// </summary>
public class TokenResult
{
    public string Token { get; set; }
    public DateTime ExpiraUtc { get; set; }

    public TokenResult(string token, DateTime expiraUtc)
    {
        Token = token;
        ExpiraUtc = expiraUtc;
    }
}

/// <summary>
/// Interfaz para el servicio de generación de tokens JWT.
/// </summary>
public interface IJwtService
{
    TokenResult GenerarToken(Usuario usuario);
}

/// <summary>
/// Implementación del servicio de generación de tokens JWT.
/// </summary>
public class JwtService : IJwtService
{
    private readonly JwtOptions _options;

    public JwtService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public TokenResult GenerarToken(Usuario usuario)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiraUtc = DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Name, usuario.NombreCompleto),
            new(ClaimTypes.Role, usuario.Rol.ToString()),
            new("sv", usuario.VersionSesion.ToString())
        };

        if (usuario.DocenteId.HasValue)
        {
            claims.Add(new Claim("docenteId", usuario.DocenteId.Value.ToString()));
        }

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiraUtc,
            signingCredentials: credentials
        );

        var tokenHandler = new JwtSecurityTokenHandler();
        return new TokenResult(tokenHandler.WriteToken(token), expiraUtc);
    }
}
