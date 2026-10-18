using ControlClientesRestaurante.Data;
using ControlClientesRestaurante.Models;

namespace ControlClientesRestaurante.Repositories
{
    public class MesaRepository : IMesaRepository
    {
        private readonly ApplicationDbContext _context;
        public MesaRepository(ApplicationDbContext context) => _context = context;

        public List<Mesa> ObtenerTodas() =>
            _context.Mesas.OrderBy(m => m.Numero).ToList();

        public Mesa? ObtenerPorNumero(int numero) =>
            _context.Mesas.FirstOrDefault(m => m.Numero == numero);

        public Mesa? ObtenerPorId(int id) =>
            _context.Mesas.Find(id);

        public void Actualizar(Mesa mesa) => _context.Mesas.Update(mesa);

        public void GuardarCambios() => _context.SaveChanges();
    }
}
