using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ControlClientesRestaurante.Hubs
{
    public class CocinaHub : Hub
    {
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
        public async Task SeguirPedido(string pedidoNumero)
        {
            if (!string.IsNullOrWhiteSpace(pedidoNumero))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Pedido_{pedidoNumero}");
            }
        }
    }
}
