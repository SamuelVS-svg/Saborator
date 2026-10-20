using ControlClientesRestaurante.Models;
using ControlClientesRestaurante.Repositories;

namespace ControlClientesRestaurante.Services
{
    public class PedidoService
    {
        private readonly IPedidoRepository _repo;
        private readonly IProductoRepository _prodRepo;
        private readonly IMesaRepository? _mesaRepo;

        private static readonly Dictionary<string, HashSet<string>> TransicionesPermitidas = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Pendiente"]       = new(StringComparer.OrdinalIgnoreCase) { "EnPreparacion", "En Preparación", "Listo", "Cancelado" },
            ["Abierto"]         = new(StringComparer.OrdinalIgnoreCase) { "EnPreparacion", "En Preparación", "Listo", "Cancelado" },
            ["EnPreparacion"]   = new(StringComparer.OrdinalIgnoreCase) { "Pendiente", "Abierto", "Listo", "Cancelado" },
            ["En Preparación"]  = new(StringComparer.OrdinalIgnoreCase) { "Pendiente", "Abierto", "Listo", "Cancelado" },
            ["Listo"]           = new(StringComparer.OrdinalIgnoreCase) { "EnPreparacion", "En Preparación", "EnCamino", "Entregado", "Cancelado" },
            ["EnCamino"]        = new(StringComparer.OrdinalIgnoreCase) { "Listo", "Entregado", "Cancelado" },
            ["Entregado"]       = new(StringComparer.OrdinalIgnoreCase) { "Cancelado" }
        };

        public PedidoService(IPedidoRepository repo, IProductoRepository prodRepo, IMesaRepository? mesaRepo = null)
        {
            _repo     = repo;
            _prodRepo = prodRepo;
            _mesaRepo = mesaRepo;
        }

        public List<Pedido> ObtenerTodos()                      => _repo.ObtenerTodos();
        public List<Pedido> ObtenerPorMesero(string u)          => _repo.ObtenerPorMesero(u);
        public List<Pedido> ObtenerPendientes()                 => _repo.ObtenerPendientes();
        public List<Pedido> ObtenerDomicilios()                 => _repo.ObtenerDomicilios();
        public List<Pedido> ObtenerParaCocina()                 => _repo.ObtenerParaCocina();
        public List<Pedido> ObtenerListosCocina()               => _repo.ObtenerListosCocina();
        public Pedido? ObtenerPorId(int id)                     => _repo.ObtenerPorId(id);
        public Pedido? ObtenerPorNumero(string numero)          => _repo.ObtenerPorNumero(numero);
        public List<Pedido> ObtenerPorTelefono(string tel)      => _repo.ObtenerPorTelefono(tel);

