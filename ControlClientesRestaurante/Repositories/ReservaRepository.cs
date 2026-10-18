using ControlClientesRestaurante.Data;
using ControlClientesRestaurante.Models;
using Microsoft.EntityFrameworkCore;

namespace ControlClientesRestaurante.Repositories
{
    public class ReservaRepository : IReservaRepository
    {
        private readonly ApplicationDbContext _context;

        public ReservaRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Reserva> ObtenerTodas() =>
            _context.Reservas
                .OrderByDescending(r => r.Fecha)
                .ThenByDescending(r => r.Hora)
                .ToList();

        public List<Reserva> ObtenerPorFecha(DateTime fecha) =>
            _context.Reservas
                .Where(r => r.Fecha.Date == fecha.Date && r.Estado != "Cancelada")
                .ToList();

        public List<Reserva> ObtenerPorUsuarioId(int usuarioId) =>
            _context.Reservas
                .Where(r => r.UsuarioId == usuarioId)
                .OrderByDescending(r => r.Fecha)
                .ToList();

        public List<Reserva> ObtenerPorNombreCliente(string nombreCliente) =>
            _context.Reservas
                .Where(r => r.NombreCliente.ToLower() == nombreCliente.ToLower())
                .OrderByDescending(r => r.Fecha)
                .ToList();

        public Reserva? ObtenerPorId(int id) =>
            _context.Reservas
                .Include(r => r.Cliente)
                .Include(r => r.Usuario)
                .FirstOrDefault(r => r.Id == id);

        public Reserva? ObtenerPorCodigo(string codigo) =>
            _context.Reservas
                .Include(r => r.Cliente)
                .Include(r => r.Usuario)
                .FirstOrDefault(r => r.Codigo == codigo);

        public void Crear(Reserva reserva) =>
            _context.Reservas.Add(reserva);

        public void Actualizar(Reserva reserva) =>
            _context.Reservas.Update(reserva);

        public void GuardarCambios() =>
            _context.SaveChanges();
    }
}
