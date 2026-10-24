namespace TurneraJardin.Api.Services;

/// <summary>Email sending settings ("Email" section of appsettings.json or Email__* environment variables).</summary>
public class EmailOptions
{
    public const string ConfigSection = "Email";

    /// <summary>When false nothing is sent: sending is simulated and written to the log (useful for development).</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>Brevo API key. Not stored in the repo: loaded from the Email__ApiKey environment variable.</summary>
    public string ApiKey { get; set; } = "";

    /// <summary>Address the emails are sent from. It must be verified as a sender in Brevo.</summary>
    public string SenderEmail { get; set; } = "";

    public string SenderName { get; set; } = "Jardín";

    /// <summary>Public frontend URL, for the "cancel appointment" link (e.g. https://turnera-jardin-azure.vercel.app).</summary>
    public string FrontendUrl { get; set; } = "";
}
