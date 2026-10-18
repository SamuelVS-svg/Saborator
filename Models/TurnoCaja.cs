using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlClientesRestaurante.Models
{
    public class TurnoCaja : EntidadBase
    {
        [Required]
        [StringLength(50)]
        public string CodigoTurno { get; set; } = string.Empty; // TURNO-20261009-01

        [Required]
        [StringLength(100)]
        public string UsuarioCajero { get; set; } = string.Empty;

        public DateTime FechaApertura { get; set; } = DateTime.Now;

        public DateTime? FechaCierre { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999.99)]
        public decimal MontoInicial { get; set; } // Base de efectivo en caja

        [Column(TypeName = "decimal(18,2)")]
        public decimal VentasEfectivo { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal VentasTransferencia { get; set; } = 0; // Nequi / Daviplata / PSE

        [Column(TypeName = "decimal(18,2)")]
        public decimal VentasTarjeta { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalVentasSistema => VentasEfectivo + VentasTransferencia + VentasTarjeta;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalEfectivoEsperado => MontoInicial + VentasEfectivo;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? EfectivoRealDeclarado { get; set; } // Conteo físico al cierre

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Diferencia => EfectivoRealDeclarado.HasValue ? (EfectivoRealDeclarado.Value - TotalEfectivoEsperado) : null;

        [Required]
        [StringLength(30)]
        public string Estado { get; set; } = "Abierto"; // Abierto | Cerrado

        [StringLength(500)]
        public string? Observaciones { get; set; }
    }
}
