using ControlClientesRestaurante.Data;
using ControlClientesRestaurante.Models;
using Microsoft.EntityFrameworkCore;

namespace ControlClientesRestaurante.Services
{
    public record PanelMetrica(string Titulo, string Valor, string Detalle, string Color, string? Url = null);
    public record PanelItem(string Titulo, string Detalle, string Etiqueta, string Color, string? Url = null);
    public record PanelAccion(string Texto, string Url, bool Principal = false);

    public class PanelViewModel
    {
        public string Rol { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public string Titulo { get; set; } = string.Empty;
        public string Subtitulo { get; set; } = string.Empty;
        public List<PanelMetrica> Metricas { get; set; } = new();
        public string TituloLista { get; set; } = string.Empty;
        public string VacioLista { get; set; } = "Nada pendiente por ahora.";
        public List<PanelItem> Lista { get; set; } = new();
        public string? TituloLista2 { get; set; }
        public List<PanelItem> Lista2 { get; set; } = new();
        public List<PanelAccion> Acciones { get; set; } = new();
    }

    /// <summary>Arma el panel de inicio de cada rol del personal con lo que esa persona necesita ver al empezar.</summary>
    public class PanelService
    {
        private static readonly string[] EstadosCocina = { "Pendiente", "Abierto", "EnPreparacion", "En Preparación" };
        private static readonly string[] EstadosCerrados = { "Facturado", "Cancelado" };

        private readonly ApplicationDbContext _db;

        public PanelService(ApplicationDbContext db) => _db = db;

        private static string Cop(decimal v) => "$" + v.ToString("N0", Formato.Co);

        private static string Hace(DateTime fecha)
        {
            var min = (int)Math.Max(0, (DateTime.Now - fecha).TotalMinutes);
            return min < 60 ? $"hace {min} min" : $"hace {min / 60} h {min % 60} min";
        }

        public PanelViewModel? Construir(string rol, string usuario) => rol switch
        {
            Roles.Cajero       => Cajero(usuario),
            Roles.Mesero       => Mesero(usuario),
            Roles.Cocinero     => Cocinero(usuario),
            Roles.Domiciliario => Domiciliario(usuario),
            _                  => null
        };

        // ── CAJERO ────────────────────────────────────────────────────────
        private PanelViewModel Cajero(string usuario)
        {
            var hoy = DateTime.Today;
            var turno = _db.TurnosCaja.AsNoTracking()
                .Where(t => t.UsuarioCajero == usuario && t.Estado == "Abierto")
                .OrderByDescending(t => t.FechaApertura).FirstOrDefault();

            var facturasHoy = _db.Facturas.AsNoTracking()
                .Where(f => f.FechaCreacion >= hoy && f.Estado != "Anulada" && f.UsuarioCajero == usuario)
                .Select(f => f.Total).ToList();

            var porCobrar = _db.Pedidos.AsNoTracking()
                .Where(p => p.Estado == "Entregado" || (p.Estado == "Listo" && p.TipoEntrega != "Domicilio"))
                .OrderBy(p => p.FechaCreacion).Take(8).ToList();

            var vm = new PanelViewModel
            {
                Rol = Roles.Cajero, Usuario = usuario,
                Titulo = "Panel de Caja",
                Subtitulo = "Tu turno, tus cobros y lo que está esperando para facturarse",
                TituloLista = "Pedidos listos para cobrar",
                VacioLista = "No hay pedidos esperando cobro.",
                Metricas =
                {
                    turno == null
                        ? new PanelMetrica("Turno de caja", "Cerrado", "Abre tu turno para empezar a cobrar", "#EF4444", "/Caja/Index")
                        : new PanelMetrica("Turno de caja", "Abierto", $"Desde las {turno.FechaApertura:HH:mm} · base {Cop(turno.MontoInicial)}", "#10B981", "/Caja/Index"),
                    new PanelMetrica("Vendido por ti hoy", Cop(facturasHoy.Sum()), $"{facturasHoy.Count} facturas emitidas", "#C8973A", "/Factura/Index"),
                    new PanelMetrica("Por cobrar", porCobrar.Count.ToString(), "Pedidos sin facturar", "#F59E0B", "/Factura/Create")
                },
                Acciones =
                {
                    new PanelAccion("Nueva factura", "/Factura/Create", true),
                    new PanelAccion("Control de caja", "/Caja/Index"),
                    new PanelAccion("Historial", "/Factura/Index")
                }
            };
            if (turno != null)
                vm.Metricas.Add(new PanelMetrica("Efectivo esperado", Cop(turno.TotalEfectivoEsperado), $"Ventas del turno {Cop(turno.TotalVentasSistema)}", "#3B82F6", "/Caja/Index"));

            foreach (var p in porCobrar)
                vm.Lista.Add(new PanelItem($"{p.Numero} · {(p.TipoEntrega == "EnMesa" ? "Mesa " + p.Mesa : p.TipoEntrega)}",
                    $"{Cop(p.Total)} · {Hace(p.FechaCreacion)}", p.Estado, "#F59E0B", "/Factura/Create"));
            return vm;
        }

