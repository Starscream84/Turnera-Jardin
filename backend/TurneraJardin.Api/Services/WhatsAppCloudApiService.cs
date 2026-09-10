using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using TurneraJardin.Api.Models;

namespace TurneraJardin.Api.Services;

/// <summary>
/// Envía mensajes usando la WhatsApp Cloud API oficial de Meta (gratuita dentro de su cuota mensual).
/// Requiere que existan, y estén aprobadas por Meta, dos plantillas de mensaje ("message templates"):
/// una para la confirmación y otra para el recordatorio. Ver README -> "Configurar WhatsApp".
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

    public Task<bool> EnviarConfirmacionAsync(Turno turno, CancellationToken ct = default)
    {
        var parametros = new[]
        {
            turno.NombreNino ?? "",
            turno.Docente?.NombreCompleto ?? "",
            FormatearFecha(turno.Fecha),
            turno.HoraInicio.ToString("HH:mm", CultureInfo.InvariantCulture)
        };

        return EnviarTemplateAsync(turno, _options.TemplateConfirmacion, parametros, ct);
    }

    public Task<bool> EnviarRecordatorioAsync(Turno turno, CancellationToken ct = default)
    {
        var parametros = new[]
        {
            turno.NombreNino ?? "",
            turno.Docente?.NombreCompleto ?? "",
            turno.HoraInicio.ToString("HH:mm", CultureInfo.InvariantCulture)
        };

        return EnviarTemplateAsync(turno, _options.TemplateRecordatorio, parametros, ct);
    }

    private async Task<bool> EnviarTemplateAsync(Turno turno, string nombreTemplate, string[] parametros, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(turno.TelefonoPadre))
        {
            _logger.LogWarning("Turno {TurnoId} no tiene teléfono cargado, no se envía WhatsApp", turno.Id);
            return false;
        }

        if (!_options.Habilitado)
        {
            _logger.LogInformation(
                "[WhatsApp DESHABILITADO] Se simula envío de '{Template}' a {Telefono} con parámetros: {Parametros}",
                nombreTemplate, turno.TelefonoPadre, string.Join(" | ", parametros));
            return true;
        }

        var payload = new WhatsAppTemplateRequest
        {
            To = NormalizarTelefono(turno.TelefonoPadre),
            Template = new WhatsAppTemplate
            {
                Name = nombreTemplate,
                Language = new WhatsAppTemplateLanguage { Code = _options.CodigoIdiomaTemplate },
                Components = new[]
                {
                    new WhatsAppTemplateComponent
                    {
                        Type = "body",
                        Parameters = parametros.Select(p => new WhatsAppTemplateParameter { Text = p }).ToArray()
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
                    "Falló el envío de WhatsApp ({Template}) a turno {TurnoId}: {Status} - {Body}",
                    nombreTemplate, turno.Id, response.StatusCode, body);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción enviando WhatsApp ({Template}) para turno {TurnoId}", nombreTemplate, turno.Id);
            return false;
        }
    }

    private static string FormatearFecha(DateOnly fecha) =>
        fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    /// <summary>La Cloud API espera el número en formato E.164 sin el símbolo "+".</summary>
    private static string NormalizarTelefono(string telefono)
    {
        var soloDigitos = new string(telefono.Where(char.IsDigit).ToArray());
        return soloDigitos;
    }

    // --- Modelos internos del payload de la Graph API ---

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
