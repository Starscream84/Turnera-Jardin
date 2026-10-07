using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using TurneraJardin.Api.Models;

namespace TurneraJardin.Api.Services;

/// <summary>
/// Envía emails con la API HTTP de Brevo (https://api.brevo.com/v3/smtp/email).
/// Se usa una API por HTTPS y no SMTP porque el plan gratis de Render bloquea los puertos SMTP salientes.
/// </summary>
public class BrevoEmailService : IEmailService
{
    private const string UrlApi = "https://api.brevo.com/v3/smtp/email";

    // Nombres escritos a mano para no depender de que el servidor tenga instalada la cultura es-AR.
    private static readonly string[] Dias = { "domingo", "lunes", "martes", "miércoles", "jueves", "viernes", "sábado" };
    private static readonly string[] Meses =
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

    public Task<bool> EnviarConfirmacionAsync(Turno turno, CancellationToken ct = default)
    {
        var asunto = $"Turno confirmado: {FormatearFecha(turno.Fecha)} a las {FormatearHora(turno.HoraInicio)} hs";
        return EnviarAsync(turno, "confirmación", asunto, () => ArmarHtml(turno), ct);
    }

    public Task<bool> EnviarCancelacionAsync(Turno turno, CancellationToken ct = default)
    {
        var asunto = $"Turno cancelado: {FormatearFecha(turno.Fecha)} a las {FormatearHora(turno.HoraInicio)} hs";
        return EnviarAsync(turno, "cancelación", asunto, () => ArmarHtmlCancelacion(turno), ct);
    }

    /// <summary>Envío común a todos los mails: valida el destinatario, respeta Email:Habilitado y llama a Brevo.</summary>
    private async Task<bool> EnviarAsync(Turno turno, string tipo, string asunto, Func<string> armarHtml, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(turno.EmailPadre))
        {
            _logger.LogWarning("Turno {TurnoId} no tiene email cargado, no se envía el mail de {Tipo}", turno.Id, tipo);
            return false;
        }

        if (!_options.Habilitado)
        {
            _logger.LogInformation(
                "[Email DESHABILITADO] Se simula envío a {Email} con asunto: {Asunto}", turno.EmailPadre, asunto);
            return true;
        }

        var payload = new
        {
            sender = new { name = _options.RemitenteNombre, email = _options.RemitenteEmail },
            to = new[] { new { email = turno.EmailPadre, name = turno.NombrePadre ?? turno.EmailPadre } },
            subject = asunto,
            htmlContent = armarHtml()
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, UrlApi)
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
                    "Falló el envío del email de {Tipo} del turno {TurnoId}: {Status} - {Body}",
                    tipo, turno.Id, response.StatusCode, body);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción enviando el email de {Tipo} del turno {TurnoId}", tipo, turno.Id);
            return false;
        }
    }

    private string ArmarHtmlCancelacion(Turno turno)
    {
        static string E(string? texto) => WebUtility.HtmlEncode(texto ?? "");

        var urlFrontend = _options.UrlFrontend.TrimEnd('/');
        var reservar = string.IsNullOrWhiteSpace(urlFrontend)
            ? ""
            : $"<p>Podés reservar un nuevo turno desde <a href=\"{E(urlFrontend)}\">{E(urlFrontend)}</a>.</p>";

        return $"""
            <div style="font-family: Arial, sans-serif; color: #1f2937; max-width: 520px;">
              <h2 style="color: #1a3a74;">Turno cancelado</h2>
              <p>Hola {E(turno.NombrePadre)}, te avisamos que el jardín canceló la entrevista que tenías reservada.</p>
              <table style="border-collapse: collapse;">
                <tr><td style="padding: 4px 16px 4px 0;">Niño/a</td><td><strong>{E(turno.NombreNino)}</strong></td></tr>
                <tr><td style="padding: 4px 16px 4px 0;">Docente</td><td><strong>{E(turno.Docente?.NombreCompleto)}</strong></td></tr>
                <tr><td style="padding: 4px 16px 4px 0;">Fecha</td><td><strong>{E(FormatearFecha(turno.Fecha))}</strong></td></tr>
                <tr><td style="padding: 4px 16px 4px 0;">Horario</td><td><strong>{FormatearHora(turno.HoraInicio)} a {FormatearHora(turno.HoraFin)} hs</strong></td></tr>
                <tr><td style="padding: 4px 16px 4px 0;">Número de turno</td><td><strong>#{turno.Id}</strong></td></tr>
              </table>
              {reservar}
            </div>
            """;
    }

    private string ArmarHtml(Turno turno)
    {
        // Todo lo que escribió la familia se escapa antes de meterlo en el HTML.
        static string E(string? texto) => WebUtility.HtmlEncode(texto ?? "");

        var urlFrontend = _options.UrlFrontend.TrimEnd('/');
        var cancelar = string.IsNullOrWhiteSpace(urlFrontend)
            ? ""
            : $"<p>Si no vas a poder asistir, cancelá el turno desde <a href=\"{E(urlFrontend)}/cancelar\">{E(urlFrontend)}/cancelar</a> con el número de turno y el teléfono que cargaste.</p>";

        return $"""
            <div style="font-family: Arial, sans-serif; color: #1f2937; max-width: 520px;">
              <h2 style="color: #1a3a74;">¡Turno confirmado!</h2>
              <p>Hola {E(turno.NombrePadre)}, tu entrevista quedó reservada.</p>
              <table style="border-collapse: collapse;">
                <tr><td style="padding: 4px 16px 4px 0;">Niño/a</td><td><strong>{E(turno.NombreNino)}</strong></td></tr>
                <tr><td style="padding: 4px 16px 4px 0;">Docente</td><td><strong>{E(turno.Docente?.NombreCompleto)}</strong></td></tr>
                <tr><td style="padding: 4px 16px 4px 0;">Fecha</td><td><strong>{E(FormatearFecha(turno.Fecha))}</strong></td></tr>
                <tr><td style="padding: 4px 16px 4px 0;">Horario</td><td><strong>{FormatearHora(turno.HoraInicio)} a {FormatearHora(turno.HoraFin)} hs</strong></td></tr>
                <tr><td style="padding: 4px 16px 4px 0;">Modalidad</td><td><strong>{E(turno.Modalidad?.ToString())}</strong></td></tr>
                <tr><td style="padding: 4px 16px 4px 0;">Número de turno</td><td><strong>#{turno.Id}</strong></td></tr>
              </table>
              <p>Guardá el número de turno: lo vas a necesitar si querés cancelar.</p>
              {cancelar}
            </div>
            """;
    }

    private static string FormatearFecha(DateOnly fecha) =>
        $"{Dias[(int)fecha.DayOfWeek]} {fecha.Day} de {Meses[fecha.Month - 1]} de {fecha.Year}";

    private static string FormatearHora(TimeOnly hora) => hora.ToString("HH:mm", CultureInfo.InvariantCulture);
}