        // ── MESERO ────────────────────────────────────────────────────────
        private PanelViewModel Mesero(string usuario)
        {
            var hoy = DateTime.Today;
            var mios = _db.Pedidos.AsNoTracking()
                .Where(p => p.UsuarioMesero == usuario && p.FechaCreacion >= hoy && p.Estado != "Cancelado")
                .OrderBy(p => p.FechaCreacion).ToList();

            var paraServir = mios.Where(p => p.Estado == "Listo" && p.TipoEntrega != "Domicilio").ToList();
            var enCurso = mios.Where(p => !EstadosCerrados.Contains(p.Estado) && p.Estado != "Entregado").ToList();

            var mesas = _db.Mesas.AsNoTracking().ToList();
            int ocupadas = mesas.Count(m => m.Estado == "Ocupada");

            var reservas = _db.Reservas.AsNoTracking()
                .Where(r => r.Fecha >= hoy && r.Fecha < hoy.AddDays(1) && r.Estado != "Cancelada" && r.Estado != "Completada")
                .OrderBy(r => r.Hora).Take(6).ToList();

            var vm = new PanelViewModel
            {
                Rol = Roles.Mesero, Usuario = usuario,
                Titulo = "Panel de Servicio",
                Subtitulo = "Tus pedidos del día, lo que cocina ya terminó y el estado de las mesas",
                TituloLista = "Listos para llevar a la mesa",
                VacioLista = "Ningún pedido tuyo está listo todavía.",
                TituloLista2 = "Reservas de hoy",
                Metricas =
                {
                    new PanelMetrica("Listos para servir", paraServir.Count.ToString(), "Cocina ya los terminó", paraServir.Count > 0 ? "#10B981" : "#94A3B8", "/Pedido/Index"),
                    new PanelMetrica("Pedidos en curso", enCurso.Count.ToString(), "En cocina o por entregar", "#F59E0B", "/Pedido/Index"),
                    new PanelMetrica("Vendido por ti hoy", Cop(mios.Sum(p => p.Total)), $"{mios.Count} pedidos tomados", "#C8973A", "/Pedido/Index"),
                    new PanelMetrica("Mesas ocupadas", $"{ocupadas}/{mesas.Count}", $"{mesas.Count - ocupadas} libres", "#3B82F6", "/Mesa/Index")
                },
                Acciones =
                {
                    new PanelAccion("Tomar pedido", "/Pedido/Menu", true),
                    new PanelAccion("Mis pedidos", "/Pedido/Index"),
                    new PanelAccion("Mesas", "/Mesa/Index")
                }
            };
            foreach (var p in paraServir)
                vm.Lista.Add(new PanelItem($"{p.Numero} · Mesa {p.Mesa}", Hace(p.FechaCreacion), "Listo", "#10B981", "/Pedido/Index"));
            foreach (var r in reservas)
                vm.Lista2.Add(new PanelItem($"{r.Hora} · {r.NombreCliente}", $"Mesa {r.NumeroMesa}", r.Estado, "#3B82F6", "/Reserva/Admin"));
            return vm;
        }