        /// <summary>Crea un nuevo pedido con sus líneas (en mesa, para llevar o domicilio estilo KFC).</summary>
        public (bool ok, string mensaje, Pedido? pedido) Crear(
            string mesa, string? nombreCliente, string usuarioMesero,
            string? notas, List<(int productoId, int cantidad, string? notaLinea)> lineas,
            string tipoEntrega = "EnMesa",
            string? direccion = null,
            string? barrio = null,
            string? telefono = null,
            string metodoPago = "Efectivo",
            decimal costoEnvio = 0,
            decimal? pagaCon = null,
            decimal descuento = 0,
            string? codigoCupon = null)
        {
            if (lineas == null || lineas.Count == 0)
                return (false, "El pedido debe tener al menos un producto.", null);

            var lineasPedido = new List<LineaPedido>();
            decimal subtotal = 0;

            // 1. Agrupar cantidades por producto para evitar sobreventa por líneas repetidas
            var requerimientos = lineas
                .Where(l => l.cantidad > 0)
                .GroupBy(l => l.productoId)
                .Select(g => new { ProductoId = g.Key, CantidadTotal = g.Sum(x => x.cantidad) })
                .ToList();

            if (requerimientos.Count == 0)
                return (false, "El pedido debe tener al menos un producto con cantidad válida.", null);

            var prodsDict = new Dictionary<int, Producto>();
            foreach (var req in requerimientos)
            {
                var prod = _prodRepo.ObtenerPorId(req.ProductoId);
                if (prod == null || !prod.Activo)
                    return (false, $"El producto con ID {req.ProductoId} no existe o no se encuentra activo.", null);

                if (prod.Stock < req.CantidadTotal)
                    return (false, $"Stock insuficiente para '{prod.Nombre}'. Disponible actualmente: {prod.Stock} (solicitado: {req.CantidadTotal}).", null);

                prodsDict[req.ProductoId] = prod;
            }

            // 2. Descontar inmediatamente el stock en el inventario al momento de crear y comprometer el pedido
            foreach (var req in requerimientos)
            {
                var prod = prodsDict[req.ProductoId];
                prod.Stock -= req.CantidadTotal;
                _prodRepo.Actualizar(prod);
            }

            foreach (var (productoId, cantidad, notaLinea) in lineas.Where(l => l.cantidad > 0))
            {
                var prod = prodsDict[productoId];

                lineasPedido.Add(new LineaPedido
                {
                    ProductoId     = prod.Id,
                    NombreProducto = prod.Nombre,
                    PrecioUnitario = prod.Precio,
                    Cantidad       = cantidad,
                    Notas          = notaLinea
                });
                subtotal += prod.Precio * cantidad;
            }

            decimal total = Math.Max(0, subtotal - descuento) + costoEnvio;

            var pedido = new Pedido
            {
                Numero        = _repo.GenerarNumero(),
                Mesa          = string.IsNullOrWhiteSpace(mesa) ? (tipoEntrega == "Domicilio" ? "Domicilio" : "Para Llevar") : mesa,
                NombreCliente = nombreCliente,
                UsuarioMesero = usuarioMesero,
                Notas         = notas,
                Estado        = "Pendiente",
                Total         = total,
                Descuento     = descuento,
                CodigoCupon   = codigoCupon,
                TipoEntrega   = tipoEntrega,
                Direccion     = direccion,
                Barrio        = barrio,
                Telefono      = telefono,
                MetodoPago    = metodoPago,
                CostoEnvio    = costoEnvio,
                PagaCon       = pagaCon,
                Lineas        = lineasPedido
            };

            _repo.Agregar(pedido);
            _repo.GuardarCambios();
            _prodRepo.GuardarCambios();
            return (true, $"Pedido {pedido.Numero} creado correctamente.", pedido);
        }

