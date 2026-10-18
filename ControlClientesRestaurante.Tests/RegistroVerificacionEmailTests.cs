using ControlClientesRestaurante.Data;
using ControlClientesRestaurante.Models;
using ControlClientesRestaurante.Repositories;
using ControlClientesRestaurante.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ControlClientesRestaurante.Tests
{
    public class RegistroVerificacionEmailTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ApplicationDbContext _context;
        private readonly IUsuarioRepository _usuarioRepo;
        private readonly IClienteRepository _clienteRepo;
        private readonly AutenticacionService _authService;

        public RegistroVerificacionEmailTests()
        {
            // Conexión SQLite en memoria persistente durante la prueba
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new ApplicationDbContext(options);
            _context.Database.EnsureCreated();

            // Asegurar rol Cliente
            if (!_context.Roles.Any(r => r.NombreRol == "Cliente"))
            {
                _context.Roles.Add(new Rol { NombreRol = "Cliente" });
                _context.SaveChanges();
            }

            _usuarioRepo = new UsuarioRepository(_context);
            _clienteRepo = new ClienteRepository(_context);
            _authService = new AutenticacionService(_usuarioRepo, _clienteRepo, _context);
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Close();
            _connection.Dispose();
        }

        [Theory]
        [InlineData("TI-1029384756")]
        [InlineData("TI1029384756")]
        [InlineData("T.I.-1029384756")]
        public void RegistrarCliente_ConTarjetaIdentidad_RechazaYExigeDocumentoMayorDeEdad(string documentoTI)
        {
            // Arrange: Fecha de nacimiento válida de adulto (25 años) pero documento TI
            var fechaAdulto = DateTime.Today.AddYears(-25);

            // Act
            var (ok, mensaje, usuario, cliente) = _authService.RegistrarCliente(
                nombreCompleto: "Santiago Morales",
                documento: documentoTI,
                telefono: "3001234567",
                correo: "santiago@ejemplo.com",
                nombreUsuario: "santiagom",
                contraseña: "Password123!",
                confirmarContraseña: "Password123!",
                fechaNacimiento: fechaAdulto);

            // Assert
            Assert.False(ok);
            Assert.Null(usuario);
            Assert.Null(cliente);
            Assert.Contains("Tarjeta de Identidad", mensaje);
            Assert.Contains("exclusivo para personas mayores de edad", mensaje);
        }

        [Fact]
        public void RegistrarCliente_MenorDe18Anos_RechazaYNoCreaCuenta()
        {
            // Arrange: Comensal de 16 años con CC
            var fechaMenor = DateTime.Today.AddYears(-16);

            // Act
            var (ok, mensaje, usuario, cliente) = _authService.RegistrarCliente(
                nombreCompleto: "Laura Gomez",
                documento: "CC-1092837465",
                telefono: "3109876543",
                correo: "laura@ejemplo.com",
                nombreUsuario: "laurag",
                contraseña: "Password123!",
                confirmarContraseña: "Password123!",
                fechaNacimiento: fechaMenor);

            // Assert
            Assert.False(ok);
            Assert.Null(usuario);
            Assert.Null(cliente);
            Assert.Contains("Debes ser mayor de edad", mensaje);
            Assert.Contains("mínimo 18 años cumplidos", mensaje);
        }

        [Fact]
        public void RegistrarCliente_SinFechaNacimiento_Rechaza()
        {
            // Act
            var (ok, mensaje, usuario, cliente) = _authService.RegistrarCliente(
                nombreCompleto: "Andrés Silva",
                documento: "CC-1020304050",
                telefono: "3151234567",
                correo: "andres@ejemplo.com",
                nombreUsuario: "andress",
                contraseña: "Password123!",
                confirmarContraseña: "Password123!",
                fechaNacimiento: null);

            // Assert
            Assert.False(ok);
            Assert.Contains("fecha de nacimiento es obligatoria", mensaje);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("sin-arroba.com")]
        public void RegistrarCliente_SinCorreoValido_Rechaza(string? correoInvalido)
        {
            // Arrange
            var fechaAdulto = DateTime.Today.AddYears(-22);

            // Act
            var (ok, mensaje, usuario, cliente) = _authService.RegistrarCliente(
                nombreCompleto: "Felipe Ruiz",
                documento: "CC-1030405060",
                telefono: "3201234567",
                correo: correoInvalido,
                nombreUsuario: "feliper",
                contraseña: "Password123!",
                confirmarContraseña: "Password123!",
                fechaNacimiento: fechaAdulto);

            // Assert
            Assert.False(ok);
            Assert.Contains("correo electrónico es obligatorio", mensaje);
        }

        [Fact]
        public void RegistrarCliente_MayorDeEdadConCC_CreaCuentaPendienteDeValidacion()
        {
            // Arrange: 24 años con CC
            var fechaAdulto = DateTime.Today.AddYears(-24);

            // Act
            var (ok, mensaje, usuario, cliente) = _authService.RegistrarCliente(
                nombreCompleto: "Carlos Ramírez",
                documento: "CC-1023456789",
                telefono: "3001234567",
                correo: "carlos.ramirez@gmail.com",
                nombreUsuario: "carlosr",
                contraseña: "Password123!",
                confirmarContraseña: "Password123!",
                fechaNacimiento: fechaAdulto);

            // Assert
            Assert.True(ok, mensaje);
            Assert.NotNull(usuario);
            Assert.NotNull(cliente);
            Assert.False(usuario.EmailVerificado); // Debe estar en estado PENDIENTE de validación
            Assert.False(string.IsNullOrWhiteSpace(usuario.TokenVerificacionEmail)); // Token seguro generado
            Assert.NotNull(usuario.FechaExpiracionToken);
            Assert.Equal("carlos.ramirez@gmail.com", usuario.Correo);
            Assert.Equal(fechaAdulto.Date, cliente.FechaNacimiento);
        }

        [Fact]
        public void ValidarCredenciales_CuentaNoVerificada_BloqueaAccesoYExigeValidacion()
        {
            // Arrange: Registrar usuario adulto
            var fechaAdulto = DateTime.Today.AddYears(-28);
            var (okReg, _, usuario, _) = _authService.RegistrarCliente(
                "Mariana Torres", "CC-1055667788", "3112233445", "mariana@correo.com", "marianat", "ClaveSecreta123!", "ClaveSecreta123!", fechaAdulto);
            Assert.True(okReg);

            // Act: Intentar loguearse antes de validar el correo
            var resLogin = _authService.ValidarCredenciales("marianat", "ClaveSecreta123!");

            // Assert
            Assert.False(resLogin.Exitoso);
            Assert.True(resLogin.RequiereVerificacionEmail);
            Assert.Contains("aún no ha sido verificada", resLogin.Mensaje);
        }

        [Fact]
        public void VerificarEmailToken_TokenCorrecto_ValidaCuentaYPermiteAcceso()
        {
            // Arrange
            var fechaAdulto = DateTime.Today.AddYears(-30);
            var (okReg, _, usuario, _) = _authService.RegistrarCliente(
                "Diego Castro", "CC-1099887766", "3178899001", "diego@correo.com", "diegoc", "ClaveSecreta123!", "ClaveSecreta123!", fechaAdulto);
            Assert.True(okReg);
            var token = usuario!.TokenVerificacionEmail!;

            // Act: El usuario abre su correo y pulsa el botón de validación
            var (okVerify, mensajeVerify, usuarioValidado) = _authService.VerificarEmailToken(token);

            // Assert validación
            Assert.True(okVerify, mensajeVerify);
            Assert.NotNull(usuarioValidado);
            Assert.True(usuarioValidado.EmailVerificado);
            Assert.Null(usuarioValidado.TokenVerificacionEmail); // Token consumido

            // Act 2: Ahora intentar ingresar
            var resLogin = _authService.ValidarCredenciales("diegoc", "ClaveSecreta123!");

            // Assert acceso
            Assert.True(resLogin.Exitoso, resLogin.Mensaje);
            Assert.False(resLogin.RequiereVerificacionEmail);
            Assert.Equal("Cliente", resLogin.Rol);
        }

        [Fact]
        public void VerificarEmailToken_TokenInexistente_Falla()
        {
            // Act
            var (ok, mensaje, usuario) = _authService.VerificarEmailToken("token_completamente_falso_123");

            // Assert
            Assert.False(ok);
            Assert.Null(usuario);
            Assert.Contains("no es válido", mensaje);
        }
    }
}
