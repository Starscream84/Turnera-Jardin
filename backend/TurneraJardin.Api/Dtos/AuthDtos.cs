using System.ComponentModel.DataAnnotations;

namespace TurneraJardin.Api.Dtos;

public record LoginDto(
    string Email,
    string Password
);

public record AuthResponseDto(
    string Token,
    int UsuarioId,
    string NombreCompleto,
    string Email,
    string Rol
)
{
    private (string Token, DateTime ExpiraUtc) token;
    private int id;
    private string v;

    public AuthResponseDto((string Token, DateTime ExpiraUtc) token, int id, string nombreCompleto, string email, string v)
        : this(token.Token, id, nombreCompleto, email, v)
    {
        this.token = token;
    }
}

public record LoginResponseDto(string Token, DateTime ExpiraUtc, string NombreCompleto, string Rol, int? DocenteId);
