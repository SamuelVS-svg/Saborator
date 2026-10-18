using ControlClientesRestaurante.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlClientesRestaurante.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AuditoriaController : Controller
    {
        private readonly AuditoriaService _auditoriaService;

        public AuditoriaController(AuditoriaService auditoriaService)
        {
            _auditoriaService = auditoriaService;
        }

        [HttpGet]
        public IActionResult Index(
            DateTime? desde = null,
            DateTime? hasta = null,
            string? modulo = null,
            string? accion = null,
            string? usuario = null)
        {
            var logs = _auditoriaService.ObtenerLogs(desde, hasta, modulo, accion, usuario);
            var (totalHoy, anulacionesHoy, cajaHoy, usuariosHoy) = _auditoriaService.ObtenerEstadisticasHoy();

            ViewBag.TotalHoy        = totalHoy;
            ViewBag.AnulacionesHoy  = anulacionesHoy;
            ViewBag.CajaHoy         = cajaHoy;
            ViewBag.UsuariosHoy     = usuariosHoy;

            ViewBag.Desde           = desde?.ToString("yyyy-MM-dd");
            ViewBag.Hasta           = hasta?.ToString("yyyy-MM-dd");
            ViewBag.Modulo          = modulo ?? "Todos";
            ViewBag.Accion          = accion ?? "Todas";
            ViewBag.UsuarioFiltro   = usuario;

            return View(logs);
        }

        [HttpGet]
        public IActionResult ExportarCsv(
            DateTime? desde = null,
            DateTime? hasta = null,
            string? modulo = null,
            string? usuario = null)
        {
            byte[] bytes = _auditoriaService.ExportarCsv(desde, hasta, modulo, usuario);
            string archivo = $"Bitacora_Auditoria_Saborator_{DateTime.Now:yyyyMMdd_HHmm}.csv";
            return File(bytes, "text/csv; charset=utf-8", archivo);
        }
    }
}
