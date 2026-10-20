using ControlClientesRestaurante.Models;
using ControlClientesRestaurante.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlClientesRestaurante.Controllers
{
    public class ValidarCuponDto
    {
        public string? Codigo { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class CuponController : Controller
    {
        private readonly CuponService _cuponService;

        public CuponController(CuponService cuponService)
        {
            _cuponService = cuponService;
        }

        // ── API AJAX PÚBLICA PARA CHECKOUT / PEDIDOS ──────────────────────
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("autenticacion")] // evita adivinar códigos probando sin límite
        public IActionResult Validar([FromBody] ValidarCuponDto dto)
        {
            if (dto == null)
                return Json(new { ok = false, mensaje = "Datos de cupón no proporcionados." });

            var (valido, mensaje, montoDescuento, cupon) = _cuponService.Validar(dto.Codigo, dto.Subtotal);

            if (!valido)
            {
                return Json(new { ok = false, mensaje });
            }

            return Json(new
            {
                ok = true,
                mensaje,
                codigo = cupon!.Codigo,
                descuento = montoDescuento,
                tipoDescuento = cupon.TipoDescuento,
                valor = cupon.Valor,
                descripcion = cupon.Descripcion
            });
        }

        // ── PANEL DE ADMINISTRACIÓN DE CUPONES (MARKETING) ───────────────
        [Authorize(Roles = "Administrador,Cajero")]
        public IActionResult Index()
        {
            var cupones = _cuponService.ObtenerTodos();

            ViewBag.TotalCupones = cupones.Count;
            ViewBag.CuponesActivos = cupones.Count(c => c.Activo);
            ViewBag.TotalCanjes = cupones.Sum(c => c.UsosActuales);

            return View(cupones);
        }

        [Authorize(Roles = "Administrador")]
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Cupon
            {
                TipoDescuento = "Porcentaje",
                Valor = 10,
                MontoMinimo = 20000,
                UsosMaximos = 100,
                Activo = true,
                FechaVencimiento = DateTime.Today.AddMonths(1)
            });
        }

        [Authorize(Roles = "Administrador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Cupon cupon)
        {
            if (!ModelState.IsValid)
                return View(cupon);

            var (ok, mensaje) = _cuponService.Crear(cupon);
            if (!ok)
            {
                ModelState.AddModelError("Codigo", mensaje);
                return View(cupon);
            }

            TempData["Mensaje"] = mensaje;
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Administrador")]
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var cupon = _cuponService.ObtenerPorId(id);
            if (cupon == null) return NotFound();
            return View(cupon);
        }

        [Authorize(Roles = "Administrador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Cupon cupon)
        {
            if (!ModelState.IsValid)
                return View(cupon);

            var (ok, mensaje) = _cuponService.Actualizar(cupon);
            if (!ok)
            {
                ModelState.AddModelError("Codigo", mensaje);
                return View(cupon);
            }

            TempData["Mensaje"] = mensaje;
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Administrador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleEstado(int id)
        {
            var (ok, mensaje) = _cuponService.CambiarEstado(id);
            TempData[ok ? "Mensaje" : "Error"] = mensaje;
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Administrador")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Eliminar(int id)
        {
            var (ok, mensaje) = _cuponService.Eliminar(id);
            TempData[ok ? "Mensaje" : "Error"] = mensaje;
            return RedirectToAction(nameof(Index));
        }
    }
}
