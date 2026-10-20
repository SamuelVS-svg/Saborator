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
                return (false, $"El cupón '{cupon.Codigo}' requiere un consumo mínimo de ${(cupon.MontoMinimo).ToString("N0", ControlClientesRestaurante.Models.Formato.Co)} COP en productos.", 0, null);

            decimal descuento = 0;
            if (cupon.TipoDescuento.Equals("Porcentaje", StringComparison.OrdinalIgnoreCase))
            {
                descuento = Math.Round(subtotalProductos * (cupon.Valor / 100m));
                if (cupon.TopeMaximoDescuento.HasValue && cupon.TopeMaximoDescuento.Value > 0 && descuento > cupon.TopeMaximoDescuento.Value)
                {
                    descuento = cupon.TopeMaximoDescuento.Value;
                }
                descuento = Math.Min(descuento, subtotalProductos);
            }
            else // "Fijo"
            {
                descuento = Math.Min(cupon.Valor, subtotalProductos);
            }

            if (descuento <= 0)
                return (false, "El cupón no genera descuento aplicable en esta orden.", 0, null);

            return (true, $"¡Cupón '{cupon.Codigo}' aplicado! Ahorras ${(descuento).ToString("N0", ControlClientesRestaurante.Models.Formato.Co)} COP.", descuento, cupon);
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

        /// <summary>Devuelve un canje cuando se cancela el pedido que usó el cupón.</summary>
        public void DecrementarUso(string? codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return;
            var cupon = ObtenerPorCodigo(codigo);
            if (cupon != null && cupon.UsosActuales > 0)
            {
                cupon.UsosActuales--;
                _context.SaveChanges();
            }
        }

        /// <summary>
        /// Un cliente solo puede usar cada cupón una vez. Se identifica por su cuenta (si inició
        /// sesión) o por su número de celular. Los pedidos cancelados no cuentan.
        /// </summary>
        public bool YaUsadoPorCliente(string? codigo, string? telefono, string? usuario)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return false;
            var cod = codigo.Trim().ToUpperInvariant();
            var digitos = new string((telefono ?? "").Where(char.IsDigit).ToArray());
            if (digitos.Length > 10) digitos = digitos[^10..]; // ignorar prefijo +57

            var pedidos = _context.Pedidos
                .Where(p => p.CodigoCupon != null && p.CodigoCupon.ToUpper() == cod && p.Estado != "Cancelado")
                .Select(p => new { p.Telefono, p.UsuarioMesero })
                .ToList();

            return pedidos.Any(p =>
                (!string.IsNullOrWhiteSpace(usuario) && usuario != "Online" && p.UsuarioMesero == usuario) ||
                (digitos.Length >= 7 && new string((p.Telefono ?? "").Where(char.IsDigit).ToArray()).EndsWith(digitos)));
        }

        public (bool ok, string mensaje) Crear(Cupon cupon)
        {
            if (string.IsNullOrWhiteSpace(cupon.Codigo))
                return (false, "El código del cupón es obligatorio.");

            cupon.Codigo = cupon.Codigo.Trim().ToUpperInvariant();

            if (_context.Cupones.Any(c => c.Codigo.ToUpper() == cupon.Codigo))
                return (false, $"Ya existe un cupón registrado con el código '{cupon.Codigo}'.");

            var errorValidacion = ValidarDatos(cupon);
            if (errorValidacion != null) return (false, errorValidacion);

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

            var errorValidacion = ValidarDatos(cupon);
            if (errorValidacion != null) return (false, errorValidacion);

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

        /// <summary>Reglas comunes al crear o editar: tipo válido, porcentaje 1-100 y montos no negativos.</summary>
        private static string? ValidarDatos(Cupon cupon)
        {
            if (cupon.TipoDescuento != "Porcentaje" && cupon.TipoDescuento != "Fijo")
                return "El tipo de descuento debe ser 'Porcentaje' o 'Fijo'.";
            if (cupon.Valor <= 0)
                return "El valor del descuento debe ser mayor a 0.";
            if (cupon.TipoDescuento == "Porcentaje" && cupon.Valor > 100)
                return "Un descuento porcentual no puede superar el 100 %.";
            if (cupon.MontoMinimo < 0 || (cupon.TopeMaximoDescuento ?? 0) < 0 || cupon.UsosMaximos < 0)
                return "Los montos y el número de usos no pueden ser negativos.";
            return null;
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
