using TurneraJardin.Api.Models;

namespace TurneraJardin.Api.Services;

public interface IJwtService
{
    (string Token, DateTime ExpiraUtc) GenerarToken(Usuario usuario);
}
