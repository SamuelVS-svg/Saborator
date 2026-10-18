using ControlClientesRestaurante.Models;

namespace ControlClientesRestaurante.Repositories
{
    public interface IProductoRepository
    {
        List<Producto> ObtenerTodos();
        List<Producto> ObtenerActivos();
        List<Producto> Buscar(string texto);
        Producto? ObtenerPorId(int id);
        bool ExisteNombre(string nombre, int idExcluir = 0);
        void Agregar(Producto producto);
        void Actualizar(Producto producto);
        void GuardarCambios();
    }
}