        /// <summary>Cambia el estado de un pedido aplicando máquina de estados y reglas de negocio.</summary>
        public (bool ok, string mensaje) CambiarEstado(int pedidoId, string nuevoEstado)
        {
            // 1. Regla: Si el nuevo estado es "Facturado", bloquear la operación y lanzar excepción
            if (string.Equals(nuevoEstado, "Facturado", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("A 'Facturado' solo se puede pasar a través del flujo de emisión de factura.");
            }

            var pedido = _repo.ObtenerPorId(pedidoId);
            if (pedido == null) return (false, "Pedido no encontrado.");

            // Si el pedido ya tiene el mismo estado
            if (string.Equals(pedido.Estado, nuevoEstado, StringComparison.OrdinalIgnoreCase))
                return (true, $"El pedido ya se encuentra en estado '{nuevoEstado}'.");

            // 2. Regla: Si el nuevo estado es "Cancelado", verificar el estado actual antes de hacer cualquier cambio
            if (string.Equals(nuevoEstado, "Cancelado", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(pedido.Estado, "Facturado", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pedido.Estado, "Cancelado", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"No se puede cancelar un pedido que ya está '{pedido.Estado}'.");
                }

                // 3. Regla: Al cancelar, condicionar la liberación de la mesa (Mesa.Estado = "Libre")
                // Solo debe liberarse si el pedido estaba en un estado activo (ej. "Abierto" o "En Preparación")
                var estadosActivos = new[] { "Abierto", "Pendiente", "EnPreparacion", "En Preparación", "En Preparacion" };
                if (estadosActivos.Contains(pedido.Estado, StringComparer.OrdinalIgnoreCase))
                {
                    LiberarMesaAsociada(pedido);
                }

                // 4. Regla: Al cancelar el pedido, restituir el stock de cada producto reservado
                if (pedido.Lineas != null && pedido.Lineas.Count > 0)
                {
                    var productosParaRestituir = pedido.Lineas
                        .GroupBy(l => l.ProductoId)
                        .Select(g => new { ProductoId = g.Key, CantidadTotal = g.Sum(x => x.Cantidad) });

                    foreach (var item in productosParaRestituir)
                    {
                        var prod = _prodRepo.ObtenerPorId(item.ProductoId);
                        if (prod != null)
                        {
                            prod.Stock += item.CantidadTotal;
                            _prodRepo.Actualizar(prod);
                        }
                    }
                    _prodRepo.GuardarCambios();
                }

                pedido.Estado = "Cancelado";
                _repo.Actualizar(pedido);
                _repo.GuardarCambios();
                return (true, "Pedido cancelado correctamente.");
            }

            // Si el pedido ya estaba en un estado terminal (Facturado o Cancelado), no se puede cambiar a ningún otro estado
            if (string.Equals(pedido.Estado, "Facturado", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(pedido.Estado, "Cancelado", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"No se puede modificar un pedido que ya está '{pedido.Estado}'.");
            }

            // Validar transiciones de estado permitidas
            if (!TransicionesPermitidas.TryGetValue(pedido.Estado, out var destinosValidos) ||
                !destinosValidos.Contains(nuevoEstado))
            {
                throw new InvalidOperationException($"Transición no permitida de '{pedido.Estado}' a '{nuevoEstado}'.");
            }

            // "En camino" solo tiene sentido para domicilios
            if (string.Equals(nuevoEstado, "EnCamino", StringComparison.OrdinalIgnoreCase) && pedido.TipoEntrega != "Domicilio")
            {
                throw new InvalidOperationException("Solo los pedidos a domicilio pueden pasar a 'En camino'.");
            }

            if (string.Equals(nuevoEstado, "EnCamino", StringComparison.OrdinalIgnoreCase))
                pedido.FechaDespacho ??= DateTime.Now;
            if (string.Equals(nuevoEstado, "Entregado", StringComparison.OrdinalIgnoreCase))
                pedido.FechaEntrega = DateTime.Now;

            pedido.Estado = nuevoEstado;
            _repo.Actualizar(pedido);
            _repo.GuardarCambios();
            return (true, $"Estado actualizado a: {nuevoEstado}");
        }

        private void LiberarMesaAsociada(Pedido pedido)
        {
            if (_mesaRepo == null) return;

            Mesa? mesa = null;

            if (int.TryParse(pedido.Mesa, out int numMesa))
            {
                mesa = _mesaRepo.ObtenerPorNumero(numMesa);
            }
            else if (!string.IsNullOrWhiteSpace(pedido.Mesa))
            {
                var digitos = new string(pedido.Mesa.Where(char.IsDigit).ToArray());
                if (int.TryParse(digitos, out int d))
                    mesa = _mesaRepo.ObtenerPorNumero(d);
            }

            if (mesa == null)
            {
                mesa = _mesaRepo.ObtenerTodas().FirstOrDefault(m => m.PedidoId == pedido.Id);
            }

            if (mesa != null)
            {
                mesa.Estado   = "Libre";
                mesa.PedidoId = null;
                _mesaRepo.Actualizar(mesa);
                _mesaRepo.GuardarCambios();
            }
        }

        /// <summary>Marca el pedido como Facturado y guarda el Id de factura.</summary>
        public void MarcarFacturado(int pedidoId, int facturaId)
        {
            var pedido = _repo.ObtenerPorId(pedidoId);
            if (pedido == null) return;
            pedido.Estado    = "Facturado";
            pedido.FacturaId = facturaId;
            _repo.Actualizar(pedido);
            _repo.GuardarCambios();
        }

        /// <summary>Actualiza el valor de domicilio asignado y recalcula el total.</summary>
        public (bool ok, string mensaje) ActualizarCostoEnvio(int pedidoId, decimal nuevoCostoEnvio)
        {
            var pedido = _repo.ObtenerPorId(pedidoId);
            if (pedido == null) return (false, "Pedido no encontrado.");
            if (pedido.Estado == "Facturado") return (false, "No se puede modificar un pedido ya facturado.");
            if (pedido.Estado == "Cancelado") return (false, "No se puede modificar un pedido cancelado.");
            if (pedido.TipoEntrega != "Domicilio") return (false, "Solo los pedidos a domicilio tienen tarifa de envío.");

            decimal subtotal = pedido.Lineas.Sum(l => l.PrecioUnitario * l.Cantidad);
            pedido.CostoEnvio = nuevoCostoEnvio >= 0 ? nuevoCostoEnvio : 0;
            pedido.Total = Math.Max(0, subtotal - pedido.Descuento) + pedido.CostoEnvio;

            _repo.Actualizar(pedido);
            _repo.GuardarCambios();
            return (true, $"Valor de domicilio actualizado a ${(pedido.CostoEnvio).ToString("N0", ControlClientesRestaurante.Models.Formato.Co)} COP");
        }

        // ══ REPARTO A DOMICILIO (ROL DOMICILIARIO) ═══════════════════════

        /// <summary>Domicilios listos en cocina que aún no tienen domiciliario asignado.</summary>
        public List<Pedido> ObtenerRepartosDisponibles() =>
            _repo.ObtenerDomicilios()
                .Where(p => p.TipoEntrega == "Domicilio" && p.Estado == "Listo" && string.IsNullOrEmpty(p.Repartidor))
                .OrderBy(p => p.FechaCreacion)
                .ToList();

        /// <summary>Domicilios asignados a un domiciliario (activos y entregados hoy).</summary>
        public List<Pedido> ObtenerRepartosDe(string repartidor) =>
            _repo.ObtenerDomicilios()
                .Where(p => p.TipoEntrega == "Domicilio" &&
                            string.Equals(p.Repartidor, repartidor, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.FechaCreacion)
                .ToList();

        /// <summary>El cajero/administrador asigna un domiciliario a un domicilio que aún no ha salido.</summary>
        public (bool ok, string mensaje) AsignarRepartidor(int pedidoId, string? repartidor)
        {
            var pedido = _repo.ObtenerPorId(pedidoId);
            if (pedido == null) return (false, "Pedido no encontrado.");
            if (pedido.TipoEntrega != "Domicilio") return (false, "Solo los pedidos a domicilio se asignan a un domiciliario.");

            var estadosAsignables = new[] { "Pendiente", "EnPreparacion", "Listo" };
            if (!estadosAsignables.Contains(pedido.Estado))
                return (false, $"No se puede reasignar un domicilio en estado '{pedido.Estado}'.");

            pedido.Repartidor = string.IsNullOrWhiteSpace(repartidor) ? null : repartidor.Trim();
            _repo.Actualizar(pedido);
            _repo.GuardarCambios();
            return (true, pedido.Repartidor == null
                ? $"Se quitó el domiciliario del pedido {pedido.Numero}."
                : $"Pedido {pedido.Numero} asignado a {pedido.Repartidor}.");
        }

        /// <summary>El domiciliario recoge el pedido listo y sale a entregarlo (Listo → EnCamino).</summary>
        public (bool ok, string mensaje) IniciarReparto(int pedidoId, string repartidor)
        {
            var pedido = _repo.ObtenerPorId(pedidoId);
            if (pedido == null) return (false, "Pedido no encontrado.");
            if (pedido.TipoEntrega != "Domicilio") return (false, "Este pedido no es a domicilio.");
            if (pedido.Estado != "Listo") return (false, $"El pedido {pedido.Numero} aún no está listo para salir (estado: {pedido.Estado}).");
            if (!string.IsNullOrEmpty(pedido.Repartidor) &&
                !string.Equals(pedido.Repartidor, repartidor, StringComparison.OrdinalIgnoreCase))
                return (false, $"El pedido {pedido.Numero} ya está asignado a {pedido.Repartidor}.");

            pedido.Repartidor    = repartidor;
            pedido.FechaDespacho = DateTime.Now;
            pedido.Estado        = "EnCamino";
            _repo.Actualizar(pedido);
            _repo.GuardarCambios();
            return (true, $"Pedido {pedido.Numero} en camino. ¡Buen viaje!");
        }

        /// <summary>Confirma la entrega (EnCamino → Entregado). Solo el domiciliario asignado o un supervisor.</summary>
        public (bool ok, string mensaje) ConfirmarEntrega(int pedidoId, string usuario, bool esSupervisor)
        {
            var pedido = _repo.ObtenerPorId(pedidoId);
            if (pedido == null) return (false, "Pedido no encontrado.");
            if (pedido.Estado != "EnCamino") return (false, $"El pedido {pedido.Numero} no está en camino (estado: {pedido.Estado}).");
            if (!esSupervisor && !string.Equals(pedido.Repartidor, usuario, StringComparison.OrdinalIgnoreCase))
                return (false, "Solo el domiciliario asignado puede confirmar esta entrega.");

            pedido.Estado       = "Entregado";
            pedido.FechaEntrega = DateTime.Now;
            _repo.Actualizar(pedido);
            _repo.GuardarCambios();
            return (true, $"Pedido {pedido.Numero} entregado correctamente.");
        }
    }
}
