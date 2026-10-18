namespace ControlClientesRestaurante.Models
{
    public class ResultadoLogin
    {
        public bool Exitoso { get; }
        public string Mensaje { get; }
        public string? Rol { get; }
        public Usuario? Usuario { get; }
        public bool RequiereVerificacionEmail { get; }

        public ResultadoLogin(bool exitoso, string mensaje, string? rol = null, Usuario? usuario = null, bool requiereVerificacionEmail = false)
        {
            Exitoso                    = exitoso;
            Mensaje                    = mensaje;
            Rol                        = rol;
            Usuario                    = usuario;
            RequiereVerificacionEmail  = requiereVerificacionEmail;
        }
    }
}