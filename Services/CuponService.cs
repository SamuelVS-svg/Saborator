using ControlClientesRestaurante.Data;
using ControlClientesRestaurante.Models;
using Microsoft.EntityFrameworkCore;

namespace ControlClientesRestaurante.Services
{
    public class CuponService
    {
        private readonly ApplicationDbContext _context;

        public CuponService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Cupon> ObtenerTodos() =>
            _context.Cupones.OrderByDescending(c => c.FechaCreacion).ToList();

        public List<Cupon> ObtenerActivos() =>
            _context.Cupones
                .Where(c => c.Activo && (!c.FechaVencimiento.HasValue || c.FechaVencimiento.Value >= DateTime.Today))
                .OrderBy(c => c.Codigo)
                .ToList();

        public Cupon? ObtenerPorId(int id) => _context.Cupones.Find(id);

        public Cupon? ObtenerPorCodigo(string? codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return null;
            string normalizado = codigo.Trim().ToUpperInvariant();
            return _context.Cupones.FirstOrDefault(c => c.Codigo.ToUpper() == normalizado);
        }

        /// <summary>
        /// Valida si un cupón es aplicable para un subtotal de productos dado y calcula el monto exacto de descuento.
        /// </summary>
        public (bool valido, string mensaje, decimal montoDescuento, Cupon? cupon) Validar(string? codigo, decimal subtotalProductos)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                return (false, "Debes ingresar un código de cupón.", 0, null);

            var cupon = ObtenerPorCodigo(codigo);
            if (cupon == null)
                return (false, $"El cupón '{codigo.ToUpper()}' no existe o ha expirado.", 0, null);

            if (!cupon.Activo)
                return (false, $"El cupón '{cupon.Codigo}' se encuentra inactivo actualmente.", 0, null);

            if (cupon.FechaVencimiento.HasValue && DateTime.Today > cupon.FechaVencimiento.Value.Date)
                return (false, $"El cupón '{cupon.Codigo}' venció el {cupon.FechaVencimiento.Value:dd/MM/yyyy}.", 0, null);

            if (cupon.UsosMaximos > 0 && cupon.UsosActuales >= cupon.UsosMaximos)
                return (false, $"El cupón '{cupon.Codigo}' ha alcanzado el límite máximo de {cupon.UsosMaximos} canjes.", 0, null);

            if (subtotalProductos < cupon.MontoMinimo)
                return (false, $"El cupón '{cupon.Codigo}' requiere un consumo mínimo de ${cupon.MontoMinimo:N0} COP en productos.", 0, null);

            decimal descuento = 0;
            if (cupon.TipoDescuento.Equals("Porcentaje", StringComparison.OrdinalIgnoreCase))
            {
                descuento = Math.Round(subtotalProductos * (cupon.Valor / 100m));
                if (cupon.TopeMaximoDescuento.HasValue && cupon.TopeMaximoDescuento.Value > 0 && descuento > cupon.TopeMaximoDescuento.Value)
                {
                    descuento = cupon.TopeMaximoDescuento.Value;
                }
            }
            else // "Fijo"
            {
                descuento = Math.Min(cupon.Valor, subtotalProductos);
            }

            if (descuento <= 0)
                return (false, "El cupón no genera descuento aplicable en esta orden.", 0, null);

            return (true, $"¡Cupón '{cupon.Codigo}' aplicado! Ahorras ${descuento:N0} COP.", descuento, cupon);
        }

        /// <summary>
        /// Incrementa el contador de canjes de un cupón cuando se confirma una orden.
        /// </summary>
        public void IncrementarUso(string? codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return;
            var cupon = ObtenerPorCodigo(codigo);
            if (cupon != null)
            {
                cupon.UsosActuales++;
                _context.SaveChanges();
            }
        }

        public (bool ok, string mensaje) Crear(Cupon cupon)
        {
            if (string.IsNullOrWhiteSpace(cupon.Codigo))
                return (false, "El código del cupón es obligatorio.");

            cupon.Codigo = cupon.Codigo.Trim().ToUpperInvariant();

            if (_context.Cupones.Any(c => c.Codigo.ToUpper() == cupon.Codigo))
                return (false, $"Ya existe un cupón registrado con el código '{cupon.Codigo}'.");

            if (cupon.Valor <= 0)
                return (false, "El valor del descuento debe ser mayor a 0.");

            cupon.FechaCreacion = DateTime.Now;
            _context.Cupones.Add(cupon);
            _context.SaveChanges();
            return (true, $"Cupón '{cupon.Codigo}' creado exitosamente.");
        }

        public (bool ok, string mensaje) Actualizar(Cupon cupon)
        {
            var existente = _context.Cupones.Find(cupon.Id);
            if (existente == null) return (false, "Cupón no encontrado.");

            cupon.Codigo = cupon.Codigo.Trim().ToUpperInvariant();

            if (_context.Cupones.Any(c => c.Codigo.ToUpper() == cupon.Codigo && c.Id != cupon.Id))
                return (false, $"Ya existe otro cupón con el código '{cupon.Codigo}'.");

            existente.Codigo = cupon.Codigo;
            existente.Descripcion = cupon.Descripcion;
            existente.TipoDescuento = cupon.TipoDescuento;
            existente.Valor = cupon.Valor;
            existente.MontoMinimo = cupon.MontoMinimo;
            existente.TopeMaximoDescuento = cupon.TopeMaximoDescuento;
            existente.FechaVencimiento = cupon.FechaVencimiento;
            existente.UsosMaximos = cupon.UsosMaximos;
            existente.Activo = cupon.Activo;

            _context.SaveChanges();
            return (true, $"Cupón '{cupon.Codigo}' actualizado exitosamente.");
        }

        public (bool ok, string mensaje) CambiarEstado(int id)
        {
            var cupon = _context.Cupones.Find(id);
            if (cupon == null) return (false, "Cupón no encontrado.");

            cupon.Activo = !cupon.Activo;
            _context.SaveChanges();
            return (true, $"El cupón '{cupon.Codigo}' ahora está {(cupon.Activo ? "Activo" : "Inactivo")}.");
        }

        public (bool ok, string mensaje) Eliminar(int id)
        {
            var cupon = _context.Cupones.Find(id);
            if (cupon == null) return (false, "Cupón no encontrado.");

            _context.Cupones.Remove(cupon);
            _context.SaveChanges();
            return (true, $"Cupón '{cupon.Codigo}' eliminado correctamente.");
        }
    }
}
