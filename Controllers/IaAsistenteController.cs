using Microsoft.AspNetCore.Mvc;
using ControlClientesRestaurante.Services;

namespace ControlClientesRestaurante.Controllers
{
    [ApiController]
    [Route("api/ia")]
    public class IaAsistenteController : ControllerBase
    {
        private readonly IaAsistenteService _iaSvc;

        public IaAsistenteController(IaAsistenteService iaSvc)
        {
            _iaSvc = iaSvc;
        }

        [HttpGet("info")]
        public IActionResult ObtenerInfo([FromQuery] string? rolOverride)
        {
            var rolAutenticado = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value 
                                 ?? HttpContext.Session.GetString("Rol");
            var rolFinal = !string.IsNullOrWhiteSpace(rolOverride) ? rolOverride : rolAutenticado;
            var info = _iaSvc.ObtenerInfoPersona(rolFinal);
            return Ok(new { ok = true, data = info });
        }

        [HttpPost("consultar")]
        [IgnoreAntiforgeryToken]
        public IActionResult Consultar([FromBody] IaConsultaRequest request)
        {
            if (request == null)
                return BadRequest(new { ok = false, error = "Petición inválida" });

            // Priorizar el rol autenticado en Claims o sesión si el usuario está logueado
            var rolAutenticado = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value 
                                 ?? HttpContext.Session.GetString("Rol");
            if (!string.IsNullOrWhiteSpace(rolAutenticado))
            {
                request.Rol = rolAutenticado;
            }

            var respuesta = _iaSvc.ProcesarConsulta(request);
            return Ok(respuesta);
        }
    }
}
