using System.ComponentModel.DataAnnotations;

namespace ControlClientesRestaurante.Models
{
    public class AuditoriaLog : EntidadBase
    {
        [Required]
        [StringLength(100)]
        public string Usuario { get; set; } = "Sistema";

        [StringLength(50)]
        public string? Rol { get; set; }

        [Required]
        [StringLength(50)]
        public string Accion { get; set; } = string.Empty; // ANULACION, CIERRE_CAJA, APERTURA_CAJA, MODIFICACION_PRECIO, CREACION_PRODUCTO, LOGIN_EXITOSO, LOGIN_FALLIDO

        [Required]
        [StringLength(50)]
        public string Modulo { get; set; } = string.Empty; // Pedidos, Facturas, Caja, Inventario, Seguridad, Cupones, Reservas

        [Required]
        [StringLength(1000)]
        public string Detalle { get; set; } = string.Empty;

        [StringLength(50)]
        public string? EntidadAfectada { get; set; } // Pedido, Factura, TurnoCaja, Producto, Usuario

        public int? EntidadId { get; set; }

        [StringLength(100)]
        public string? IpAddress { get; set; }
    }
}
