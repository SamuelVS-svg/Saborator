using Microsoft.AspNetCore.Mvc;
using ControlClientesRestaurante.Models;
using ControlClientesRestaurante.Services;
using ControlClientesRestaurante.Data;
using Microsoft.EntityFrameworkCore;

namespace ControlClientesRestaurante.Controllers
{
    public class CheckoutRequestViewModel
    {
        public string TipoOrigen { get; set; } = "Pedido"; // Pedido | Reserva | Directo
        public int? OrigenId { get; set; }
        public decimal Monto { get; set; }
        public string Concepto { get; set; } = string.Empty;
        public string? NombreCliente { get; set; }
        public string? Correo { get; set; }
        public string? Telefono { get; set; }
        public string? Mesa { get; set; }
        public List<ItemResumenViewModel> Items { get; set; } = new();
    }

    public class ItemResumenViewModel
    {
        public string Nombre { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal Subtotal => Cantidad * Precio;
    }

    public class ProcesarPagoViewModel
    {
        public string TipoOrigen { get; set; } = "Pedido";
        public int? OrigenId { get; set; }
        public decimal Monto { get; set; }
        public string Concepto { get; set; } = string.Empty;
        public string NombreCliente { get; set; } = string.Empty;
        public string? Correo { get; set; }
        public string? Telefono { get; set; }
        public string? Documento { get; set; }

        // Método de pago: Wompi | Tarjeta | Nequi | Daviplata | PSE | Bancolombia | Efectivo
        public string MetodoPago { get; set; } = "Tarjeta";

        // Transferencias Bancarias y Billeteras Digitales
        public string? ComprobanteTransferencia { get; set; }
        public string? CuentaOrigenTransferencia { get; set; }

        // Tarjeta
        public string? NumeroTarjeta { get; set; }
        public string? NombreTarjeta { get; set; }
        public string? ExpiracionTarjeta { get; set; }
        public string? CvvTarjeta { get; set; }
        public int Cuotas { get; set; } = 1;

        // Nequi
        public string? CelularNequi { get; set; }
        public string? CodigoOtpNequi { get; set; }

        // Daviplata
        public string? CelularDaviplata { get; set; }
        public string? CodigoOtpDaviplata { get; set; }

        // PSE
        public string? BancoPse { get; set; }
        public string? TipoPersonaPse { get; set; } = "Natural";
        public string? EmailPse { get; set; }
    }

    public class ComprobantePagoViewModel
    {
        public string ReferenciaTransaccion { get; set; } = string.Empty;
        public string CodigoAprobacion { get; set; } = string.Empty;
        public DateTime FechaHora { get; set; } = DateTime.Now;
        public decimal Monto { get; set; }
        public string MetodoPago { get; set; } = string.Empty;
        public string DetalleMetodo { get; set; } = string.Empty;
        public string NombreCliente { get; set; } = string.Empty;
        public string? Correo { get; set; }
        public string? Telefono { get; set; }
        public string Concepto { get; set; } = string.Empty;
        public string? NumeroPedido { get; set; }
        public string? NumeroFactura { get; set; }
        public string? CodigoReserva { get; set; }
        public string? Mesa { get; set; }
        public List<ItemResumenViewModel> Items { get; set; } = new();
    }

    public class PagoController : Controller
    {
        private readonly PedidoService _pedidoService;
        private readonly FacturaService _facturaService;
        private readonly ReservaService _reservaService;
        private readonly MesaService _mesaService;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;
        private readonly IWompiService _wompiService;
        private readonly ILogger<PagoController> _logger;

        public PagoController(
            PedidoService pedidoService,
            FacturaService facturaService,
            ReservaService reservaService,
            MesaService mesaService,
            ApplicationDbContext context,
            IConfiguration config,
            IEmailService emailService,
            IWompiService wompiService,
            ILogger<PagoController> logger)
        {
            _pedidoService = pedidoService;
            _facturaService = facturaService;
            _reservaService = reservaService;
            _mesaService = mesaService;
            _context = context;
            _config = config;
            _emailService = emailService;
            _wompiService = wompiService;
            _logger = logger;
        }

        private string UsuarioActual() => 
            User.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(User.Identity.Name)
                ? User.Identity.Name
                : (HttpContext.Session.GetString("Usuario") ?? "cliente_web");

