using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlClientesRestaurante.Models
{
    public class Cupon : EntidadBase
    {
        [Required(ErrorMessage = "El código del cupón es obligatorio.")]
        [StringLength(50)]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La descripción de la promoción es obligatoria.")]
        [StringLength(200)]
        public string Descripcion { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string TipoDescuento { get; set; } = "Porcentaje"; // "Porcentaje" o "Fijo"

        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999.99, ErrorMessage = "El valor del descuento debe ser mayor a cero.")]
        public decimal Valor { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999.99, ErrorMessage = "El monto mínimo no puede ser negativo.")]
        public decimal MontoMinimo { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TopeMaximoDescuento { get; set; }

        public DateTime? FechaVencimiento { get; set; }

        public int UsosMaximos { get; set; } = 100;

        public int UsosActuales { get; set; } = 0;

        public bool Activo { get; set; } = true;
    }
}
