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

        /// <summary>
        /// Rol con el que responde el asistente. Sale siempre de la cookie de autenticación
        /// (visitantes = Cliente). Antes un visitante anónimo podía enviar "Administrador" y
        /// obtener stock, precios y pedidos internos. Solo el administrador puede probar
        /// los perfiles de otros roles desde el selector del asistente.
        /// </summary>
        private string RolEfectivo(string? rolSolicitado)
        {
            var rolReal = User.Identity?.IsAuthenticated == true
                ? (User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "Cliente")
                : "Cliente";
            if (rolReal == "Administrador" && !string.IsNullOrWhiteSpace(rolSolicitado))
                return rolSolicitado;
            return rolReal;
        }

        [HttpGet("info")]
        public IActionResult ObtenerInfo([FromQuery] string? rolOverride)
        {
            var rolFinal = RolEfectivo(rolOverride);
            var info = _iaSvc.ObtenerInfoPersona(rolFinal);
            return Ok(new { ok = true, data = info });
        }

        [HttpPost("consultar")]
        [IgnoreAntiforgeryToken]
        public IActionResult Consultar([FromBody] IaConsultaRequest request)
        {
            if (request == null)
                return BadRequest(new { ok = false, error = "Petición inválida" });

            request.Rol = RolEfectivo(request.Rol);

            var respuesta = _iaSvc.ProcesarConsulta(request);
            return Ok(respuesta);
        }
    }
}
