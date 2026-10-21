using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ControlClientesRestaurante.Services;

namespace ControlClientesRestaurante.Controllers
{
    [Authorize(Roles = "Administrador,Mesero")]
    public class MesaController : Controller
    {
        private readonly MesaService _mesaService;

        public MesaController(MesaService mesaService)
        {
            _mesaService = mesaService;
        }

        private string Rol() => User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";

        // ── GET: /Mesa ──────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Index(string? filtro)
        {
            var detalles = _mesaService.ObtenerDetalleMesas();

            if (!string.IsNullOrWhiteSpace(filtro) && filtro != "Todas")
            {
                detalles = detalles.Where(d =>
                    d.Mesa.Estado.Equals(filtro, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            ViewBag.FiltroActual = filtro ?? "Todas";
            ViewBag.Total = _mesaService.ObtenerTodas().Count;
            ViewBag.Libres = _mesaService.MesasLibres();
            ViewBag.Ocupadas = _mesaService.MesasOcupadas();
            ViewBag.Reservadas = _mesaService.MesasReservadas();
            ViewBag.EnLimpieza = _mesaService.MesasEnLimpieza();
            ViewBag.Rol = Rol();

            return View(detalles);
        }

        // ── POST: /Mesa/CambiarEstado (Formulario clásico o AJAX) ─────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CambiarEstado(int numeroMesa, string nuevoEstado, string? returnUrl)
        {

            var (ok, mensaje) = _mesaService.CambiarEstado(numeroMesa, nuevoEstado);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = ok, message = mensaje, nuevoEstado, mesa = numeroMesa });
            }

            if (ok) TempData["Mensaje"] = mensaje;
            else TempData["Error"] = mensaje;

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index");
        }

        // ── POST: /Mesa/CambiarEstadoRapido (Para peticiones JSON directas) ──
        [HttpPost]
        public IActionResult CambiarEstadoRapido([FromBody] CambiarEstadoDto dto)
        {
            if (dto == null || dto.NumeroMesa < 1 || dto.NumeroMesa > 30 || string.IsNullOrWhiteSpace(dto.NuevoEstado))
                return BadRequest(new { success = false, message = "Datos de mesa no válidos" });

            var (ok, mensaje) = _mesaService.CambiarEstado(dto.NumeroMesa, dto.NuevoEstado);

            return Json(new {
                success = ok,
                message = mensaje,
                nuevoEstado = dto.NuevoEstado,
                mesa = dto.NumeroMesa
            });
        }
    }

    public class CambiarEstadoDto
    {
        public int NumeroMesa { get; set; }
        public string NuevoEstado { get; set; } = string.Empty;
    }
}
