using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlClientesRestaurante.Models
{
    public class Factura : EntidadBase
    {
        [Required]
        public string Numero { get; set; } = string.Empty;   // FAC-00001

        // Cliente opcional (puede ser venta sin cliente registrado)
        public int? ClienteId { get; set; }
        public Cliente? Cliente { get; set; }

        [StringLength(150)]
        public string? NombreClienteLibre { get; set; }   // si no hay cliente registrado

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999.99, ErrorMessage = "El subtotal no puede ser negativo.")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 100, ErrorMessage = "El porcentaje de IVA debe estar entre 0 y 100.")]
        public decimal ImpuestoPorcentaje { get; set; } = 0;   // ej. 19 para IVA 19 %

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 9999999.99, ErrorMessage = "La propina no puede ser negativa.")]
        public decimal Propina { get; set; } = 0;   // 10 % opcional

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 9999999.99, ErrorMessage = "El costo de envío no puede ser negativo.")]
        public decimal CostoEnvio { get; set; } = 0; // tarifa de despacho a domicilio

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999.99, ErrorMessage = "El descuento no puede ser negativo.")]
        public decimal Descuento { get; set; } = 0;

        [StringLength(50)]
        public string? CodigoCupon { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999.99, ErrorMessage = "El total no puede ser negativo.")]
        public decimal Total { get; set; }

        public string UsuarioCajero { get; set; } = string.Empty;

        public string Estado { get; set; } = "Emitida";   // Emitida | Anulada

        public ICollection<LineaFactura> Lineas { get; set; } = new List<LineaFactura>();
    }
}
