namespace TurneraJardin.Api.Services;

/// <summary>Configuración del envío de emails (sección "Email" de appsettings.json o variables de entorno Email__*).</summary>
public class EmailOptions
{
    public const string SeccionConfig = "Email";

    /// <summary>Si está en false no se manda nada: el envío se simula y queda en el log (útil para desarrollo).</summary>
    public bool Habilitado { get; set; } = false;

    /// <summary>API key de Brevo. No va en el repo: se carga como variable de entorno Email__ApiKey.</summary>
    public string ApiKey { get; set; } = "";

    /// <summary>Dirección desde la que salen los mails. Tiene que estar verificada como remitente en Brevo.</summary>
    public string RemitenteEmail { get; set; } = "";

    public string RemitenteNombre { get; set; } = "Jardín";

    /// <summary>URL pública del frontend, para el link de "cancelar turno" (ej: https://turnera-jardin-azure.vercel.app).</summary>
    public string UrlFrontend { get; set; } = "";
}
