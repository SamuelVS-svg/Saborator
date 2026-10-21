using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ControlClientesRestaurante.Models;
using ControlClientesRestaurante.Services;

namespace ControlClientesRestaurante.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class ProveedorController : Controller
    {
        private readonly ProveedorService _service;

        public ProveedorController(ProveedorService service) => _service = service;

        // ── INDEX ──────────────────────────────────────────────────────────
        public IActionResult Index(string? filtro)
        {
            ViewBag.Filtro = filtro;
            return View(_service.ObtenerTodos(filtro));
        }

        // ── CREATE ─────────────────────────────────────────────────────────
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create([Bind("Nombre,Contacto,Telefono,Correo,Direccion,Nit,Categoria,Notas")] Proveedor proveedor)
        {
            if (!ModelState.IsValid) return View(proveedor);

            var (ok, mensaje) = _service.Registrar(proveedor);
            if (!ok) { ModelState.AddModelError("Nit", mensaje); return View(proveedor); }

            TempData["Mensaje"] = mensaje;
            return RedirectToAction("Index");
        }

        // ── EDIT ───────────────────────────────────────────────────────────
        public IActionResult Edit(int id)
        {
            var proveedor = _service.ObtenerPorId(id);
            return proveedor == null ? NotFound() : View(proveedor);
        }

        [HttpPost]
        public IActionResult Edit([Bind("Id,Nombre,Contacto,Telefono,Correo,Direccion,Nit,Categoria,Notas,Activo")] Proveedor proveedor)
        {
            if (!ModelState.IsValid) return View(proveedor);

            var (ok, mensaje) = _service.Actualizar(proveedor);
            if (!ok) { ModelState.AddModelError("Nit", mensaje); return View(proveedor); }

            TempData["Mensaje"] = mensaje;
            return RedirectToAction("Index");
        }

        // ── DESACTIVAR ─────────────────────────────────────────────────────
        [HttpPost]
        public IActionResult Desactivar(int id)
        {
            var (_, mensaje) = _service.Desactivar(id);
            TempData["Mensaje"] = mensaje;
            return RedirectToAction("Index");
        }
    }
}
