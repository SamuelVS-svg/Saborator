using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlClientesRestaurante.Models
{
    public class Producto : EntidadBase
    {
        [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Descripcion { get; set; }

        [Required(ErrorMessage = "El precio es obligatorio.")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 9999999, ErrorMessage = "El precio debe ser mayor a 0.")]
        public decimal Precio { get; set; }

        [Required(ErrorMessage = "El stock es obligatorio.")]
        [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo.")]
        public int Stock { get; set; }

        public bool Activo { get; set; } = true;

        [StringLength(50)]
        public string Categoria { get; set; } = "Hamburguesas & Carnes";

        [StringLength(250)]
        public string? Alergenos { get; set; } // Ej: "Gluten, Lácteos, Huevos"

        [StringLength(500)]
        public string? Ingredientes { get; set; }

        public bool EsVegetariano { get; set; } = false;

        public bool EsLibreGluten { get; set; } = false;

        [Range(1, 120)]
        public int TiempoCoccionMinutos { get; set; } = 15;
    }
}
