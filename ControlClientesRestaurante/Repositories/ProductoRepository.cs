using ControlClientesRestaurante.Data;
using ControlClientesRestaurante.Models;
using Microsoft.EntityFrameworkCore;

namespace ControlClientesRestaurante.Repositories
{
    public class ProductoRepository : IProductoRepository
    {
        private readonly ApplicationDbContext _context;

        public ProductoRepository(ApplicationDbContext context) => _context = context;

        public List<Producto> ObtenerTodos() =>
            _context.Productos.OrderBy(p => p.Nombre).ToList();

        public List<Producto> ObtenerActivos() =>
            _context.Productos.Where(p => p.Activo).OrderBy(p => p.Nombre).ToList();

        public List<Producto> Buscar(string texto) =>
            _context.Productos.Where(p =>
                p.Nombre.Contains(texto) ||
                (p.Descripcion != null && p.Descripcion.Contains(texto)))
                .OrderBy(p => p.Nombre).ToList();

        public Producto? ObtenerPorId(int id) => _context.Productos.Find(id);

        public bool ExisteNombre(string nombre, int idExcluir = 0) =>
            _context.Productos.Any(p =>
                p.Nombre.ToLower() == nombre.ToLower() && p.Id != idExcluir);

        public void Agregar(Producto producto) => _context.Productos.Add(producto);

        public void Actualizar(Producto producto) => _context.Productos.Update(producto);

        public void GuardarCambios() => _context.SaveChanges();
    }
}
