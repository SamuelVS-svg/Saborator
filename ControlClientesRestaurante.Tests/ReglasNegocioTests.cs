using ControlClientesRestaurante.Data;
using ControlClientesRestaurante.Models;
using ControlClientesRestaurante.Repositories;
using ControlClientesRestaurante.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ControlClientesRestaurante.Tests
{
    /// <summary>Reglas de negocio de cupones, reservas, caja y facturación sobre una base SQLite en memoria.</summary>
    public class ReglasNegocioTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ApplicationDbContext _context;

        public ReglasNegocioTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            _context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_connection).Options);
            _context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Close();
            _connection.Dispose();
        }

        private Usuario CrearUsuario(string nombre, string rol = Roles.Cliente)
        {
            var r = _context.Roles.FirstOrDefault(x => x.NombreRol == rol) ?? new Rol { NombreRol = rol };
            var u = new Usuario { NombreUsuario = nombre, ContraseñaHash = "x", Rol = r, Estado = true };
            _context.Usuarios.Add(u);
            _context.SaveChanges();
            return u;
        }

        // ══ CUPONES ═══════════════════════════════════════════════════════

        [Theory]
        [InlineData("Porcentaje", 150, false)]
        [InlineData("Porcentaje", 100, true)]
        [InlineData("Fijo", 0, false)]
        [InlineData("Regalo", 10, false)]
        public void Cupon_Crear_ValidaTipoYValor(string tipo, decimal valor, bool esperado)
        {
            var svc = new CuponService(_context);
            var (ok, _) = svc.Crear(new Cupon { Codigo = $"T{tipo}{valor}", Descripcion = "x", TipoDescuento = tipo, Valor = valor });
            Assert.Equal(esperado, ok);
        }

        [Fact]
        public void Cupon_Validar_AplicaTopeYNuncaSuperaElSubtotal()
        {
            var svc = new CuponService(_context);
            svc.Crear(new Cupon { Codigo = "TOPE", Descripcion = "x", TipoDescuento = "Porcentaje", Valor = 50, TopeMaximoDescuento = 5000, UsosMaximos = 10 });
            svc.Crear(new Cupon { Codigo = "FIJO", Descripcion = "x", TipoDescuento = "Fijo", Valor = 20000, UsosMaximos = 10 });

            Assert.Equal(5000m, svc.Validar("tope", 40000).montoDescuento);   // 50 % = 20.000, tope 5.000
            Assert.Equal(8000m, svc.Validar("FIJO", 8000).montoDescuento);    // fijo 20.000 sobre 8.000
        }

        [Fact]
        public void Cupon_Validar_RechazaVencidoAgotadoYMontoMinimo()
        {
            var svc = new CuponService(_context);
            svc.Crear(new Cupon { Codigo = "VENCIDO", Descripcion = "x", Valor = 10, FechaVencimiento = DateTime.Today.AddDays(-1) });
            svc.Crear(new Cupon { Codigo = "AGOTADO", Descripcion = "x", Valor = 10, UsosMaximos = 1, UsosActuales = 1 });
            svc.Crear(new Cupon { Codigo = "MINIMO", Descripcion = "x", Valor = 10, MontoMinimo = 30000, UsosMaximos = 5 });

            Assert.False(svc.Validar("VENCIDO", 50000).valido);
            Assert.False(svc.Validar("AGOTADO", 50000).valido);
            Assert.False(svc.Validar("MINIMO", 20000).valido);
            Assert.True(svc.Validar("MINIMO", 30000).valido);
        }

        private void CrearPedidoConCupon(string cupon, string telefono, string usuario, string estado = "Pendiente")
        {
            _context.Pedidos.Add(new Pedido
            {
                Numero = $"PED-{Guid.NewGuid():N}"[..12], Mesa = "Domicilio", UsuarioMesero = usuario,
                Telefono = telefono, CodigoCupon = cupon, Estado = estado, TipoEntrega = "Domicilio"
            });
            _context.SaveChanges();
        }

        [Fact]
        public void Cupon_UnUsoPorCliente_PorTelefonoOCuenta()
        {
            var svc = new CuponService(_context);
            CrearPedidoConCupon("BIENVENIDA", "+57 300 111 2233", "Online");

            Assert.True(svc.YaUsadoPorCliente("bienvenida", "3001112233", null));      // mismo celular, otro formato
            Assert.False(svc.YaUsadoPorCliente("BIENVENIDA", "3009998877", null));     // otro cliente
            Assert.False(svc.YaUsadoPorCliente("BURGER15", "3001112233", null));       // otro cupón

            CrearPedidoConCupon("BURGER15", "3005556677", "ana");
            Assert.True(svc.YaUsadoPorCliente("BURGER15", "3000000000", "ana"));       // misma cuenta, otro celular
        }

        [Fact]
        public void Cupon_PedidoCanceladoNoCuentaYDevuelveElUso()
        {
            var svc = new CuponService(_context);
            svc.Crear(new Cupon { Codigo = "UNO", Descripcion = "x", Valor = 10, UsosMaximos = 1, UsosActuales = 1 });
            CrearPedidoConCupon("UNO", "3001112233", "Online", estado: "Cancelado");

            Assert.False(svc.YaUsadoPorCliente("UNO", "3001112233", null));
            svc.DecrementarUso("UNO");
            Assert.True(svc.Validar("UNO", 50000).valido);   // vuelve a quedar un canje disponible
        }

        // ══ RESERVAS ══════════════════════════════════════════════════════

        private ReservaService Reservas() => new(
            new ReservaRepository(_context), new MesaRepository(_context),
            new ClienteRepository(_context), new UsuarioRepository(_context));

        private static DateTime ProximoDia(DayOfWeek dia)
        {
            var d = DateTime.Today.AddDays(1);
            while (d.DayOfWeek != dia) d = d.AddDays(1);
            return d;
        }

        [Fact]
        public void Reserva_RechazaMasPersonasQueLaCapacidadDeLaMesa()
        {
            CrearUsuario("ana");
            var (ok, mensaje, _) = Reservas().CrearReserva("Ana", "3001112233", null, 1, ProximoDia(DayOfWeek.Wednesday), "19:30", 6, null, "ana");
            Assert.False(ok);
            Assert.Contains("máximo 2", mensaje);
        }

        [Fact]
        public void Reserva_RechazaLunesYHorarioInvalido()
        {
            CrearUsuario("ana");
            var svc = Reservas();
            Assert.False(svc.CrearReserva("Ana", "300", null, 2, ProximoDia(DayOfWeek.Monday), "19:30", 2, null, "ana").ok);
            Assert.False(svc.CrearReserva("Ana", "300", null, 2, ProximoDia(DayOfWeek.Wednesday), "7pm", 2, null, "ana").ok);
        }

        [Fact]
        public void Reserva_NoPermiteDobleReservaDeLaMismaMesaYHora()
        {
            CrearUsuario("ana");
            var svc = Reservas();
            var dia = ProximoDia(DayOfWeek.Thursday);
            Assert.True(svc.CrearReserva("Ana", "300", null, 4, dia, "19:30", 2, null, "ana").ok);
            Assert.False(svc.CrearReserva("Ana", "300", null, 4, dia, "19:30", 2, null, "ana").ok);
            Assert.True(svc.CrearReserva("Ana", "300", null, 4, dia, "20:30", 2, null, "ana").ok);
        }

        [Fact]
        public void Reserva_SoloElDuenioLaVeYLaCancela()
        {
            CrearUsuario("ana");
            CrearUsuario("intruso");
            var svc = Reservas();
            var (_, _, reserva) = svc.CrearReserva("Ana", "300", null, 4, ProximoDia(DayOfWeek.Friday), "19:30", 2, null, "ana");

            // Aunque el intruso ponga "Ana" como nombre, no ve ni cancela la reserva ajena
            Assert.Empty(svc.ObtenerPorUsuario("intruso"));
            Assert.False(svc.CancelarReserva(reserva!.Id, "intruso", esStaff: false).ok);
            Assert.True(svc.CancelarReserva(reserva.Id, "ana", esStaff: false).ok);
            Assert.False(svc.CancelarReserva(reserva.Id, "ana", esStaff: false).ok); // ya cancelada
        }

        [Fact]
        public void Reserva_CambiarEstado_SoloEstadosValidos()
        {
            CrearUsuario("ana");
            var svc = Reservas();
            var (_, _, reserva) = svc.CrearReserva("Ana", "300", null, 4, ProximoDia(DayOfWeek.Saturday), "19:30", 2, null, "ana");
            Assert.False(svc.CambiarEstadoReserva(reserva!.Id, "<script>").ok);
            Assert.True(svc.CambiarEstadoReserva(reserva.Id, "Completada").ok);
        }

        // ══ FACTURACIÓN Y CAJA ════════════════════════════════════════════

        private Producto CrearProducto(decimal precio, int stock = 50)
        {
            var p = new Producto { Nombre = $"Prod{precio}", Precio = precio, Stock = stock, Activo = true, Categoria = "Test" };
            _context.Productos.Add(p);
            _context.SaveChanges();
            return p;
        }

        private FacturaService Facturas() => new(new FacturaRepository(_context), new ProductoRepository(_context), _context);

        [Fact]
        public void Factura_IvaSeCalculaSobreLaBaseConDescuento()
        {
            var prod = CrearProducto(10000);
            var (ok, _, factura) = Facturas().Emitir(
                new Factura { ImpuestoPorcentaje = 19, Descuento = 5000 },
                new List<(int, int)> { (prod.Id, 2) }, "cajero", null, "Cliente");

            Assert.True(ok);
            // Base 20.000 - 5.000 = 15.000; IVA 19 % = 2.850
            Assert.Equal(17850m, factura!.Total);
        }

        [Fact]
        public void Caja_CuentaVentasFacturadasYSoloElEfectivoDelCajero()
        {
            var caja = new CajaService(_context);
            var facturas = Facturas();
            var prod = CrearProducto(12000);

            var (_, _, turno) = caja.AbrirTurno("cajero1", 50000, null);
            Assert.NotNull(turno);
            // Las transacciones se comparan contra la apertura: asegurar que sean posteriores
            turno!.FechaApertura = DateTime.Now.AddSeconds(-5);
            _context.SaveChanges();

            var (_, _, f1) = facturas.Emitir(new Factura(), new List<(int, int)> { (prod.Id, 2) }, "cajero1", null, "A");
            facturas.RegistrarCobro(f1!, "Efectivo", "cajero1");          // 24.000 en efectivo de cajero1
            var (_, _, f2) = facturas.Emitir(new Factura(), new List<(int, int)> { (prod.Id, 1) }, "cajero2", null, "B");
            facturas.RegistrarCobro(f2!, "Efectivo", "cajero2");          // efectivo de OTRO cajero
            var (_, _, f3) = facturas.Emitir(new Factura(), new List<(int, int)> { (prod.Id, 1) }, "cajero1", null, "C");
            facturas.RegistrarCobro(f3!, "Nequi", "cajero1");              // transferencia

            var (ok, mensaje, cerrado) = caja.CerrarTurno(turno.Id, 74000, null);

            Assert.True(ok);
            Assert.Equal(24000m, cerrado!.VentasEfectivo);
            Assert.Equal(12000m, cerrado.VentasTransferencia);
            Assert.Equal(0m, cerrado.Diferencia);
            Assert.Contains("Cuadre exacto", mensaje);
        }

        [Fact]
        public void Caja_NoPermiteDosTurnosAbiertosDelMismoCajero()
        {
            var caja = new CajaService(_context);
            Assert.True(caja.AbrirTurno("cajero1", 10000, null).ok);
            Assert.False(caja.AbrirTurno("cajero1", 10000, null).ok);
            Assert.False(caja.AbrirTurno("cajero2", -1, null).ok);
        }

        [Fact]
        public void Factura_RechazaStockInsuficienteSinDescontarNada()
        {
            var prod = CrearProducto(5000, stock: 2);
            var (ok, _, _) = Facturas().Emitir(new Factura(), new List<(int, int)> { (prod.Id, 3) }, "cajero", null, "X");

            Assert.False(ok);
            _context.Entry(prod).Reload();
            Assert.Equal(2, prod.Stock);
        }
    }
}
