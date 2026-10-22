using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ControlClientesRestaurante.Data;
using ControlClientesRestaurante.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ControlClientesRestaurante.Services
{
    public class WompiService : IWompiService
    {
        private readonly WompiSettings _options;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WompiService> _logger;

        public WompiService(
            IOptions<WompiSettings> options,
            IHttpClientFactory httpClientFactory,
            ApplicationDbContext context,
            ILogger<WompiService> logger)
        {
            _options = options.Value;
            _httpClientFactory = httpClientFactory;
            _context = context;
            _logger = logger;
        }

        public WompiSettings ObtenerConfiguracion(bool esLocal = false)
        {
            // Leer variables de entorno con prioridad sobre appsettings
            var envPublicKey = Environment.GetEnvironmentVariable("WOMPI_PUBLIC_KEY");
            var envPrivateKey = Environment.GetEnvironmentVariable("WOMPI_PRIVATE_KEY");
            var envIntegritySecret = Environment.GetEnvironmentVariable("WOMPI_INTEGRITY_SECRET");
            var envEventSecret = Environment.GetEnvironmentVariable("WOMPI_EVENTS_SECRET")
                              ?? Environment.GetEnvironmentVariable("WOMPI_EVENT_SECRET");
            var envAmbiente = Environment.GetEnvironmentVariable("WOMPI_ENVIRONMENT");

            var pubKey = !string.IsNullOrWhiteSpace(envPublicKey) ? envPublicKey : _options.PublicKey;
            var prvKey = !string.IsNullOrWhiteSpace(envPrivateKey) ? envPrivateKey : _options.PrivateKey;
            var integritySecret = !string.IsNullOrWhiteSpace(envIntegritySecret) ? envIntegritySecret : _options.IntegritySecret;
            var eventSecret = !string.IsNullOrWhiteSpace(envEventSecret) ? envEventSecret : _options.EventSecret;
            var ambiente = !string.IsNullOrWhiteSpace(envAmbiente) ? envAmbiente : _options.Environment;

            // En entorno local (localhost), Wompi Producción rechaza orígenes http://localhost.
            // Para que el widget cargue y funcione sin quedarse congelado en desarrollo,
            // usamos llaves sandbox en localhost salvo que se fuerce producción por variable de entorno.
            bool forzarProd = string.Equals(Environment.GetEnvironmentVariable("WOMPI_FORCE_PROD"), "true", StringComparison.OrdinalIgnoreCase);
            // Si ya configuraste tus propias llaves de pruebas (pub_test_...), se respetan también en localhost
            bool llavesDePruebasPropias = pubKey.StartsWith("pub_test_", StringComparison.Ordinal);
            if (esLocal && !forzarProd && !llavesDePruebasPropias)
            {
                pubKey = !string.IsNullOrWhiteSpace(_options.SandboxPublicKey) ? _options.SandboxPublicKey : "pub_test_Q5yDA9xoKdePium1amu0Bush320D4HfR";
                // Las llaves privadas (aunque sean de pruebas) no van en el código: variable de entorno o appsettings
                prvKey = Environment.GetEnvironmentVariable("WOMPI_SANDBOX_PRIVATE_KEY") ?? _options.SandboxPrivateKey;
                ambiente = "sandbox";
            }

            // Una llave de pruebas (pub_test_...) siempre opera contra la API sandbox, sin importar el entorno declarado
            if (pubKey.StartsWith("pub_test_", StringComparison.Ordinal))
            {
                ambiente = "sandbox";
            }

            // La aprobación simulada (para demostraciones) solo existe en sandbox y solo si se activa de forma explícita
            bool simulacionActiva = string.Equals(Environment.GetEnvironmentVariable("WOMPI_PERMITIR_SIMULACION"), "true", StringComparison.OrdinalIgnoreCase)
                                    || _options.PermitirSimulacion
                                    || string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase);
            // WOMPI_MODO_DEMO=true: demostración pública sin llaves de pruebas. Fuerza el checkout simulado en cualquier entorno
            // (nunca se abre el widget real, así que no hay cobros). Cualquiera podría "aprobar" pedidos: solo para demos.
            bool modoDemo = string.Equals(Environment.GetEnvironmentVariable("WOMPI_MODO_DEMO"), "true", StringComparison.OrdinalIgnoreCase);
            bool permitirSimulacion = modoDemo || (simulacionActiva && string.Equals(ambiente, "sandbox", StringComparison.OrdinalIgnoreCase));

            return new WompiSettings
            {
                PermitirSimulacion = permitirSimulacion,
                // Sin llaves de pruebas propias no hay widget real que abrir: se usa el checkout simulado
                SimularPasarela = modoDemo || (permitirSimulacion && !llavesDePruebasPropias),
                PublicKey = pubKey,
                PrivateKey = prvKey,
                Environment = ambiente,
                Currency = _options.Currency ?? "COP",
                IntegritySecret = integritySecret,
                EventSecret = eventSecret,
                WebhookUrl = _options.WebhookUrl
            };
        }

        public string CalcularFirmaIntegridad(string referencia, long montoEnCentavos, string moneda = "COP")
        {
            if (string.IsNullOrWhiteSpace(referencia))
                throw new ArgumentException("La referencia no puede ser nula o vacía", nameof(referencia));

            var config = ObtenerConfiguracion();
            var secreto = config.IntegritySecret ?? string.Empty;
            // Sin secreto de integridad no se envía firma (el widget sandbox funciona sin ella si el comercio no la exige)
            if (string.IsNullOrWhiteSpace(secreto))
                return string.Empty;

            // Especificación oficial de Wompi:
            // SHA256(Referencia + MontoEnCentavos + Moneda + SecretoIntegridad)
            var cadenaAHashear = $"{referencia}{montoEnCentavos}{moneda}{secreto}";
            var bytes = Encoding.UTF8.GetBytes(cadenaAHashear);
            var hashBytes = SHA256.HashData(bytes);

            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        public bool ValidarChecksumEvento(string rawJson, string checksumRecibido, IEnumerable<string> properties, long? timestamp)
        {
            if (string.IsNullOrWhiteSpace(checksumRecibido))
                return false;

            var config = ObtenerConfiguracion();
            var eventSecret = config.EventSecret;
            if (string.IsNullOrWhiteSpace(eventSecret))
            {
                _logger.LogWarning("Wompi EventSecret no configurado. Se omite validación local de checksum para consultar por API.");
                return false;
            }

            try
            {
                using var doc = JsonDocument.Parse(rawJson);
                var root = doc.RootElement;
                var sb = new StringBuilder();

                foreach (var prop in properties)
                {
                    var partes = prop.Split('.');
                    JsonElement actual = root;
                    bool encontrada = true;

                    // Wompi especifica properties como "transaction.id", que en el JSON reside bajo "data.transaction.id"
                    if (!prop.StartsWith("data.") && root.ValueKind == JsonValueKind.Object && root.TryGetProperty("data", out var dataElem))
                    {
                        actual = dataElem;
                    }

                    foreach (var p in partes)
                    {
                        if (actual.ValueKind == JsonValueKind.Object && actual.TryGetProperty(p, out var sub))
                        {
                            actual = sub;
                        }
                        else
                        {
                            encontrada = false;
                            break;
                        }
                    }

                    if (encontrada)
                    {
                        sb.Append(actual.ToString());
                    }
                }

                if (timestamp.HasValue)
                {
                    sb.Append(timestamp.Value.ToString());
                }

                sb.Append(eventSecret.Trim());

                var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
                var checksumCalculado = Convert.ToHexString(hashBytes).ToLowerInvariant();

                // Comparación en tiempo constante para no filtrar información por tiempos de respuesta
                return CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(checksumCalculado),
                    Encoding.UTF8.GetBytes(checksumRecibido.Trim().ToLowerInvariant()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al calcular el checksum del evento webhook Wompi");
                return false;
            }
        }

        public async Task<WompiTransactionData?> ConsultarTransaccionAsync(string transaccionId)
        {
            if (string.IsNullOrWhiteSpace(transaccionId))
                return null;

            var config = ObtenerConfiguracion();
            var client = _httpClientFactory.CreateClient("WompiClient");
            client.Timeout = TimeSpan.FromSeconds(6);

            var url = $"{config.ApiBaseUrl}/transactions/{transaccionId}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Autenticación con llave privada Wompi
            if (!string.IsNullOrWhiteSpace(config.PrivateKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.PrivateKey);
            }

            try
            {
                var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Wompi API respondió con código {StatusCode} al consultar transacción {TxId}", 
                        response.StatusCode, transaccionId);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                var resultado = JsonSerializer.Deserialize<WompiConsultaTransaccionResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return resultado?.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción de conexión al consultar transacción Wompi {TxId}", transaccionId);
                return null;
            }
        }

        public async Task<SesionWompiFirmadaResponse> CrearSesionPagoAsync(IniciarSesionWompiRequest? request, string baseUrl, bool esLocal)
        {
            request ??= new IniciarSesionWompiRequest();
            var config = ObtenerConfiguracion(esLocal);
            decimal monto = 0;
            string concepto = string.Empty;
            string referencia = string.Empty;
            string? email = request.Correo;
            string? nombre = request.NombreCliente;
            string? tel = request.Telefono;
            string tipoOrigen = request.TipoOrigen ?? "Directo";

            // Validación estricta en servidor contra base de datos: EL CLIENTE NUNCA DEFINE EL MONTO
            if (string.Equals(tipoOrigen, "Pedido", StringComparison.OrdinalIgnoreCase) && request.OrigenId.HasValue)
            {
                var pedido = await _context.Pedidos
                    .Include(p => p.Lineas)
                    .FirstOrDefaultAsync(p => p.Id == request.OrigenId.Value);

                if (pedido == null)
                {
                    return new SesionWompiFirmadaResponse { Success = false, Error = "Pedido no encontrado" };
                }

                if (pedido.Estado == "Facturado")
                {
                    return new SesionWompiFirmadaResponse { Success = false, Error = "El pedido ya se encuentra pagado y facturado." };
                }

                monto = pedido.Total;
                concepto = $"Consumo Restaurante Saborator - Pedido {pedido.Numero}";
                // Referencia única y trazable
                referencia = $"{pedido.Numero}-{DateTime.UtcNow:yyMMddHHmmss}";
                nombre = pedido.NombreCliente ?? nombre;
                tel = pedido.Telefono ?? tel;
            }
            else if (string.Equals(tipoOrigen, "Reserva", StringComparison.OrdinalIgnoreCase) && request.OrigenId.HasValue)
            {
                var reserva = await _context.Reservas.FirstOrDefaultAsync(r => r.Id == request.OrigenId.Value);
                if (reserva == null)
                {
                    return new SesionWompiFirmadaResponse { Success = false, Error = "Reserva no encontrada" };
                }

                decimal depositoPorPersona = 25000m;
                monto = reserva.NumeroPersonas * depositoPorPersona;
                concepto = $"Depósito de Reserva Exclusiva #{reserva.Codigo} ({reserva.NumeroPersonas} pers)";
                referencia = $"{reserva.Codigo}-{DateTime.UtcNow:yyMMddHHmmss}";
                nombre = reserva.NombreCliente ?? nombre;
                email = reserva.Correo ?? email;
                tel = reserva.Telefono ?? tel;
            }
            else
            {
                // Consumo directo / Bono
                monto = 35000m;
                concepto = "Experiencia Gastronómica Saborator";
                referencia = $"DIR-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}";
            }

            if (monto <= 0)
            {
                return new SesionWompiFirmadaResponse { Success = false, Error = "El monto liquidado no es válido" };
            }

            long montoEnCentavos = (long)(monto * 100);
            string firmaIntegridad = CalcularFirmaIntegridad(referencia, montoEnCentavos, config.Currency);
            string redirectUrl = $"{baseUrl.TrimEnd('/')}/Pago/RetornoWompi";

            // Registrar la transacción en estado "Iniciada" para trazabilidad y conciliación
            try
            {
                var transaccionDb = new TransaccionPago
                {
                    ReferenciaTransaccion = referencia,
                    CodigoAprobacion = "PENDIENTE",
                    TipoOrigen = request.TipoOrigen ?? "Directo",
                    OrigenId = request.OrigenId,
                    Monto = monto,
                    MetodoPago = "Wompi",
                    DetalleMetodo = "Sesión de pago Wompi generada y firmada por backend",
                    NombreCliente = nombre ?? "Cliente Saborator",
                    Correo = email,
                    Telefono = tel,
                    Concepto = concepto,
                    Estado = "Iniciada",
                    UsuarioRegistro = "WompiService"
                };

                _context.TransaccionesPago.Add(transaccionDb);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo pre-registrar transacción en BD, continuando con sesión de pago");
            }

            return new SesionWompiFirmadaResponse
            {
                Success = true,
                PublicKey = config.PublicKey,
                Currency = config.Currency,
                AmountInCents = montoEnCentavos,
                MontoPesos = monto,
                Reference = referencia,
                IntegritySignature = firmaIntegridad,
                RedirectUrl = redirectUrl,
                Concepto = concepto,
                CustomerData = new CustomerDataWompi
                {
                    Email = email,
                    FullName = nombre,
                    PhoneNumber = tel,
                    PhoneNumberPrefix = "+57",
                    LegalIdType = "CC"
                }
            };
        }
    }
}
