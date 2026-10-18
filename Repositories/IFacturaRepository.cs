using ControlClientesRestaurante.Models;

namespace ControlClientesRestaurante.Repositories
{
    public interface IFacturaRepository
    {
        List<Factura> ObtenerTodas();
        Factura? ObtenerPorId(int id);
        string GenerarNumero();
        void Agregar(Factura factura);
        void Actualizar(Factura factura);
        void GuardarCambios();
    }
}
