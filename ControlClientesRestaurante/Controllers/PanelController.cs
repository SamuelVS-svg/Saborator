using ControlClientesRestaurante.Models;
using ControlClientesRestaurante.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlClientesRestaurante.Controllers
{
    [Authorize]
    public class PanelController : Controller
    {
        private readonly PanelService _panel;

        public PanelController(PanelService panel) => _panel = panel;

        // Panel de inicio personalizado según el rol de quien entra
        [HttpGet("/Panel")]
        public IActionResult Index()
        {
            var rol = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (rol == Roles.Administrador) return Redirect(Roles.PaginaInicio(rol));

            var modelo = _panel.Construir(rol ?? string.Empty, User.Identity?.Name ?? string.Empty);
            if (modelo == null) return Redirect("/");
            return View(modelo);
        }
    }
}
