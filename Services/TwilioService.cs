using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ControlClientesRestaurante.Services
{
    public interface ITwilioService
    {
        Task<(bool ok, string? sid, string? error)> EnviarOtpSmsAsync(string telefono, string codigo);
        Task<(bool ok, string? sid, string? error)> EnviarOtpWhatsAppAsync(string telefono, string codigo);
        Task<bool> VerificarCodigoTwilioAsync(string telefono, string codigo);
    }

    public class TwilioService : ITwilioService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<TwilioService> _logger;

        public TwilioService(
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            ILogger<TwilioService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _logger = logger;
        }

        private (string accountSid, string authToken, string phoneNumber, string whatsappNumber, string businessName, string verifyServiceSid) ObtenerConfiguracion()
        {
            CargarVariablesEnv();

            var accountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID")
                ?? _config["Twilio:AccountSid"] ?? "";

            var authToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN")
                ?? _config["Twilio:AuthToken"] ?? "";

            var phoneNumber = Environment.GetEnvironmentVariable("TWILIO_PHONE_NUMBER")
                ?? _config["Twilio:PhoneNumber"] ?? "";

            var whatsappNumber = Environment.GetEnvironmentVariable("TWILIO_WHATSAPP_NUMBER")
                ?? _config["Twilio:WhatsAppNumber"] ?? "whatsapp:+14155238886";

            var businessName = Environment.GetEnvironmentVariable("BUSINESS_NAME")
                ?? _config["Twilio:BusinessName"] ?? "Saborator Soledad";

            var verifyServiceSid = Environment.GetEnvironmentVariable("TWILIO_VERIFY_SERVICE_SID")
                ?? _config["Twilio:VerifyServiceSid"] ?? "VA71c1a0282605c5df407367145f847415";

            return (accountSid.Trim(), authToken.Trim(), phoneNumber.Trim(), whatsappNumber.Trim(), businessName.Trim(), verifyServiceSid.Trim());
        }

        private static bool _envCargado = false;
        private static readonly object _envLock = new();

        private void CargarVariablesEnv()
        {
            if (_envCargado) return;

            lock (_envLock)
            {
                if (_envCargado) return;

                var rutas = new[]
                {
                    Path.Combine(Directory.GetCurrentDirectory(), ".env"),
                    Path.Combine(Directory.GetCurrentDirectory(), "..", ".env"),
                    Path.Combine(AppContext.BaseDirectory, ".env")
                };

                foreach (var ruta in rutas)
                {
                    if (File.Exists(ruta))
                    {
                        try
                        {
                            foreach (var linea in File.ReadAllLines(ruta))
                            {
                                var l = linea.Trim();
                                if (string.IsNullOrEmpty(l) || l.StartsWith("#")) continue;

                                var partes = l.Split('=', 2);
                                if (partes.Length == 2)
                                {
                                    var clave = partes[0].Trim();
                                    var valor = partes[1].Trim();
                                    if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(clave)))
                                    {
                                        Environment.SetEnvironmentVariable(clave, valor);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "No se pudo leer el archivo .env en {Ruta}", ruta);
                        }
                        break;
                    }
                }

                _envCargado = true;
            }
        }

        private string NormalizarTelefonoColombiano(string telefono)
        {
            if (string.IsNullOrWhiteSpace(telefono)) return string.Empty;

            var limpio = new string(telefono.Where(char.IsDigit).ToArray());

            if (limpio.StartsWith("57") && limpio.Length == 12)
            {
                return $"+{limpio}";
            }

            if (limpio.Length == 10 && limpio.StartsWith("3"))
            {
                return $"+57{limpio}";
            }

            if (telefono.StartsWith("+"))
            {
                return telefono.Trim();
            }

            return $"+57{limpio}";
        }

        public async Task<(bool ok, string? sid, string? error)> EnviarOtpSmsAsync(string telefono, string codigo)
        {
            var config = ObtenerConfiguracion();
            var destino = NormalizarTelefonoColombiano(telefono);

            // Si hay Twilio Verify configurado, usar Verify API (soporta SMS directo a Colombia)
            if (!string.IsNullOrWhiteSpace(config.verifyServiceSid) && !string.IsNullOrWhiteSpace(config.accountSid) && !string.IsNullOrWhiteSpace(config.authToken))
            {
                return await EjecutarTwilioVerifyAsync(config.accountSid, config.authToken, config.verifyServiceSid, destino, "sms");
            }

            var cuerpoMensaje = $"🍔 [{config.businessName}] Tu código es: {codigo}. Válido por 5 min.";

            if (string.IsNullOrWhiteSpace(config.accountSid) || string.IsNullOrWhiteSpace(config.authToken) || string.IsNullOrWhiteSpace(config.phoneNumber))
            {
                _logger.LogInformation("📱 [Twilio SMS - MODO SIMULACIÓN]: {Destino} | {Mensaje}", destino, cuerpoMensaje);
                return (true, "SIMULATED_SMS_SID", null);
            }

            return await EjecutarTwilioApiAsync(
                config.accountSid,
                config.authToken,
                config.phoneNumber,
                destino,
                cuerpoMensaje,
                "SMS");
        }

        public async Task<(bool ok, string? sid, string? error)> EnviarOtpWhatsAppAsync(string telefono, string codigo)
        {
            var config = ObtenerConfiguracion();
            var destinoTel = NormalizarTelefonoColombiano(telefono);

            // Si hay Twilio Verify configurado, intentar canal whatsapp/sms
            if (!string.IsNullOrWhiteSpace(config.verifyServiceSid) && !string.IsNullOrWhiteSpace(config.accountSid) && !string.IsNullOrWhiteSpace(config.authToken))
            {
                var (okVerify, sidVerify, errVerify) = await EjecutarTwilioVerifyAsync(config.accountSid, config.authToken, config.verifyServiceSid, destinoTel, "whatsapp");
                if (okVerify) return (okVerify, sidVerify, errVerify);
                
                // Fallback automático a SMS por Verify
                return await EjecutarTwilioVerifyAsync(config.accountSid, config.authToken, config.verifyServiceSid, destinoTel, "sms");
            }

            var destinoWhatsApp = destinoTel.StartsWith("whatsapp:") ? destinoTel : $"whatsapp:{destinoTel}";
            var remitenteWhatsApp = config.whatsappNumber.StartsWith("whatsapp:") ? config.whatsappNumber : $"whatsapp:{config.whatsappNumber}";
            var cuerpoMensaje = $"🍔 Tu código de verificación es *{codigo}* - Expira en 5 min. Gracias por pedir en {config.businessName}";

            if (string.IsNullOrWhiteSpace(config.accountSid) || string.IsNullOrWhiteSpace(config.authToken))
            {
                _logger.LogInformation("💬 [Twilio WhatsApp - MODO SIMULACIÓN]: {Destino} | {Mensaje}", destinoWhatsApp, cuerpoMensaje);
                return (true, "SIMULATED_WHATSAPP_SID", null);
            }

            return await EjecutarTwilioApiAsync(
                config.accountSid,
                config.authToken,
                remitenteWhatsApp,
                destinoWhatsApp,
                cuerpoMensaje,
                "WhatsApp");
        }

        public async Task<bool> VerificarCodigoTwilioAsync(string telefono, string codigo)
        {
            var config = ObtenerConfiguracion();
            if (string.IsNullOrWhiteSpace(config.verifyServiceSid) || string.IsNullOrWhiteSpace(config.accountSid) || string.IsNullOrWhiteSpace(config.authToken))
                return false;

            try
            {
                var destino = NormalizarTelefonoColombiano(telefono);
                var client = _httpClientFactory.CreateClient("TwilioClient");
                var requestUrl = $"https://verify.twilio.com/v2/Services/{config.verifyServiceSid}/VerificationCheck";

                using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                var credencialesBase64 = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{config.accountSid}:{config.authToken}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credencialesBase64);

                var formValues = new Dictionary<string, string>
                {
                    { "To", destino },
                    { "Code", codigo.Trim() }
                };
                request.Content = new FormUrlEncodedContent(formValues);

                var response = await client.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("status", out var statusProp) && statusProp.GetString() == "approved")
                    {
                        _logger.LogInformation("✅ [Twilio Verify]: Código aprobado exitosamente para {Destino}", destino);
                        return true;
                    }
                }

                _logger.LogWarning("⚠️ [Twilio Verify]: Código no aprobado para {Destino}. Respuesta: {Json}", destino, json);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al comprobar código en Twilio Verify");
                return false;
            }
        }

        private async Task<(bool ok, string? sid, string? error)> EjecutarTwilioVerifyAsync(
            string accountSid,
            string authToken,
            string serviceSid,
            string to,
            string channel)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("TwilioClient");
                var requestUrl = $"https://verify.twilio.com/v2/Services/{serviceSid}/Verifications";

                using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                var credencialesBase64 = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credencialesBase64);

                var formValues = new Dictionary<string, string>
                {
                    { "To", to },
                    { "Channel", channel }
                };
                request.Content = new FormUrlEncodedContent(formValues);

                _logger.LogInformation("Enviando Twilio Verify {Channel} a {To}...", channel, to);
                var response = await client.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseContent);
                    var sid = doc.RootElement.TryGetProperty("sid", out var sidProp) ? sidProp.GetString() : "VERIFY_SID";
                    _logger.LogInformation("✅ [Twilio Verify - Éxito]: SID={Sid} enviado a {To} por {Channel}", sid, to, channel);
                    return (true, sid, null);
                }
                else
                {
                    _logger.LogError("❌ [Twilio Verify - Error {StatusCode}]: {Response}", response.StatusCode, responseContent);
                    return (false, null, $"Twilio Verify Error: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [Twilio Verify - Excepción]: {Mensaje}", ex.Message);
                return (false, null, ex.Message);
            }
        }

        private async Task<(bool ok, string? sid, string? error)> EjecutarTwilioApiAsync(
            string accountSid,
            string authToken,
            string from,
            string to,
            string body,
            string canal)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("TwilioClient");
                var requestUrl = $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json";

                using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);

                var credencialesBase64 = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credencialesBase64);

                var formValues = new Dictionary<string, string>
                {
                    { "From", from },
                    { "To", to },
                    { "Body", body }
                };

                request.Content = new FormUrlEncodedContent(formValues);

                _logger.LogInformation("Enviando mensaje Twilio {Canal} a {To} desde {From}...", canal, to, from);

                var response = await client.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseContent);
                    var sid = doc.RootElement.TryGetProperty("sid", out var sidProp) ? sidProp.GetString() : "UNKNOWN_SID";
                    _logger.LogInformation("✅ [Twilio {Canal} - Éxito]: SID={Sid} entregado a {To}", canal, sid, to);
                    return (true, sid, null);
                }
                else
                {
                    _logger.LogError("❌ [Twilio {Canal} - Error {StatusCode}]: {Response}", canal, response.StatusCode, responseContent);
                    return (false, null, $"Twilio Error ({(int)response.StatusCode}): {responseContent}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [Twilio {Canal} - Excepción al conectar]: {Mensaje}", canal, ex.Message);
                return (false, null, ex.Message);
            }
        }
    }
}
