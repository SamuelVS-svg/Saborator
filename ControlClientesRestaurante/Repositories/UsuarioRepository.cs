using ControlClientesRestaurante.Data;
using ControlClientesRestaurante.Models;
using Microsoft.EntityFrameworkCore;

namespace ControlClientesRestaurante.Repositories
{
    public class UsuarioRepository : IUsuarioRepository
    {
        private readonly ApplicationDbContext _context;

        public UsuarioRepository(ApplicationDbContext context) => _context = context;

        public Usuario? ObtenerPorNombre(string nombreUsuario) =>
            _context.Usuarios.Include(u => u.Rol)
                .FirstOrDefault(u => u.NombreUsuario.ToLower() == nombreUsuario.ToLower());

        public Usuario? ObtenerPorTokenVerificacion(string token) =>
            _context.Usuarios.Include(u => u.Rol)
                .FirstOrDefault(u => u.TokenVerificacionEmail == token);

        public Usuario? ObtenerPorCorreo(string correo) =>
            _context.Usuarios.Include(u => u.Rol)
                .FirstOrDefault(u => u.Correo != null && u.Correo.ToLower() == correo.Trim().ToLower());

        public List<Usuario> ObtenerActivosPorRol(string nombreRol) =>
            _context.Usuarios.Include(u => u.Rol)
                .Where(u => u.Estado && u.Rol != null && u.Rol.NombreRol == nombreRol)
                .OrderBy(u => u.NombreUsuario)
                .ToList();

        public bool ExisteNombre(string nombreUsuario) =>
            _context.Usuarios.Any(u => u.NombreUsuario.ToLower() == nombreUsuario.ToLower());

        public bool ExisteCorreo(string correo) =>
            _context.Usuarios.Any(u => u.Correo != null && u.Correo.ToLower() == correo.Trim().ToLower());

        public void Agregar(Usuario usuario) => _context.Usuarios.Add(usuario);

        public void Actualizar(Usuario usuario) => _context.Usuarios.Update(usuario);

        public void GuardarCambios() => _context.SaveChanges();
    }
}
