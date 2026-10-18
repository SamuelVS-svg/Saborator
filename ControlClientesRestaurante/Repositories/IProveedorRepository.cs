using ControlClientesRestaurante.Models;

namespace ControlClientesRestaurante.Repositories
{
    public interface IProveedorRepository
    {
        List<Proveedor> ObtenerTodos();
        List<Proveedor> Buscar(string texto);
        Proveedor? ObtenerPorId(int id);
        bool ExisteNit(string nit, int idExcluir = 0);
        void Agregar(Proveedor proveedor);
        void Actualizar(Proveedor proveedor);
        void GuardarCambios();
    }
}
