using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ControlClientesRestaurante.Models
{
    public class Proveedor : EntidadBase
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Contacto { get; set; }   // Nombre de la persona de contacto

        [StringLength(20)]
        public string? Telefono { get; set; }

        [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
        [StringLength(100)]
        public string? Correo { get; set; }

        [StringLength(200)]
        public string? Direccion { get; set; }

        [StringLength(20)]
        public string? Nit { get; set; }        // NIT o RUT del proveedor

        [StringLength(100)]
        public string? Categoria { get; set; }  // Ej: Bebidas, Carnes, Insumos

        [StringLength(300)]
        public string? Notas { get; set; }

        public bool Activo { get; set; } = true;
    }
}
