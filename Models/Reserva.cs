using System.ComponentModel.DataAnnotations;

namespace ControlClientesRestaurante.Models
{
    public class Reserva : EntidadBase
    {
        [Required]
        [StringLength(50)]
        public string Codigo { get; set; } = string.Empty;

        public int? ClienteId { get; set; }
        public Cliente? Cliente { get; set; }

        public int? UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(150)]
        public string NombreCliente { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono de contacto es obligatorio.")]
        [StringLength(50)]
        public string Telefono { get; set; } = string.Empty;

        [StringLength(150)]
        [EmailAddress(ErrorMessage = "Correo electrónico no válido.")]
        public string? Correo { get; set; }

        [Range(1, 30, ErrorMessage = "El número de mesa debe ser entre 1 y 30.")]
        public int NumeroMesa { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Fecha { get; set; } = DateTime.Today;

        [Required]
        [StringLength(20)]
        public string Hora { get; set; } = "19:00";

        [Range(1, 12, ErrorMessage = "El número de comensales debe ser de 1 a 12.")]
        public int NumeroPersonas { get; set; } = 2;

        [StringLength(500)]
        public string? Notas { get; set; }

        /// <summary>Confirmada | En Espera | Completada | Cancelada</summary>
        [StringLength(50)]
        public string Estado { get; set; } = "Confirmada";
    }
}
