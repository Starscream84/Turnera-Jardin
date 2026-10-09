namespace TurneraJardin.Api.Auth;

/// <summary>
/// Constantes de políticas de autorización para ASP.NET Core.
/// </summary>
public static class Politicas
{
    /// <summary>Solo SuperAdmin puede acceder.</summary>
    public const string SoloSuperAdmin = nameof(SoloSuperAdmin);

    /// <summary>Admin y SuperAdmin pueden acceder.</summary>
    public const string Direccion = nameof(Direccion);

    /// <summary>Solo usuarios autenticados pueden acceder.</summary>
    public const string Personal = nameof(Personal);
}