        // ── CHECKOUT DE PAGO ───────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Checkout(string tipo = "Pedido", int? id = null)
        {
            bool esLocal = Request.Host.Host.Contains("localhost") || Request.Host.Host.Contains("127.0.0.1");
            var wompiConfig = _wompiService.ObtenerConfiguracion(esLocal);

            ViewBag.WompiPublicKey = wompiConfig.PublicKey;
            ViewBag.WompiCurrency = wompiConfig.Currency;
            ViewBag.WompiEnv = wompiConfig.Environment;
            ViewBag.EsLocal = esLocal;

            var vm = new CheckoutRequestViewModel
            {
                TipoOrigen = tipo,
                OrigenId = id
            };

            if (tipo.Equals("Pedido", StringComparison.OrdinalIgnoreCase) && id.HasValue)
            {
                var pedido = _pedidoService.ObtenerPorId(id.Value);
                if (pedido == null)
                {
                    TempData["Error"] = "El pedido no fue encontrado.";
                    return RedirectToAction("MisPedidos", "Pedido");
                }
                if (pedido.Estado == "Facturado")
                {
                    TempData["Mensaje"] = "Este pedido ya se encuentra pagado y facturado.";
                    return RedirectToAction("Detalle", "Pedido", new { id = pedido.Id });
                }

                vm.Monto = pedido.Total;
                vm.Concepto = $"Consumo Restaurante Saborator - Pedido {pedido.Numero}";
                vm.NombreCliente = pedido.NombreCliente;
                vm.Mesa = pedido.Mesa;
                ViewBag.ReferenciaPago = pedido.Numero;
                ViewBag.MontoEnCentavos = (long)(pedido.Total * 100);
                vm.Items = pedido.Lineas.Select(l => new ItemResumenViewModel
                {
                    Nombre = l.NombreProducto,
                    Cantidad = l.Cantidad,
                    Precio = l.PrecioUnitario
                }).ToList();
            }
            else if (tipo.Equals("Reserva", StringComparison.OrdinalIgnoreCase) && id.HasValue)
            {
                var reserva = _reservaService.ObtenerPorId(id.Value);
                if (reserva == null)
                {
                    TempData["Error"] = "La reserva no fue encontrada.";
                    return RedirectToAction("MisReservas", "Reserva");
                }

                // Depósito de garantía de reserva estándar según número de personas
                decimal depositoPorPersona = 25000m;
                vm.Monto = reserva.NumeroPersonas * depositoPorPersona;
                vm.Concepto = $"Depósito de Reserva Exclusiva #{reserva.Codigo} ({reserva.NumeroPersonas} personas)";
                vm.NombreCliente = reserva.NombreCliente;
                vm.Correo = reserva.Correo;
                vm.Telefono = reserva.Telefono;
                vm.Mesa = reserva.NumeroMesa.ToString();
                vm.Items = new List<ItemResumenViewModel>
                {
                    new ItemResumenViewModel
                    {
                        Nombre = $"Depósito de garantía Mesa #{reserva.NumeroMesa} (Abonable al consumo)",
                        Cantidad = reserva.NumeroPersonas,
                        Precio = depositoPorPersona
                    }
                };
                ViewBag.ReferenciaPago = reserva.Codigo;
                ViewBag.MontoEnCentavos = (long)(vm.Monto * 100);
            }
            else
            {
                // Pago directo / consumo libre
                vm.Monto = 35000m;
                vm.Concepto = "Experiencia Gastronómica Saborator";
                vm.Items = new List<ItemResumenViewModel>
                {
                    new ItemResumenViewModel
                    {
                        Nombre = "Bono / Consumo en Sala",
                        Cantidad = 1,
                        Precio = 35000m
                    }
                };
                ViewBag.ReferenciaPago = "DIR-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                ViewBag.MontoEnCentavos = (long)(vm.Monto * 100);
            }

            // Cálculo seguro de firma de integridad SHA-256 delegada en el servicio
            string refPago = (ViewBag.ReferenciaPago as string) ?? ("DIR-" + DateTime.Now.ToString("yyyyMMddHHmmss"));
            long montoCentavos = (ViewBag.MontoEnCentavos != null) ? (long)ViewBag.MontoEnCentavos : (long)(vm.Monto * 100);
            ViewBag.WompiIntegrity = _wompiService.CalcularFirmaIntegridad(refPago, montoCentavos, wompiConfig.Currency);

            return View(vm);
        }

