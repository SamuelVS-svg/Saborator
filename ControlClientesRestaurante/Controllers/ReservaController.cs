using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ControlClientesRestaurante.Models;
using ControlClientesRestaurante.Services;
using ControlClientesRestaurante.Repositories;

namespace ControlClientesRestaurante.Controllers
{
    public class NuevaReservaViewModel
    {
        public DateTime Fecha { get; set; } = DateTime.Today;
        public string Hora { get; set; } = "19:00";
        public int NumeroPersonas { get; set; } = 2;
        public int NumeroMesa { get; set; }
        public string NombreCliente { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string? Correo { get; set; }
        public string? Notas { get; set; }
        // Campo trampa (honeypot) contra bots automatizados
        public string? WebsiteTrap { get; set; }
    }

    public class ReservaController : Controller
    {
        private readonly ReservaService     _reservaService;
        private readonly IUsuarioRepository _usuarioRepo;
        private readonly IClienteRepository _clienteRepo;
        private readonly IEmailService      _emailService;

        public ReservaController(
            ReservaService reservaService,
            IUsuarioRepository usuarioRepo,
            IClienteRepository clienteRepo,
            IEmailService emailService)
        {
            _reservaService = reservaService;
            _usuarioRepo    = usuarioRepo;
            _clienteRepo    = clienteRepo;
            _emailService   = emailService;
        }

        private string? UsuarioActual() => User.Identity?.Name ?? HttpContext.Session.GetString("Usuario");
        private string  RolActual()     => User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? HttpContext.Session.GetString("Rol") ?? "";

        // ── Nueva Reserva (Web de Cine & Plano de Mesas) ───────────────────
        [HttpGet]
        public IActionResult Nueva(int? mesaPrevia, string? fechaPrevia, string? horaPrevia)
        {
            var usuario = UsuarioActual();
            var rol = RolActual();

            ViewBag.Usuario = usuario;
            ViewBag.Rol = rol;
            ViewBag.EsCliente = rol == "Cliente" || string.IsNullOrEmpty(rol);
            ViewBag.EstaAutenticado = !string.IsNullOrEmpty(usuario);

            // Prellenar datos si el cliente está registrado
            if (!string.IsNullOrEmpty(usuario))
            {
                var cli = _clienteRepo.Buscar(usuario).FirstOrDefault();
                ViewBag.NombreDefault = cli?.Nombre ?? usuario;
                ViewBag.TelefonoDefault = cli?.Telefono ?? "";
                ViewBag.CorreoDefault = cli?.Correo ?? "";
            }

            var fecha = DateTime.Today;
            if (!string.IsNullOrEmpty(fechaPrevia) && DateTime.TryParse(fechaPrevia, out var fp))
                fecha = fp;

            string hora = !string.IsNullOrEmpty(horaPrevia) ? horaPrevia : "19:00";

            ViewBag.FechaSeleccionada = fecha;
            ViewBag.HoraSeleccionada = hora;
            ViewBag.MesaPrevia = mesaPrevia ?? 0;

            // Obtener estado inicial de mesas
            ViewBag.Mesas = _reservaService.ObtenerEstadoMesas(fecha, hora);

            return View();
        }

        // Endpoint AJAX para consultar disponibilidad de mesas al cambiar fecha u hora
        [HttpGet]
        public IActionResult MesasDisponibles(DateTime fecha, string hora)
        {
            if (fecha == default) fecha = DateTime.Today;
            if (string.IsNullOrWhiteSpace(hora)) hora = "19:00";

            var mesas = _reservaService.ObtenerEstadoMesas(fecha, hora);
            return Json(mesas);
        }

