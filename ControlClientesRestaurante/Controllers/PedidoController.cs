using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ControlClientesRestaurante.Models;
using ControlClientesRestaurante.Services;
using ControlClientesRestaurante.Hubs;
using ControlClientesRestaurante.Repositories;

namespace ControlClientesRestaurante.Controllers
{
    public class PedidoController : Controller
    {
        private readonly PedidoService   _pedidoService;
        private readonly ProductoService _productoService;
        private readonly MesaService     _mesaService;
        private readonly CuponService    _cuponService;
        private readonly AuditoriaService _auditoriaService;
        private readonly IConfiguration _config;
        private readonly IHubContext<CocinaHub> _cocinaHub;
        private readonly IUsuarioRepository _usuarioRepo;

        public PedidoController(
            PedidoService   pedidoService,
            ProductoService productoService,
            MesaService     mesaService,
            CuponService    cuponService,
            AuditoriaService auditoriaService,
            IConfiguration config,
            IHubContext<CocinaHub> cocinaHub,
            IUsuarioRepository usuarioRepo)
        {
            _pedidoService   = pedidoService;
            _productoService = productoService;
            _mesaService     = mesaService;
            _cuponService    = cuponService;
            _auditoriaService = auditoriaService;
            _config          = config;
            _cocinaHub       = cocinaHub;
            _usuarioRepo     = usuarioRepo;
        }

        // La identidad y el rol se toman únicamente de la cookie de autenticación:
        // la sesión puede sobrevivir a la cookie y no debe conceder permisos.
        private bool EstaAutenticado() => User.Identity?.IsAuthenticated == true;

        private string Rol()     => EstaAutenticado() ? (User.FindFirst(ClaimTypes.Role)?.Value ?? "") : "";
        private string Usuario() => EstaAutenticado() ? (User.Identity?.Name ?? "") : "";

        private bool EsMesero()       => User.IsInRole(Roles.Mesero);
        private bool EsAdminOCajero() => User.IsInRole(Roles.Administrador) || User.IsInRole(Roles.Cajero);
        private bool EsCliente()      => User.IsInRole(Roles.Cliente);
        private bool EsCocinero()     => User.IsInRole(Roles.Cocinero);
        private bool EsDomiciliario() => User.IsInRole(Roles.Domiciliario);

        private const string SesionPedidosOnline = "PedidosOnline";

