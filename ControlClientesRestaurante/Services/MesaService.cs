using ControlClientesRestaurante.Data;
using ControlClientesRestaurante.Models;
using ControlClientesRestaurante.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ControlClientesRestaurante.Services
{
    public class MesaDetalleDto
    {
        public Mesa Mesa { get; set; } = null!;
        public Pedido? PedidoActivo { get; set; }
        public Reserva? ReservaActiva { get; set; }
    }

    public class MesaService
    {
        private readonly IMesaRepository _repo;
        private readonly ApplicationDbContext _db;

        public MesaService(IMesaRepository repo, ApplicationDbContext db)
        {
            _repo = repo;
            _db = db;
        }

        public List<Mesa> ObtenerTodas() => _repo.ObtenerTodas();

        public Mesa? ObtenerPorNumero(int numero) => _repo.ObtenerPorNumero(numero);

        /// <summary>
        /// Cambia el estado de una mesa (Libre, Ocupada, Reservada, En Limpieza, Mantenimiento).
        /// </summary>
        public (bool ok, string mensaje) CambiarEstado(int numeroMesa, string nuevoEstado)
        {
            var estadosValidos = new[] { "Libre", "Ocupada", "Reservada", "En Limpieza", "Mantenimiento" };
            if (!estadosValidos.Contains(nuevoEstado))
                return (false, $"Estado no reconocido: '{nuevoEstado}'.");

            var mesa = _repo.ObtenerPorNumero(numeroMesa);
            if (mesa == null)
                return (false, $"La mesa #{numeroMesa} no fue encontrada.");

            // No liberar una mesa que aún tiene un pedido sin cobrar: se perdería el vínculo
            // entre la mesa y la cuenta pendiente.
            if (mesa.PedidoId.HasValue && (nuevoEstado == "Libre" || nuevoEstado == "En Limpieza" || nuevoEstado == "Mantenimiento"))
            {
                var pedido = _db.Pedidos.FirstOrDefault(p => p.Id == mesa.PedidoId.Value);
                if (pedido != null && pedido.Estado != "Facturado" && pedido.Estado != "Cancelado")
                    return (false, $"La mesa #{numeroMesa} tiene el pedido {pedido.Numero} sin cobrar. Cóbralo o cancélalo antes de liberarla.");
            }

            var estadoAnterior = mesa.Estado;
            mesa.Estado = nuevoEstado;

            // Si se libera, se pone en limpieza o mantenimiento, se desvincula el pedido actual
            if (nuevoEstado == "Libre" || nuevoEstado == "En Limpieza" || nuevoEstado == "Mantenimiento")
            {
                mesa.PedidoId = null;
            }

            _repo.Actualizar(mesa);
            _repo.GuardarCambios();

            return (true, $"Mesa #{numeroMesa}: estado cambiado de '{estadoAnterior}' a '{nuevoEstado}'.");
        }

        /// <summary>
        /// Intenta ocupar la mesa indicada.
        /// Retorna false si ya está ocupada.
        /// </summary>
        public (bool ok, string mensaje) OcuparMesa(int numeroMesa, int pedidoId)
        {
            var mesa = _repo.ObtenerPorNumero(numeroMesa);
            if (mesa == null)
                return (false, $"La mesa {numeroMesa} no existe.");

            if (mesa.Estado == "Ocupada")
                return (false, $"La mesa {numeroMesa} ya está ocupada.");

            mesa.Estado   = "Ocupada";
            mesa.PedidoId = pedidoId;
            _repo.Actualizar(mesa);
            _repo.GuardarCambios();
            return (true, $"Mesa {numeroMesa} ocupada.");
        }

        /// <summary>
        /// Libera la mesa vinculada al pedido indicado.
        /// </summary>
        public void LiberarMesa(int numeroMesa)
        {
            var mesa = _repo.ObtenerPorNumero(numeroMesa);
            if (mesa == null) return;
            mesa.Estado   = "Libre";
            mesa.PedidoId = null;
            _repo.Actualizar(mesa);
            _repo.GuardarCambios();
        }

        public int ContarPorEstado(string estado) => _repo.ObtenerTodas().Count(m => m.Estado == estado);
        public int MesasLibres()      => _repo.ObtenerTodas().Count(m => m.Estado == "Libre");
        public int MesasOcupadas()    => _repo.ObtenerTodas().Count(m => m.Estado == "Ocupada");
        public int MesasReservadas()  => _repo.ObtenerTodas().Count(m => m.Estado == "Reservada");
        public int MesasEnLimpieza()  => _repo.ObtenerTodas().Count(m => m.Estado == "En Limpieza");

        /// <summary>
        /// Devuelve la lista completa de mesas con sus detalles operativos:
        /// pedido activo (si está ocupada) y reservas programadas del día.
        /// </summary>
        public List<MesaDetalleDto> ObtenerDetalleMesas()
        {
            var mesas = _repo.ObtenerTodas();

            var pedidosActivos = _db.Pedidos
                .Include(p => p.Lineas)
                .Where(p => p.Estado != "Completado" && p.Estado != "Cancelado")
                .ToList();

            var hoy = DateTime.Today;
            var reservasHoy = _db.Reservas
                .Where(r => r.Fecha.Date == hoy && r.Estado != "Cancelada")
                .ToList();

            var resultado = new List<MesaDetalleDto>();

            foreach (var m in mesas)
            {
                // Buscar pedido activo vinculado por ID o por texto de mesa
                Pedido? pedido = null;
                if (m.PedidoId.HasValue)
                {
                    pedido = pedidosActivos.FirstOrDefault(p => p.Id == m.PedidoId.Value);
                }
                if (pedido == null)
                {
                    pedido = pedidosActivos.FirstOrDefault(p => p.Mesa == m.Numero.ToString());
                }

                // Buscar reserva del día asignada a esta mesa
                var reserva = reservasHoy.FirstOrDefault(r => r.NumeroMesa == m.Numero);

                resultado.Add(new MesaDetalleDto
                {
                    Mesa = m,
                    PedidoActivo = pedido,
                    ReservaActiva = reserva
                });
            }

            return resultado;
        }
    }
}
