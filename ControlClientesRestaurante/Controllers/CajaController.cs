using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ControlClientesRestaurante.Models;
using ControlClientesRestaurante.Services;

namespace ControlClientesRestaurante.Controllers
{
    [Authorize(Roles = "Administrador,Cajero")]
    public class CajaController : Controller
    {
        private readonly CajaService _cajaService;
        private readonly AuditoriaService _auditoriaService;

        public CajaController(CajaService cajaService, AuditoriaService auditoriaService)
        {
            _cajaService = cajaService;
            _auditoriaService = auditoriaService;
        }

        private string UsuarioActual() => User.Identity?.Name ?? "cajero";

        [HttpGet]
        public IActionResult Index()
        {
            var usuario = UsuarioActual();
            var turnoActivo = _cajaService.ObtenerTurnoActivo(usuario);

            if (turnoActivo != null)
            {
                turnoActivo = _cajaService.CalcularResumenEnVivo(turnoActivo.Id);
            }

            ViewBag.TurnoActivo = turnoActivo;
            ViewBag.Historial = _cajaService.ObtenerHistorialTurnos();
            ViewBag.Usuario = usuario;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Abrir(decimal montoInicial, string? observaciones)
        {
            var usuario = UsuarioActual();
            var (ok, mensaje, turnoCreado) = _cajaService.AbrirTurno(usuario, montoInicial, observaciones);

            if (ok)
            {
                if (turnoCreado != null)
                {
                    _auditoriaService.Registrar(
                        usuario,
                        "Cajero",
                        "APERTURA_CAJA",
                        "Caja",
                        $"Turno {turnoCreado.CodigoTurno} aperturado por {usuario} con base inicial de ${(montoInicial).ToString("N0", ControlClientesRestaurante.Models.Formato.Co)} COP.",
                        "TurnoCaja",
                        turnoCreado.Id,
                        HttpContext.Connection.RemoteIpAddress?.ToString());
                }
                TempData["Mensaje"] = mensaje;
            }
            else TempData["Error"] = mensaje;

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Cerrar(int turnoId, decimal efectivoDeclarado, string? observacionesCierre)
        {
            var (ok, mensaje, turnoCerrado) = _cajaService.CerrarTurno(turnoId, efectivoDeclarado, observacionesCierre);

            if (ok)
            {
                if (turnoCerrado != null)
                {
                    _auditoriaService.Registrar(
                        UsuarioActual(),
                        "Cajero",
                        "CIERRE_CAJA",
                        "Caja",
                        $"Turno {turnoCerrado.CodigoTurno} cerrado por {turnoCerrado.UsuarioCajero}. Efectivo declarado: ${(efectivoDeclarado).ToString("N0", ControlClientesRestaurante.Models.Formato.Co)} COP. Diferencia/Descuadre: ${((turnoCerrado.Diferencia ?? 0)).ToString("N0", ControlClientesRestaurante.Models.Formato.Co)} COP.",
                        "TurnoCaja",
                        turnoId,
                        HttpContext.Connection.RemoteIpAddress?.ToString());
                }
                TempData["Mensaje"] = mensaje;
            }
            else TempData["Error"] = mensaje;

            return RedirectToAction("Index");
        }

        // ── EXPORTAR HISTORIAL DE ARQUEOS A EXCEL (CSV UTF-8) ───────────────
        [HttpGet]
        public IActionResult ExportarHistorialTurnosCsv()
        {
            var historial = _cajaService.ObtenerHistorialTurnos(limite: 200);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("sep=;");
            sb.AppendLine("Código Turno;Cajero;Apertura;Cierre;Estado;Base Inicial COP;Ventas Efectivo COP;Transferencias COP;Tarjetas COP;Total Recaudado COP;Efectivo Declarado COP;Diferencia / Descuadre COP;Observaciones");

            foreach (var t in historial)
            {
                string cierreStr = t.FechaCierre.HasValue ? t.FechaCierre.Value.ToString("dd/MM/yyyy HH:mm") : "Turno Abierto";
                sb.AppendLine($"\"{t.CodigoTurno}\";\"{t.UsuarioCajero}\";\"{t.FechaApertura:dd/MM/yyyy HH:mm}\";\"{cierreStr}\";\"{t.Estado}\";{t.MontoInicial:F0};{t.VentasEfectivo:F0};{t.VentasTransferencia:F0};{t.VentasTarjeta:F0};{t.TotalVentasSistema:F0};{(t.EfectivoRealDeclarado ?? 0):F0};{(t.Diferencia ?? 0):F0};\"{t.Observaciones?.Replace("\"", "'")}\"");
            }

            byte[] bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv; charset=utf-8", $"Arqueos_Caja_Saborator_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }
    }
}
