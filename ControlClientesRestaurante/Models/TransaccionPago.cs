using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlClientesRestaurante.Models
{
    public class TransaccionPago : EntidadBase
    {
        [Required]
        [StringLength(100)]
        public string ReferenciaTransaccion { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string CodigoAprobacion { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string TipoOrigen { get; set; } = "Pedido"; // Pedido | Reserva | Directo

        public int? OrigenId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999.99)]
        public decimal Monto { get; set; }

        [Required]
        [StringLength(50)]
        public string MetodoPago { get; set; } = "Tarjeta"; // Tarjeta | Nequi | Daviplata | PSE | Efectivo

        [StringLength(250)]
        public string DetalleMetodo { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string NombreCliente { get; set; } = string.Empty;

        [StringLength(150)]
        public string? Correo { get; set; }

        [StringLength(50)]
        public string? Telefono { get; set; }

        [StringLength(250)]
        public string? Concepto { get; set; }

        [StringLength(50)]
        public string? NumeroPedido { get; set; }

        [StringLength(50)]
        public string? NumeroFactura { get; set; }

        [StringLength(50)]
        public string? CodigoReserva { get; set; }

        [StringLength(50)]
        public string? Mesa { get; set; }

        [Required]
        [StringLength(50)]
        public string Estado { get; set; } = "Aprobado"; // Aprobado | Rechazado | Reversado

        [StringLength(100)]
        public string UsuarioRegistro { get; set; } = "Sistema";
    }
}
