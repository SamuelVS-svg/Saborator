using ControlClientesRestaurante.Models;
using ControlClientesRestaurante.Repositories;

namespace ControlClientesRestaurante.Services
{
    public class EstadoMesaDto
    {
        public int Numero { get; set; }
        public string Zona { get; set; } = string.Empty;
        public int Capacidad { get; set; } = 4;
        public bool Disponible { get; set; } = true;
        public string? EstadoTexto { get; set; }
    }

    public class ReservaService
    {
        private readonly IReservaRepository _reservaRepo;
        private readonly IMesaRepository    _mesaRepo;
        private readonly IClienteRepository _clienteRepo;
        private readonly IUsuarioRepository _usuarioRepo;

        // Lista estática en memoria para avisos rápidos en tiempo real para el personal
        private static readonly List<string> _ultimosAvisos = new();
        private static readonly object _lockAvisos = new();

        public ReservaService(
            IReservaRepository reservaRepo,
            IMesaRepository mesaRepo,
            IClienteRepository clienteRepo,
            IUsuarioRepository usuarioRepo)
        {
            _reservaRepo = reservaRepo;
            _mesaRepo    = mesaRepo;
            _clienteRepo = clienteRepo;
            _usuarioRepo = usuarioRepo;
        }

        public List<Reserva> ObtenerTodas() => _reservaRepo.ObtenerTodas();

        public Reserva? ObtenerPorId(int id) => _reservaRepo.ObtenerPorId(id);

        public Reserva? ObtenerPorCodigo(string codigo) => _reservaRepo.ObtenerPorCodigo(codigo);

        public List<Reserva> ObtenerPorUsuario(string nombreUsuario)
        {
            var u = _usuarioRepo.ObtenerPorNombre(nombreUsuario);
            if (u != null)
            {
                var porUsuario = _reservaRepo.ObtenerPorUsuarioId(u.Id);
                if (porUsuario.Any()) return porUsuario;
            }
            return _reservaRepo.ObtenerPorNombreCliente(nombreUsuario);
        }

        /// <summary>
        /// Obtiene el plano completo de las 30 mesas del restaurante con su estado en la fecha y hora seleccionadas.
        /// </summary>
        public List<EstadoMesaDto> ObtenerEstadoMesas(DateTime fecha, string hora)
        {
            var reservasEnHorario = _reservaRepo.ObtenerPorFecha(fecha)
                .Where(r => string.Equals(r.Hora, hora, StringComparison.OrdinalIgnoreCase) && r.Estado != "Cancelada")
                .ToDictionary(r => r.NumeroMesa);

            var lista = new List<EstadoMesaDto>();

            for (int i = 1; i <= 30; i++)
            {
                string zona;
                int capacidad;

                if (i <= 12)
                {
                    zona = "Salón Principal";
                    capacidad = (i % 2 == 0) ? 4 : 2;
                }
                else if (i <= 22)
                {
                    zona = "Terraza & Ventanal";
                    capacidad = (i % 3 == 0) ? 6 : 4;
                }
                else
                {
                    zona = "Salón Cava & VIP";
                    capacidad = (i % 2 == 0) ? 8 : 6;
                }

                bool estaReservada = reservasEnHorario.ContainsKey(i);

                lista.Add(new EstadoMesaDto
                {
                    Numero = i,
                    Zona = zona,
                    Capacidad = capacidad,
                    Disponible = !estaReservada,
                    EstadoTexto = estaReservada ? "Reservada" : "Disponible"
                });
            }

            return lista;
        }

