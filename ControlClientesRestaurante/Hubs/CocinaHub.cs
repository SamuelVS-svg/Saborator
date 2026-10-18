using Microsoft.AspNetCore.SignalR;

namespace ControlClientesRestaurante.Hubs
{
    public class CocinaHub : Hub
    {
        // Grupo para estaciones de cocina / KDS
        public async Task UnirseACocina()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Cocina");
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
