using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ControlClientesRestaurante.Models;
using ControlClientesRestaurante.Services;

namespace ControlClientesRestaurante.Controllers
{
    [Authorize(Roles = "Administrador,Cajero")]
    public class ClienteController : Controller
    {
        private readonly ClienteService _clienteService;

        public ClienteController(ClienteService clienteService)
            => _clienteService = clienteService;

        private bool EsAdministrador() => User.IsInRole("Administrador");

        // ── INDEX ──────────────────────────────────────────────────────────
        public IActionResult Index(string? filtro)
        {
            ViewBag.EsAdministrador = EsAdministrador();
            return View(_clienteService.ObtenerClientes(filtro));
        }

        // ── CREATE ─────────────────────────────────────────────────────────
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Cliente cliente)
        {
            cliente.Nombre = AplicarCamelCase(cliente.Nombre);
            if (!ModelState.IsValid) return View(cliente);

            if (cliente.FechaNacimiento.HasValue)
            {
                var error = ValidarMayoriaEdad(cliente.FechaNacimiento.Value);
                if (error != null) { ModelState.AddModelError("FechaNacimiento", error); return View(cliente); }
            }

            var (ok, mensaje) = _clienteService.Registrar(cliente);
            if (!ok) { ModelState.AddModelError("Documento", mensaje); return View(cliente); }

            TempData["ClienteCreado"]       = "true";
            TempData["NombreClienteCreado"] = cliente.Nombre;
            TempData["Mensaje"]             = mensaje;
            return RedirectToAction("Index");
        }

        // ── EDIT ───────────────────────────────────────────────────────────
        public IActionResult Edit(string codigo)
        {
            var cliente = _clienteService.ObtenerPorCodigo(codigo);
            if (cliente == null) return NotFound();

            ViewBag.EsCajero = !EsAdministrador();
            return View(cliente);
        }

        [HttpPost]
        public IActionResult Edit(Cliente cliente)
        {
            if (!EsAdministrador())
            {
                var original = cliente.Id > 0 
                    ? _clienteService.ObtenerPorId(cliente.Id) 
                    : _clienteService.ObtenerPorCodigo(cliente.Codigo);

                if (original == null) return NotFound();
                cliente.Nombre          = original.Nombre;
                cliente.Documento       = original.Documento;
                cliente.FechaNacimiento = original.FechaNacimiento;

                ModelState.Remove("Nombre");
                ModelState.Remove("Documento");
            }
            else
            {
                cliente.Nombre = AplicarCamelCase(cliente.Nombre);
            }

            if (!ModelState.IsValid)
            {
                ViewBag.EsCajero = !EsAdministrador();
                return View(cliente);
            }

            if (EsAdministrador() && cliente.FechaNacimiento.HasValue)
            {
                var error = ValidarMayoriaEdad(cliente.FechaNacimiento.Value);
                if (error != null)
                {
                    ModelState.AddModelError("FechaNacimiento", error);
                    ViewBag.EsCajero = false;
                    return View(cliente);
                }
            }

            var (ok, mensaje) = _clienteService.Actualizar(cliente);
            if (!ok)
            {
                ModelState.AddModelError("Documento", mensaje);
                ViewBag.EsCajero = !EsAdministrador();
                return View(cliente);
            }

            TempData["Mensaje"] = mensaje;
            return RedirectToAction("Index");
        }

        // ── HELPERS ────────────────────────────────────────────────────────
        private static string AplicarCamelCase(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return texto;
            return string.Join(" ", texto.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant()));
        }

        private static string? ValidarMayoriaEdad(DateTime fechaNacimiento)
        {
            var hoy  = DateOnly.FromDateTime(DateTime.Today);
            var nac  = DateOnly.FromDateTime(fechaNacimiento);
            var edad = hoy.Year - nac.Year;
            if (nac > hoy.AddYears(-edad)) edad--;
            return edad < 18 ? "El cliente debe ser mayor de edad (18 años cumplidos)." : null;
        }
    }
}
