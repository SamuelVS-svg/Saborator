using ControlClientesRestaurante.Models;

namespace ControlClientesRestaurante.Repositories
{
    public interface IMesaRepository
    {
        List<Mesa> ObtenerTodas();
        Mesa? ObtenerPorNumero(int numero);
        Mesa? ObtenerPorId(int id);
        void Actualizar(Mesa mesa);
        void GuardarCambios();
    }
}
