using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using TurneraJardin.Api.Models;
using TurneraJardin.Api.Models.Enums;

namespace TurneraJardin.Api.Services;

/// <summary>
/// Sends emails through the Brevo HTTP API (https://api.brevo.com/v3/smtp/email).
/// An HTTPS API is used instead of SMTP so we don't depend on the hosting allowing outbound SMTP ports.
/// </summary>
public class BrevoEmailService : IEmailService
{
    private const string ApiUrl = "https://api.brevo.com/v3/smtp/email";

    // Names written by hand so we don't depend on the server having the es-AR culture installed.
    private static readonly string[] DayNames = { "domingo", "lunes", "martes", "miércoles", "jueves", "viernes", "sábado" };
    private static readonly string[] MonthNames =
    {
        "enero", "febrero", "marzo", "abril", "mayo", "junio",
        "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"
    };

    private readonly HttpClient _http;
    private readonly EmailOptions _options;
    private readonly ILogger<BrevoEmailService> _logger;

    public BrevoEmailService(HttpClient http, IOptions<EmailOptions> options, ILogger<BrevoEmailService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public Task<bool> SendConfirmationAsync(Appointment appointment, CancellationToken ct = default)
    {
        var subject = $"Turno confirmado: {FormatDate(appointment.Date)} a las {FormatTime(appointment.StartTime)} hs";
        return SendAsync(appointment, "confirmation", subject, () => BuildConfirmationHtml(appointment), ct);
    }

    public Task<bool> SendCancellationAsync(Appointment appointment, CancellationToken ct = default)
    {
        var subject = $"Turno cancelado: {FormatDate(appointment.Date)} a las {FormatTime(appointment.StartTime)} hs";
        return SendAsync(appointment, "cancellation", subject, () => BuildCancellationHtml(appointment), ct);
    }

    /// <summary>Common sending logic for every email: validates the recipient, honors Email:Enabled and calls Brevo.</summary>
    private async Task<bool> SendAsync(Appointment appointment, string kind, string subject, Func<string> buildHtml, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(appointment.GuardianEmail))
        {
            _logger.LogWarning("Appointment {AppointmentId} has no email, the {Kind} email is not sent", appointment.Id, kind);
            return false;
        }

        if (!_options.Enabled)
        {
            _logger.LogInformation(
                "[Email DISABLED] Simulating send to {Email} with subject: {Subject}", appointment.GuardianEmail, subject);
            return true;
        }

        var payload = new
        {
            sender = new { name = _options.SenderName, email = _options.SenderEmail },
            to = new[] { new { email = appointment.GuardianEmail, name = GuardianFullName(appointment) is { Length: > 0 } name ? name : appointment.GuardianEmail } },
            subject,
            htmlContent = buildHtml()
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Add("api-key", _options.ApiKey);
            request.Headers.Add("accept", "application/json");

            var response = await _http.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError(
                    "Failed to send the {Kind} email for appointment {AppointmentId}: {Status} - {Body}",
                    kind, appointment.Id, response.StatusCode, body);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception sending the {Kind} email for appointment {AppointmentId}", kind, appointment.Id);
            return false;
        }
    }

    private string BuildCancellationHtml(Appointment appointment)
    {
        static string E(string? text) => WebUtility.HtmlEncode(text ?? "");

        var frontendUrl = _options.FrontendUrl.TrimEnd('/');
        var bookAgain = string.IsNullOrWhiteSpace(frontendUrl)
            ? ""
            : $"<p>Podés reservar un nuevo turno desde <a href=\"{E(frontendUrl)}\">{E(frontendUrl)}</a>.</p>";

        return $"""
            <div style="font-family: Arial, sans-serif; color: #1f2937; max-width: 520px;">
              <h2 style="color: #1a3a74;">Turno cancelado</h2>
              <p>Hola {E(GuardianFullName(appointment))}, te avisamos que el jardín canceló la entrevista que tenías reservada.</p>
              <table style="border-collapse: collapse;">
                <tr><td style="padding: 4px 16px 4px 0;">Niño/a</td><td><strong>{E(ChildFullName(appointment))}</strong></td></tr>
                <tr><td style="padding: 4px 16px 4px 0;">Docente</td><td><strong>{E(appointment.Teacher?.FullName)}</strong></td></tr>
                <tr><td style="padding: 4px 16px 4px 0;">Fecha</td><td><strong>{E(FormatDate(appointment.Date))}</strong></td></tr>
                <tr><td style="padding: 4px 16px 4px 0;">Horario</td><td><strong>{FormatTime(appointment.StartTime)} hs</strong></td></tr>
                <tr><td style="padding: 4px 16px 4px 0;">Número de turno</td><td><strong>#{appointment.Id}</strong></td></tr>
              </table>
              {bookAgain}
            </div>
            """;
    }

    private string BuildConfirmationHtml(Appointment appointment)
    {
        // Everything the family typed is escaped before putting it into the HTML.
        static string E(string? text) => WebUtility.HtmlEncode(text ?? "");

        var frontendUrl = _options.FrontendUrl.TrimEnd('/');
        var cancel = string.IsNullOrWhiteSpace(frontendUrl)
            ? ""
            : $"<p>Si no vas a poder asistir, cancelá el turno desde <a href=\"{E(frontendUrl)}/cancelar\">{E(frontendUrl)}/cancelar</a> con el número de turno y el teléfono que cargaste.</p>";

        return $"""
            <div style="font-family: Arial, sans-serif; color: #1f2937; max-width: 520px;">
              <h2 style="color: #1a3a74;">¡Turno confirmado!</h2>
              <p>Hola {E(GuardianFullName(appointment))}, tu entrevista quedó reservada.</p>
              <table style="border-collapse: collapse;">
                <tr><td style="padding: 4px 16px 4px 0;">Niño/a</td><td><strong>{E(ChildFullName(appointment))}</strong></td></tr>
                <tr><td style="padding: 4px 16px 4px 0;">Docente</td><td><strong>{E(appointment.Teacher?.FullName)}</strong></td></tr>
                <tr><td style="padding: 4px 16px 4px 0;">Fecha</td><td><strong>{E(FormatDate(appointment.Date))}</strong></td></tr>
                <tr><td style="padding: 4px 16px 4px 0;">Horario</td><td><strong>{FormatTime(appointment.StartTime)} hs</strong></td></tr>
                <tr><td style="padding: 4px 16px 4px 0;">Modalidad</td><td><strong>{E(ModeLabel(appointment.Mode))}</strong></td></tr>
                <tr><td style="padding: 4px 16px 4px 0;">Número de turno</td><td><strong>#{appointment.Id}</strong></td></tr>
              </table>
              <p>Guardá el número de turno: lo vas a necesitar si querés cancelar.</p>
              {cancel}
            </div>
            """;
    }

    private static string FormatDate(DateOnly date) =>
        $"{DayNames[(int)date.DayOfWeek]} {date.Day} de {MonthNames[date.Month - 1]} de {date.Year}";

    private static string FormatTime(TimeOnly time) => time.ToString("HH:mm", CultureInfo.InvariantCulture);

    /// <summary>Label shown to the family. The enum values are in English, the email text is in Spanish.</summary>
    private static string ModeLabel(InterviewMode? mode) => mode switch
    {
        InterviewMode.InPerson => "Presencial",
        InterviewMode.Online => "Virtual",
        _ => ""
    };

    private static string GuardianFullName(Appointment a) => $"{a.GuardianFirstName} {a.GuardianLastName}".Trim();

    private static string ChildFullName(Appointment a) => $"{a.ChildFirstName} {a.ChildLastName}".Trim();
}
