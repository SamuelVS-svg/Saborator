using System.Security.Claims;
using ControlClientesRestaurante.Models;

namespace ControlClientesRestaurante.Services
{
    /// <summary>
    /// Reglas centrales de propiedad de recursos (anti-IDOR): quién puede ver un pedido o una reserva.
    /// Se usa desde controladores y desde el hub de SignalR para que todos apliquen la misma regla.
    /// </summary>
    public static class AccesoRecursos
    {
        /// <summary>Clave de sesión con los números de pedido hechos sin cuenta desde este navegador.</summary>
        public const string SesionPedidosOnline = "PedidosOnline";

        /// <summary>Registra en la sesión que este navegador creó el pedido (pedidos sin cuenta).</summary>
        public static void RegistrarPedidoEnSesion(ISession session, string numeroPedido)
        {
            var previos = session.GetString(SesionPedidosOnline);
            session.SetString(SesionPedidosOnline,
                string.IsNullOrEmpty(previos) ? numeroPedido : $"{previos},{numeroPedido}");
        }

        /// <summary>
        /// ¿Puede el visitante ver los datos del pedido? Personal de sala/caja, el domiciliario asignado,
        /// el usuario que lo creó o el mismo navegador que lo hizo sin cuenta.
        /// </summary>
        public static bool PuedeVerPedido(ClaimsPrincipal user, ISession session, Pedido pedido)
        {
            bool autenticado = user.Identity?.IsAuthenticated == true;
            string nombre = autenticado ? (user.Identity?.Name ?? "") : "";

            if (user.IsInRole(Roles.Administrador) || user.IsInRole(Roles.Cajero) ||
                user.IsInRole(Roles.Mesero) || user.IsInRole(Roles.Cocinero))
                return true;

            if (user.IsInRole(Roles.Domiciliario))
                return string.Equals(pedido.Repartidor, nombre, StringComparison.OrdinalIgnoreCase);

            if (autenticado && !string.IsNullOrEmpty(nombre) && pedido.UsuarioMesero == nombre)
                return true;

            var propios = (session.GetString(SesionPedidosOnline) ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries);
            return propios.Contains(pedido.Numero);
        }

        /// <summary>¿Puede el visitante ver la reserva? Solo el administrador o quien la hizo con su cuenta.</summary>
        public static bool PuedeVerReserva(ClaimsPrincipal user, Reserva reserva)
        {
            if (user.Identity?.IsAuthenticated != true) return false;
            if (user.IsInRole(Roles.Administrador)) return true;

            var nombre = user.Identity?.Name;
            return !string.IsNullOrEmpty(nombre) &&
                   string.Equals(reserva.Usuario?.NombreUsuario, nombre, StringComparison.OrdinalIgnoreCase);
        }
    }
}
