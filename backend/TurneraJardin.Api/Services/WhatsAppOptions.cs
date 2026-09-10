namespace TurneraJardin.Api.Services;

/// <summary>
/// Configuración de la integración con WhatsApp Cloud API (Meta).
/// Se completa en appsettings.json / variables de entorno / user-secrets,
/// nunca hardcodeada en el código. Ver el README para cómo obtener estos valores.
/// </summary>
public class WhatsAppOptions
{
    public const string SeccionConfig = "WhatsApp";

    /// <summary>Si está en false, el servicio solo loguea el mensaje en vez de llamar a la API (útil para desarrollo local).</summary>
    public bool Habilitado { get; set; } = false;

    /// <summary>Access Token permanente de la app de Meta for Developers.</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Phone Number ID del número de WhatsApp Business verificado.</summary>
    public string PhoneNumberId { get; set; } = string.Empty;

    public string ApiVersion { get; set; } = "v20.0";

    /// <summary>Nombre exacto de la plantilla aprobada en Meta para el mensaje de confirmación.</summary>
    public string TemplateConfirmacion { get; set; } = "confirmacion_turno";

    /// <summary>Nombre exacto de la plantilla aprobada en Meta para el recordatorio del día anterior.</summary>
    public string TemplateRecordatorio { get; set; } = "recordatorio_turno";

    public string CodigoIdiomaTemplate { get; set; } = "es_AR";
}
