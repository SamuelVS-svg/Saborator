using ControlClientesRestaurante.Models;
using ControlClientesRestaurante.Repositories;

namespace ControlClientesRestaurante.Services
{
    public class ProductoService
    {
        private readonly IProductoRepository _repo;

        public ProductoService(IProductoRepository repo) => _repo = repo;

        public List<Producto> ObtenerTodos(string? filtro = null) =>
            string.IsNullOrWhiteSpace(filtro) ? _repo.ObtenerTodos() : _repo.Buscar(filtro);

        public List<Producto> ObtenerActivos() => _repo.ObtenerActivos();

        public Producto? ObtenerPorId(int id) => _repo.ObtenerPorId(id);

        public (bool ok, string mensaje) Registrar(Producto producto)
        {
            producto.Nombre = producto.Nombre.Trim();
            if (_repo.ExisteNombre(producto.Nombre))
                return (false, $"Ya existe un producto llamado '{producto.Nombre}'.");

            _repo.Agregar(producto);
            _repo.GuardarCambios();
            return (true, "Producto registrado correctamente.");
        }

        public (bool ok, string mensaje) Actualizar(Producto producto)
        {
            var existente = _repo.ObtenerPorId(producto.Id);
            if (existente == null) return (false, "Producto no encontrado.");
            producto.Nombre = producto.Nombre.Trim();
            if (_repo.ExisteNombre(producto.Nombre, producto.Id))
                return (false, $"Ya existe otro producto llamado '{producto.Nombre}'.");

            existente.Nombre                = producto.Nombre;
            existente.Descripcion           = producto.Descripcion;
            existente.Precio                = producto.Precio;
            existente.Stock                 = producto.Stock;
            existente.Activo                = producto.Activo;
            existente.Categoria             = producto.Categoria;
            existente.Alergenos             = producto.Alergenos;
            existente.Ingredientes          = producto.Ingredientes;
            existente.EsVegetariano         = producto.EsVegetariano;
            existente.EsLibreGluten         = producto.EsLibreGluten;
            existente.TiempoCoccionMinutos  = producto.TiempoCoccionMinutos;

            _repo.Actualizar(existente);
            _repo.GuardarCambios();
            return (true, "Producto actualizado correctamente.");
        }

        public (bool ok, string mensaje) DesactivarProducto(int id)
        {
            var producto = _repo.ObtenerPorId(id);
            if (producto == null) return (false, "Producto no encontrado.");
            producto.Activo = false;
            _repo.Actualizar(producto);
            _repo.GuardarCambios();
            return (true, "Producto desactivado.");
        }

        /// <summary>Descuenta stock al emitir una factura. Retorna false si stock insuficiente.</summary>
        public bool DescontarStock(int productoId, int cantidad)
        {
            var producto = _repo.ObtenerPorId(productoId);
            if (producto == null || producto.Stock < cantidad) return false;
            producto.Stock -= cantidad;
            _repo.Actualizar(producto);
            _repo.GuardarCambios();
            return true;
        }

        /// <summary>Devuelve stock al anular una factura.</summary>
        public void DevolverStock(int productoId, int cantidad)
        {
            var producto = _repo.ObtenerPorId(productoId);
            if (producto == null) return;
            producto.Stock += cantidad;
            _repo.Actualizar(producto);
            _repo.GuardarCambios();
        }
    }
}
