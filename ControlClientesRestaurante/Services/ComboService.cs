using ControlClientesRestaurante.Models;

namespace ControlClientesRestaurante.Services
{
    /// <summary>Producto que compone un combo (se busca por nombre en la carta).</summary>
    public sealed record ComboItemDef(string Producto, int Cantidad);

    /// <summary>Definición de un combo: un precio fijo por un conjunto de productos de la carta.</summary>
    public sealed record ComboDef(string Id, string Nombre, string Descripcion, string Etiqueta, decimal Precio, ComboItemDef[] Items);

    /// <summary>Un producto de un combo ya resuelto contra la carta real.</summary>
    public sealed record ComboLinea(int ProductoId, string Nombre, int Cantidad, decimal PrecioUnitario, string Imagen);

    /// <summary>Combo con sus productos reales, su precio normal y cuánto se ahorra.</summary>
    public sealed record ComboDisponible(
        string Id, string Nombre, string Descripcion, string Etiqueta,
        decimal Precio, decimal PrecioNormal, decimal Ahorro, bool HayStock, List<ComboLinea> Lineas);

    /// <summary>Resultado del cálculo de ahorro para un conjunto de líneas de pedido.</summary>
    public sealed record ResultadoCombos(decimal Descuento, List<(string Nombre, int Veces, decimal Ahorro)> Detalle);

    /// <summary>
    /// Combos de Saborator. No son productos nuevos: cada combo es un conjunto de productos de la carta con un
    /// precio especial. Quien arma un pedido con esos productos recibe el ahorro automáticamente, y el stock de
    /// cada componente se descuenta como siempre. El descuento lo calcula el servidor con los precios de la base
    /// de datos; el navegador solo lo muestra (mismo algoritmo en wwwroot/js/combos.js).
    /// </summary>
    public class ComboService
    {
        private readonly ProductoService _productos;

        public ComboService(ProductoService productos) => _productos = productos;

        // Orden de prioridad: primero los combos más grandes, para que no se "consuman" los productos en otro más pequeño.
        public static readonly ComboDef[] Definiciones =
        {
            new("familiar", "Combo Familiar", "2 hamburguesas, 1 perro caliente, 2 papas y 2 Pepsi", "Para compartir", 42000m, new[]
            {
                new ComboItemDef("Hamburguesa", 2), new ComboItemDef("Perro Caliente", 1),
                new ComboItemDef("Papas a la Francesa", 2), new ComboItemDef("Pepsi", 2),
            }),
            new("duo", "Combo Dúo", "Hamburguesa, salchipapa y 2 Coca-Cola", "Para dos", 22000m, new[]
            {
                new ComboItemDef("Hamburguesa", 1), new ComboItemDef("Salchipapa", 1), new ComboItemDef("Coca-Cola", 2),
            }),
            new("clasico", "Combo Clásico", "Hamburguesa, papas a la francesa y Coca-Cola", "El favorito", 17500m, new[]
            {
                new ComboItemDef("Hamburguesa", 1), new ComboItemDef("Papas a la Francesa", 1), new ComboItemDef("Coca-Cola", 1),
            }),
            new("perro", "Combo Perro Caliente", "Perro caliente, papas a la francesa y Pepsi", "Rápido y sabroso", 14000m, new[]
            {
                new ComboItemDef("Perro Caliente", 1), new ComboItemDef("Papas a la Francesa", 1), new ComboItemDef("Pepsi", 1),
            }),
        };

        /// <summary>Foto del producto según su nombre, sin fondo, para la portada y los combos.</summary>
        public static string ImagenPorNombre(string nombre)
        {
            var n = (nombre ?? "").ToLowerInvariant();
            if (n.Contains("perro")) return "/img/recorte/perrocaliente.webp";
            if (n.Contains("salchipapa")) return "/img/recorte/salchipapa.webp";
            if (n.Contains("papa")) return "/img/recorte/papasfrancesa.webp";
            if (n.Contains("coca")) return "/img/recorte/cocacola.webp";
            if (n.Contains("pepsi")) return "/img/recorte/pepsi.webp";
            if (n.Contains("agua")) return "/img/recorte/agua.webp";
            return "/img/recorte/hamburguesa.webp";
        }

        /// <summary>Los combos disponibles en JSON (camelCase) para que las vistas los entreguen al navegador.</summary>
        public string DisponiblesJson() => System.Text.Json.JsonSerializer.Serialize(Disponibles(),
            new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });

        /// <summary>Combos cuyos productos existen y están activos en la carta.</summary>
        public List<ComboDisponible> Disponibles() => Resolver(_productos.ObtenerTodos());

        public static List<ComboDisponible> Resolver(IEnumerable<Producto> productos)
        {
            var activos = productos.Where(p => p.Activo).ToList();
            var resultado = new List<ComboDisponible>();

            foreach (var def in Definiciones)
            {
                var lineas = new List<ComboLinea>();
                bool completo = true, hayStock = true;
                foreach (var item in def.Items)
                {
                    var prod = activos.FirstOrDefault(p => string.Equals(p.Nombre.Trim(), item.Producto, StringComparison.OrdinalIgnoreCase));
                    if (prod == null) { completo = false; break; }
                    if (prod.Stock < item.Cantidad) hayStock = false;
                    lineas.Add(new ComboLinea(prod.Id, prod.Nombre, item.Cantidad, prod.Precio, ImagenPorNombre(prod.Nombre)));
                }
                if (!completo) continue;   // si falta un producto, el combo simplemente no se ofrece

                var normal = lineas.Sum(l => l.PrecioUnitario * l.Cantidad);
                var ahorro = Math.Max(0, normal - def.Precio);
                resultado.Add(new ComboDisponible(def.Id, def.Nombre, def.Descripcion, def.Etiqueta, def.Precio, normal, ahorro, hayStock, lineas));
            }
            return resultado;
        }

        /// <summary>Ahorro por combos del pedido, calculado con los precios actuales de la base de datos.</summary>
        public ResultadoCombos CalcularDescuento(IEnumerable<(int productoId, int cantidad)> lineas) =>
            CalcularDescuento(Disponibles(), lineas);

        public static ResultadoCombos CalcularDescuento(List<ComboDisponible> combos, IEnumerable<(int productoId, int cantidad)> lineas)
        {
            var tiene = new Dictionary<int, int>();
            foreach (var (id, cant) in lineas)
                if (cant > 0) tiene[id] = (tiene.TryGetValue(id, out var actual) ? actual : 0) + cant;

            decimal total = 0;
            var detalle = new List<(string, int, decimal)>();
            foreach (var combo in combos)
            {
                if (combo.Ahorro <= 0) continue;
                int veces = combo.Lineas.Min(l => (tiene.TryGetValue(l.ProductoId, out var n) ? n : 0) / l.Cantidad);
                if (veces <= 0) continue;
                foreach (var l in combo.Lineas) tiene[l.ProductoId] -= l.Cantidad * veces;   // los productos usados no cuentan para otro combo
                total += combo.Ahorro * veces;
                detalle.Add((combo.Nombre, veces, combo.Ahorro * veces));
            }
            return new ResultadoCombos(total, detalle);
        }
    }
}
