namespace ControlClientesRestaurante.Models
{
    /// <summary>Nombres de rol del sistema y página de inicio de cada uno.</summary>
    public static class Roles
    {
        public const string Administrador = "Administrador";
        public const string Cajero        = "Cajero";
        public const string Mesero        = "Mesero";
        public const string Cocinero      = "Cocinero";
        public const string Domiciliario  = "Domiciliario";
        public const string Cliente       = "Cliente";

        /// <summary>Roles del personal interno (todos excepto Cliente).</summary>
        public static readonly string[] Personal = { Administrador, Cajero, Mesero, Cocinero, Domiciliario };

        public static bool EsPersonal(string? rol) => !string.IsNullOrEmpty(rol) && Personal.Contains(rol);

        /// <summary>Ruta a la que se envía a cada rol tras iniciar sesión.</summary>
        public static string PaginaInicio(string? rol) => rol switch
        {
            Administrador => "/Home/Dashboard",
            Cajero        => "/Caja/Index",
            Mesero        => "/Pedido/Menu",
            Cocinero      => "/Pedido/Cocina",
            Domiciliario  => "/Pedido/Repartos",
            _             => "/#menu"
        };
    }
}
