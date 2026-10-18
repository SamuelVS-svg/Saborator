using ControlClientesRestaurante.Models;

namespace ControlClientesRestaurante.Repositories
{
    public interface IPedidoRepository
    {
        List<Pedido> ObtenerTodos();
        List<Pedido> ObtenerPorMesero(string usuario);
        List<Pedido> ObtenerPendientes();
        List<Pedido> ObtenerDomicilios();
        List<Pedido> ObtenerParaCocina();
        List<Pedido> ObtenerListosCocina();
        Pedido? ObtenerPorId(int id);
        Pedido? ObtenerPorNumero(string numero);
        List<Pedido> ObtenerPorTelefono(string telefono);
        string GenerarNumero();
        void Agregar(Pedido pedido);
        void Actualizar(Pedido pedido);
        void GuardarCambios();
    }
}
