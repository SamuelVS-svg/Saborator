using System.ComponentModel.DataAnnotations;

namespace ControlClientesRestaurante.Models
{
    public class Usuario : EntidadBase
    {
        [Required]
        public string NombreUsuario { get; set; } = string.Empty;

        public string ContraseñaHash { get; set; } = string.Empty;

        public int RolId { get; set; }
        public Rol? Rol { get; set; }

        public bool Estado { get; set; } = true;

        public bool DebeCambiarContraseña { get; set; } = false;

        public string? FotoPerfil { get; set; }

        public string? Tema { get; set; } = "clasico";

        [StringLength(150)]
        public string? Correo { get; set; }

        public bool EmailVerificado { get; set; } = false;

        [StringLength(100)]
        public string? TokenVerificacionEmail { get; set; }

        public DateTime? FechaExpiracionToken { get; set; }
    }
}