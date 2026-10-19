using System.Text;
using ControlClientesRestaurante.Data;
using ControlClientesRestaurante.Models;
using Microsoft.EntityFrameworkCore;

namespace ControlClientesRestaurante.Services
{
    public class AuditoriaService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditoriaService> _logger;

        public AuditoriaService(ApplicationDbContext context, ILogger<AuditoriaService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Registra un evento de auditoría de forma segura y no bloqueante.
        /// </summary>
        public void Registrar(
            string usuario,
            string? rol,
            string accion,
            string modulo,
            string detalle,
            string? entidadAfectada = null,
            int? entidadId = null,
            string? ip = null)
        {
            try
            {
                var log = new AuditoriaLog
                {
                    Usuario         = string.IsNullOrWhiteSpace(usuario) ? "Sistema" : usuario.Trim(),
                    Rol             = string.IsNullOrWhiteSpace(rol) ? "N/A" : rol.Trim(),
                    Accion          = accion.Trim().ToUpperInvariant(),
                    Modulo          = modulo.Trim(),
                    Detalle         = detalle.Length > 990 ? detalle.Substring(0, 990) + "..." : detalle,
                    EntidadAfectada = entidadAfectada,
                    EntidadId       = entidadId,
                    IpAddress       = ip ?? "Local",
                    FechaCreacion   = DateTime.Now
                };

                _context.AuditoriaLogs.Add(log);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar evento de auditoría: {Accion} en {Modulo}", accion, modulo);
            }
        }

        public List<AuditoriaLog> ObtenerLogs(
            DateTime? desde = null,
            DateTime? hasta = null,
            string? modulo = null,
            string? accion = null,
            string? usuario = null,
            int limite = 300)
        {
            var query = _context.AuditoriaLogs.AsNoTracking().AsQueryable();

            // Excluir eventos cosméticos y de preferencias de interfaz
            query = query.Where(a => a.Modulo != "Personalización"
                                  && a.Modulo != "Perfil"
                                  && a.Accion != "CAMBIO_TEMA"
                                  && a.Accion != "ACTUALIZACION_AVATAR_PRESET"
                                  && a.Accion != "ACTUALIZACION_FOTO_PERFIL"
                                  && a.Accion != "ELIMINACION_FOTO_PERFIL");

            if (desde.HasValue)
            {
                var d = desde.Value.Date;
                query = query.Where(a => a.FechaCreacion >= d);
            }

            if (hasta.HasValue)
            {
                var h = hasta.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(a => a.FechaCreacion <= h);
            }

            if (!string.IsNullOrWhiteSpace(modulo) && modulo != "Todos")
            {
                query = query.Where(a => a.Modulo == modulo);
            }

            if (!string.IsNullOrWhiteSpace(accion) && accion != "Todas")
            {
                query = query.Where(a => a.Accion == accion);
            }

            if (!string.IsNullOrWhiteSpace(usuario))
            {
                var u = usuario.Trim().ToLower();
                query = query.Where(a => a.Usuario.ToLower().Contains(u));
            }

            return query
                .OrderByDescending(a => a.FechaCreacion)
                .Take(limite)
                .ToList();
        }

        public (int totalHoy, int anulacionesHoy, int operacionesCajaHoy, int usuariosUnicosHoy) ObtenerEstadisticasHoy()
        {
            var inicioHoy = DateTime.Today;
            var logsHoy = _context.AuditoriaLogs
                .AsNoTracking()
                .Where(a => a.FechaCreacion >= inicioHoy
                         && a.Modulo != "Personalización"
                         && a.Modulo != "Perfil"
                         && a.Accion != "CAMBIO_TEMA"
                         && a.Accion != "ACTUALIZACION_AVATAR_PRESET"
                         && a.Accion != "ACTUALIZACION_FOTO_PERFIL"
                         && a.Accion != "ELIMINACION_FOTO_PERFIL")
                .ToList();

            int totalHoy = logsHoy.Count;
            int anulacionesHoy = logsHoy.Count(l => l.Accion.Contains("ANULACION") || l.Accion.Contains("CANCELACION"));
            int cajaHoy = logsHoy.Count(l => l.Modulo == "Caja");
            int usuariosHoy = logsHoy.Select(l => l.Usuario).Distinct().Count();

            return (totalHoy, anulacionesHoy, cajaHoy, usuariosHoy);
        }

        public byte[] ExportarCsv(
            DateTime? desde = null,
            DateTime? hasta = null,
            string? modulo = null,
            string? usuario = null)
        {
            var logs = ObtenerLogs(desde, hasta, modulo, null, usuario, limite: 2000);

            var sb = new StringBuilder();
            sb.AppendLine("sep=;");
            sb.AppendLine("ID;Fecha;Hora;Usuario;Rol;Módulo;Acción Crítica;Detalle;Entidad;ID Entidad;Dirección IP");

            foreach (var l in logs)
            {
                string detalleLimpio = l.Detalle.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ");
                sb.AppendLine($"{l.Id};\"{l.FechaCreacion:dd/MM/yyyy}\";\"{l.FechaCreacion:HH:mm:ss}\";\"{l.Usuario}\";\"{l.Rol}\";\"{l.Modulo}\";\"{l.Accion}\";\"{detalleLimpio}\";\"{l.EntidadAfectada ?? "N/A"}\";{l.EntidadId?.ToString() ?? "N/A"};\"{l.IpAddress}\"");
            }

            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        }
    }
}
