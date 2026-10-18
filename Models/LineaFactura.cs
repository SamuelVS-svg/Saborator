using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlClientesRestaurante.Models
{
    public class LineaFactura : EntidadBase
    {
        [Required]
        public int FacturaId { get; set; }
        public Factura? Factura { get; set; }

        [Required]
        public int ProductoId { get; set; }
        public Producto? Producto { get; set; }

        [StringLength(100)]
        public string NombreProducto { get; set; } = string.Empty;   // snapshot

        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 9999999.99, ErrorMessage = "El precio unitario debe ser mayor a 0.")]
        public decimal PrecioUnitario { get; set; }

        [Range(1, 999, ErrorMessage = "La cantidad debe estar entre 1 y 999.")]
        public int Cantidad { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal => PrecioUnitario * Cantidad;
    }
}
