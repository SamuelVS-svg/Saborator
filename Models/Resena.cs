using System.ComponentModel.DataAnnotations;

namespace ControlClientesRestaurante.Models
{
    public class Resena : EntidadBase
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(150, ErrorMessage = "El nombre no puede exceder 150 caracteres")]
        public string NombreCliente { get; set; } = string.Empty;

        [StringLength(150)]
        [EmailAddress(ErrorMessage = "Correo electrónico no válido")]
        public string? Correo { get; set; }

        [Range(1, 5, ErrorMessage = "La calificación debe estar entre 1 y 5 estrellas")]
        public int Calificacion { get; set; } = 5;

        [Required(ErrorMessage = "El comentario es obligatorio")]
        [StringLength(1000, MinimumLength = 5, ErrorMessage = "El comentario debe tener entre 5 y 1000 caracteres")]
        public string Comentario { get; set; } = string.Empty;

        [StringLength(150)]
        public string? PlatoRecomendado { get; set; }

        [StringLength(50)]
        public string TipoExperiencia { get; set; } = "En Restaurante";

        public bool Aprobada { get; set; } = true;

        public bool Verificado { get; set; } = true;
    }

    public class CrearResenaDto
    {
        public string NombreCliente { get; set; } = string.Empty;
        public string? Correo { get; set; }
        public int Calificacion { get; set; } = 5;
        public string Comentario { get; set; } = string.Empty;
        public string? PlatoRecomendado { get; set; }
        public string? TipoExperiencia { get; set; }
    }
}
