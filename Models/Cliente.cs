using System.ComponentModel.DataAnnotations;

namespace ControlClientesRestaurante.Models
{
    public class Cliente : EntidadBase
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
        public string Nombre { get; set; } = string.Empty;

        // Documento: se guarda con prefijo tipo opcional, ej. "CC-1234567890" o "12345678"
        // La parte numérica debe tener entre 6 y 10 dígitos
        [Required(ErrorMessage = "El documento es obligatorio.")]
        [StringLength(20, MinimumLength = 6, ErrorMessage = "El documento debe tener entre 6 y 20 caracteres.")]
        [RegularExpression(@"^(?:[A-Za-z]{2,3}-)?\d{6,10}$", ErrorMessage = "El documento debe contener entre 6 y 10 dígitos numéricos (ej. CC-12345678 o 12345678).")]
        public string Documento { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio.")]
        [StringLength(25, MinimumLength = 7, ErrorMessage = "El teléfono debe tener entre 7 y 25 caracteres.")]
        public string Telefono { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Formato de correo inválido.")]
        [StringLength(100, ErrorMessage = "El correo no puede superar los 100 caracteres.")]
        public string? Correo { get; set; }

        [StringLength(200, ErrorMessage = "La dirección no puede superar los 200 caracteres.")]
        public string? Direccion { get; set; }

        [DataType(DataType.Date)]
        public DateTime? FechaNacimiento { get; set; }

        public bool Activo { get; set; } = true;
        public string Codigo { get; set; } = Guid.NewGuid().ToString("N");
    }
}