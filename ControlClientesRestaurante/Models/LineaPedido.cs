using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlClientesRestaurante.Models
{
    public class LineaPedido : EntidadBase
    {
        [Required]
        public int PedidoId { get; set; }
        public Pedido? Pedido { get; set; }

        [Required]
        public int ProductoId { get; set; }
        public Producto? Producto { get; set; }

        /// <summary>Snapshot del nombre al crear el pedido</summary>
        [StringLength(100)]
        public string NombreProducto { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecioUnitario { get; set; }

        [Range(1, 99, ErrorMessage = "La cantidad debe ser entre 1 y 99.")]
        public int Cantidad { get; set; }

        public string? Notas { get; set; }   // ej: "sin cebolla"

        [NotMapped]
        public decimal Subtotal => PrecioUnitario * Cantidad;
    }
}
