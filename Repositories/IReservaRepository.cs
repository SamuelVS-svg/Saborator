using ControlClientesRestaurante.Models;

namespace ControlClientesRestaurante.Repositories
{
    public interface IReservaRepository
    {
        List<Reserva> ObtenerTodas();
        List<Reserva> ObtenerPorFecha(DateTime fecha);
        List<Reserva> ObtenerPorUsuarioId(int usuarioId);
        List<Reserva> ObtenerPorNombreCliente(string nombreCliente);
        Reserva? ObtenerPorId(int id);
        Reserva? ObtenerPorCodigo(string codigo);
        void Crear(Reserva reserva);
        void Actualizar(Reserva reserva);
        void GuardarCambios();
    }
}
