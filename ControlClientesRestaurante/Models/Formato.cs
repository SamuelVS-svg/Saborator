using System.Globalization;

namespace ControlClientesRestaurante.Models
{
    /// <summary>
    /// Formato de números colombiano para mostrar montos ($37.000).
    /// Solo afecta la presentación: el servidor sigue leyendo los formularios con cultura invariante.
    /// No depende de que el contenedor tenga instalados los datos de idioma (ICU).
    /// </summary>
    public static class Formato
    {
        public static readonly NumberFormatInfo Co = new()
        {
            NumberGroupSeparator   = ".",
            NumberDecimalSeparator = ",",
            NumberGroupSizes       = new[] { 3 }
        };
    }
}