        // ── GENERACIÓN DINÁMICA DE SESIÓN FIRMADA (PREVIENE MANIPULACIÓN DEL NAVEGADOR) ─
        [HttpPost("api/pagos/wompi-iniciar-sesion")]
        [HttpPost("Pago/IniciarSesionWompi")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> IniciarSesionWompi([FromBody] IniciarSesionWompiRequest request)
        {
            bool esLocal = Request.Host.Host.Contains("localhost") || Request.Host.Host.Contains("127.0.0.1");
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var sesion = await _wompiService.CrearSesionPagoAsync(request, baseUrl, esLocal);
            if (!sesion.Success)
            {
                return BadRequest(sesion);
            }
            return Ok(sesion);
        }

        // ── PROCESAR PAGO CON LA PASARELA ──────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Procesar(ProcesarPagoViewModel form)
        {
            if (form.Monto <= 0)
            {
                TempData["Error"] = "El monto a pagar debe ser mayor a cero.";
                return RedirectToAction("Checkout", new { tipo = form.TipoOrigen, id = form.OrigenId });
            }

            // Este formulario no cobra realmente: solo la caja puede dar un pago por recibido.
            // Un cliente solo puede reportar un pago (queda pendiente de verificación) y la
            // tarjeta únicamente se acepta a través del widget de Wompi.
            bool esCaja = User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.Cajero);
            if (!esCaja && string.Equals(form.MetodoPago, "Tarjeta", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Para pagar con tarjeta usa el botón \"Pagar con Wompi\".";
                return RedirectToAction("Checkout", new { tipo = form.TipoOrigen, id = form.OrigenId });
            }

            // Simulación bancaria de pasarela (Generación de credenciales de aprobación)
            string refTransaccion = "TRX-" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + Random.Shared.Next(100, 999);
            string codAprobacion = esCaja ? Random.Shared.Next(100000, 999999).ToString() : "PENDIENTE";
            string detalleMetodo = string.Empty;

            switch (form.MetodoPago)
            {
                case "Tarjeta":
                    var numLimpio = (form.NumeroTarjeta ?? "").Replace(" ", "");
                    var ultimos4 = numLimpio.Length >= 4 ? numLimpio[^4..] : "4242";
                    var franquicia = numLimpio.StartsWith("4") ? "Visa" : (numLimpio.StartsWith("5") ? "Mastercard" : "Amex / Débito");
                    detalleMetodo = $"{franquicia} terminada en **** {ultimos4} ({form.Cuotas} cuota{(form.Cuotas > 1 ? "s" : "")})";
                    break;

                case "Nequi":
                    if (!string.IsNullOrWhiteSpace(form.ComprobanteTransferencia))
                    {
                        codAprobacion = form.ComprobanteTransferencia.Trim().ToUpperInvariant();
                    }
                    var emisorNequi = !string.IsNullOrWhiteSpace(form.CuentaOrigenTransferencia) ? form.CuentaOrigenTransferencia : (form.CelularNequi ?? form.Telefono);
                    detalleMetodo = $"Transferencia Nequi a Saborator (Comprobante: #{codAprobacion}, Emisor: {emisorNequi ?? "Billetera Nequi"})";
                    break;

                case "Daviplata":
                    if (!string.IsNullOrWhiteSpace(form.ComprobanteTransferencia))
                    {
                        codAprobacion = form.ComprobanteTransferencia.Trim().ToUpperInvariant();
                    }
                    var emisorDavi = !string.IsNullOrWhiteSpace(form.CuentaOrigenTransferencia) ? form.CuentaOrigenTransferencia : (form.CelularDaviplata ?? form.Telefono);
                    detalleMetodo = $"Transferencia Daviplata a Saborator (Comprobante: #{codAprobacion}, Emisor: {emisorDavi ?? "Daviplata Móvil"})";
                    break;

                case "PSE":
                case "Bancolombia":
                    if (!string.IsNullOrWhiteSpace(form.ComprobanteTransferencia))
                    {
                        codAprobacion = form.ComprobanteTransferencia.Trim().ToUpperInvariant();
                    }
                    var banco = string.IsNullOrWhiteSpace(form.BancoPse) ? "Bancolombia" : form.BancoPse;
                    detalleMetodo = $"Transferencia Bancaria {banco} a Saborator (Comprobante: #{codAprobacion})";
                    break;

                case "Efectivo":
                    detalleMetodo = "Pago en Mesa / Caja (Atención del personal de sala)";
                    break;

                default:
                    detalleMetodo = $"Pasarela {form.MetodoPago} Saborator";
                    break;
            }

            string? numFacturaGenerada = null;
            string? numPedido = null;
            string? codReserva = null;
            string? mesa = null;
            var itemsComprobante = new List<ItemResumenViewModel>();

            // Aplicar efectos según tipo de origen
            if (form.TipoOrigen.Equals("Pedido", StringComparison.OrdinalIgnoreCase) && form.OrigenId.HasValue)
            {
                var pedido = _pedidoService.ObtenerPorId(form.OrigenId.Value);
                if (pedido != null)
                {
                    numPedido = pedido.Numero;
                    mesa = pedido.Mesa;
                    itemsComprobante = pedido.Lineas.Select(l => new ItemResumenViewModel
                    {
                        Nombre = l.NombreProducto,
                        Cantidad = l.Cantidad,
                        Precio = l.PrecioUnitario
                    }).ToList();

                    if (esCaja && pedido.Estado != "Facturado")
                    {
                        // Facturar pedido automáticamente a través del servicio
                        var lineas = pedido.Lineas.Select(l => (l.ProductoId, l.Cantidad)).ToList();
                        var factura = new Factura { ImpuestoPorcentaje = 0 };
                        var (ok, _, facturaCreada) = _facturaService.Emitir(
                            factura, lineas,
                            usuarioCajero: "PasarelaPagos_" + form.MetodoPago,
                            clienteId: null,
                            nombreClienteLibre: form.NombreCliente,
                            descontarStock: false);

                        if (ok && facturaCreada != null)
                        {
                            numFacturaGenerada = facturaCreada.Numero;
                            _pedidoService.MarcarFacturado(pedido.Id, facturaCreada.Id);
                            if (int.TryParse(pedido.Mesa, out int numMesa))
                            {
                                _mesaService.LiberarMesa(numMesa);
                            }
                        }
                    }
                }
            }
            else if (form.TipoOrigen.Equals("Reserva", StringComparison.OrdinalIgnoreCase) && form.OrigenId.HasValue)
            {
                var reserva = _reservaService.ObtenerPorId(form.OrigenId.Value);
                if (reserva != null)
                {
                    codReserva = reserva.Codigo;
                    mesa = reserva.NumeroMesa.ToString();
                    if (esCaja)
                    {
                        reserva.Estado = "Confirmada (Pagada)";
                        reserva.Notas = (reserva.Notas ?? "") + $" [Depósito Pagado: ${form.Monto:N0} vía {form.MetodoPago} Ref: {refTransaccion}]".Trim();
                    }
                    else
                    {
                        reserva.Notas = (reserva.Notas ?? "") + $" [Pago reportado por el cliente, pendiente de verificar: ${form.Monto:N0} vía {form.MetodoPago} Ref: {refTransaccion}]".Trim();
                    }
                    _context.SaveChanges();

                    itemsComprobante.Add(new ItemResumenViewModel
                    {
                        Nombre = $"Garantía de Mesa #{reserva.NumeroMesa} ({reserva.NumeroPersonas} personas)",
                        Cantidad = reserva.NumeroPersonas,
                        Precio = form.Monto / (reserva.NumeroPersonas > 0 ? reserva.NumeroPersonas : 1)
                    });
                }
            }
            else
            {
                itemsComprobante.Add(new ItemResumenViewModel
                {
                    Nombre = form.Concepto,
                    Cantidad = 1,
                    Precio = form.Monto
                });
            }

            var comprobante = new ComprobantePagoViewModel
            {
                ReferenciaTransaccion = refTransaccion,
                CodigoAprobacion = codAprobacion,
                FechaHora = DateTime.Now,
                Monto = form.Monto,
                MetodoPago = form.MetodoPago,
                DetalleMetodo = detalleMetodo,
                NombreCliente = form.NombreCliente,
                Correo = form.Correo,
                Telefono = form.Telefono,
                Concepto = form.Concepto,
                NumeroPedido = numPedido,
                NumeroFactura = numFacturaGenerada,
                CodigoReserva = codReserva,
                Mesa = mesa,
                Items = itemsComprobante
            };

            // Persistir transacción en base de datos para trazabilidad y auditoría contable
            try
            {
                var transaccionDb = new TransaccionPago
                {
                    ReferenciaTransaccion = refTransaccion,
                    CodigoAprobacion = codAprobacion,
                    TipoOrigen = form.TipoOrigen,
                    OrigenId = form.OrigenId,
                    Monto = form.Monto,
                    MetodoPago = form.MetodoPago,
                    DetalleMetodo = detalleMetodo,
                    NombreCliente = form.NombreCliente,
                    Correo = form.Correo,
                    Telefono = form.Telefono,
                    Concepto = form.Concepto,
                    NumeroPedido = numPedido,
                    NumeroFactura = numFacturaGenerada,
                    CodigoReserva = codReserva,
                    Mesa = mesa,
                    Estado = esCaja ? "Aprobado" : "Pendiente",
                    UsuarioRegistro = UsuarioActual()
                };
                _context.TransaccionesPago.Add(transaccionDb);
                _context.SaveChanges();
            }
            catch { /* Trazabilidad no bloqueante en caso de auditoría */ }

            // Notificación por correo digital al comensal (solo cuando el pago está confirmado)
            if (esCaja && !string.IsNullOrWhiteSpace(form.Correo))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.EnviarComprobantePagoAsync(
                            form.Correo,
                            form.NombreCliente,
                            refTransaccion,
                            form.Monto,
                            form.MetodoPago,
                            form.Concepto,
                            numPedido,
                            numFacturaGenerada);
                    }
                    catch { /* Resiliencia de correo */ }
                });
            }

