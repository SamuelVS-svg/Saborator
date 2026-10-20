using ControlClientesRestaurante.Models;

namespace ControlClientesRestaurante.Repositories
{
    public interface IUsuarioRepository
    {
        Usuario? ObtenerPorNombre(string nombreUsuario);
        Usuario? ObtenerPorTokenVerificacion(string token);
        Usuario? ObtenerPorCorreo(string correo);
        List<Usuario> ObtenerActivosPorRol(string nombreRol);
        bool ExisteNombre(string nombreUsuario);
        bool ExisteCorreo(string correo);
        void Agregar(Usuario usuario);
        void Actualizar(Usuario usuario);
        void GuardarCambios();
    }
}
