using System.ComponentModel.DataAnnotations;

namespace ControlClientesRestaurante.Models
{
    public class Mesa : EntidadBase
    {
        /// <summary>Número de mesa: 1-30</summary>
        public int Numero { get; set; }

        /// <summary>Libre | Ocupada</summary>
        public string Estado { get; set; } = "Libre";

        /// <summary>Pedido activo asignado a esta mesa (null si libre)</summary>
        public int? PedidoId { get; set; }
    }
}
