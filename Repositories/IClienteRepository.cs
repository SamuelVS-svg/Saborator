using ControlClientesRestaurante.Models;

namespace ControlClientesRestaurante.Repositories
{
    public interface IClienteRepository
    {
        List<Cliente> ObtenerTodos();
        List<Cliente> Buscar(string texto);
        Cliente? ObtenerPorId(int id);
        Cliente? ObtenerPorCodigo(string codigo);
        bool ExisteDocumento(string documento, int idExcluir = 0);
        void Agregar(Cliente cliente);
        void Actualizar(Cliente cliente);
        void GuardarCambios();
    }
}