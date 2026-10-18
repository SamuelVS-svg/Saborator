using ControlClientesRestaurante.Data;
using ControlClientesRestaurante.Models;
using Microsoft.EntityFrameworkCore;

namespace ControlClientesRestaurante.Repositories
{
    public class PedidoRepository : IPedidoRepository
    {
        private readonly ApplicationDbContext _context;
        public PedidoRepository(ApplicationDbContext context) => _context = context;

        private IQueryable<Pedido> ConLineas() =>
            _context.Pedidos
                .Include(p => p.Lineas).ThenInclude(l => l.Producto)
                .Include(p => p.Factura);

        public List<Pedido> ObtenerTodos() =>
            ConLineas().OrderByDescending(p => p.FechaCreacion).ToList();

        public List<Pedido> ObtenerPorMesero(string usuario) =>
            ConLineas().Where(p => p.UsuarioMesero == usuario)
                .OrderByDescending(p => p.FechaCreacion).ToList();

        public List<Pedido> ObtenerPendientes() =>
            ConLineas().Where(p => p.Estado == "Pendiente" || p.Estado == "EnPreparacion" || p.Estado == "EnCamino" || p.Estado == "Listo")
                .OrderBy(p => p.FechaCreacion).ToList();

        public List<Pedido> ObtenerDomicilios() =>
            ConLineas().Where(p => p.TipoEntrega == "Domicilio" || p.TipoEntrega == "ParaLlevar")
                .OrderByDescending(p => p.FechaCreacion).ToList();

        public List<Pedido> ObtenerParaCocina() =>
            ConLineas().Where(p => p.Estado == "Pendiente" || p.Estado == "EnPreparacion")
                .OrderBy(p => p.FechaCreacion).ToList();

        public List<Pedido> ObtenerListosCocina() =>
            ConLineas().Where(p => p.Estado == "Listo" || p.Estado == "EnCamino" || p.Estado == "Entregado")
                .OrderByDescending(p => p.FechaCreacion).Take(20).ToList();

        public Pedido? ObtenerPorId(int id) =>
            ConLineas().FirstOrDefault(p => p.Id == id);

        public Pedido? ObtenerPorNumero(string numero) =>
            ConLineas().FirstOrDefault(p => p.Numero.ToLower() == numero.ToLower().Trim());

        public List<Pedido> ObtenerPorTelefono(string telefono) =>
            ConLineas().Where(p => p.Telefono != null && p.Telefono.Contains(telefono.Trim()))
                .OrderByDescending(p => p.FechaCreacion).ToList();

        public string GenerarNumero()
        {
            var ultimo = _context.Pedidos
                .OrderByDescending(p => p.Id)
                .Select(p => p.Numero)
                .FirstOrDefault();
            if (ultimo == null) return "PED-00001";
            var partes = ultimo.Split('-');
            if (partes.Length == 2 && int.TryParse(partes[1], out int n))
                return $"PED-{(n + 1):D5}";
            return $"PED-{(_context.Pedidos.Count() + 1):D5}";
        }

        public void Agregar(Pedido pedido) => _context.Pedidos.Add(pedido);
        public void Actualizar(Pedido pedido) => _context.Pedidos.Update(pedido);
        public void GuardarCambios() => _context.SaveChanges();
    }
}
