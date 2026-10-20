using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlClientesRestaurante.Models
{
    public class Pedido : EntidadBase
    {
        [Required]
        public string Numero { get; set; } = string.Empty;

        [StringLength(50)]
        public string Mesa { get; set; } = string.Empty;

        [StringLength(100)]
        public string? NombreCliente { get; set; }

        public string UsuarioMesero { get; set; } = string.Empty;

        public string Estado { get; set; } = "Pendiente";

        [StringLength(300)]
        public string? Notas { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Descuento { get; set; } = 0;

        [StringLength(50)]
        public string? CodigoCupon { get; set; }

        // Campos de Domicilios y Envíos (KFC Delivery System)
        [StringLength(50)]
        public string TipoEntrega { get; set; } = "EnMesa"; // Domicilio, ParaLlevar, EnMesa

        [StringLength(250)]
        public string? Direccion { get; set; }

        [StringLength(100)]
        public string? Barrio { get; set; }

        [StringLength(50)]
        public string? Telefono { get; set; }

        [StringLength(50)]
        public string MetodoPago { get; set; } = "Efectivo"; // Efectivo, Nequi, Tarjeta

        [Column(TypeName = "decimal(18,2)")]
        public decimal CostoEnvio { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? PagaCon { get; set; }

        // Domiciliario (usuario con rol "Domiciliario") que lleva el pedido
        [StringLength(100)]
        public string? Repartidor { get; set; }

        public DateTime? FechaDespacho { get; set; }

        public DateTime? FechaEntrega { get; set; }

        public int? FacturaId { get; set; }
        public Factura? Factura { get; set; }

        public ICollection<LineaPedido> Lineas { get; set; } = new List<LineaPedido>();
    }
}
