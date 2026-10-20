using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ControlClientesRestaurante.Data;
using ControlClientesRestaurante.Models;
using ControlClientesRestaurante.Services;

namespace ControlClientesRestaurante.Controllers
{
    /// <summary>Gestión de cuentas del personal (cajeros, meseros, cocineros, domiciliarios, administradores).</summary>
    [Authorize(Roles = "Administrador")]
    public class PersonalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditoriaService _auditoriaService;

        public PersonalController(ApplicationDbContext context, AuditoriaService auditoriaService)
        {
            _context          = context;
            _auditoriaService = auditoriaService;
        }

        private string UsuarioActual() => User.Identity?.Name ?? "";

        private void Auditar(string accion, string detalle, int? usuarioId) =>
            _auditoriaService.Registrar(UsuarioActual(), Roles.Administrador, accion, "Personal", detalle,
                "Usuario", usuarioId, HttpContext.Connection.RemoteIpAddress?.ToString());

        /// <summary>Contraseña temporal aleatoria y legible (sin caracteres ambiguos).</summary>
        private static string GenerarClaveTemporal()
        {
            const string letras  = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz";
            const string digitos = "23456789";
            var chars = new char[10];
            for (int i = 0; i < 8; i++) chars[i] = letras[RandomNumberGenerator.GetInt32(letras.Length)];
            chars[8] = digitos[RandomNumberGenerator.GetInt32(digitos.Length)];
            chars[9] = digitos[RandomNumberGenerator.GetInt32(digitos.Length)];
            return new string(chars) + "#";
        }

        [HttpGet]
        public IActionResult Index()
        {
            var personal = _context.Usuarios.Include(u => u.Rol)
                .Where(u => u.Rol != null && Roles.Personal.Contains(u.Rol.NombreRol))
                .OrderBy(u => u.Rol!.NombreRol).ThenBy(u => u.NombreUsuario)
                .ToList();
            return View(personal);
        }

        [HttpPost]
        public IActionResult Crear(string nombreUsuario, string rol, string? correo)
        {
            nombreUsuario = (nombreUsuario ?? "").Trim().ToLowerInvariant();

            if (nombreUsuario.Length < 3 || nombreUsuario.Length > 40 ||
                !nombreUsuario.All(c => char.IsLetterOrDigit(c) || c == '.' || c == '_' || c == '-'))
            {
                TempData["Error"] = "El usuario debe tener entre 3 y 40 caracteres: letras, números, punto, guion o guion bajo.";
                return RedirectToAction(nameof(Index));
            }
            if (!Roles.Personal.Contains(rol))
            {
                TempData["Error"] = "Selecciona un rol válido.";
                return RedirectToAction(nameof(Index));
            }
            if (_context.Usuarios.Any(u => u.NombreUsuario.ToLower() == nombreUsuario))
            {
                TempData["Error"] = $"Ya existe una cuenta con el usuario '{nombreUsuario}'.";
                return RedirectToAction(nameof(Index));
            }
            correo = string.IsNullOrWhiteSpace(correo) ? null : correo.Trim().ToLowerInvariant();
            if (correo != null && _context.Usuarios.Any(u => u.Correo != null && u.Correo.ToLower() == correo))
            {
                TempData["Error"] = "Ese correo ya está asociado a otra cuenta.";
                return RedirectToAction(nameof(Index));
            }

            var clave = GenerarClaveTemporal();
            var usuario = new Usuario
            {
                NombreUsuario         = nombreUsuario,
                ContraseñaHash        = BCrypt.Net.BCrypt.HashPassword(clave),
                RolId                 = _context.Roles.First(r => r.NombreRol == rol).Id,
                Estado                = true,
                DebeCambiarContraseña = true,
                EmailVerificado       = true,
                Correo                = correo
            };
            _context.Usuarios.Add(usuario);
            _context.SaveChanges();

            Auditar("CREAR_PERSONAL", $"Cuenta '{nombreUsuario}' creada con rol {rol}.", usuario.Id);
            TempData["ClaveTemporal"] = $"{nombreUsuario}|{clave}";
            TempData["Mensaje"] = $"Cuenta '{nombreUsuario}' creada como {rol}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult CambiarRol(int id, string rol)
        {
            var usuario = _context.Usuarios.Include(u => u.Rol).FirstOrDefault(u => u.Id == id);
            if (usuario == null || usuario.Rol == null || !Roles.Personal.Contains(usuario.Rol.NombreRol))
                return NotFound();
            if (!Roles.Personal.Contains(rol))
            {
                TempData["Error"] = "Selecciona un rol válido.";
                return RedirectToAction(nameof(Index));
            }
            if (usuario.NombreUsuario == UsuarioActual())
            {
                TempData["Error"] = "No puedes cambiar tu propio rol.";
                return RedirectToAction(nameof(Index));
            }

            var anterior = usuario.Rol.NombreRol;
            usuario.RolId = _context.Roles.First(r => r.NombreRol == rol).Id;
            _context.SaveChanges();

            Auditar("CAMBIAR_ROL_PERSONAL", $"'{usuario.NombreUsuario}': {anterior} → {rol}. El cambio aplica en su próximo inicio de sesión.", usuario.Id);
            TempData["Mensaje"] = $"'{usuario.NombreUsuario}' ahora es {rol}. Verá el cambio al volver a iniciar sesión.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult CambiarEstado(int id)
        {
            var usuario = _context.Usuarios.Include(u => u.Rol).FirstOrDefault(u => u.Id == id);
            if (usuario == null || usuario.Rol == null || !Roles.Personal.Contains(usuario.Rol.NombreRol))
                return NotFound();
            if (usuario.NombreUsuario == UsuarioActual())
            {
                TempData["Error"] = "No puedes desactivar tu propia cuenta.";
                return RedirectToAction(nameof(Index));
            }
            if (usuario.Estado && usuario.Rol.NombreRol == Roles.Administrador &&
                _context.Usuarios.Count(u => u.Estado && u.Rol!.NombreRol == Roles.Administrador) <= 1)
            {
                TempData["Error"] = "Debe quedar al menos un administrador activo.";
                return RedirectToAction(nameof(Index));
            }

            usuario.Estado = !usuario.Estado;
            _context.SaveChanges();

            var accion = usuario.Estado ? "activada" : "desactivada";
            Auditar(usuario.Estado ? "ACTIVAR_PERSONAL" : "DESACTIVAR_PERSONAL", $"Cuenta '{usuario.NombreUsuario}' {accion}.", usuario.Id);
            TempData["Mensaje"] = $"Cuenta '{usuario.NombreUsuario}' {accion}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult RestablecerContrasena(int id)
        {
            var usuario = _context.Usuarios.Include(u => u.Rol).FirstOrDefault(u => u.Id == id);
            if (usuario == null || usuario.Rol == null || !Roles.Personal.Contains(usuario.Rol.NombreRol))
                return NotFound();

            var clave = GenerarClaveTemporal();
            usuario.ContraseñaHash        = BCrypt.Net.BCrypt.HashPassword(clave);
            usuario.DebeCambiarContraseña = true;
            _context.SaveChanges();

            Auditar("RESTABLECER_CLAVE_PERSONAL", $"Contraseña temporal generada para '{usuario.NombreUsuario}'.", usuario.Id);
            TempData["ClaveTemporal"] = $"{usuario.NombreUsuario}|{clave}";
            TempData["Mensaje"] = $"Nueva contraseña temporal generada para '{usuario.NombreUsuario}'.";
            return RedirectToAction(nameof(Index));
        }
    }
}