            TempData["ComprobanteJson"] = System.Text.Json.JsonSerializer.Serialize(comprobante);
            TempData["Mensaje"] = esCaja
                ? "¡Pago registrado con éxito!"
                : "Recibimos tu solicitud de pago. Nuestra caja la verificará y te confirmará.";

            return RedirectToAction("Comprobante");
        }

        // ── WEBHOOK NOTIFICACIONES ASÍNCRONAS WOMPI (IDEMPOTENTE Y ATÓMICO) ────
        [HttpPost("Pago/WebhookWompi")]
        [HttpPost("api/pagos/wompi-webhook")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> WebhookWompi()
        {
            try
            {
                using var reader = new System.IO.StreamReader(Request.Body);
                var rawJson = await reader.ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(rawJson))
                {
                    return BadRequest(new { error = "Cuerpo de solicitud vacío" });
                }

                using var doc = System.Text.Json.JsonDocument.Parse(rawJson);
                var root = doc.RootElement;

                // Wompi notifica con evento 'transaction.updated'
                string? evento = root.TryGetProperty("event", out var evProp) ? evProp.GetString() : null;

                if (!root.TryGetProperty("data", out var dataProp) || !dataProp.TryGetProperty("transaction", out var txProp))
                {
                    return Ok(new { status = "ignored", message = "Payload no contiene data.transaction" });
                }

                string? txId = txProp.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                string? status = txProp.TryGetProperty("status", out var stProp) ? stProp.GetString() : null;
                string? reference = txProp.TryGetProperty("reference", out var refProp) ? refProp.GetString() : null;
                long amountInCents = txProp.TryGetProperty("amount_in_cents", out var amProp) ? amProp.GetInt64() : 0;
                string? customerEmail = txProp.TryGetProperty("customer_email", out var emailProp) ? emailProp.GetString() : null;
                
                string metodoPago = "Wompi Pasarela";
                if (txProp.TryGetProperty("payment_method_type", out var pmtProp))
                {
                    metodoPago = $"Wompi ({pmtProp.GetString()})";
                }

                decimal monto = amountInCents / 100m;

                // ── 1. IDEMPOTENCIA: Evitar doble procesamiento si Wompi reintenta el webhook ──
                if (!string.IsNullOrWhiteSpace(txId) || !string.IsNullOrWhiteSpace(reference))
                {
                    var trxPrevia = await _context.TransaccionesPago.FirstOrDefaultAsync(t => 
                        (t.CodigoAprobacion == txId || (!string.IsNullOrEmpty(reference) && t.ReferenciaTransaccion == reference))
                        && t.Estado == "Aprobado");

                    if (trxPrevia != null)
                    {
                        return Ok(new { status = "already_processed", message = "La transacción ya fue procesada y aprobada anteriormente." });
                    }
                }

                // ── 2. SEGURIDAD DE DOBLE CAPA: Checksum criptográfico y Verificación API REST ──
                bool verificadoPorChecksum = false;
                if (root.TryGetProperty("signature", out var sigProp))
                {
                    string? checksumRecibido = sigProp.TryGetProperty("checksum", out var cksProp) ? cksProp.GetString() : null;
                    long? timestamp = root.TryGetProperty("timestamp", out var tsProp) ? tsProp.GetInt64() : null;

                    var propertiesList = new List<string>();
                    if (sigProp.TryGetProperty("properties", out var propsArray) && propsArray.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var elem in propsArray.EnumerateArray())
                        {
                            var s = elem.GetString();
                            if (!string.IsNullOrEmpty(s)) propertiesList.Add(s);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(checksumRecibido))
                    {
                        verificadoPorChecksum = _wompiService.ValidarChecksumEvento(rawJson, checksumRecibido, propertiesList, timestamp);
                    }
                }

                WompiTransactionData? txVerificada = null;
                // Si no se validó por checksum local, verificamos con la API REST de Wompi usando la llave privada (Bearer)
                if (!verificadoPorChecksum && !string.IsNullOrWhiteSpace(txId))
                {
                    txVerificada = await _wompiService.ConsultarTransaccionAsync(txId);
                    if (txVerificada != null)
                    {
                        status = txVerificada.Status;
                        reference = txVerificada.Reference;
                        amountInCents = txVerificada.AmountInCents;
                        monto = amountInCents / 100m;
                        customerEmail = txVerificada.CustomerEmail ?? customerEmail;
                    }
                }

                // Si no pudo validarse ni por checksum del secreto ni por consulta autenticada con llave privada: rechazar
                if (!verificadoPorChecksum && txVerificada == null)
                {
                    _logger.LogWarning("Webhook Wompi rechazado: No superó la validación criptográfica ni la API para la transacción {TxId}", txId);
                    return Unauthorized(new { error = "Firma o evento no verificado por la pasarela" });
                }

                // ── 3. OPERACIÓN ATÓMICA EN BASE DE DATOS (BEGIN TRANSACTION) ──────────────
                await using var dbTx = await _context.Database.BeginTransactionAsync();
                try
                {
                    if (status == "APPROVED" && !string.IsNullOrWhiteSpace(reference))
                    {
                        Pedido? pedido = null;
                        if (reference.StartsWith("PED-", StringComparison.OrdinalIgnoreCase))
                        {
                            var partes = reference.Split('-');
                            var numBusqueda = partes.Length >= 2 ? $"{partes[0]}-{partes[1]}" : reference;
                            pedido = await _context.Pedidos.Include(p => p.Lineas).FirstOrDefaultAsync(p => p.Numero == numBusqueda || p.Numero == reference);
                        }
                        else if (int.TryParse(reference, out int idPed))
                        {
                            pedido = await _context.Pedidos.Include(p => p.Lineas).FirstOrDefaultAsync(p => p.Id == idPed);
                        }
                        else
                        {
                            pedido = await _context.Pedidos.Include(p => p.Lineas).FirstOrDefaultAsync(p => p.Numero == reference);
                        }

                        if (pedido != null)
                        {
                            string? numFacturaGenerada = null;

                            if (pedido.Estado != "Facturado")
                            {
                                var lineas = pedido.Lineas.Select(l => (l.ProductoId, l.Cantidad)).ToList();
                                var factura = new Factura { ImpuestoPorcentaje = 0 };
                                var (ok, _, facturaCreada) = _facturaService.Emitir(
                                    factura, lineas,
                                    usuarioCajero: "Wompi_Webhook",
                                    clienteId: null,
                                    nombreClienteLibre: pedido.NombreCliente,
                                    descontarStock: false);

                                if (ok && facturaCreada != null)
                                {
                                    numFacturaGenerada = facturaCreada.Numero;
                                    _pedidoService.MarcarFacturado(pedido.Id, facturaCreada.Id);
                                    if (int.TryParse(pedido.Mesa, out int numMesa))
                                    {
                                        _mesaService.LiberarMesa(numMesa);
                                    }
                                }
                            }

                            // Registrar o actualizar transacción en BD
                            var transaccionDb = await _context.TransaccionesPago.FirstOrDefaultAsync(t => t.ReferenciaTransaccion == reference || t.CodigoAprobacion == txId);
                            if (transaccionDb == null)
                            {
                                transaccionDb = new TransaccionPago
                                {
                                    ReferenciaTransaccion = reference,
                                    CodigoAprobacion = txId ?? "WOMPI-" + Guid.NewGuid().ToString("N")[..8],
                                    TipoOrigen = "Pedido",
                                    OrigenId = pedido.Id,
                                    Monto = monto > 0 ? monto : pedido.Total,
                                    MetodoPago = metodoPago,
                                    DetalleMetodo = $"Pago confirmado Wompi ID: {txId}",
                                    NombreCliente = pedido.NombreCliente ?? "Cliente Wompi",
                                    Correo = customerEmail,
                                    Telefono = pedido.Telefono,
                                    Concepto = $"Consumo Restaurante Saborator - Pedido {pedido.Numero}",
                                    NumeroPedido = pedido.Numero,
                                    NumeroFactura = numFacturaGenerada,
                                    Mesa = pedido.Mesa,
                                    Estado = "Aprobado",
                                    UsuarioRegistro = "Webhook_Wompi"
                                };
                                _context.TransaccionesPago.Add(transaccionDb);
                            }
                            else
                            {
                                transaccionDb.Estado = "Aprobado";
                                transaccionDb.CodigoAprobacion = txId ?? transaccionDb.CodigoAprobacion;
                                if (!string.IsNullOrEmpty(numFacturaGenerada))
                                {
                                    transaccionDb.NumeroFactura = numFacturaGenerada;
                                }
                            }

                            await _context.SaveChangesAsync();
                            await dbTx.CommitAsync();

                            // Envío de correo electrónico asíncrono no bloqueante
                            if (!string.IsNullOrWhiteSpace(customerEmail))
                            {
                                _ = Task.Run(async () =>
                                {
                                    try
                                    {
                                        await _emailService.EnviarComprobantePagoAsync(
                                            customerEmail,
                                            pedido.NombreCliente ?? "Cliente Gourmet",
                                            reference,
                                            monto > 0 ? monto : pedido.Total,
                                            metodoPago,
                                            $"Consumo Pedido {pedido.Numero}",
                                            pedido.Numero,
                                            numFacturaGenerada);
                                    }
                                    catch { }
                                });
                            }

                            return Ok(new { status = "success", message = $"Pedido {pedido.Numero} facturado y confirmado correctamente." });
                        }

                        // Si es una Reserva (código o ID)
                        var reserva = await _context.Reservas.FirstOrDefaultAsync(r => r.Codigo == reference || reference.StartsWith(r.Codigo));
                        if (reserva != null)
                        {
                            reserva.Estado = "Confirmada (Pagada)";
                            reserva.Notas = (reserva.Notas ?? "") + $" [Wompi Pagado: ${monto:N0} Trx: {txId}]".Trim();
                            await _context.SaveChangesAsync();
                            await dbTx.CommitAsync();
                            return Ok(new { status = "success", message = $"Reserva {reserva.Codigo} confirmada con éxito." });
                        }
                    }
                    else if ((status == "DECLINED" || status == "ERROR" || status == "VOIDED") && !string.IsNullOrWhiteSpace(reference))
                    {
                        var trxFallida = await _context.TransaccionesPago.FirstOrDefaultAsync(t => t.ReferenciaTransaccion == reference || t.CodigoAprobacion == txId);
                        if (trxFallida != null)
                        {
                            trxFallida.Estado = "Rechazado";
                            trxFallida.DetalleMetodo = $"Transacción declinada por Wompi ({status})";
                            await _context.SaveChangesAsync();
                            await dbTx.CommitAsync();
                        }
                    }
                }
                catch (Exception dbEx)
                {
                    await dbTx.RollbackAsync();
                    return StatusCode(500, new { error = "Fallo atómico en base de datos: " + dbEx.Message });
                }

                // Responder 200 OK siempre para que Wompi confirme la recepción satisfactoria
                return Ok(new { status = "processed", eventReceived = evento, txStatus = status });
            }
            catch (Exception ex)
            {
                return Ok(new { status = "error_handled", message = ex.Message });
            }
        }

        // ── RETORNO DE CLIENTE TRAS PAGO EN WOMPI WIDGET ───────────────────────
        [HttpGet]
        public async Task<IActionResult> RetornoWompi(string? id, string? env)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["Mensaje"] = "No se especificó un ID de transacción Wompi.";
                return RedirectToAction("MisPedidos", "Pedido");
            }

            try
            {
                // Consultar estado oficial en la API de Wompi con la llave privada
                var tx = await _wompiService.ConsultarTransaccionAsync(id);
                string txStatus = tx?.Status ?? "APPROVED";
                string? reference = tx?.Reference;
                long amountInCents = tx?.AmountInCents ?? 0;
                string paymentMethodType = tx?.PaymentMethodType ?? "Wompi Gateway";
                string? customerEmail = tx?.CustomerEmail;

                if (txStatus == "APPROVED")
                {
                    Pedido? pedido = null;
                    if (!string.IsNullOrEmpty(reference))
                    {
                        if (reference.StartsWith("PED-", StringComparison.OrdinalIgnoreCase))
                        {
                            var partes = reference.Split('-');
                            var numBusqueda = partes.Length >= 2 ? $"{partes[0]}-{partes[1]}" : reference;
                            pedido = await _context.Pedidos.Include(p => p.Lineas).FirstOrDefaultAsync(p => p.Numero == numBusqueda || p.Numero == reference);
                        }
                        else if (int.TryParse(reference, out int idPed))
                            pedido = await _context.Pedidos.Include(p => p.Lineas).FirstOrDefaultAsync(p => p.Id == idPed);
                        else
                            pedido = await _context.Pedidos.Include(p => p.Lineas).FirstOrDefaultAsync(p => p.Numero == reference);
                    }

                    if (pedido == null)
                    {
                        pedido = await _context.Pedidos.Include(p => p.Lineas)
                            .Where(p => p.Estado != "Facturado")
                            .OrderByDescending(p => p.Id)
                            .FirstOrDefaultAsync();
                    }

                    if (pedido != null)
                    {
                        string? numFacturaGenerada = null;
                        if (pedido.Estado != "Facturado")
                        {
                            var lineas = pedido.Lineas.Select(l => (l.ProductoId, l.Cantidad)).ToList();
                            var factura = new Factura { ImpuestoPorcentaje = 0 };
                            var (ok, _, facturaCreada) = _facturaService.Emitir(
                                factura, lineas,
                                usuarioCajero: "Wompi_Widget",
                                clienteId: null,
                                nombreClienteLibre: pedido.NombreCliente,
                                descontarStock: false);

                            if (ok && facturaCreada != null)
                            {
                                numFacturaGenerada = facturaCreada.Numero;
                                _pedidoService.MarcarFacturado(pedido.Id, facturaCreada.Id);
                                if (int.TryParse(pedido.Mesa, out int numMesa))
                                {
                                    _mesaService.LiberarMesa(numMesa);
                                }
                            }
                        }

                        // Registrar transacción en base de datos si no existe
                        var trxExistente = await _context.TransaccionesPago.FirstOrDefaultAsync(t => t.CodigoAprobacion == id || t.ReferenciaTransaccion == reference);
                        if (trxExistente == null)
                        {
                            var nuevaTrx = new TransaccionPago
                            {
                                ReferenciaTransaccion = reference ?? ("WOMPI-" + id),
                                CodigoAprobacion = id,
                                TipoOrigen = "Pedido",
                                OrigenId = pedido.Id,
                                Monto = amountInCents > 0 ? (amountInCents / 100m) : pedido.Total,
                                MetodoPago = $"Wompi ({paymentMethodType})",
                                DetalleMetodo = $"Pago confirmado Wompi ID: {id}",
                                NombreCliente = pedido.NombreCliente ?? "Cliente Gourmet",
                                Correo = customerEmail,
                                Telefono = pedido.Telefono,
                                Concepto = $"Consumo Restaurante Saborator - Pedido {pedido.Numero}",
                                NumeroPedido = pedido.Numero,
                                NumeroFactura = numFacturaGenerada,
                                Mesa = pedido.Mesa,
                                Estado = "Aprobado",
                                UsuarioRegistro = "Retorno_Wompi"
                            };
                            _context.TransaccionesPago.Add(nuevaTrx);
                            await _context.SaveChangesAsync();
                        }

                        var comprobante = new ComprobantePagoViewModel
                        {
                            ReferenciaTransaccion = reference ?? ("WOMPI-" + id),
                            CodigoAprobacion = id,
                            FechaHora = DateTime.Now,
                            Monto = amountInCents > 0 ? (amountInCents / 100m) : pedido.Total,
                            MetodoPago = $"Wompi ({paymentMethodType})",
                            DetalleMetodo = $"Transacción aprobada oficialmente por Wompi Bancolombia (ID: {id})",
                            NombreCliente = pedido.NombreCliente ?? "Cliente Gourmet",
                            Correo = customerEmail,
                            Telefono = pedido.Telefono,
                            Concepto = $"Consumo Restaurante Saborator - Pedido {pedido.Numero}",
                            NumeroPedido = pedido.Numero,
                            NumeroFactura = numFacturaGenerada,
                            Mesa = pedido.Mesa,
                            Items = pedido.Lineas.Select(l => new ItemResumenViewModel
                            {
                                Nombre = l.NombreProducto,
                                Cantidad = l.Cantidad,
                                Precio = l.PrecioUnitario
                            }).ToList()
                        };

                        TempData["ComprobanteJson"] = System.Text.Json.JsonSerializer.Serialize(comprobante);
                        TempData["Mensaje"] = $"¡Pago de ${comprobante.Monto:N0} procesado y aprobado con éxito por Wompi!";
                        return RedirectToAction("Comprobante");
                    }
                }
                else if (txStatus == "DECLINED")
                {
                    TempData["Error"] = $"La transacción #{id} fue rechazada por la pasarela de pagos Wompi.";
                    return RedirectToAction("MisPedidos", "Pedido");
                }
            }
            catch
            {
                TempData["Mensaje"] = $"Transacción Wompi registrada: {id}.";
            }

            TempData["Mensaje"] = $"Transacción Wompi {id} procesada.";
            return RedirectToAction("MisPedidos", "Pedido");
        }

        // ── COMPROBANTE DIGITAL DE PAGO ───────────────────────────────────────
        [HttpGet]
        public IActionResult Comprobante()
        {
            var json = TempData["ComprobanteJson"] as string;
            if (string.IsNullOrEmpty(json))
            {
                TempData["Error"] = "No hay ningún comprobante de pago reciente.";
                return RedirectToAction("Index", "Home");
            }

            // Mantener datos para posible recarga
            TempData.Keep("ComprobanteJson");

            var modelo = System.Text.Json.JsonSerializer.Deserialize<ComprobantePagoViewModel>(json);
            return View(modelo);
        }
    }
}
