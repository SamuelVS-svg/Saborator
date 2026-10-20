using ControlClientesRestaurante.Models;
using ControlClientesRestaurante.Services;
using Xunit;

namespace ControlClientesRestaurante.Tests
{
    public class RepartoDomiciliarioTests
    {
        private static (PedidoService service, FakePedidoRepository repo) CrearEscenario()
        {
            var prodRepo = new FakeProductoRepository();
            prodRepo.Agregar(new Producto { Id = 1, Nombre = "Hamburguesa", Precio = 12000, Stock = 50, Activo = true });
            var repo = new FakePedidoRepository();
            return (new PedidoService(repo, prodRepo), repo);
        }

        private static Pedido CrearDomicilio(PedidoService service, string estado = "Listo", decimal descuento = 0)
        {
            var (_, _, pedido) = service.Crear("Domicilio", "Ana", "Online", null,
                new List<(int, int, string?)> { (1, 2, null) },
                tipoEntrega: "Domicilio", direccion: "Calle 1", barrio: "Boston", telefono: "3001234567",
                costoEnvio: 6000, descuento: descuento);
            pedido!.Estado = estado;
            return pedido;
        }

        [Fact]
        public void Domiciliario_TomaPedidoListo_YLoEntrega()
        {
            var (service, _) = CrearEscenario();
            var pedido = CrearDomicilio(service);

            Assert.Single(service.ObtenerRepartosDisponibles());

            var (ok, _) = service.IniciarReparto(pedido.Id, "domiciliario");
            Assert.True(ok);
            Assert.Equal("EnCamino", pedido.Estado);
            Assert.Equal("domiciliario", pedido.Repartidor);
            Assert.NotNull(pedido.FechaDespacho);
            Assert.Empty(service.ObtenerRepartosDisponibles());

            var (okEntrega, _) = service.ConfirmarEntrega(pedido.Id, "domiciliario", esSupervisor: false);
            Assert.True(okEntrega);
            Assert.Equal("Entregado", pedido.Estado);
            Assert.NotNull(pedido.FechaEntrega);
        }

        [Fact]
        public void Domiciliario_NoPuedeTomarPedidoQueNoEstaListo()
        {
            var (service, _) = CrearEscenario();
            var pedido = CrearDomicilio(service, estado: "EnPreparacion");

            var (ok, _) = service.IniciarReparto(pedido.Id, "domiciliario");

            Assert.False(ok);
            Assert.Equal("EnPreparacion", pedido.Estado);
        }

        [Fact]
        public void Domiciliario_NoPuedeTomarPedidoAsignadoAOtro()
        {
            var (service, _) = CrearEscenario();
            var pedido = CrearDomicilio(service);
            service.AsignarRepartidor(pedido.Id, "carlos");

            var (ok, _) = service.IniciarReparto(pedido.Id, "domiciliario");

            Assert.False(ok);
            Assert.Equal("Listo", pedido.Estado);
            Assert.Equal("carlos", pedido.Repartidor);
        }

        [Fact]
        public void SoloElDomiciliarioAsignadoOSupervisor_ConfirmaEntrega()
        {
            var (service, _) = CrearEscenario();
            var pedido = CrearDomicilio(service);
            service.IniciarReparto(pedido.Id, "carlos");

            var (okOtro, _) = service.ConfirmarEntrega(pedido.Id, "domiciliario", esSupervisor: false);
            Assert.False(okOtro);
            Assert.Equal("EnCamino", pedido.Estado);

            var (okCajero, _) = service.ConfirmarEntrega(pedido.Id, "cajero", esSupervisor: true);
            Assert.True(okCajero);
            Assert.Equal("Entregado", pedido.Estado);
        }

        [Fact]
        public void PedidoParaLlevar_NoPuedePasarAEnCamino()
        {
            var (service, _) = CrearEscenario();
            var (_, _, pedido) = service.Crear("Para Llevar", "Luis", "Online", null,
                new List<(int, int, string?)> { (1, 1, null) }, tipoEntrega: "ParaLlevar");
            pedido!.Estado = "Listo";

            Assert.Throws<InvalidOperationException>(() => service.CambiarEstado(pedido.Id, "EnCamino"));
            Assert.False(service.AsignarRepartidor(pedido.Id, "domiciliario").ok);
        }

        [Fact]
        public void ActualizarCostoEnvio_ConservaElDescuentoDelCupon()
        {
            var (service, _) = CrearEscenario();
            // Subtotal 24.000 - descuento 5.000 + envío 6.000 = 25.000
            var pedido = CrearDomicilio(service, estado: "Pendiente", descuento: 5000);
            Assert.Equal(25000m, pedido.Total);

            var (ok, _) = service.ActualizarCostoEnvio(pedido.Id, 9000);

            Assert.True(ok);
            Assert.Equal(28000m, pedido.Total);
        }
    }
}