        /// <summary>¿Puede el visitante actual ver los datos personales (dirección, teléfono) del pedido?</summary>
        private bool PuedeVerDatosPrivados(Pedido pedido)
        {
            if (EsAdminOCajero() || EsMesero() || EsCocinero()) return true;
            if (EsDomiciliario()) return string.Equals(pedido.Repartidor, Usuario(), StringComparison.OrdinalIgnoreCase);
            if (EstaAutenticado() && pedido.UsuarioMesero == Usuario()) return true;

            // Pedidos hechos sin cuenta desde este mismo navegador
            var propios = (HttpContext.Session.GetString(SesionPedidosOnline) ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
            return propios.Contains(pedido.Numero);
        }

        // ── MENU ──────────────────────────────────────────────────────────
        [HttpGet]
        [Authorize(Roles = "Mesero,Administrador,Cajero,Cliente")]
        public IActionResult Menu(string? mesa = null)
        {
            // La pantalla "Tomar pedido" con mapa de mesas es exclusiva del personal (mesero/cajero/admin).
            // Los clientes solo pueden pedir para domicilio o para llevar.
            if (EsCliente())
                return RedirectToAction("Checkout", new { tipo = "Domicilio" });

            ViewBag.Productos = _productoService.ObtenerActivos();
            ViewBag.Mesas     = _mesaService.ObtenerTodas();   // para el selector visual
            ViewBag.MesaPreseleccionada = mesa;
            return View();
        }

        private async Task NotificarNuevaComandaSignalR(Pedido pedido)
        {
            try
            {
                await _cocinaHub.Clients.Group("Cocina").SendAsync("NuevaComanda", new
                {
                    id = pedido.Id,
                    numero = pedido.Numero,
                    mesa = pedido.Mesa,
                    tipoEntrega = pedido.TipoEntrega,
                    cliente = pedido.NombreCliente,
                    estado = pedido.Estado,
                    notas = pedido.Notas,
                    minutos = 0,
                    lineas = pedido.Lineas.Select(l => new
                    {
                        nombre = l.NombreProducto,
                        cantidad = l.Cantidad,
                        notas = l.Notas
                    })
                });
            }
            catch { /* Non-blocking */ }
        }

        private async Task NotificarCambioEstadoSignalR(int id, string nuevoEstado)
        {
            try
            {
                var ped = _pedidoService.ObtenerPorId(id);
                await _cocinaHub.Clients.Group("Cocina").SendAsync("ComandaActualizada", new
                {
                    id = id,
                    numero = ped?.Numero,
                    nuevoEstado = nuevoEstado
                });

                if (ped != null)
                {
                    await _cocinaHub.Clients.Group($"Pedido_{ped.Numero}").SendAsync("EstadoPedidoCambiado", new
                    {
                        numero = ped.Numero,
                        estado = nuevoEstado
                    });
                }
            }
            catch { /* Non-blocking */ }
        }

        [HttpPost]
        [Authorize(Roles = "Mesero,Administrador,Cajero,Cliente")]
        public async Task<IActionResult> Menu(
            [FromForm] string   mesa,
            [FromForm] string?  nombreCliente,
            [FromForm] string?  notas,
            [FromForm] int[]    productoIds,
            [FromForm] int[]    cantidades,
            [FromForm] string[] notasLineas)
        {
            if (EsCliente())
                return RedirectToAction("Checkout", new { tipo = "Domicilio" });

            // Validaciones
            if (string.IsNullOrWhiteSpace(mesa) || !int.TryParse(mesa.Trim(), out int numMesa) || numMesa < 1 || numMesa > 30)
            {
                TempData["Error"] = "Selecciona una mesa válida (1-30).";
                ViewBag.Productos = _productoService.ObtenerActivos();
                ViewBag.Mesas     = _mesaService.ObtenerTodas();
                return View();
            }
            if (string.IsNullOrWhiteSpace(nombreCliente))
            {
                TempData["Error"] = "El nombre del cliente es obligatorio.";
                ViewBag.Productos = _productoService.ObtenerActivos();
                ViewBag.Mesas     = _mesaService.ObtenerTodas();
                return View();
            }
            if (productoIds == null || productoIds.Length == 0)
            {
                TempData["Error"] = "Agrega al menos un producto al pedido.";
                ViewBag.Productos = _productoService.ObtenerActivos();
                ViewBag.Mesas     = _mesaService.ObtenerTodas();
                return View();
            }

            if (cantidades != null && cantidades.Any(c => c > 999))
            {
                TempData["Error"] = "La cantidad máxima permitida por producto es 999 unidades.";
                ViewBag.Productos = _productoService.ObtenerActivos();
                ViewBag.Mesas     = _mesaService.ObtenerTodas();
                return View();
            }

            // Verificar que la mesa esté libre
            var mesaObj = _mesaService.ObtenerTodas().FirstOrDefault(m => m.Numero == numMesa);
            if (mesaObj?.Estado == "Ocupada" || mesaObj?.Estado == "En Limpieza" || mesaObj?.Estado == "Mantenimiento")
            {
                TempData["Error"] = $"La mesa {numMesa} se encuentra actualmente '{mesaObj.Estado}'. Libérala en el Control de Mesas para tomar pedidos en ella.";
                ViewBag.Productos = _productoService.ObtenerActivos();
                ViewBag.Mesas     = _mesaService.ObtenerTodas();
                return View();
            }

            var lineas = productoIds
                .Select((id, i) => (
                    productoId: id,
                    cantidad:   cantidades != null && cantidades.Length > i ? cantidades[i] : 1,
                    nota:       notasLineas != null && notasLineas.Length > i ? notasLineas[i] : (string?)null))
                .Where(t => t.cantidad > 0)
                .ToList();

            var (ok, mensaje, pedido) = _pedidoService.Crear(
                mesa.Trim(), nombreCliente, Usuario(), notas, lineas);

            if (!ok)
            {
                TempData["Error"] = mensaje;
                ViewBag.Productos = _productoService.ObtenerActivos();
                ViewBag.Mesas     = _mesaService.ObtenerTodas();
                return View();
            }

            // Ocupar la mesa
            _mesaService.OcuparMesa(numMesa, pedido!.Id);

            // Notificar en tiempo real a la pantalla de cocina (KDS)
            await NotificarNuevaComandaSignalR(pedido);

            TempData["Mensaje"] = $"{mensaje} — Mesa {numMesa} ocupada.";
            return RedirectToAction("MisPedidos");
        }

        // ── MIS PEDIDOS ───────────────────────────────────────────────────
        // ── MIS PEDIDOS ───────────────────────────────────────────────────
        [Authorize(Roles = "Mesero,Cliente")]
        public IActionResult MisPedidos()
        {
            var pedidos = _pedidoService.ObtenerPorMesero(Usuario());
            return View(pedidos);
        }

        // ── INDEX (admin/cajero — cola de pedidos activos) ────────────────
        [Authorize(Roles = "Administrador,Cajero")]
        public IActionResult Index()
        {
            ViewBag.MesasLibres   = _mesaService.MesasLibres();
            ViewBag.MesasOcupadas = _mesaService.MesasOcupadas();
            var pedidos = _pedidoService.ObtenerPendientes();
            return View(pedidos);
        }

        // ── DETALLE ───────────────────────────────────────────────────────
        [Authorize]
        public IActionResult Detalle(int id)
        {
            var pedido = _pedidoService.ObtenerPorId(id);
            if (pedido == null) return NotFound();

            if ((EsMesero() || EsCliente()) && pedido.UsuarioMesero != Usuario())
            {
                TempData["Error"] = "No tienes permiso para ver pedidos de otros usuarios.";
                return RedirectToAction("MisPedidos");
            }
            if (EsDomiciliario() && !string.Equals(pedido.Repartidor, Usuario(), StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Solo puedes ver los domicilios que tienes asignados.";
                return RedirectToAction("Repartos");
            }

            ViewBag.EsAdminOCajero = EsAdminOCajero();
            ViewBag.WompiPublicKey = _config["Wompi:PublicKey"] ?? "pub_prod_3LfjIbKhTNWppn3OIYoG3aVNYUzmWUnw";
            ViewBag.WompiCurrency  = _config["Wompi:Currency"] ?? "COP";
            return View(pedido);
        }

        // ── CAMBIAR ESTADO ────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> CambiarEstado(int id, string estado)
        {
            if (!EstaAutenticado()) return RedirectToAction("Index", "Login");
            if (!EsAdminOCajero()) return Forbid();

            try
            {
                var (ok, mensaje) = _pedidoService.CambiarEstado(id, estado);
                if (ok)
                {
                    await NotificarCambioEstadoSignalR(id, estado);
                    if (string.Equals(estado, "Cancelado", StringComparison.OrdinalIgnoreCase))
                    {
                        var pedObj = _pedidoService.ObtenerPorId(id);
                        _auditoriaService.Registrar(
                            Usuario(),
                            Rol(),
                            "CANCELACION_PEDIDO",
                            "Pedidos",
                            $"El pedido {pedObj?.Numero ?? id.ToString()} por valor de ${(pedObj?.Total ?? 0):N0} COP fue cancelado.",
                            "Pedido",
                            id,
                            HttpContext.Connection.RemoteIpAddress?.ToString());
                    }
                }
                TempData[ok ? "Mensaje" : "Error"] = mensaje;
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cambiar estado: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // ══════════════════════════════════════════════════════════════════
        // ══ SISTEMA DE DOMICILIOS & ENTREGAS ESTILO KFC ════════════════════
        // ══════════════════════════════════════════════════════════════════

        // ── CHECKOUT DE DOMICILIO & PARA LLEVAR ───────────────────────────
        [HttpGet]
        public IActionResult Checkout(string? tipo = "Domicilio")
        {
            bool esCliente = EsCliente();
            // Los clientes no comen en el local desde este flujo: solo Domicilio o Para Llevar
            if (esCliente && (string.IsNullOrWhiteSpace(tipo) || tipo == "EnMesa"))
                tipo = "Domicilio";

            ViewBag.Productos = _productoService.ObtenerActivos();
            ViewBag.Mesas     = _mesaService.ObtenerTodas();
            ViewBag.TipoEntregaDefecto = tipo ?? "Domicilio";
            ViewBag.UsuarioActual = Usuario();
            var nombreClaim = User.FindFirst("NombreCompleto")?.Value;
            ViewBag.NombreUsuario = !string.IsNullOrWhiteSpace(nombreClaim) ? nombreClaim : (HttpContext.Session.GetString("UsuarioNombre") ?? Usuario());
            ViewBag.EsCajeroOAdmin = EsAdminOCajero();
            ViewBag.EsCliente = esCliente;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmarCheckout(
            [FromForm] string   tipoEntrega,
            [FromForm] string   nombreCliente,
            [FromForm] string   telefono,
            [FromForm] string?  correo,
            [FromForm] string?  direccion,
            [FromForm] string?  barrio,
            [FromForm] string?  mesa,
            [FromForm] string?  notas,
            [FromForm] string   metodoPago,
            [FromForm] decimal? pagaCon,
            [FromForm] decimal? costoEnvio,
            [FromForm] string?  codigoCupon,
            [FromForm] int[]    productoIds,
            [FromForm] int[]    cantidades,
            [FromForm] string[] notasLineas)
        {
            if (EsCliente() && tipoEntrega == "EnMesa")
            {
                TempData["Error"] = "Los pedidos de clientes son solo para Domicilio o Para Llevar.";
                return RedirectToAction("Checkout", new { tipo = "Domicilio" });
            }

            if (string.IsNullOrWhiteSpace(nombreCliente))
            {
                TempData["Error"] = "Por favor indica tu nombre completo.";
                return RedirectToAction("Checkout", new { tipo = tipoEntrega });
            }

            if (string.IsNullOrWhiteSpace(telefono))
            {
                TempData["Error"] = "El número telefónico es obligatorio para el contacto del pedido.";
                return RedirectToAction("Checkout", new { tipo = tipoEntrega });
            }

            if (tipoEntrega == "Domicilio" && string.IsNullOrWhiteSpace(direccion))
            {
                TempData["Error"] = "Por favor ingresa la dirección de entrega de tu domicilio.";
                return RedirectToAction("Checkout", new { tipo = tipoEntrega });
            }

            if (productoIds == null || productoIds.Length == 0)
            {
                TempData["Error"] = "Tu carrito de compras está vacío. Agrega productos de la carta.";
                return RedirectToAction("Index", "Home");
            }

            if (cantidades != null && cantidades.Any(c => c > 999))
            {
                TempData["Error"] = "La cantidad máxima permitida por producto es 999 unidades.";
                return RedirectToAction("Checkout", new { tipo = tipoEntrega });
            }

            var lineas = productoIds
                .Select((id, i) => (
                    productoId: id,
                    cantidad:   cantidades != null && cantidades.Length > i ? cantidades[i] : 1,
                    nota:       notasLineas != null && notasLineas.Length > i ? notasLineas[i] : (string?)null))
                .Where(t => t.cantidad > 0)
                .ToList();

            if (lineas.Count == 0)
            {
                TempData["Error"] = "Debes incluir al menos un producto válido.";
                return RedirectToAction("Checkout", new { tipo = tipoEntrega });
            }

            // ── LÍMITE DE DOMICILIO: MONTO MÍNIMO DE $20.000 COP ──────────────
            decimal subtotalProductos = 0;
            foreach (var l in lineas)
            {
                var prod = _productoService.ObtenerPorId(l.productoId);
                if (prod != null) subtotalProductos += prod.Precio * l.cantidad;
            }

            const decimal LimiteMinimoDomicilio = 20000m;
            if (tipoEntrega == "Domicilio" && subtotalProductos < LimiteMinimoDomicilio)
            {
                TempData["Error"] = $"El pedido mínimo para despacho a domicilio es de ${LimiteMinimoDomicilio:N0} COP. Tu subtotal actual es de ${subtotalProductos:N0} COP. Agrega más productos a tu orden o elige la opción 'Para Llevar' sin monto mínimo.";
                return RedirectToAction("Checkout", new { tipo = tipoEntrega });
            }

            // ── VALIDACIÓN Y APLICACIÓN SEGURA DE CUPÓN PROMOCIONAL EN SERVIDOR ──
            decimal descuentoCupon = 0;
            string? cuponAplicado = null;
            if (!string.IsNullOrWhiteSpace(codigoCupon))
            {
                var (validoCupon, mensajeCupon, montoDescuento, objCupon) = _cuponService.Validar(codigoCupon, subtotalProductos);
                if (validoCupon && objCupon != null)
                {
                    descuentoCupon = montoDescuento;
                    cuponAplicado = objCupon.Codigo;
                }
            }

            // ── TARIFA DE DOMICILIO SEGURA (PROTECCIÓN CONTRA MANIPULACIÓN DEL CLIENTE) ──
            decimal costoEnvioFinal = 0;
            if (tipoEntrega == "Domicilio")
            {
                decimal tarifaOficial = CalcularTarifaOficialPorBarrio(barrio);

                // ÚNICAMENTE el Cajero o Administrador pueden fijar una tarifa manual o de cortesía ($0)
                if (EsAdminOCajero() && costoEnvio.HasValue && costoEnvio.Value >= 0)
                {
                    costoEnvioFinal = costoEnvio.Value;
                }
                else
                {
                    // Clientes externos / comensales web: la tarifa la determina estrictamente el servidor según la zona
                    costoEnvioFinal = tarifaOficial;
                }
            }
            string mesaFinal = tipoEntrega == "EnMesa" ? (string.IsNullOrWhiteSpace(mesa) ? "Mesa 1" : mesa) : (tipoEntrega == "Domicilio" ? "Domicilio" : "Para Llevar");
            string usuarioMesero = EstaAutenticado() ? Usuario() : "Online";

            var (ok, mensaje, pedido) = _pedidoService.Crear(
                mesaFinal,
                nombreCliente.Trim(),
                usuarioMesero,
                notas,
                lineas,
                tipoEntrega: tipoEntrega,
                direccion: direccion?.Trim(),
                barrio: barrio?.Trim(),
                telefono: telefono.Trim(),
                metodoPago: metodoPago,
                costoEnvio: costoEnvioFinal,
                pagaCon: pagaCon,
                descuento: descuentoCupon,
                codigoCupon: cuponAplicado);

            if (!ok || pedido == null)
            {
                TempData["Error"] = mensaje;
                return RedirectToAction("Checkout", new { tipo = tipoEntrega });
            }

            if (!string.IsNullOrEmpty(cuponAplicado))
            {
                _cuponService.IncrementarUso(cuponAplicado);
            }

            // Si es pedido en mesa y la mesa es un número, ocupar mesa
            if (tipoEntrega == "EnMesa" && int.TryParse(mesaFinal.Replace("Mesa", "").Trim(), out int numMesa))
            {
                _mesaService.OcuparMesa(numMesa, pedido.Id);
            }

            // Notificar en tiempo real a la cocina (SignalR)
            await NotificarNuevaComandaSignalR(pedido);

            // Recordar el pedido en este navegador para poder mostrar sus datos en el rastreador
            var previos = HttpContext.Session.GetString(SesionPedidosOnline);
            HttpContext.Session.SetString(SesionPedidosOnline, string.IsNullOrEmpty(previos) ? pedido.Numero : $"{previos},{pedido.Numero}");

            TempData["Exito"] = $"¡Pedido {pedido.Numero} recibido con éxito!";
            return RedirectToAction("Seguimiento", new { id = pedido.Numero });
        }

        // ── RASTREADOR EN VIVO ESTILO KFC (LIVE ORDER TRACKER) ────────────
        [HttpGet]
        public IActionResult Seguimiento(string? id, string? telefono)
        {
            Pedido? pedido = null;

            if (!string.IsNullOrWhiteSpace(id))
            {
                pedido = _pedidoService.ObtenerPorNumero(id);
            }
            else if (!string.IsNullOrWhiteSpace(telefono))
            {
                // Coincidencia exacta de dígitos (antes bastaba un fragmento como "3" para ver pedidos ajenos)
                var digitos = new string(telefono.Where(char.IsDigit).ToArray());
                if (digitos.Length >= 7)
                {
                    pedido = _pedidoService.ObtenerPorTelefono(digitos)
                        .FirstOrDefault(p => new string((p.Telefono ?? "").Where(char.IsDigit).ToArray()).EndsWith(digitos));
                }
            }

            if (pedido == null)
            {
                ViewBag.BusquedaFallida = true;
                ViewBag.CodigoBuscado = id ?? telefono;
                return View("Rastrear");
            }

            ViewBag.WompiPublicKey = _config["Wompi:PublicKey"] ?? "pub_prod_3LfjIbKhTNWppn3OIYoG3aVNYUzmWUnw";
            ViewBag.WompiCurrency  = _config["Wompi:Currency"] ?? "COP";
            ViewBag.DatosPrivados  = PuedeVerDatosPrivados(pedido);
            return View(pedido);
        }

        // ── PÁGINA DE BÚSQUEDA / CONSULTA DE PEDIDO ───────────────────────
        [HttpGet]
        public IActionResult Rastrear()
        {
            return View();
        }

        // ── API JSON DE ESTADO EN TIEMPO REAL (POLLING) ───────────────────
        [HttpGet]
        public IActionResult EstadoApi(string numero)
        {
            var p = _pedidoService.ObtenerPorNumero(numero);
            if (p == null) return NotFound(new { error = "Pedido no encontrado" });

            return Json(new
            {
                numero      = p.Numero,
                estado      = p.Estado,
                tipoEntrega = p.TipoEntrega,
                total       = p.Total,
                repartidor  = p.Estado == "EnCamino" ? p.Repartidor : null,
                fecha       = p.FechaCreacion.ToString("HH:mm - dd/MM/yyyy")
            });
        }

        // ── PANEL DE GESTIÓN DE DOMICILIOS (RESTAURANTE / MESERO / CAJERO) ─
        [HttpGet]
        [Authorize(Roles = "Administrador,Cajero,Mesero")]
        public IActionResult Domicilios()
        {
            var domicilios = _pedidoService.ObtenerDomicilios();
            ViewBag.EsAdminOCajero  = EsAdminOCajero();
            ViewBag.Domiciliarios   = _usuarioRepo.ObtenerActivosPorRol(Roles.Domiciliario)
                .Select(u => u.NombreUsuario).ToList();
            return View(domicilios);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador,Cajero,Mesero")]
        public async Task<IActionResult> ActualizarEstadoDomicilio(int id, string nuevoEstado)
        {
            if (string.Equals(nuevoEstado, "EnCamino", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Para despachar un domicilio asígnale un domiciliario; él confirma la salida desde su panel.";
                return RedirectToAction("Domicilios");
            }
            if (string.Equals(nuevoEstado, "Cancelado", StringComparison.OrdinalIgnoreCase) && !EsAdminOCajero())
            {
                TempData["Error"] = "Solo el cajero o el administrador pueden cancelar pedidos.";
                return RedirectToAction("Domicilios");
            }

            try
            {
                var (ok, mensaje) = _pedidoService.CambiarEstado(id, nuevoEstado);
                if (ok)
                {
                    await NotificarCambioEstadoSignalR(id, nuevoEstado);
                }
                TempData[ok ? "Mensaje" : "Error"] = mensaje;
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction("Domicilios");
        }

        [HttpPost]
        [Authorize(Roles = "Administrador,Cajero")]
        public IActionResult AsignarValorDomicilio(int id, decimal nuevoCostoEnvio)
        {
            var (ok, mensaje) = _pedidoService.ActualizarCostoEnvio(id, nuevoCostoEnvio);
            TempData[ok ? "Mensaje" : "Error"] = mensaje;
            return RedirectToAction("Domicilios");
        }

        public class AsignarTarifaDto
        {
            public int Id { get; set; }
            public decimal NuevoCostoEnvio { get; set; }
        }

        [HttpPost]
        public IActionResult AsignarValorDomicilioApi([FromBody] AsignarTarifaDto dto)
        {
            if (!EstaAutenticado() || !EsAdminOCajero())
                return StatusCode(403, new { ok = false, mensaje = "Solo el Cajero o Administrador tienen autorización para modificar o exonerar tarifas de domicilio." });

            var (ok, mensaje) = _pedidoService.ActualizarCostoEnvio(dto.Id, dto.NuevoCostoEnvio);
            var pedido = _pedidoService.ObtenerPorId(dto.Id);
            return Json(new 
            { 
                ok, 
                mensaje, 
                costoEnvio = pedido?.CostoEnvio ?? dto.NuevoCostoEnvio, 
                total = pedido?.Total ?? 0 
            });
        }

        // ── ASIGNAR DOMICILIARIO (CAJERO / ADMIN) ─────────────────────────
        [HttpPost]
        [Authorize(Roles = "Administrador,Cajero")]
        public IActionResult AsignarRepartidor(int id, string? repartidor)
        {
            if (!string.IsNullOrWhiteSpace(repartidor))
            {
                var usuario = _usuarioRepo.ObtenerPorNombre(repartidor);
                if (usuario == null || !usuario.Estado || usuario.Rol?.NombreRol != Roles.Domiciliario)
                {
                    TempData["Error"] = "El usuario seleccionado no es un domiciliario activo.";
                    return RedirectToAction("Domicilios");
                }
                repartidor = usuario.NombreUsuario;
            }

            var (ok, mensaje) = _pedidoService.AsignarRepartidor(id, repartidor);
            if (ok)
            {
                _auditoriaService.Registrar(Usuario(), Rol(), "ASIGNAR_DOMICILIARIO", "Domicilios", mensaje,
                    "Pedido", id, HttpContext.Connection.RemoteIpAddress?.ToString());
            }
            TempData[ok ? "Mensaje" : "Error"] = mensaje;
            return RedirectToAction("Domicilios");
        }

        // ══════════════════════════════════════════════════════════════════
        // ══ PANEL DEL DOMICILIARIO ════════════════════════════════════════
        // ══════════════════════════════════════════════════════════════════

        [HttpGet]
        [Authorize(Roles = "Domiciliario,Administrador")]
        public IActionResult Repartos()
        {
            var yo = Usuario();
            var mios = _pedidoService.ObtenerRepartosDe(yo);

            ViewBag.Disponibles  = _pedidoService.ObtenerRepartosDisponibles();
            ViewBag.Asignados    = mios.Where(p => p.Estado is "Pendiente" or "EnPreparacion" or "Listo").ToList();
            ViewBag.EnCamino     = mios.Where(p => p.Estado == "EnCamino").ToList();
            ViewBag.EntregadosHoy = mios
                .Where(p => (p.Estado == "Entregado" || p.Estado == "Facturado") &&
                            (p.FechaEntrega ?? p.FechaCreacion).Date == DateTime.Today)
                .OrderByDescending(p => p.FechaEntrega)
                .ToList();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Domiciliario,Administrador")]
        public async Task<IActionResult> TomarReparto(int id)
        {
            var (ok, mensaje) = _pedidoService.IniciarReparto(id, Usuario());
            if (ok)
            {
                await NotificarCambioEstadoSignalR(id, "EnCamino");
                _auditoriaService.Registrar(Usuario(), Rol(), "DOMICILIO_EN_CAMINO", "Domicilios", mensaje,
                    "Pedido", id, HttpContext.Connection.RemoteIpAddress?.ToString());
            }
            TempData[ok ? "Mensaje" : "Error"] = mensaje;
            return RedirectToAction("Repartos");
        }

        [HttpPost]
        [Authorize(Roles = "Domiciliario,Administrador,Cajero")]
        public async Task<IActionResult> ConfirmarEntrega(int id, string? volverA)
        {
            var (ok, mensaje) = _pedidoService.ConfirmarEntrega(id, Usuario(), esSupervisor: EsAdminOCajero());
            if (ok)
            {
                await NotificarCambioEstadoSignalR(id, "Entregado");
                _auditoriaService.Registrar(Usuario(), Rol(), "DOMICILIO_ENTREGADO", "Domicilios", mensaje,
                    "Pedido", id, HttpContext.Connection.RemoteIpAddress?.ToString());
            }
            TempData[ok ? "Mensaje" : "Error"] = mensaje;
            return volverA == "Domicilios" && EsAdminOCajero()
                ? RedirectToAction("Domicilios")
                : RedirectToAction("Repartos");
        }

        private static decimal CalcularTarifaOficialPorBarrio(string? barrio)
        {
            if (string.IsNullOrWhiteSpace(barrio)) return 4000m;
            string b = barrio.Trim().ToLowerInvariant();

            // Zona 3: Norte / Riomar / El Prado ($9.000 COP)
            string[] norte = { "prado", "alto prado", "riomar", "miramar", "buenavista", "villa campestre", "villa santos", "paraíso", "paraiso" };
            if (norte.Any(n => b.Contains(n))) return 9000m;

            // Zona 2: Centro / Suroccidente / Soledad ($6.000 COP)
            string[] centro = { "san roque", "rebolo", "chiquinquira", "chiquinquirá", "san felipe", "boston", "bostón", "lucero", "los pinos", "el valle", "cevillar", "soledad", "hipodromo", "hipódromo" };
            if (centro.Any(c => b.Contains(c))) return 6000m;

            // Zona 1: Sur Cercano (Default oficial $4.000 COP)
            return 4000m;
        }

        // ══════════════════════════════════════════════════════════════════
        // ══ TAREAS DEL COCINERO (KITCHEN DISPLAY SYSTEM / KDS) ════════════
        // ══════════════════════════════════════════════════════════════════

        // ── PANTALLA DE COCINA (KDS / COMANDAS ACTIVAS) ───────────────────
        [HttpGet]
        [Authorize(Roles = "Cocinero,Administrador,Mesero")]
        public IActionResult Cocina()
        {
            var pedidosCocina = _pedidoService.ObtenerParaCocina();
            ViewBag.PedidosListos = _pedidoService.ObtenerListosCocina();
            ViewBag.EsCocinero = EsCocinero();
            ViewBag.PuedeCocinar = EsCocinero() || User.IsInRole(Roles.Administrador);
            return View(pedidosCocina);
        }

        // ── ACCIONES RÁPIDAS DEL COCINERO ─────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "Cocinero,Administrador")]
        public async Task<IActionResult> CocinaCambiarEstado(int id, string nuevoEstado)
        {
            // Cocinero puede cambiar a "EnPreparacion" o "Listo"
            try
            {
                var (ok, mensaje) = _pedidoService.CambiarEstado(id, nuevoEstado);
                if (ok)
                {
                    await NotificarCambioEstadoSignalR(id, nuevoEstado);
                }
                TempData[ok ? "Mensaje" : "Error"] = mensaje;
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction("Cocina");
        }

        // ── API EN TIEMPO REAL PARA LA COCINA (POLLING & ALERTAS) ─────────
        [HttpGet]
        [Authorize(Roles = "Cocinero,Administrador,Mesero")]
        public IActionResult CocinaApi()
        {
            var pedidos = _pedidoService.ObtenerParaCocina();

            return Json(new
            {
                total = pedidos.Count,
                pendientes = pedidos.Count(p => p.Estado == "Pendiente"),
                enPreparacion = pedidos.Count(p => p.Estado == "EnPreparacion"),
                pedidos = pedidos.Select(p => new
                {
                    id = p.Id,
                    numero = p.Numero,
                    mesa = p.Mesa,
                    tipoEntrega = p.TipoEntrega,
                    estado = p.Estado,
                    cliente = p.NombreCliente,
                    minutos = (int)(DateTime.Now - p.FechaCreacion).TotalMinutes,
                    lineas = p.Lineas.Select(l => new
                    {
                        nombre = l.NombreProducto,
                        cantidad = l.Cantidad,
                        notas = l.Notas
                    })
                })
            });
        }

        // ── RECETARIO Y FICHAS TÉCNICAS PARA EL COCINERO ───────────────────
        [HttpGet]
        [Authorize(Roles = "Cocinero,Administrador,Mesero")]
        public IActionResult Recetario()
        {
            var productos = _productoService.ObtenerActivos();
            return View(productos);
        }
    }
}
