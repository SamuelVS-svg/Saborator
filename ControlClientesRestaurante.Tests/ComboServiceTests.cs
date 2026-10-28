using ControlClientesRestaurante.Models;
using ControlClientesRestaurante.Services;
using Xunit;

namespace ControlClientesRestaurante.Tests
{
    /// <summary>Los combos son precios especiales para conjuntos de productos de la carta; el ahorro lo calcula el servidor.</summary>
    public class ComboServiceTests
    {
        // Carta de prueba con los mismos nombres y precios que la real
        private static List<Producto> Carta(int stock = 50) => new()
        {
            new Producto { Id = 1, Nombre = "Perro Caliente",       Precio = 8500m,  Stock = stock },
            new Producto { Id = 2, Nombre = "Hamburguesa",          Precio = 12000m, Stock = stock },
            new Producto { Id = 3, Nombre = "Salchipapa",           Precio = 7000m,  Stock = stock },
            new Producto { Id = 4, Nombre = "Papas a la Francesa",  Precio = 4500m,  Stock = stock },
            new Producto { Id = 5, Nombre = "Agua",                 Precio = 2000m,  Stock = stock },
            new Producto { Id = 6, Nombre = "Coca-Cola",            Precio = 3000m,  Stock = stock },
            new Producto { Id = 7, Nombre = "Pepsi",                Precio = 2800m,  Stock = stock },
        };

        private static decimal Ahorro(List<ComboDisponible> combos, params (int id, int cant)[] lineas) =>
            ComboService.CalcularDescuento(combos, lineas).Descuento;

        [Fact]
        public void Resolver_ArmaLosCuatroCombosConPrecioNormalYAhorro()
        {
            var combos = ComboService.Resolver(Carta());

            Assert.Equal(new[] { "familiar", "duo", "clasico", "perro" }, combos.Select(c => c.Id));
            var clasico = combos.Single(c => c.Id == "clasico");
            Assert.Equal(19500m, clasico.PrecioNormal);   // 12.000 + 4.500 + 3.000
            Assert.Equal(17500m, clasico.Precio);
            Assert.Equal(2000m, clasico.Ahorro);
            Assert.All(combos, c => Assert.True(c.Ahorro > 0));
        }

        [Fact]
        public void Resolver_NoOfreceUnComboSiFaltaUnProductoOEstaInactivo()
        {
            var sinPepsi = Carta().Where(p => p.Nombre != "Pepsi").ToList();
            var ids = ComboService.Resolver(sinPepsi).Select(c => c.Id).ToList();
            Assert.DoesNotContain("perro", ids);
            Assert.DoesNotContain("familiar", ids);
            Assert.Contains("clasico", ids);

            var inactiva = Carta();
            inactiva.Single(p => p.Nombre == "Hamburguesa").Activo = false;
            var idsInactiva = ComboService.Resolver(inactiva).Select(c => c.Id).ToList();
            Assert.DoesNotContain("clasico", idsInactiva);
            Assert.DoesNotContain("duo", idsInactiva);
            Assert.DoesNotContain("familiar", idsInactiva);
        }

        [Fact]
        public void Resolver_MarcaElComboSinStockSuficiente()
        {
            var carta = Carta();
            carta.Single(p => p.Nombre == "Coca-Cola").Stock = 1;   // el Dúo necesita 2
            var combos = ComboService.Resolver(carta);

            Assert.False(combos.Single(c => c.Id == "duo").HayStock);
            Assert.True(combos.Single(c => c.Id == "clasico").HayStock);
        }

        [Fact]
        public void Descuento_UnComboCompletoAhorraLaDiferencia()
        {
            var combos = ComboService.Resolver(Carta());
            Assert.Equal(2000m, Ahorro(combos, (2, 1), (4, 1), (6, 1)));     // Combo Clásico
            Assert.Equal(1800m, Ahorro(combos, (1, 1), (4, 1), (7, 1)));     // Combo Perro Caliente
        }

        [Fact]
        public void Descuento_SinTodosLosProductosNoHayAhorro()
        {
            var combos = ComboService.Resolver(Carta());
            Assert.Equal(0m, Ahorro(combos, (2, 1), (4, 1)));                // faltan las bebidas
            Assert.Equal(0m, Ahorro(combos, (2, 5)));                        // solo hamburguesas
            Assert.Equal(0m, Ahorro(combos));
        }

        [Fact]
        public void Descuento_SeRepiteSegunLaCantidadDeCombos()
        {
            var combos = ComboService.Resolver(Carta());
            Assert.Equal(4000m, Ahorro(combos, (2, 2), (4, 2), (6, 2)));     // dos Combos Clásicos
            Assert.Equal(2000m, Ahorro(combos, (2, 2), (4, 1), (6, 1)));     // la 2.ª hamburguesa sobra
        }

        [Fact]
        public void Descuento_UnProductoNoSeUsaEnDosCombos()
        {
            var combos = ComboService.Resolver(Carta());
            // Un Clásico (hamburguesa+papas+coca) y un Perro (perro+papas+pepsi) necesitan 2 papas: con 1 sola solo cabe uno.
            Assert.Equal(2000m, Ahorro(combos, (2, 1), (1, 1), (4, 1), (6, 1), (7, 1)));
            // Con 2 papas caben los dos combos.
            Assert.Equal(3800m, Ahorro(combos, (2, 1), (1, 1), (4, 2), (6, 1), (7, 1)));
        }

        [Fact]
        public void Descuento_ElComboMasGrandeTienePrioridad()
        {
            var combos = ComboService.Resolver(Carta());
            // Combo Familiar: 2 hamburguesas, 1 perro, 2 papas, 2 Pepsi → ahorra 5.100 (y no dos Clásicos ni otros)
            Assert.Equal(5100m, Ahorro(combos, (2, 2), (1, 1), (4, 2), (7, 2)));
        }

        [Fact]
        public void Descuento_UnaMismaLineaRepetidaSeSuma()
        {
            var combos = ComboService.Resolver(Carta());
            Assert.Equal(2000m, Ahorro(combos, (2, 1), (4, 1), (6, 1), (2, 0)));
            Assert.Equal(2000m, Ahorro(combos, (2, 1), (2, 0), (4, 1), (6, 1)));
        }

        [Fact]
        public void Descuento_SiElPrecioDelComboSuperaAlNormalNoHayAhorroNegativo()
        {
            var carta = Carta();
            carta.Single(p => p.Nombre == "Hamburguesa").Precio = 100m;      // la suma queda por debajo del precio del combo
            var combos = ComboService.Resolver(carta);
            Assert.Equal(0m, combos.Single(c => c.Id == "clasico").Ahorro);
            Assert.Equal(0m, Ahorro(combos, (2, 1), (4, 1), (6, 1)));
        }
    }
}
