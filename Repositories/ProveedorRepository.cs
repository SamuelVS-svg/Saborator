using ControlClientesRestaurante.Data;
using ControlClientesRestaurante.Models;

namespace ControlClientesRestaurante.Repositories
{
    public class ProveedorRepository : IProveedorRepository
    {
        private readonly ApplicationDbContext _context;
        public ProveedorRepository(ApplicationDbContext context) => _context = context;

        public List<Proveedor> ObtenerTodos() =>
            _context.Proveedores.OrderBy(p => p.Nombre).ToList();

        public List<Proveedor> Buscar(string texto) =>
            _context.Proveedores.Where(p =>
                p.Nombre.Contains(texto) ||
                (p.Contacto != null && p.Contacto.Contains(texto)) ||
                (p.Nit != null && p.Nit.Contains(texto)) ||
                (p.Categoria != null && p.Categoria.Contains(texto)))
            .OrderBy(p => p.Nombre).ToList();

        public Proveedor? ObtenerPorId(int id) => _context.Proveedores.Find(id);

        public bool ExisteNit(string nit, int idExcluir = 0) =>
            _context.Proveedores.Any(p =>
                p.Nit != null && p.Nit == nit && p.Id != idExcluir);

        public void Agregar(Proveedor proveedor) => _context.Proveedores.Add(proveedor);
        public void Actualizar(Proveedor proveedor) => _context.Proveedores.Update(proveedor);
        public void GuardarCambios() => _context.SaveChanges();
    }
}