        /// <summary>
        /// Registra una nueva reserva de mesa.
        /// </summary>
        public (bool ok, string mensaje, Reserva? reserva) CrearReserva(
            string nombreCliente,
            string telefono,
            string? correo,
            int numeroMesa,
            DateTime fecha,
            string hora,
            int personas,
            string? notas,
            string? usuarioLogueado)
        {
            if (string.IsNullOrWhiteSpace(nombreCliente))
                return (false, "El nombre del cliente es obligatorio.", null);

            if (string.IsNullOrWhiteSpace(telefono))
                return (false, "El teléfono de contacto es obligatorio.", null);

            if (numeroMesa < 1 || numeroMesa > 30)
                return (false, "Mesa inválida. Debe ser entre 1 y 30.", null);

            if (fecha.Date < DateTime.Today)
                return (false, "La fecha de reserva no puede ser en el pasado.", null);

            if (string.IsNullOrWhiteSpace(hora))
                return (false, "Debes seleccionar un horario.", null);

            if (personas < 1 || personas > 12)
                return (false, "El número de personas debe ser entre 1 y 12.", null);

            // Verificar disponibilidad en fecha y hora
            var reservasExistentes = _reservaRepo.ObtenerPorFecha(fecha);
            bool ocupada = reservasExistentes.Any(r => 
                r.NumeroMesa == numeroMesa && 
                string.Equals(r.Hora, hora, StringComparison.OrdinalIgnoreCase) &&
                r.Estado != "Cancelada");

            if (ocupada)
                return (false, $"La mesa {numeroMesa} ya se encuentra reservada para {fecha:dd/MM/yyyy} a las {hora}.", null);

            // Asociar usuario y cliente si están disponibles
            int? usuarioId = null;
            int? clienteId = null;

            if (!string.IsNullOrWhiteSpace(usuarioLogueado))
            {
                var u = _usuarioRepo.ObtenerPorNombre(usuarioLogueado);
                if (u != null)
                {
                    usuarioId = u.Id;
                    var c = _clienteRepo.Buscar(u.NombreUsuario).FirstOrDefault();
                    if (c != null) clienteId = c.Id;
                }
            }

            // Generar código único de reserva (ej. RES-8492)
            string codigo;
            do
            {
                codigo = $"RES-{Random.Shared.Next(1000, 9999)}";
            }
            while (_reservaRepo.ObtenerPorCodigo(codigo) != null);

            var reserva = new Reserva
            {
                Codigo = codigo,
                ClienteId = clienteId,
                UsuarioId = usuarioId,
                NombreCliente = nombreCliente.Trim(),
                Telefono = telefono.Trim(),
                Correo = correo?.Trim(),
                NumeroMesa = numeroMesa,
                Fecha = fecha.Date,
                Hora = hora.Trim(),
                NumeroPersonas = personas,
                Notas = notas?.Trim(),
                Estado = "Confirmada",
                FechaCreacion = DateTime.Now
            };

            _reservaRepo.Crear(reserva);
            _reservaRepo.GuardarCambios();

            // Registrar aviso en el sistema para Admin / Mesero / Cajero
            lock (_lockAvisos)
            {
                _ultimosAvisos.Insert(0, $"✨ ¡Nueva reserva {codigo}! Mesa #{numeroMesa} para {nombreCliente} ({personas} pers.) el {fecha:dd/MM} a las {hora}.");
                if (_ultimosAvisos.Count > 20) _ultimosAvisos.RemoveAt(_ultimosAvisos.Count - 1);
            }

            return (true, $"¡Reserva {codigo} confirmada exitosamente!", reserva);
        }

        public (bool ok, string mensaje) CancelarReserva(int id, string usuarioSolicitante, bool esStaff)
        {
            var res = _reservaRepo.ObtenerPorId(id);
            if (res == null) return (false, "Reserva no encontrada.");

            if (!esStaff)
            {
                // Un cliente solo puede cancelar sus propias reservas
                bool esDuenio = (res.Usuario?.NombreUsuario == usuarioSolicitante) ||
                                (string.Equals(res.NombreCliente, usuarioSolicitante, StringComparison.OrdinalIgnoreCase));
                if (!esDuenio) return (false, "No tienes permiso para cancelar esta reserva.");
            }

            res.Estado = "Cancelada";
            _reservaRepo.Actualizar(res);
            _reservaRepo.GuardarCambios();

            lock (_lockAvisos)
            {
                _ultimosAvisos.Insert(0, $"⚠️ Reserva {res.Codigo} cancelada (Mesa #{res.NumeroMesa}).");
            }

            return (true, $"Reserva {res.Codigo} cancelada.");
        }

        public (bool ok, string mensaje) CambiarEstadoReserva(int id, string nuevoEstado)
        {
            var res = _reservaRepo.ObtenerPorId(id);
            if (res == null) return (false, "Reserva no encontrada.");

            res.Estado = nuevoEstado;
            _reservaRepo.Actualizar(res);
            _reservaRepo.GuardarCambios();

            lock (_lockAvisos)
            {
                _ultimosAvisos.Insert(0, $"ℹ️ Reserva {res.Codigo} (Mesa #{res.NumeroMesa}) actualizada a estado: {nuevoEstado}.");
            }

            return (true, $"Reserva {res.Codigo} actualizada a '{nuevoEstado}'.");
        }

        public void CompletarReserva(int id)
        {
            CambiarEstadoReserva(id, "Completada");
        }

        public List<string> ObtenerAvisosRecientes()
        {
            lock (_lockAvisos)
            {
                return _ultimosAvisos.Take(6).ToList();
            }
        }
    }
}
