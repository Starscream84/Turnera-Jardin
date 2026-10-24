using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using TurneraJardin.Api.Models;

namespace TurneraJardin.Api.Services;

/// <summary>
/// Sends messages through Meta's official WhatsApp Cloud API (free within its monthly quota).
/// Requires two message templates to exist and be approved by Meta:
/// one for the confirmation and another for the reminder. See README -> "Configurar WhatsApp".
/// </summary>
public class WhatsAppCloudApiService : IWhatsAppService
{
    private readonly HttpClient _http;
    private readonly WhatsAppOptions _options;
    private readonly ILogger<WhatsAppCloudApiService> _logger;

    public WhatsAppCloudApiService(HttpClient http, IOptions<WhatsAppOptions> options, ILogger<WhatsAppCloudApiService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public Task<bool> SendConfirmationAsync(Appointment appointment, CancellationToken ct = default)
    {
        var parameters = new[]
        {
            $"{appointment.ChildFirstName} {appointment.ChildLastName}".Trim(),
            appointment.Teacher?.FullName ?? "",
            FormatDate(appointment.Date),
            appointment.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture)
        };

        return SendTemplateAsync(appointment, _options.ConfirmationTemplate, parameters, ct);
    }

    public Task<bool> SendReminderAsync(Appointment appointment, CancellationToken ct = default)
    {
        var parameters = new[]
        {
            $"{appointment.ChildFirstName} {appointment.ChildLastName}".Trim(),
            appointment.Teacher?.FullName ?? "",
            appointment.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture)
        };

        return SendTemplateAsync(appointment, _options.ReminderTemplate, parameters, ct);
    }

    private async Task<bool> SendTemplateAsync(Appointment appointment, string templateName, string[] parameters, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(appointment.GuardianPhone))
        {
            _logger.LogWarning("Appointment {AppointmentId} has no phone number, WhatsApp is not sent", appointment.Id);
            return false;
        }

        if (!_options.Enabled)
        {
            _logger.LogInformation(
                "[WhatsApp DISABLED] Simulating send of '{Template}' to {Phone} with parameters: {Parameters}",
                templateName, appointment.GuardianPhone, string.Join(" | ", parameters));
            return true;
        }

        var payload = new WhatsAppTemplateRequest
        {
            To = NormalizePhone(appointment.GuardianPhone),
            Template = new WhatsAppTemplate
            {
                Name = templateName,
                Language = new WhatsAppTemplateLanguage { Code = _options.TemplateLanguageCode },
                Components = new[]
                {
                    new WhatsAppTemplateComponent
                    {
                        Type = "body",
                        Parameters = parameters.Select(p => new WhatsAppTemplateParameter { Text = p }).ToArray()
                    }
                }
            }
        };

        var url = $"https://graph.facebook.com/{_options.ApiVersion}/{_options.PhoneNumberId}/messages";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.AccessToken);

            var response = await _http.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError(
                    "Failed to send WhatsApp ({Template}) for appointment {AppointmentId}: {Status} - {Body}",
                    templateName, appointment.Id, response.StatusCode, body);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception sending WhatsApp ({Template}) for appointment {AppointmentId}", templateName, appointment.Id);
            return false;
        }
    }

    private static string FormatDate(DateOnly date) =>
        date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    /// <summary>The Cloud API expects the number in E.164 format without the "+" sign.</summary>
    private static string NormalizePhone(string phone)
    {
        var digitsOnly = new string(phone.Where(char.IsDigit).ToArray());
        return digitsOnly;
    }

    // --- Internal Graph API payload models ---

    private class WhatsAppTemplateRequest
    {
        [JsonPropertyName("messaging_product")]
        public string MessagingProduct { get; set; } = "whatsapp";

        [JsonPropertyName("to")]
        public required string To { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "template";

        [JsonPropertyName("template")]
        public required WhatsAppTemplate Template { get; set; }
    }

    private class WhatsAppTemplate
    {
        [JsonPropertyName("name")]
        public required string Name { get; set; }

        [JsonPropertyName("language")]
        public required WhatsAppTemplateLanguage Language { get; set; }

        [JsonPropertyName("components")]
        public required WhatsAppTemplateComponent[] Components { get; set; }
    }

    private class WhatsAppTemplateLanguage
    {
        [JsonPropertyName("code")]
        public required string Code { get; set; }
    }

    private class WhatsAppTemplateComponent
    {
        [JsonPropertyName("type")]
        public required string Type { get; set; }

        [JsonPropertyName("parameters")]
        public required WhatsAppTemplateParameter[] Parameters { get; set; }
    }

    private class WhatsAppTemplateParameter
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "text";

        [JsonPropertyName("text")]
        public required string Text { get; set; }
    }
}
