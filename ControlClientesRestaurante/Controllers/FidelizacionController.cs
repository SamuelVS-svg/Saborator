using ControlClientesRestaurante.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlClientesRestaurante.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class FidelizacionController : Controller
    {
        private readonly FidelizacionService _fidelizacion;

        public FidelizacionController(FidelizacionService fidelizacion) => _fidelizacion = fidelizacion;

        public IActionResult Index(string? segmento, string? nivel, int diasRiesgo = 30, int diasPerdido = 60)
        {
            var resumen = _fidelizacion.Calcular(diasRiesgo, diasPerdido);
            ViewBag.Segmento = segmento;
            ViewBag.Nivel = nivel;
            return View(resumen);
        }
    }
}
