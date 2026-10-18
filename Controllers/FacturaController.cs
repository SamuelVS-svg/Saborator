using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ControlClientesRestaurante.Models;
using ControlClientesRestaurante.Services;
using ControlClientesRestaurante.Repositories;

namespace ControlClientesRestaurante.Controllers
{
    [Authorize(Roles = "Administrador,Cajero")]
    public class FacturaController : Controller
    {
        private readonly FacturaService    _facturaService;
        private readonly ProductoService   _productoService;
        private readonly IClienteRepository _clienteRepo;
        private readonly PedidoService     _pedidoService;
        private readonly MesaService       _mesaService;
        private readonly AuditoriaService  _auditoriaService;

        public FacturaController(
            FacturaService     facturaService,
            ProductoService    productoService,
            IClienteRepository clienteRepo,
            PedidoService      pedidoService,
            MesaService        mesaService,
            AuditoriaService   auditoriaService)
        {
            _facturaService  = facturaService;
            _productoService = productoService;
            _clienteRepo     = clienteRepo;
            _pedidoService   = pedidoService;
            _mesaService     = mesaService;
            _auditoriaService = auditoriaService;
        }

        private string UsuarioActual() => User.Identity?.Name ?? "sistema";

        // ── INDEX CON FILTROS DE FECHA Y BÚSQUEDA ─────────────────────────
        public IActionResult Index(DateTime? desde = null, DateTime? hasta = null, string? buscar = null, string? cajero = null)
        {
            var facturas = _facturaService.ObtenerTodas();

            if (desde.HasValue)
            {
                var fDesde = desde.Value.Date;
                facturas = facturas.Where(f => f.FechaCreacion >= fDesde).ToList();
            }

            if (hasta.HasValue)
            {
                var fHasta = hasta.Value.Date.AddDays(1).AddTicks(-1);
                facturas = facturas.Where(f => f.FechaCreacion <= fHasta).ToList();
            }

            if (!string.IsNullOrWhiteSpace(cajero))
            {
                facturas = facturas.Where(f => f.UsuarioCajero.Equals(cajero.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var b = buscar.Trim().ToLower();
                facturas = facturas.Where(f =>
                    f.Numero.ToLower().Contains(b) ||
                    (f.Cliente != null && f.Cliente.Nombre.ToLower().Contains(b)) ||
                    (f.Cliente != null && f.Cliente.Documento != null && f.Cliente.Documento.ToLower().Contains(b)) ||
                    (!string.IsNullOrEmpty(f.NombreClienteLibre) && f.NombreClienteLibre.ToLower().Contains(b))
                ).ToList();
            }

            // Cálculo de KPIs contables del periodo seleccionado
            var emitidas = facturas.Where(f => f.Estado == "Emitida").ToList();
            ViewBag.TotalFacturado  = emitidas.Sum(f => f.Total);
            ViewBag.TotalIva        = emitidas.Sum(f => f.Subtotal * f.ImpuestoPorcentaje / 100m);
            ViewBag.TotalPropinas   = emitidas.Sum(f => f.Propina);
            ViewBag.TotalDomicilios = emitidas.Sum(f => f.CostoEnvio);
            ViewBag.TotalFacturas   = facturas.Count;
            ViewBag.TicketPromedio  = emitidas.Any() ? emitidas.Average(f => f.Total) : 0m;

            ViewBag.Desde  = desde?.ToString("yyyy-MM-dd");
            ViewBag.Hasta  = hasta?.ToString("yyyy-MM-dd");
            ViewBag.Buscar = buscar;
            ViewBag.CajeroSeleccionado = cajero;
            ViewBag.ListaCajeros = _facturaService.ObtenerTodas().Select(f => f.UsuarioCajero).Distinct().ToList();

            return View(facturas);
        }

        // ── EXPORTAR REPORTE RESUMIDO A EXCEL (CSV UTF-8) ───────────────────
        [HttpGet]
        public IActionResult ExportarCsv(DateTime? desde = null, DateTime? hasta = null, string? buscar = null, string? cajero = null)
        {
            var facturas = _facturaService.ObtenerTodas();

            if (desde.HasValue)
            {
                var fDesde = desde.Value.Date;
                facturas = facturas.Where(f => f.FechaCreacion >= fDesde).ToList();
            }
            if (hasta.HasValue)
            {
                var fHasta = hasta.Value.Date.AddDays(1).AddTicks(-1);
                facturas = facturas.Where(f => f.FechaCreacion <= fHasta).ToList();
            }
            if (!string.IsNullOrWhiteSpace(cajero))
            {
                facturas = facturas.Where(f => f.UsuarioCajero.Equals(cajero.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
            }
            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var b = buscar.Trim().ToLower();
                facturas = facturas.Where(f =>
                    f.Numero.ToLower().Contains(b) ||
                    (f.Cliente != null && f.Cliente.Nombre.ToLower().Contains(b)) ||
                    (!string.IsNullOrEmpty(f.NombreClienteLibre) && f.NombreClienteLibre.ToLower().Contains(b))
                ).ToList();
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("sep=;");
            sb.AppendLine("N° Factura;Fecha;Hora;Cliente;Documento / NIT;Cajero;Estado;Subtotal COP;Servicio Domicilio COP;Cupón;Descuento COP;IVA COP;Propina COP;Total Factura COP");

            foreach (var f in facturas)
            {
                string cliente = f.Cliente?.Nombre ?? f.NombreClienteLibre ?? "Consumidor Final";
                string doc = f.Cliente?.Documento ?? "N/A";
                decimal iva = f.Subtotal * f.ImpuestoPorcentaje / 100m;
                string cupon = string.IsNullOrWhiteSpace(f.CodigoCupon) ? "Ninguno" : f.CodigoCupon;
                sb.AppendLine($"\"{f.Numero}\";\"{f.FechaCreacion:dd/MM/yyyy}\";\"{f.FechaCreacion:HH:mm}\";\"{cliente}\";\"{doc}\";\"{f.UsuarioCajero}\";\"{f.Estado}\";{f.Subtotal:F0};{f.CostoEnvio:F0};\"{cupon}\";{f.Descuento:F0};{iva:F0};{f.Propina:F0};{f.Total:F0}");
            }

            byte[] bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            string nombreArchivo = $"Ventas_Saborator_{(desde.HasValue ? desde.Value.ToString("yyyyMMdd") : "Inicio")}_{(hasta.HasValue ? hasta.Value.ToString("yyyyMMdd") : DateTime.Now.ToString("yyyyMMdd"))}.csv";
            return File(bytes, "text/csv; charset=utf-8", nombreArchivo);
        }

        // ── EXPORTAR REPORTE DETALLADO POR PRODUCTO (CSV UTF-8) ─────────────
        [HttpGet]
        public IActionResult ExportarDetalladoCsv(DateTime? desde = null, DateTime? hasta = null, string? buscar = null, string? cajero = null)
        {
            var facturas = _facturaService.ObtenerTodas();

            if (desde.HasValue)
            {
                var fDesde = desde.Value.Date;
                facturas = facturas.Where(f => f.FechaCreacion >= fDesde).ToList();
            }
            if (hasta.HasValue)
            {
                var fHasta = hasta.Value.Date.AddDays(1).AddTicks(-1);
                facturas = facturas.Where(f => f.FechaCreacion <= fHasta).ToList();
            }
            if (!string.IsNullOrWhiteSpace(cajero))
            {
                facturas = facturas.Where(f => f.UsuarioCajero.Equals(cajero.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
            }
            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var b = buscar.Trim().ToLower();
                facturas = facturas.Where(f =>
                    f.Numero.ToLower().Contains(b) ||
                    (f.Cliente != null && f.Cliente.Nombre.ToLower().Contains(b)) ||
                    (!string.IsNullOrEmpty(f.NombreClienteLibre) && f.NombreClienteLibre.ToLower().Contains(b))
                ).ToList();
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("sep=;");
            sb.AppendLine("N° Factura;Fecha;Hora;Cajero;Cliente;Producto / Plato;Cantidad;Precio Unitario COP;Subtotal Línea COP;Total Factura COP");

            foreach (var f in facturas)
            {
                string cliente = f.Cliente?.Nombre ?? f.NombreClienteLibre ?? "Consumidor Final";
                foreach (var l in f.Lineas)
                {
                    sb.AppendLine($"\"{f.Numero}\";\"{f.FechaCreacion:dd/MM/yyyy}\";\"{f.FechaCreacion:HH:mm}\";\"{f.UsuarioCajero}\";\"{cliente}\";\"{l.NombreProducto}\";{l.Cantidad};{l.PrecioUnitario:F0};{(l.PrecioUnitario * l.Cantidad):F0};{f.Total:F0}");
                }
            }

            byte[] bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            string nombreArchivo = $"Platos_Vendidos_Saborator_{(desde.HasValue ? desde.Value.ToString("yyyyMMdd") : "Inicio")}_{(hasta.HasValue ? hasta.Value.ToString("yyyyMMdd") : DateTime.Now.ToString("yyyyMMdd"))}.csv";
            return File(bytes, "text/csv; charset=utf-8", nombreArchivo);
        }

        // ── CREATE (factura manual, sin pedido) ────────────────────────────
        public IActionResult Create()
        {
            ViewBag.Productos = _productoService.ObtenerActivos();
            ViewBag.Clientes  = _clienteRepo.ObtenerTodos().Where(c => c.Activo).ToList();
            return View();
        }

        [HttpPost]
        public IActionResult Create(
            [FromForm] int?    clienteId,
            [FromForm] string? nombreClienteLibre,
            [FromForm] decimal impuestoPorcentaje,
            [FromForm] bool    incluirPropina,
            [FromForm] int[]   productoIds,
            [FromForm] int[]   cantidades)
        {
            // Validar IVA con rango [0, 100]
            if (impuestoPorcentaje < 0 || impuestoPorcentaje > 100)
            {
                ModelState.AddModelError("impuestoPorcentaje", "El porcentaje de IVA debe estar entre 0 y 100.");
            }

            if (productoIds == null || productoIds.Length == 0 ||
                cantidades  == null || cantidades.Length  == 0 ||
                productoIds.Length != cantidades.Length)
            {
                ModelState.AddModelError("productoIds", "Debes agregar al menos un producto.");
            }
            else
            {
                for (int i = 0; i < cantidades.Length; i++)
                {
                    if (cantidades[i] <= 0)
                    {
                        ModelState.AddModelError("cantidades", "La cantidad debe ser mayor a 0.");
                        break;
                    }
                    if (cantidades[i] > 999)
                    {
                        ModelState.AddModelError("cantidades", "La cantidad máxima permitida por producto es 999.");
                        break;
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                var errorMsg = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                               ?? "Datos de facturación inválidos.";
                TempData["Error"] = errorMsg;
                ViewBag.Clientes  = _clienteRepo.ObtenerTodos().Where(c => c.Activo).ToList();
                return View();
            }

            var lineas = productoIds!
                .Zip(cantidades!, (pId, cant) => (pId, cant))
                .Where(t => t.cant > 0)
                .ToList();

            // Calcular subtotal provisional para la propina (10%)
            decimal subtotalPrev = 0;
            foreach (var (pId, cant) in lineas)
            {
                var prod = _productoService.ObtenerPorId(pId);
                if (prod != null) subtotalPrev += prod.Precio * cant;
            }
            decimal propina = incluirPropina ? Math.Round(subtotalPrev * 0.10m, 2) : 0;

            var factura = new Factura
            {
                ImpuestoPorcentaje = impuestoPorcentaje,
                Propina            = propina
            };
            var (ok, mensaje, facturaCreada) = _facturaService.Emitir(
                factura, lineas, UsuarioActual(), clienteId, nombreClienteLibre);

            if (!ok) { TempData["Error"] = mensaje; return RedirectToAction("Create"); }

            TempData["Mensaje"] = mensaje;
            return RedirectToAction("Detalle", new { id = facturaCreada!.Id });
        }

        // ── FACTURAR PEDIDO (cajero convierte pedido "Listo" a factura) ────
        public IActionResult FacturarPedido(int id)
        {
            var pedido = _pedidoService.ObtenerPorId(id);
            if (pedido == null) return NotFound();
            if (pedido.Estado == "Facturado")
            {
                TempData["Error"] = "Este pedido ya fue facturado.";
                return RedirectToAction("Index", "Pedido");
            }
            if (pedido.Estado == "Cancelado")
            {
                TempData["Error"] = "No se puede facturar un pedido cancelado.";
                return RedirectToAction("Index", "Pedido");
            }

            // Construir líneas desde el pedido
            var lineas = pedido.Lineas
                .Select(l => (l.ProductoId, l.Cantidad))
                .ToList();

            var factura = new Factura 
            { 
                ImpuestoPorcentaje = 0,
                CostoEnvio = pedido.TipoEntrega == "Domicilio" ? pedido.CostoEnvio : 0,
                Descuento = pedido.Descuento,
                CodigoCupon = pedido.CodigoCupon
            };
            var (ok, mensaje, facturaCreada) = _facturaService.Emitir(
                factura, lineas, UsuarioActual(),
                clienteId: null,
                nombreClienteLibre: pedido.NombreCliente ?? pedido.Mesa,
                descontarStock: false);

            if (!ok) { TempData["Error"] = mensaje; return RedirectToAction("Index", "Pedido"); }

            // Marcar pedido como facturado y liberar la mesa
            _pedidoService.MarcarFacturado(pedido.Id, facturaCreada!.Id);
            if (int.TryParse(pedido.Mesa, out int numMesa))
                _mesaService.LiberarMesa(numMesa);

            _auditoriaService.Registrar(
                UsuarioActual(),
                "Cajero",
                "EMISION_FACTURA",
                "Facturas",
                $"Factura {facturaCreada.Numero} emitida para pedido {pedido.Numero} por valor de ${facturaCreada.Total:N0} COP.",
                "Factura",
                facturaCreada.Id,
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Mensaje"] = $"Pedido {pedido.Numero} cobrado. {mensaje} Mesa {pedido.Mesa} liberada.";
            return RedirectToAction("Detalle", new { id = facturaCreada.Id });
        }

        // ── DETALLE ───────────────────────────────────────────────────────
        public IActionResult Detalle(int id)
        {
            var factura = _facturaService.ObtenerPorId(id);
            return factura == null ? NotFound() : View(factura);
        }

        // ── ANULAR: deshabilitado por política de negocio ──────────────────
        [HttpPost]
        public IActionResult Anular(int id)
        {
            _auditoriaService.Registrar(
                UsuarioActual(),
                "Cajero",
                "INTENTO_ANULACION_FACTURA",
                "Facturas",
                $"Intento de anulación bloqueado en Factura ID #{id} por estricta política fiscal.",
                "Factura",
                id,
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Error"] = "El historial de ventas no puede modificarse.";
            return RedirectToAction("Index");
        }
    }
}