        // ── Guardar Reserva ───────────────────────────────────────────────
        [HttpPost]
        [Route("Reserva/Crear")]
        [Route("Reserva/Nueva")]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("publico")]
        public IActionResult Crear(NuevaReservaViewModel vm)
        {
            // Verificación anti-spam honeypot
            if (!string.IsNullOrEmpty(vm.WebsiteTrap))
            {
                // Bot detectado silenciosamente, se descarta la solicitud
                return RedirectToAction("Index", "Home");
            }

            var usuario = UsuarioActual();

            // Si por algún motivo no está logueado al enviar el POST
            if (string.IsNullOrEmpty(usuario))
            {
                TempData["Error"] = "Debes iniciar sesión para confirmar tu reserva.";
                return RedirectToAction("Nueva", new { 
                    mesaPrevia = vm.NumeroMesa, 
                    fechaPrevia = vm.Fecha.ToString("yyyy-MM-dd"), 
                    horaPrevia = vm.Hora 
                });
            }

            var (ok, mensaje, reserva) = _reservaService.CrearReserva(
                nombreCliente: vm.NombreCliente,
                telefono: vm.Telefono,
                correo: vm.Correo,
                numeroMesa: vm.NumeroMesa,
                fecha: vm.Fecha,
                hora: vm.Hora,
                personas: vm.NumeroPersonas,
                notas: vm.Notas,
                usuarioLogueado: usuario
            );

            if (!ok)
            {
                TempData["Error"] = mensaje;
                return RedirectToAction("Nueva", new { 
                    mesaPrevia = vm.NumeroMesa, 
                    fechaPrevia = vm.Fecha.ToString("yyyy-MM-dd"), 
                    horaPrevia = vm.Hora 
                });
            }

            TempData["Mensaje"] = mensaje;

            // Enviar correo de confirmación de reserva (Asíncrono)
            if (reserva != null && !string.IsNullOrWhiteSpace(reserva.Correo))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.EnviarConfirmacionReservaAsync(
                            reserva.Correo,
                            reserva.NombreCliente,
                            reserva.Codigo,
                            reserva.Fecha,
                            reserva.Hora,
                            reserva.NumeroMesa,
                            reserva.NumeroPersonas,
                            null);
                    }
                    catch { /* Resiliencia de correo */ }
                });
            }

            return RedirectToAction("Confirmacion", new { id = reserva!.Id });
        }

        // ── Confirmación Digital (Pase / Ticket de Cine Gourmet) ──────────
        [HttpGet]
        public IActionResult Confirmacion(int id)
        {
            var reserva = _reservaService.ObtenerPorId(id);
            if (reserva == null)
            {
                TempData["Error"] = "La reserva solicitada no existe.";
                return RedirectToAction("Index", "Home");
            }

            // Anti-IDOR: los ids son consecutivos; solo quien hizo la reserva (o el administrador) la ve
            if (!AccesoRecursos.PuedeVerReserva(User, reserva))
            {
                TempData["Error"] = "La reserva solicitada no existe.";
                return RedirectToAction("Index", "Home");
            }

            return View(reserva);
        }

        // ── Mis Reservas (Solo comensal / cliente) ─────────────────────────
        [HttpGet]
        [Authorize]
        public IActionResult MisReservas()
        {
            var usuario = UsuarioActual()!;
            var reservas = _reservaService.ObtenerPorUsuario(usuario);
            return View(reservas);
        }

        // ── Cancelar Reserva (por cliente o staff) ────────────────────────
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult Cancelar(int id)
        {
            var usuario = UsuarioActual()!;
            var rol = RolActual();
            bool esStaff = rol == "Administrador" || rol == "Mesero" || rol == "Cajero";

            var (ok, mensaje) = _reservaService.CancelarReserva(id, usuario, esStaff);
            if (ok) TempData["Mensaje"] = mensaje;
            else TempData["Error"] = mensaje;

            if (esStaff) return RedirectToAction("Admin");
            return RedirectToAction("MisReservas");
        }

        // ── Cambiar Estado de Reserva (Staff: Confirmada, Completada, No Asistió) ──
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public IActionResult CambiarEstado(int id, string nuevoEstado)
        {
            var (ok, mensaje) = _reservaService.CambiarEstadoReserva(id, nuevoEstado);
            if (ok) TempData["Mensaje"] = mensaje;
            else TempData["Error"] = mensaje;

            return RedirectToAction("Admin");
        }

        // ── Panel Administrativo de Reservas con Filtros (Staff) ───────────
        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public IActionResult Admin(string? filtro = "hoy", DateTime? fecha = null, string? estado = null, string? buscar = null)
        {
            var reservas = _reservaService.ObtenerTodas();
            var hoy = DateTime.Today;

            // Filtro por rangos rápidos
            if (!string.IsNullOrWhiteSpace(filtro))
            {
                if (filtro == "hoy")
                {
                    reservas = reservas.Where(r => r.Fecha.Date == hoy).ToList();
                }
                else if (filtro == "manana")
                {
                    reservas = reservas.Where(r => r.Fecha.Date == hoy.AddDays(1)).ToList();
                }
                else if (filtro == "semana")
                {
                    var finSemana = hoy.AddDays(7);
                    reservas = reservas.Where(r => r.Fecha.Date >= hoy && r.Fecha.Date <= finSemana).ToList();
                }
            }
            else if (fecha.HasValue)
            {
                reservas = reservas.Where(r => r.Fecha.Date == fecha.Value.Date).ToList();
            }

            if (!string.IsNullOrWhiteSpace(estado))
            {
                reservas = reservas.Where(r => r.Estado.Equals(estado.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var b = buscar.Trim().ToLower();
                reservas = reservas.Where(r =>
                    r.Codigo.ToLower().Contains(b) ||
                    r.NombreCliente.ToLower().Contains(b) ||
                    r.Telefono.Contains(b) ||
                    (r.Notas != null && r.Notas.ToLower().Contains(b))
                ).ToList();
            }

            // Métricas rápidas
            var todasSinFiltro = _reservaService.ObtenerTodas();
            ViewBag.TotalHoy         = todasSinFiltro.Count(r => r.Fecha.Date == hoy);
            ViewBag.TotalManana      = todasSinFiltro.Count(r => r.Fecha.Date == hoy.AddDays(1));
            ViewBag.TotalConfirmadas = todasSinFiltro.Count(r => r.Estado == "Confirmada" && r.Fecha.Date >= hoy);
            ViewBag.TotalGeneral     = todasSinFiltro.Count;

            ViewBag.FiltroSeleccionado = filtro ?? "";
            ViewBag.FechaSeleccionada  = fecha?.ToString("yyyy-MM-dd") ?? "";
            ViewBag.EstadoSeleccionado = estado ?? "";
            ViewBag.Buscar             = buscar ?? "";
            ViewBag.Avisos             = _reservaService.ObtenerAvisosRecientes();

            return View(reservas);
        }

        // ── Exportar Reporte de Reservas a Excel (CSV UTF-8) ───────────────
        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public IActionResult ExportarCsv(string? filtro = null, DateTime? fecha = null, string? estado = null, string? buscar = null)
        {
            var reservas = _reservaService.ObtenerTodas();
            var hoy = DateTime.Today;

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                if (filtro == "hoy") reservas = reservas.Where(r => r.Fecha.Date == hoy).ToList();
                else if (filtro == "manana") reservas = reservas.Where(r => r.Fecha.Date == hoy.AddDays(1)).ToList();
                else if (filtro == "semana") reservas = reservas.Where(r => r.Fecha.Date >= hoy && r.Fecha.Date <= hoy.AddDays(7)).ToList();
            }
            else if (fecha.HasValue)
            {
                reservas = reservas.Where(r => r.Fecha.Date == fecha.Value.Date).ToList();
            }

            if (!string.IsNullOrWhiteSpace(estado))
            {
                reservas = reservas.Where(r => r.Estado.Equals(estado.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var b = buscar.Trim().ToLower();
                reservas = reservas.Where(r =>
                    r.Codigo.ToLower().Contains(b) ||
                    r.NombreCliente.ToLower().Contains(b) ||
                    r.Telefono.Contains(b)
                ).ToList();
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("sep=;");
            sb.AppendLine("Código Reserva;Fecha Servicio;Hora;Mesa;Comensal;Teléfono;Personas;Estado;Petición Especial;Fecha Registro");

            foreach (var r in reservas)
            {
                sb.AppendLine($"\"{r.Codigo}\";\"{r.Fecha:dd/MM/yyyy}\";\"{r.Hora}\";\"Mesa #{r.NumeroMesa}\";\"{r.NombreCliente}\";\"{r.Telefono}\";{r.NumeroPersonas};\"{r.Estado}\";\"{r.Notas?.Replace("\"", "'")}\";\"{r.FechaCreacion:dd/MM/yyyy HH:mm}\"");
            }

            byte[] bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv; charset=utf-8", $"Reservas_Saborator_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }

        // ── Endpoint JSON para Avisos en Tiempo Real (Topbar Staff) ───────
        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public IActionResult Avisos()
        {
            var avisos = _reservaService.ObtenerAvisosRecientes();
            return Json(avisos);
        }
    }
}
