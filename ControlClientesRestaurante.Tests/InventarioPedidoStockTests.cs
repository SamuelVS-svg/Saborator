using ControlClientesRestaurante.Models;
using ControlClientesRestaurante.Repositories;
using ControlClientesRestaurante.Services;
using Xunit;

namespace ControlClientesRestaurante.Tests
{
    public class FakeProductoRepository : IProductoRepository
    {
        public List<Producto> Productos { get; } = new();

        public List<Producto> ObtenerTodos() => Productos;
        public List<Producto> ObtenerActivos() => Productos.Where(p => p.Activo).ToList();
        public List<Producto> Buscar(string texto) => Productos.Where(p => p.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase)).ToList();
        public Producto? ObtenerPorId(int id) => Productos.FirstOrDefault(p => p.Id == id);
        public bool ExisteNombre(string nombre, int idExcluir = 0) => Productos.Any(p => p.Id != idExcluir && p.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase));
        public void Agregar(Producto producto) => Productos.Add(producto);
        public void Actualizar(Producto producto) { }
        public void GuardarCambios() { }
    }

    public class FakePedidoRepository : IPedidoRepository
    {
        public List<Pedido> Pedidos { get; } = new();
        private int _nextId = 1;

        public List<Pedido> ObtenerTodos() => Pedidos;
        public List<Pedido> ObtenerPorMesero(string usuario) => Pedidos.Where(p => p.UsuarioMesero == usuario).ToList();
        public List<Pedido> ObtenerPendientes() => Pedidos.Where(p => p.Estado == "Pendiente").ToList();
        public List<Pedido> ObtenerDomicilios() => Pedidos.Where(p => p.TipoEntrega == "Domicilio").ToList();
        public List<Pedido> ObtenerParaCocina() => Pedidos.Where(p => p.Estado == "Pendiente" || p.Estado == "EnPreparacion").ToList();
        public List<Pedido> ObtenerListosCocina() => Pedidos.Where(p => p.Estado == "Listo").ToList();
        public Pedido? ObtenerPorId(int id) => Pedidos.FirstOrDefault(p => p.Id == id);
        public Pedido? ObtenerPorNumero(string numero) => Pedidos.FirstOrDefault(p => p.Numero.Equals(numero, StringComparison.OrdinalIgnoreCase));
        public List<Pedido> ObtenerPorTelefono(string telefono) => Pedidos.Where(p => p.Telefono != null && p.Telefono.Contains(telefono)).ToList();
        public string GenerarNumero() => $"PED-{_nextId:D5}";

        public void Agregar(Pedido pedido)
        {
            pedido.Id = _nextId++;
            Pedidos.Add(pedido);
        }

        public void Actualizar(Pedido pedido) { }
        public void GuardarCambios() { }
    }

    public class InventarioPedidoStockTests
    {
        [Fact]
        public void CrearPedido_DeduceStockInmediatamenteAlEnviarACocina()
        {
            // Arrange
            var prodRepo = new FakeProductoRepository();
            var burger = new Producto { Id = 1, Nombre = "Hamburguesa Artesanal", Precio = 25000, Stock = 10, Activo = true };
            var soda = new Producto { Id = 2, Nombre = "Gaseosa 400ml", Precio = 6000, Stock = 5, Activo = true };
            prodRepo.Agregar(burger);
            prodRepo.Agregar(soda);

            var pedidoRepo = new FakePedidoRepository();
            var service = new PedidoService(pedidoRepo, prodRepo);

            var lineas = new List<(int productoId, int cantidad, string? notaLinea)>
            {
                (1, 2, "Sin cebolla"),
                (2, 1, "Fría")
            };

            // Act
            var (ok, mensaje, pedido) = service.Crear(
                mesa: "1",
                nombreCliente: "Juan Perez",
                usuarioMesero: "mesero1",
                notas: null,
                lineas: lineas);

            // Assert
            Assert.True(ok, mensaje);
            Assert.NotNull(pedido);
            Assert.Equal(8, burger.Stock); // 10 - 2 = 8
            Assert.Equal(4, soda.Stock);   // 5 - 1 = 4
        }

        [Fact]
        public void CrearPedido_StockInsuficiente_FallaYNoDeduceInventario()
        {
            // Arrange
            var prodRepo = new FakeProductoRepository();
            var postre = new Producto { Id = 3, Nombre = "Brownie con Helado", Precio = 12000, Stock = 2, Activo = true };
            prodRepo.Agregar(postre);

            var pedidoRepo = new FakePedidoRepository();
            var service = new PedidoService(pedidoRepo, prodRepo);

            var lineas = new List<(int productoId, int cantidad, string? notaLinea)>
            {
                (3, 5, null) // Pide 5 habiendo solo 2
            };

            // Act
            var (ok, mensaje, pedido) = service.Crear(
                mesa: "2",
                nombreCliente: "Maria Gomez",
                usuarioMesero: "mesero1",
                notas: null,
                lineas: lineas);

            // Assert
            Assert.False(ok);
            Assert.Null(pedido);
            Assert.Contains("Stock insuficiente", mensaje);
            Assert.Equal(2, postre.Stock); // El stock permanece intacto
        }

        [Fact]
        public void CancelarPedido_RestituyeStockExactoEnInventario()
        {
            // Arrange
            var prodRepo = new FakeProductoRepository();
            var pizza = new Producto { Id = 4, Nombre = "Pizza Familiar", Precio = 45000, Stock = 8, Activo = true };
            prodRepo.Agregar(pizza);

            var pedidoRepo = new FakePedidoRepository();
            var service = new PedidoService(pedidoRepo, prodRepo);

            var lineas = new List<(int productoId, int cantidad, string? notaLinea)>
            {
                (4, 3, null)
            };

            var (okCrear, _, pedido) = service.Crear("Mesa 3", "Carlos", "mesero1", null, lineas);
            Assert.True(okCrear);
            Assert.Equal(5, pizza.Stock); // 8 - 3 = 5

            // Act: Cancelar pedido
            var (okCancel, mensajeCancel) = service.CambiarEstado(pedido!.Id, "Cancelado");

            // Assert
            Assert.True(okCancel, mensajeCancel);
            Assert.Equal("Cancelado", pedido.Estado);
            Assert.Equal(8, pizza.Stock); // Stock devuelto: 5 + 3 = 8
        }

        [Fact]
        public void CancelarPedido_LineasRepetidasMismoProducto_RestituyeSumaTotal()
        {
            // Arrange
            var prodRepo = new FakeProductoRepository();
            var cerveza = new Producto { Id = 5, Nombre = "Cerveza Corona", Precio = 10000, Stock = 12, Activo = true };
            prodRepo.Agregar(cerveza);

            var pedidoRepo = new FakePedidoRepository();
            var service = new PedidoService(pedidoRepo, prodRepo);

            var lineas = new List<(int productoId, int cantidad, string? notaLinea)>
            {
                (5, 2, "Con limón"),
                (5, 3, "Sin limón")
            };

            var (okCrear, _, pedido) = service.Crear("Mesa 4", "Andrés", "mesero1", null, lineas);
            Assert.True(okCrear);
            Assert.Equal(7, cerveza.Stock); // 12 - 5 = 7

            // Act: Cancelar pedido
            var (okCancel, mensajeCancel) = service.CambiarEstado(pedido!.Id, "Cancelado");

            // Assert
            Assert.True(okCancel, mensajeCancel);
            Assert.Equal(12, cerveza.Stock); // Stock devuelto por completo: 7 + 5 = 12
        }
    }
}
