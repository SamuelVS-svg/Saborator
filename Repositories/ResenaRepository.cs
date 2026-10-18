using ControlClientesRestaurante.Data;
using ControlClientesRestaurante.Models;

namespace ControlClientesRestaurante.Repositories
{
    public class ResenaRepository : IResenaRepository
    {
        private readonly ApplicationDbContext _context;

        public ResenaRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Resena> ObtenerTodas() =>
            _context.Resenas
                .OrderByDescending(r => r.FechaCreacion)
                .ToList();

        public List<Resena> ObtenerAprobadas() =>
            _context.Resenas
                .Where(r => r.Aprobada)
                .OrderByDescending(r => r.FechaCreacion)
                .ToList();

        public Resena? ObtenerPorId(int id) =>
            _context.Resenas.FirstOrDefault(r => r.Id == id);

        public void Crear(Resena resena) =>
            _context.Resenas.Add(resena);

        public void Actualizar(Resena resena) =>
            _context.Resenas.Update(resena);

        public void Eliminar(int id)
        {
            var r = ObtenerPorId(id);
            if (r != null) _context.Resenas.Remove(r);
        }

        public void GuardarCambios() =>
            _context.SaveChanges();
    }
}
