using ControlClientesRestaurante.Data;
using ControlClientesRestaurante.Models;
using ControlClientesRestaurante.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ControlClientesRestaurante.Services
{
    public class FacturaService
    {
        private readonly IFacturaRepository   _repo;
        private readonly IProductoRepository  _productoRepo;
        private readonly ApplicationDbContext _context;

        public FacturaService(
            IFacturaRepository repo,
            IProductoRepository productoRepo,
            ApplicationDbContext context)
        {
            _repo         = repo;
            _productoRepo = productoRepo;
            _context      = context;
        }

        public List<Factura> ObtenerTodas() => _repo.ObtenerTodas();

        public Factura? ObtenerPorId(int id) => _repo.ObtenerPorId(id);

        /// <summary>
        /// Emite una factura en una sola transacción atómica:
        ///  1. Valida y descuenta el stock de cada producto si descontarStock es true (venta directa), sin descontar doble si proviene de un Pedido previo.
        ///  2. Agrega la factura y sus detalles.
        ///  3. Ejecuta un único GuardarCambios() al final para persistir todo atómicamente.
        /// </summary>
        public (bool ok, string mensaje, Factura? factura) Emitir(
            Factura factura,
            List<(int productoId, int cantidad)> lineas,
            string usuarioCajero,
            int? clienteId,
            string? nombreClienteLibre,
            bool descontarStock = true)
        {
            if (lineas == null || lineas.Count == 0)
                return (false, "La factura debe tener al menos un producto.", null);

            var lineasValidas = lineas.Where(l => l.cantidad > 0).ToList();
            if (lineasValidas.Count == 0)
                return (false, "La factura debe tener al menos un producto con cantidad mayor a cero.", null);

            using var tx = _context.Database.BeginTransaction();
            try
            {
                // 1. Agrupar las líneas por ProductoId y sumar las cantidades solicitadas (Bug 1.2 Sobreventa por líneas repetidas)
                var requerimientosPorProducto = lineasValidas
                    .GroupBy(l => l.productoId)
                    .Select(g => new
                    {
                        ProductoId    = g.Key,
                        CantidadTotal = g.Sum(x => x.cantidad)
                    })
                    .ToList();

                // 2. Validar productos y descontar stock si corresponde (descontarStock == true)
                var productosDict = new Dictionary<int, Producto>();

                foreach (var req in requerimientosPorProducto)
                {
                    var prod = _productoRepo.ObtenerPorId(req.ProductoId);
                    if (prod == null || !prod.Activo)
                    {
                        tx.Rollback();
                        return (false, $"El producto con ID {req.ProductoId} no existe o está inactivo.", null);
                    }

                    if (descontarStock)
                    {
                        if (prod.Stock < req.CantidadTotal)
                        {
                            tx.Rollback();
                            return (false, $"Stock insuficiente para '{prod.Nombre}'. Disponible actualmente: {prod.Stock}.", null);
                        }

                        // Descontar la cantidad total solicitada en el stock del producto
                        prod.Stock -= req.CantidadTotal;
                        _productoRepo.Actualizar(prod);
                    }

                    productosDict[req.ProductoId] = prod;
                }

                // 3. Asignar los precios y agregar las líneas a la factura
                decimal subtotal = 0;
                var lineasFactura = new List<LineaFactura>();

                foreach (var (productoId, cantidad) in lineasValidas)
                {
                    var prod = productosDict[productoId];
                    lineasFactura.Add(new LineaFactura
                    {
                        ProductoId     = prod.Id,
                        NombreProducto = prod.Nombre,
                        PrecioUnitario = prod.Precio,
                        Cantidad       = cantidad
                    });
                    subtotal += prod.Precio * cantidad;
                }

                var impuesto = factura.ImpuestoPorcentaje;
                var propina  = factura.Propina;

                factura.Numero             = _repo.GenerarNumero();
                factura.Subtotal           = subtotal;
                factura.Propina            = propina;
                factura.Total              = Math.Max(0, subtotal - factura.Descuento) + (subtotal * impuesto / 100m) + propina + factura.CostoEnvio;
                factura.Lineas             = lineasFactura;
                factura.UsuarioCajero      = usuarioCajero;
                factura.Estado             = "Emitida";
                factura.ClienteId          = clienteId == 0 ? null : clienteId;
                factura.NombreClienteLibre = nombreClienteLibre;

                // 4. Agregar factura y sus detalles
                _repo.Agregar(factura);

                // 5. Único GuardarCambios() al final de todo el método Emitir para garantizar atomicidad
                _repo.GuardarCambios();

                tx.Commit();
                return (true, $"Factura {factura.Numero} emitida correctamente.", factura);
            }
            catch (Exception ex)
            {
                tx.Rollback();
                return (false, $"Error al emitir factura: {ex.Message}", null);
            }
        }

        /// <summary>Anula la factura y devuelve el stock de cada línea.</summary>
        public (bool ok, string mensaje) Anular(int facturaId)
        {
            var factura = _repo.ObtenerPorId(facturaId);
            if (factura == null) return (false, "Factura no encontrada.");
            if (factura.Estado == "Anulada") return (false, "La factura ya estaba anulada.");

            foreach (var linea in factura.Lineas)
            {
                var prod = _productoRepo.ObtenerPorId(linea.ProductoId);
                if (prod != null) { prod.Stock += linea.Cantidad; _productoRepo.Actualizar(prod); }
            }
            _productoRepo.GuardarCambios();

            factura.Estado = "Anulada";
            _repo.Actualizar(factura);
            _repo.GuardarCambios();

            return (true, "Factura anulada y stock restaurado.");
        }
    }
}
