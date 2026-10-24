namespace TurneraJardin.Api.Services;

/// <summary>
/// WhatsApp Cloud API (Meta) integration settings.
/// Filled in from appsettings.json / environment variables / user-secrets,
/// never hardcoded. See the README for how to obtain these values.
/// </summary>
public class WhatsAppOptions
{
    public const string ConfigSection = "WhatsApp";

    /// <summary>When false, the service only logs the message instead of calling the API (useful for local development).</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>Permanent access token of the Meta for Developers app.</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Phone Number ID of the verified WhatsApp Business number.</summary>
    public string PhoneNumberId { get; set; } = string.Empty;

    public string ApiVersion { get; set; } = "v20.0";

    /// <summary>Exact name of the template approved in Meta for the confirmation message.</summary>
    public string ConfirmationTemplate { get; set; } = "confirmacion_turno";

    /// <summary>Exact name of the template approved in Meta for the day-before reminder.</summary>
    public string ReminderTemplate { get; set; } = "recordatorio_turno";

    public string TemplateLanguageCode { get; set; } = "es_AR";
}
