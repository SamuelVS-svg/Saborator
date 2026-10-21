using Microsoft.AspNetCore.Mvc;
using ControlClientesRestaurante.Models;
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
        /// Rol con el que se atiende la consulta: el de la cookie autenticada. Solo el administrador
        /// puede pedir otro rol (selector de pruebas); cualquier otro visitante se atiende como Cliente.
        /// </summary>
        private string RolPermitido(string? rolSolicitado)
        {
            var rolAutenticado = User.Identity?.IsAuthenticated == true
                ? (User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "")
                : "";

            if (rolAutenticado == Roles.Administrador &&
                !string.IsNullOrWhiteSpace(rolSolicitado) &&
                (Roles.Personal.Contains(rolSolicitado) || rolSolicitado == Roles.Cliente))
                return rolSolicitado;

            return string.IsNullOrEmpty(rolAutenticado) ? Roles.Cliente : rolAutenticado;
        }

        [HttpGet("info")]
        public IActionResult ObtenerInfo([FromQuery] string? rolOverride)
        {
            var rolFinal = RolPermitido(rolOverride);
            var info = _iaSvc.ObtenerInfoPersona(rolFinal);
            return Ok(new { ok = true, data = info });
        }

        [HttpPost("consultar")]
        [IgnoreAntiforgeryToken]
        [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("publico")]
        public IActionResult Consultar([FromBody] IaConsultaRequest request)
        {
            if (request == null)
                return BadRequest(new { ok = false, error = "Petición inválida" });

            // El rol nunca se toma del cliente: sin sesión se atiende como "Cliente"
            request.Rol = RolPermitido(request.Rol);

            var respuesta = _iaSvc.ProcesarConsulta(request);
            return Ok(respuesta);
        }
    }
}