        // ── COCINERO ──────────────────────────────────────────────────────
        private PanelViewModel Cocinero(string usuario)
        {
            var hoy = DateTime.Today;
            var cola = _db.Pedidos.AsNoTracking()
                .Include(p => p.Lineas)
                .Where(p => EstadosCocina.Contains(p.Estado))
                .OrderBy(p => p.FechaCreacion).ToList();

            int pendientes = cola.Count(p => p.Estado == "Pendiente" || p.Estado == "Abierto");
            int enPrep = cola.Count - pendientes;
            var masAntiguo = cola.FirstOrDefault();
            int listosHoy = _db.Pedidos.AsNoTracking().Count(p => p.FechaCreacion >= hoy && (p.Estado == "Listo" || p.Estado == "EnCamino" || p.Estado == "Entregado" || p.Estado == "Facturado"));

            var topPlatos = _db.LineasPedido.AsNoTracking()
                .Where(l => l.Pedido!.FechaCreacion >= hoy && l.Pedido.Estado != "Cancelado")
                .GroupBy(l => l.NombreProducto)
                .Select(g => new { Nombre = g.Key, Cantidad = g.Sum(l => l.Cantidad) })
                .OrderByDescending(x => x.Cantidad).Take(5).ToList();

            var vm = new PanelViewModel
            {
                Rol = Roles.Cocinero, Usuario = usuario,
                Titulo = "Panel de Cocina",
                Subtitulo = "La cola de comandas, qué lleva más tiempo esperando y qué se pide más hoy",
                TituloLista = "Comandas en cola (las más antiguas primero)",
                VacioLista = "No hay comandas en cola. Cocina al día.",
                TituloLista2 = "Lo más pedido hoy",
                Metricas =
                {
                    new PanelMetrica("Por empezar", pendientes.ToString(), "Comandas nuevas", pendientes > 0 ? "#EF4444" : "#94A3B8", "/Pedido/Cocina"),
                    new PanelMetrica("En preparación", enPrep.ToString(), "Cocinándose ahora", "#F59E0B", "/Pedido/Cocina"),
                    new PanelMetrica("Espera más larga", masAntiguo == null ? "—" : Hace(masAntiguo.FechaCreacion).Replace("hace ", ""), masAntiguo == null ? "Sin comandas" : masAntiguo.Numero, "#3B82F6", "/Pedido/Cocina"),
                    new PanelMetrica("Despachados hoy", listosHoy.ToString(), "Comandas terminadas", "#10B981")
                },
                Acciones = { new PanelAccion("Abrir pantalla de cocina", "/Pedido/Cocina", true) }
            };
            foreach (var p in cola.Take(8))
            {
                int platos = p.Lineas.Sum(l => l.Cantidad);
                vm.Lista.Add(new PanelItem($"{p.Numero} · {(p.TipoEntrega == "EnMesa" ? "Mesa " + p.Mesa : p.TipoEntrega)}",
                    $"{platos} platos · {Hace(p.FechaCreacion)}", p.Estado == "Abierto" ? "Pendiente" : p.Estado,
                    (DateTime.Now - p.FechaCreacion).TotalMinutes > 20 ? "#EF4444" : "#F59E0B", "/Pedido/Cocina"));
            }
            foreach (var t in topPlatos)
                vm.Lista2.Add(new PanelItem(t.Nombre, "", $"x{t.Cantidad}", "#C8973A"));
            return vm;
        }

        // ── DOMICILIARIO ──────────────────────────────────────────────────
        private PanelViewModel Domiciliario(string usuario)
        {
            var hoy = DateTime.Today;
            var domicilios = _db.Pedidos.AsNoTracking()
                .Where(p => p.TipoEntrega == "Domicilio" && p.Estado != "Cancelado")
                .ToList();

            var mios = domicilios.Where(p => string.Equals(p.Repartidor, usuario, StringComparison.OrdinalIgnoreCase)).ToList();
            var enCamino = mios.Where(p => p.Estado == "EnCamino").ToList();
            var porSalir = mios.Where(p => p.Estado == "Listo").ToList();
            var entregadosHoy = mios.Where(p => (p.Estado == "Entregado" || p.Estado == "Facturado") && (p.FechaEntrega ?? p.FechaCreacion) >= hoy).ToList();
            var disponibles = domicilios.Where(p => p.Estado == "Listo" && string.IsNullOrEmpty(p.Repartidor)).OrderBy(p => p.FechaCreacion).ToList();

            var vm = new PanelViewModel
            {
                Rol = Roles.Domiciliario, Usuario = usuario,
                Titulo = "Panel de Reparto",
                Subtitulo = "Tus entregas en curso y los domicilios que puedes tomar",
                TituloLista = "Tus entregas pendientes",
                VacioLista = "No tienes entregas pendientes.",
                TituloLista2 = "Domicilios disponibles para tomar",
                Metricas =
                {
                    new PanelMetrica("En camino", enCamino.Count.ToString(), "Los llevas ahora mismo", "#3B82F6", "/Pedido/Repartos"),
                    new PanelMetrica("Por salir", porSalir.Count.ToString(), "Asignados y listos", "#F59E0B", "/Pedido/Repartos"),
                    new PanelMetrica("Entregados hoy", entregadosHoy.Count.ToString(), $"Valor {Cop(entregadosHoy.Sum(p => p.Total))}", "#10B981"),
                    new PanelMetrica("Disponibles", disponibles.Count.ToString(), "Sin repartidor asignado", disponibles.Count > 0 ? "#C8973A" : "#94A3B8", "/Pedido/Repartos")
                },
                Acciones = { new PanelAccion("Ir a mis repartos", "/Pedido/Repartos", true) }
            };
            foreach (var p in enCamino.Concat(porSalir))
                vm.Lista.Add(new PanelItem($"{p.Numero} · {p.Barrio ?? "Sin barrio"}", $"{p.Direccion} · {Cop(p.Total)}", p.Estado == "EnCamino" ? "En camino" : "Por salir", p.Estado == "EnCamino" ? "#3B82F6" : "#F59E0B", "/Pedido/Repartos"));
            foreach (var p in disponibles.Take(6))
                vm.Lista2.Add(new PanelItem($"{p.Numero} · {p.Barrio ?? "Sin barrio"}", $"{p.Direccion} · {Hace(p.FechaCreacion)}", "Disponible", "#C8973A", "/Pedido/Repartos"));
            return vm;
        }
    }
}
