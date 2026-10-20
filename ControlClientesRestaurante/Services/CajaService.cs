using Microsoft.EntityFrameworkCore;
using ControlClientesRestaurante.Data;
using ControlClientesRestaurante.Models;

namespace ControlClientesRestaurante.Services
{
    public class CajaService
    {
        private readonly ApplicationDbContext _context;

        public CajaService(ApplicationDbContext context)
        {
            _context = context;
        }

        public TurnoCaja? ObtenerTurnoActivo(string? usuario = null)
        {
            var query = _context.TurnosCaja.Where(t => t.Estado == "Abierto");
            if (!string.IsNullOrWhiteSpace(usuario))
            {
                query = query.Where(t => t.UsuarioCajero == usuario);
            }
            return query.OrderByDescending(t => t.FechaApertura).FirstOrDefault();
        }

        public List<TurnoCaja> ObtenerHistorialTurnos(int limite = 30)
        {
            return _context.TurnosCaja
                .OrderByDescending(t => t.FechaApertura)
                .Take(limite)
                .ToList();
        }

        public (bool ok, string mensaje, TurnoCaja? turno) AbrirTurno(string usuario, decimal montoInicial, string? observaciones)
        {
            if (montoInicial < 0)
                return (false, "El monto base inicial no puede ser negativo.", null);

            var turnoExistente = ObtenerTurnoActivo(usuario);
            if (turnoExistente != null)
                return (false, $"Ya tienes un turno activo abierto ({turnoExistente.CodigoTurno}). Debes cerrarlo antes de iniciar otro.", turnoExistente);

            var codigo = "TURNO-" + DateTime.Now.ToString("yyyyMMdd") + "-" + (_context.TurnosCaja.Count(t => t.FechaApertura.Date == DateTime.Today) + 1).ToString("D2");

            var nuevoTurno = new TurnoCaja
            {
                CodigoTurno = codigo,
                UsuarioCajero = usuario,
                FechaApertura = DateTime.Now,
                MontoInicial = montoInicial,
                Estado = "Abierto",
                Observaciones = observaciones
            };

            _context.TurnosCaja.Add(nuevoTurno);
            _context.SaveChanges();

            return (true, $"Turno {codigo} abierto exitosamente con base de ${(montoInicial).ToString("N0", ControlClientesRestaurante.Models.Formato.Co)} COP.", nuevoTurno);
        }

        public (bool ok, string mensaje, TurnoCaja? turno) CerrarTurno(int turnoId, decimal efectivoDeclarado, string? observacionesCierre)
        {
            var turno = _context.TurnosCaja.FirstOrDefault(t => t.Id == turnoId);
            if (turno == null)
                return (false, "Turno de caja no encontrado.", null);

            if (turno.Estado == "Cerrado")
                return (false, "Este turno ya se encuentra cerrado.", turno);

            // Calcular ventas del periodo desde transacciones aprobadas
            var transacciones = _context.TransaccionesPago
                .Where(t => t.FechaCreacion >= turno.FechaApertura && t.Estado == "Aprobado")
                .ToList();

            turno.VentasEfectivo = transacciones.Where(t => t.MetodoPago == "Efectivo").Sum(t => t.Monto);
            turno.VentasTarjeta = transacciones.Where(t => t.MetodoPago == "Tarjeta").Sum(t => t.Monto);
            turno.VentasTransferencia = transacciones.Where(t => t.MetodoPago == "Nequi" || t.MetodoPago == "Daviplata" || t.MetodoPago == "PSE").Sum(t => t.Monto);

            turno.EfectivoRealDeclarado = efectivoDeclarado;
            turno.FechaCierre = DateTime.Now;
            turno.Estado = "Cerrado";
            if (!string.IsNullOrWhiteSpace(observacionesCierre))
            {
                turno.Observaciones = (turno.Observaciones != null ? turno.Observaciones + " | " : "") + observacionesCierre;
            }

            _context.SaveChanges();

            var dif = turno.Diferencia ?? 0;
            string balance = dif == 0 ? "Cuadre exacto" : (dif > 0 ? $"Sobrante de ${(dif).ToString("N0", ControlClientesRestaurante.Models.Formato.Co)} COP" : $"Faltante de ${(Math.Abs(dif)).ToString("N0", ControlClientesRestaurante.Models.Formato.Co)} COP");

            return (true, $"Turno {turno.CodigoTurno} cerrado exitosamente. Balance: {balance}.", turno);
        }

        public TurnoCaja CalcularResumenEnVivo(int turnoId)
        {
            var turno = _context.TurnosCaja.FirstOrDefault(t => t.Id == turnoId);
            if (turno == null) return new TurnoCaja();

            if (turno.Estado == "Abierto")
            {
                var transacciones = _context.TransaccionesPago
                    .Where(t => t.FechaCreacion >= turno.FechaApertura && t.Estado == "Aprobado")
                    .ToList();

                turno.VentasEfectivo = transacciones.Where(t => t.MetodoPago == "Efectivo").Sum(t => t.Monto);
                turno.VentasTarjeta = transacciones.Where(t => t.MetodoPago == "Tarjeta").Sum(t => t.Monto);
                turno.VentasTransferencia = transacciones.Where(t => t.MetodoPago == "Nequi" || t.MetodoPago == "Daviplata" || t.MetodoPago == "PSE").Sum(t => t.Monto);
            }

            return turno;
        }
    }
}
