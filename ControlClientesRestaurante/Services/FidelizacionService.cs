using ControlClientesRestaurante.Data;
using Microsoft.EntityFrameworkCore;

namespace ControlClientesRestaurante.Services
{
    public class ClienteFidelizacion
    {
        public int ClienteId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string? Correo { get; set; }
        public int Visitas { get; set; }
        public decimal TotalGastado { get; set; }
        public decimal TicketPromedio => Visitas > 0 ? Math.Round(TotalGastado / Visitas) : 0;
        public DateTime? UltimaVisita { get; set; }
        public int? DiasSinVenir { get; set; }
        public double? FrecuenciaDias { get; set; }      // promedio de días entre visitas
        public string Segmento { get; set; } = "Sin compras"; // Activo | En riesgo | Perdido | Sin compras
        public string Nivel { get; set; } = "Bronce";         // Bronce | Plata | Oro | Platino
        public bool MasTardeDeLoHabitual { get; set; }
        public string? TelefonoWhatsApp { get; set; }
    }

    public class ResumenFidelizacion
    {
        public int DiasRiesgo { get; set; }
        public int DiasPerdido { get; set; }
        public List<ClienteFidelizacion> Clientes { get; set; } = new();
        public int Activos => Clientes.Count(c => c.Segmento == "Activo");
        public int EnRiesgo => Clientes.Count(c => c.Segmento == "En riesgo");
        public int Perdidos => Clientes.Count(c => c.Segmento == "Perdido");
        public int SinCompras => Clientes.Count(c => c.Segmento == "Sin compras");
        public int Vip => Clientes.Count(c => c.Nivel is "Oro" or "Platino");
    }

    /// <summary>
    /// Calcula la fidelización a partir del historial de facturas (no crea tablas nuevas):
    /// cuánto hace que cada cliente no viene, qué tan frecuente es y qué nivel de recompensa le corresponde.
    /// </summary>
    public class FidelizacionService
    {
        private readonly ApplicationDbContext _context;

        public FidelizacionService(ApplicationDbContext context) => _context = context;

        public static string CalcularNivel(int visitas) => visitas switch
        {
            >= 12 => "Platino",
            >= 6  => "Oro",
            >= 3  => "Plata",
            _     => "Bronce"
        };

        public ResumenFidelizacion Calcular(int diasRiesgo = 30, int diasPerdido = 60)
        {
            diasRiesgo = Math.Clamp(diasRiesgo, 1, 365);
            diasPerdido = Math.Clamp(diasPerdido, diasRiesgo + 1, 730);

            var clientes = _context.Clientes.AsNoTracking()
                .Where(c => c.Activo)
                .Select(c => new { c.Id, c.Nombre, c.Telefono, c.Correo })
                .ToList();

            var facturas = _context.Facturas.AsNoTracking()
                .Where(f => f.ClienteId != null && f.Estado != "Anulada")
                .Select(f => new { ClienteId = f.ClienteId!.Value, f.FechaCreacion, f.Total })
                .ToList()
                .GroupBy(f => f.ClienteId)
                .ToDictionary(g => g.Key, g => g.OrderBy(f => f.FechaCreacion).ToList());

            var hoy = DateTime.Now;
            var resumen = new ResumenFidelizacion { DiasRiesgo = diasRiesgo, DiasPerdido = diasPerdido };

            foreach (var c in clientes)
            {
                var item = new ClienteFidelizacion
                {
                    ClienteId = c.Id,
                    Nombre = c.Nombre,
                    Telefono = c.Telefono,
                    Correo = c.Correo,
                    TelefonoWhatsApp = NormalizarWhatsApp(c.Telefono)
                };

                if (facturas.TryGetValue(c.Id, out var lista) && lista.Count > 0)
                {
                    item.Visitas = lista.Count;
                    item.TotalGastado = lista.Sum(f => f.Total);
                    item.UltimaVisita = lista[^1].FechaCreacion;
                    item.DiasSinVenir = Math.Max(0, (int)(hoy.Date - item.UltimaVisita.Value.Date).TotalDays);
                    item.Nivel = CalcularNivel(item.Visitas);

                    if (lista.Count >= 2)
                    {
                        double span = (lista[^1].FechaCreacion - lista[0].FechaCreacion).TotalDays;
                        item.FrecuenciaDias = Math.Round(span / (lista.Count - 1), 1);
                        // Si ya tardó más del doble de lo que suele tardar, merece atención aunque aún no sea "en riesgo"
                        item.MasTardeDeLoHabitual = item.FrecuenciaDias > 0 && item.DiasSinVenir > item.FrecuenciaDias * 2;
                    }

                    item.Segmento = item.DiasSinVenir <= diasRiesgo ? "Activo"
                                  : item.DiasSinVenir <= diasPerdido ? "En riesgo"
                                  : "Perdido";
                }

                resumen.Clientes.Add(item);
            }

            return resumen;
        }

        /// <summary>Número en formato internacional para wa.me (asume Colombia si son 10 dígitos).</summary>
        private static string? NormalizarWhatsApp(string? telefono)
        {
            if (string.IsNullOrWhiteSpace(telefono)) return null;
            var digitos = new string(telefono.Where(char.IsDigit).ToArray());
            if (digitos.Length == 10 && digitos.StartsWith('3')) return "57" + digitos;
            return digitos.Length >= 10 ? digitos : null;
        }
    }
}
