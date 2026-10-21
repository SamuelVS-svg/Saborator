using ControlClientesRestaurante.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ControlClientesRestaurante.Hubs
{
    public class CocinaHub : Hub
    {
        private readonly PedidoService _pedidoService;

        public CocinaHub(PedidoService pedidoService)
        {
            _pedidoService = pedidoService;
        }

        // Grupo para estaciones de cocina / KDS (recibe nombres y pedidos de clientes: solo personal)
        [Authorize(Roles = "Cocinero,Administrador,Mesero")]
        public async Task UnirseACocina()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Cocina");
        }

        // Grupo de domiciliarios: avisos de domicilios listos para recoger o asignados
        [Authorize(Roles = "Domiciliario,Administrador")]
        public async Task UnirseARepartos()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Repartos");
        }

        // Grupo para seguimiento de un pedido específico por su ID o número
        // Anti-IDOR: los números de pedido son consecutivos; solo se puede seguir uno propio (o ser personal)
        public async Task SeguirPedido(string pedidoNumero)
        {
            if (string.IsNullOrWhiteSpace(pedidoNumero)) return;

            var http = Context.GetHttpContext();
            var pedido = _pedidoService.ObtenerPorNumero(pedidoNumero.Trim());
            if (http == null || pedido == null ||
                !AccesoRecursos.PuedeVerPedido(Context.User ?? http.User, http.Session, pedido))
                return;

            await Groups.AddToGroupAsync(Context.ConnectionId, $"Pedido_{pedido.Numero}");
        }
    }
}
