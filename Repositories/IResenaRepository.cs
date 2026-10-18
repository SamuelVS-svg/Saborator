using ControlClientesRestaurante.Models;

namespace ControlClientesRestaurante.Repositories
{
    public interface IResenaRepository
    {
        List<Resena> ObtenerTodas();
        List<Resena> ObtenerAprobadas();
        Resena? ObtenerPorId(int id);
        void Crear(Resena resena);
        void Actualizar(Resena resena);
        void Eliminar(int id);
        void GuardarCambios();
    }
}
