using ControlClientesRestaurante.Data;
using ControlClientesRestaurante.Models;
using Microsoft.EntityFrameworkCore;

namespace ControlClientesRestaurante.Repositories
{
    public class FacturaRepository : IFacturaRepository
    {
        private readonly ApplicationDbContext _context;

        public FacturaRepository(ApplicationDbContext context) => _context = context;

        public List<Factura> ObtenerTodas() =>
            _context.Facturas
                .Include(f => f.Cliente)
                .Include(f => f.Lineas)
                    .ThenInclude(l => l.Producto)
                .OrderByDescending(f => f.FechaCreacion)
                .ToList();

        public Factura? ObtenerPorId(int id) =>
            _context.Facturas
                .Include(f => f.Cliente)
                .Include(f => f.Lineas)
                    .ThenInclude(l => l.Producto)
                .FirstOrDefault(f => f.Id == id);

        public string GenerarNumero()
        {
            var ultimo = _context.Facturas
                .OrderByDescending(f => f.Id)
                .Select(f => f.Numero)
                .FirstOrDefault();

            if (ultimo == null) return "FAC-00001";

            var partes = ultimo.Split('-');
            if (partes.Length == 2 && int.TryParse(partes[1], out int num))
                return $"FAC-{(num + 1):D5}";

            return $"FAC-{(_context.Facturas.Count() + 1):D5}";
        }

        public void Agregar(Factura factura) => _context.Facturas.Add(factura);

        public void Actualizar(Factura factura) => _context.Facturas.Update(factura);

        public void GuardarCambios() => _context.SaveChanges();
    }
}
