using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ControlClientesRestaurante.Models;
using ControlClientesRestaurante.Services;

namespace ControlClientesRestaurante.Controllers
{
    [Authorize]
    public class ProductoController : Controller
    {
        private readonly ProductoService _productoService;

        public ProductoController(ProductoService productoService)
            => _productoService = productoService;

        // ── INDEX — solo administrador puede ver inventario ───────────────────
        [Authorize(Roles = "Administrador")]
        public IActionResult Index(string? filtro)
        {
            ViewBag.EsAdministrador = true;
            return View(_productoService.ObtenerTodos(filtro));
        }

        // ── CREATE ────────────────────────────────────────────────────────────
        [Authorize(Roles = "Administrador")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public IActionResult Create(Producto producto)
        {
            if (!ModelState.IsValid) return View(producto);

            var (ok, mensaje) = _productoService.Registrar(producto);
            if (!ok) { ModelState.AddModelError("", mensaje); return View(producto); }

            TempData["Mensaje"] = mensaje;
            return RedirectToAction("Index");
        }

        // ── EDIT ──────────────────────────────────────────────────────────────
        [Authorize(Roles = "Administrador")]
        public IActionResult Edit(int id)
        {
            var producto = _productoService.ObtenerPorId(id);
            return producto == null ? NotFound() : View(producto);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public IActionResult Edit(Producto producto)
        {
            if (!ModelState.IsValid) return View(producto);

            var (ok, mensaje) = _productoService.Actualizar(producto);
            if (!ok) { ModelState.AddModelError("", mensaje); return View(producto); }

            TempData["Mensaje"] = mensaje;
            return RedirectToAction("Index");
        }

        // ── DESACTIVAR ────────────────────────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public IActionResult Desactivar(int id)
        {
            var (_, mensaje) = _productoService.DesactivarProducto(id);
            TempData["Mensaje"] = mensaje;
            return RedirectToAction("Index");
        }

        // ── API para el cajero: devuelve producto en JSON ─────────────────────
        [HttpGet]
        public IActionResult ObtenerJson(int id)
        {
            var prod = _productoService.ObtenerPorId(id);
            if (prod == null || !prod.Activo) return NotFound();
            return Json(new { prod.Id, prod.Nombre, prod.Precio, prod.Stock });
        }

        // ── API: lista activos para el select del cajero ──────────────────────
        [HttpGet]
        public IActionResult ListaActivos()
        {
            var lista = _productoService.ObtenerActivos()
                .Select(p => new { p.Id, p.Nombre, p.Precio, p.Stock });
            return Json(lista);
        }
    }
}
