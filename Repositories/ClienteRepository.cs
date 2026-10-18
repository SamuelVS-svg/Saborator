using ControlClientesRestaurante.Data;
using ControlClientesRestaurante.Models;
using Microsoft.EntityFrameworkCore;

namespace ControlClientesRestaurante.Repositories
{
    public class ClienteRepository : IClienteRepository
    {
        private readonly ApplicationDbContext _context;

        public ClienteRepository(ApplicationDbContext context) => _context = context;

        public List<Cliente> ObtenerTodos() =>
            _context.Clientes.OrderBy(c => c.Nombre).ToList();

        public List<Cliente> Buscar(string texto) =>
            _context.Clientes.Where(c =>
                c.Nombre.Contains(texto) ||
                c.Documento.Contains(texto) ||
                c.Telefono.Contains(texto)).ToList();

        public Cliente? ObtenerPorId(int id) => _context.Clientes.Find(id);

        public Cliente? ObtenerPorCodigo(string codigo) =>
            _context.Clientes.FirstOrDefault(c => c.Codigo == codigo);

        public bool ExisteDocumento(string documento, int idExcluir = 0) =>
            _context.Clientes.Any(c => c.Documento == documento && c.Id != idExcluir);

        public void Agregar(Cliente cliente) => _context.Clientes.Add(cliente);

        public void Actualizar(Cliente cliente) => _context.Clientes.Update(cliente);

        public void GuardarCambios() => _context.SaveChanges();
    }
}