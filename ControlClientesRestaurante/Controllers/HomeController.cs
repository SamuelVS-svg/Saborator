using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ControlClientesRestaurante.Models;

namespace ControlClientesRestaurante.Controllers
{
    public class HomeController : Controller
    {
        private readonly Services.ResenaService _resenaService;

        public HomeController(Services.ResenaService resenaService)
        {
            _resenaService = resenaService;
        }

        // Página principal pública — no requiere autenticación
        // Página principal gastronómica — Carta y experiencia del restaurante
        public IActionResult Index() => PaginaPublica("inicio");

        // Cada enlace de la barra superior abre su propia página (todas comparten la misma vista y su carrito)
        [HttpGet("/Carta")]     public IActionResult Carta()     => PaginaPublica("carta");
        [HttpGet("/Sede")]      public IActionResult Sede()      => PaginaPublica("sede");
        [HttpGet("/Resenas")]   public IActionResult Resenas()   => PaginaPublica("resenas");
        [HttpGet("/Nosotros")]  public IActionResult Nosotros()  => PaginaPublica("nosotros");
        [HttpGet("/Contacto")]  public IActionResult Contacto()  => PaginaPublica("contacto");

        private IActionResult PaginaPublica(string pagina)
        {
            // El personal va directo a su panel; los clientes sí ven la carta pública
            var rol = User.FindFirst(ClaimTypes.Role)?.Value;
            if (User.Identity?.IsAuthenticated == true && Roles.EsPersonal(rol))
            {
                return Redirect(Roles.PaginaInicio(rol));
            }
            ViewBag.Pagina = pagina;
            ViewBag.Resenas = _resenaService.ObtenerAprobadas();
            ViewBag.ResenaStats = _resenaService.ObtenerEstadisticas();
            return View("Index");
        }

        // Vista preservada de información técnica y módulos del sistema de gestión
        public IActionResult Sistema()
        {
            return View();
        }

        // Dashboard V12 — Centro de Control y Analítica Operativa
        [HttpGet("/Dashboard")]
        [HttpGet("/Home/Dashboard")]
        [Authorize(Roles = "Administrador")]
        public IActionResult Dashboard()
        {
            return View();
        }

        // Términos y Condiciones, Políticas de Domicilios, Reservas, Tratamiento de Datos (Ley 1581) y Cookies
        public IActionResult Terminos()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // Página de error 404 personalizada con diseño de alta gastronomía
        [Route("Home/NotFound")]
        public IActionResult NotFound(int? statusCode = 404)
        {
            ViewBag.StatusCode = statusCode ?? 404;
            Response.StatusCode = statusCode ?? 404;
            return View();
        }
    }
}
