using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ControlClientesRestaurante.Models;
using ControlClientesRestaurante.Services;

namespace ControlClientesRestaurante.Controllers
{
    public class ResenaController : Controller
    {
        private readonly ResenaService _resenaService;
        private readonly ProductoService _productoService;

        public ResenaController(ResenaService resenaService, ProductoService productoService)
        {
            _resenaService = resenaService;
            _productoService = productoService;
        }

        private string Rol() => User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? HttpContext.Session.GetString("Rol") ?? "";
        private bool EsAdminOCajero() => EsAdmin();
        private bool EsAdmin() => User.IsInRole("Administrador") || Rol() == "Administrador";

        // GET: /Resena
        public IActionResult Index()
        {
            bool esStaff = EsAdminOCajero();
            var resenas = esStaff ? _resenaService.ObtenerTodas() : _resenaService.ObtenerAprobadas();
            var stats = _resenaService.ObtenerEstadisticas();

            ViewBag.Stats = stats;
            ViewBag.EsStaff = esStaff;
            ViewBag.EsAdmin = EsAdmin();
            ViewBag.Platos = _productoService.ObtenerActivos();

            return View(resenas);
        }

        // GET: /Resena/ListarApi (para cargar dinámicamente o filtros AJAX)
        [HttpGet]
        public IActionResult ListarApi()
        {
            var resenas = _resenaService.ObtenerAprobadas()
                .Select(r => new
                {
                    r.Id,
                    r.NombreCliente,
                    r.Calificacion,
                    r.Comentario,
                    r.PlatoRecomendado,
                    r.TipoExperiencia,
                    r.Verificado,
                    Fecha = r.FechaCreacion.ToString("dd/MM/yyyy HH:mm"),
                    HaceCuanto = ObtenerTiempoRelativo(r.FechaCreacion)
                });

            var stats = _resenaService.ObtenerEstadisticas();

            return Json(new { ok = true, stats, resenas });
        }

        // POST: /Resena/CrearApi (vía fetch AJAX desde Home/Index o Resena/Index)
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult CrearApi([FromBody] CrearResenaDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new { ok = false, mensaje = "Datos de reseña inválidos." });
            }

            var (ok, mensaje, resena) = _resenaService.CrearResena(dto);
            if (!ok)
            {
                return Json(new { ok = false, mensaje });
            }

            var stats = _resenaService.ObtenerEstadisticas();

            return Json(new
            {
                ok = true,
                mensaje,
                stats,
                resena = new
                {
                    resena!.Id,
                    resena.NombreCliente,
                    resena.Calificacion,
                    resena.Comentario,
                    resena.PlatoRecomendado,
                    resena.TipoExperiencia,
                    resena.Verificado,
                    Fecha = resena.FechaCreacion.ToString("dd/MM/yyyy HH:mm"),
                    HaceCuanto = "Justo ahora"
                }
            });
        }

        // POST: /Resena/Crear (Fallback formulario HTML clásico)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Crear([FromForm] CrearResenaDto dto, string? returnUrl)
        {
            var (ok, mensaje, _) = _resenaService.CrearResena(dto);
            if (ok) TempData["Mensaje"] = mensaje;
            else TempData["Error"] = mensaje;

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index");
        }

        // POST: /Resena/CambiarAprobacion
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public IActionResult CambiarAprobacion(int id, bool aprobada)
        {
            var (ok, mensaje) = _resenaService.CambiarAprobacion(id, aprobada);
            if (ok) TempData["Mensaje"] = mensaje;
            else TempData["Error"] = mensaje;

            return RedirectToAction("Index");
        }

        // POST: /Resena/Eliminar
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public IActionResult Eliminar(int id)
        {
            var (ok, mensaje) = _resenaService.Eliminar(id);
            if (ok) TempData["Mensaje"] = mensaje;
            else TempData["Error"] = mensaje;

            return RedirectToAction("Index");
        }

        private static string ObtenerTiempoRelativo(DateTime fecha)
        {
            var span = DateTime.Now - fecha;
            if (span.TotalMinutes < 2) return "Hace un momento";
            if (span.TotalMinutes < 60) return $"Hace {(int)span.TotalMinutes} minutos";
            if (span.TotalHours < 24) return $"Hace {(int)span.TotalHours} horas";
            if (span.TotalDays < 7) return $"Hace {(int)span.TotalDays} días";
            if (span.TotalDays < 30) return $"Hace {(int)(span.TotalDays / 7)} semanas";
            return fecha.ToString("dd MMM yyyy");
        }
    }
}
