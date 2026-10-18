using ControlClientesRestaurante.Models;
using ControlClientesRestaurante.Repositories;

namespace ControlClientesRestaurante.Services
{
    public class ClienteService
    {
        private readonly IClienteRepository _repo;

        public ClienteService(IClienteRepository repo) => _repo = repo;

        public List<Cliente> ObtenerClientes(string? filtro) =>
            string.IsNullOrWhiteSpace(filtro) ? _repo.ObtenerTodos() : _repo.Buscar(filtro);

        public Cliente? ObtenerPorId(int id) => _repo.ObtenerPorId(id);
        public Cliente? ObtenerPorCodigo(string codigo) => _repo.ObtenerPorCodigo(codigo);

        public (bool ok, string mensaje) Registrar(Cliente cliente)
        {
            if (_repo.ExisteDocumento(cliente.Documento))
                return (false, "Ya existe un cliente con ese documento.");

            _repo.Agregar(cliente);
            _repo.GuardarCambios();
            return (true, "Cliente registrado correctamente.");
        }

        public (bool ok, string mensaje) Actualizar(Cliente cliente)
        {
            // 1. Buscar primero el cliente en la base de datos usando su Id (o Código)
            var clienteDb = cliente.Id > 0 
                ? _repo.ObtenerPorId(cliente.Id) 
                : _repo.ObtenerPorCodigo(cliente.Codigo);

            if (clienteDb == null)
                return (false, "Cliente no encontrado.");

            if (_repo.ExisteDocumento(cliente.Documento, clienteDb.Id))
                return (false, "Ese documento ya pertenece a otro cliente.");

            // 2. Actualizar únicamente los campos seguros y editables (previniendo overposting)
            clienteDb.Nombre          = cliente.Nombre;
            clienteDb.Documento       = cliente.Documento;
            clienteDb.Telefono        = cliente.Telefono;
            clienteDb.Correo          = cliente.Correo;
            clienteDb.Direccion       = cliente.Direccion;
            clienteDb.FechaNacimiento = cliente.FechaNacimiento;

            // 3. Los campos protegidos como FechaCreacion, Activo y Codigo NO se sobreescriben

            _repo.Actualizar(clienteDb);
            _repo.GuardarCambios();
            return (true, "Cliente actualizado correctamente.");
        }

        public void Desactivar(int id)
        {
            var cliente = _repo.ObtenerPorId(id);
            if (cliente != null)
            {
                cliente.Activo = false; // borrado lógico (RF06)
                _repo.Actualizar(cliente);
                _repo.GuardarCambios();
            }
        }
        public void DesactivarPorCodigo(string codigo)
{
    var cliente = _repo.ObtenerPorCodigo(codigo);
    if (cliente != null)
    {
        cliente.Activo = false;
        _repo.Actualizar(cliente);
        _repo.GuardarCambios();
    }
}
    }
}