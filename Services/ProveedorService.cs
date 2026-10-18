using ControlClientesRestaurante.Models;
using ControlClientesRestaurante.Repositories;

namespace ControlClientesRestaurante.Services
{
    public class ProveedorService
    {
        private readonly IProveedorRepository _repo;
        public ProveedorService(IProveedorRepository repo) => _repo = repo;

        public List<Proveedor> ObtenerTodos(string? filtro = null) =>
            string.IsNullOrWhiteSpace(filtro) ? _repo.ObtenerTodos() : _repo.Buscar(filtro);

        public Proveedor? ObtenerPorId(int id) => _repo.ObtenerPorId(id);

        public (bool ok, string mensaje) Registrar(Proveedor proveedor)
        {
            if (!string.IsNullOrWhiteSpace(proveedor.Nit) &&
                _repo.ExisteNit(proveedor.Nit))
                return (false, "Ya existe un proveedor con ese NIT.");

            proveedor.Nombre = AplicarPascalCase(proveedor.Nombre);
            _repo.Agregar(proveedor);
            _repo.GuardarCambios();
            return (true, "Proveedor registrado correctamente.");
        }

        public (bool ok, string mensaje) Actualizar(Proveedor proveedor)
        {
            if (!string.IsNullOrWhiteSpace(proveedor.Nit) &&
                _repo.ExisteNit(proveedor.Nit, proveedor.Id))
                return (false, "Ese NIT ya pertenece a otro proveedor.");

            var existente = _repo.ObtenerPorId(proveedor.Id);
            if (existente == null) return (false, "Proveedor no encontrado.");

            existente.Nombre    = AplicarPascalCase(proveedor.Nombre);
            existente.Contacto  = proveedor.Contacto;
            existente.Telefono  = proveedor.Telefono;
            existente.Correo    = proveedor.Correo;
            existente.Direccion = proveedor.Direccion;
            existente.Nit       = proveedor.Nit;
            existente.Categoria = proveedor.Categoria;
            existente.Notas     = proveedor.Notas;
            existente.Activo    = proveedor.Activo;

            _repo.Actualizar(existente);
            _repo.GuardarCambios();
            return (true, "Proveedor actualizado correctamente.");
        }

        public (bool ok, string mensaje) Desactivar(int id)
        {
            var proveedor = _repo.ObtenerPorId(id);
            if (proveedor == null) return (false, "Proveedor no encontrado.");
            proveedor.Activo = false;
            _repo.Actualizar(proveedor);
            _repo.GuardarCambios();
            return (true, "Proveedor desactivado.");
        }

        private static string AplicarPascalCase(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return texto;
            return string.Join(" ", texto.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant()));
        }
    }
}
