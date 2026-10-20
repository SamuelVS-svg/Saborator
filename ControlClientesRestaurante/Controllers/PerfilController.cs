using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ControlClientesRestaurante.Models;
using ControlClientesRestaurante.Services;
using ControlClientesRestaurante.Repositories;

namespace ControlClientesRestaurante.Controllers
{
    [Authorize]
    public class PerfilController : Controller
    {
        private readonly AutenticacionService _authService;
        private readonly IUsuarioRepository   _usuarioRepo;
        private readonly IClienteRepository   _clienteRepo;
        private readonly AuditoriaService     _auditoriaService;
        private readonly IWebHostEnvironment  _env;

        public PerfilController(
            AutenticacionService authService,
            IUsuarioRepository usuarioRepo,
            IClienteRepository clienteRepo,
            AuditoriaService auditoriaService,
            IWebHostEnvironment env)
        {
            _authService      = authService;
            _usuarioRepo      = usuarioRepo;
            _clienteRepo      = clienteRepo;
            _auditoriaService = auditoriaService;
            _env              = env;
        }

        private string UsuarioActual() => User.Identity?.Name ?? "";

        [HttpGet]
        public IActionResult Index(bool? bienvenida = false)
        {
            var nombre = UsuarioActual();
            var usuario = _usuarioRepo.ObtenerPorNombre(nombre);
            if (usuario == null) return RedirectToAction("Index", "Login");

            Cliente? cliente = null;
            if (usuario.Rol?.NombreRol == "Cliente")
            {
                cliente = _clienteRepo.ObtenerTodos().FirstOrDefault(c =>
                    c.Documento == usuario.NombreUsuario ||
                    (c.Correo != null && c.Correo == usuario.NombreUsuario) ||
                    c.Nombre == usuario.NombreUsuario)
                    ?? _clienteRepo.Buscar(usuario.NombreUsuario).FirstOrDefault();
            }

            ViewBag.Usuario      = usuario;
            ViewBag.Cliente      = cliente;
            ViewBag.Rol          = User.FindFirst(ClaimTypes.Role)?.Value ?? usuario.Rol?.NombreRol ?? "Usuario";
            ViewBag.FotoPerfil   = usuario.FotoPerfil;
            ViewBag.Tema         = usuario.Tema ?? "clasico";
            ViewBag.EsBienvenida = bienvenida == true;
            return View();
        }

        // ── Sincronizador de Claims y Sesión del Usuario Autenticado ───────
        private async Task RefrescarClaimsSesionAsync(Usuario usuario)
        {
            // Conservar el rol activo de la sesión (el admin puede haber cambiado de rol temporalmente)
            var rol = User.FindFirst(ClaimTypes.Role)?.Value ?? usuario.Rol?.NombreRol ?? "Cliente";
            var nombreMostrar = User.FindFirst("NombreCompleto")?.Value ?? usuario.NombreUsuario;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, usuario.NombreUsuario),
                new Claim(ClaimTypes.Role, rol),
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim("NombreCompleto", nombreMostrar),
                new Claim("DebeCambiarContraseña", usuario.DebeCambiarContraseña.ToString()),
                new Claim("FotoPerfil", usuario.FotoPerfil ?? ""),
                new Claim("Tema", usuario.Tema ?? "clasico")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });

            HttpContext.Session.SetString("Usuario", usuario.NombreUsuario);
            HttpContext.Session.SetString("UsuarioNombre", nombreMostrar);
            HttpContext.Session.SetString("Rol", rol);
            HttpContext.Session.SetString("FotoPerfil", usuario.FotoPerfil ?? "");
            HttpContext.Session.SetString("Tema", usuario.Tema ?? "clasico");
        }

        // ── Actualizar Foto de Perfil (Archivo o Avatar Prediseñado) ───────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarFotoPerfil(IFormFile? fotoArchivo, string? avatarPreset)
        {
            var nombre = UsuarioActual();
            var usuario = _usuarioRepo.ObtenerPorNombre(nombre);
            if (usuario == null) return RedirectToAction("Index", "Login");

            try
            {
                // 1. Subida de archivo personalizada
                if (fotoArchivo != null && fotoArchivo.Length > 0)
                {
                    if (fotoArchivo.Length > 5 * 1024 * 1024)
                    {
                        TempData["Error"] = "La imagen no debe superar los 5MB.";
                        return RedirectToAction(nameof(Index));
                    }

                    var extension = Path.GetExtension(fotoArchivo.FileName).ToLowerInvariant();
                    var permitidas = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                    if (!permitidas.Contains(extension))
                    {
                        TempData["Error"] = "Formato no válido. Usa JPG, PNG, WEBP o GIF.";
                        return RedirectToAction(nameof(Index));
                    }

                    // Verificar el contenido real (firma del archivo), no solo la extensión
                    if (!EsImagenValida(fotoArchivo))
                    {
                        TempData["Error"] = "El archivo no es una imagen válida.";
                        return RedirectToAction(nameof(Index));
                    }

                    var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "avatars");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = $"avatar_{usuario.Id}_{DateTime.UtcNow.Ticks}{extension}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await fotoArchivo.CopyToAsync(stream);
                    }

                    usuario.FotoPerfil = $"/uploads/avatars/{uniqueFileName}";
                    _usuarioRepo.Actualizar(usuario);
                    _usuarioRepo.GuardarCambios();

                    await RefrescarClaimsSesionAsync(usuario);

                    TempData["Mensaje"] = "¡Tu foto de perfil fue guardada con éxito!";
                    return RedirectToAction(nameof(Index));
                }

                // 2. Selección de avatar prediseñado de alta cocina
                if (!string.IsNullOrWhiteSpace(avatarPreset))
                {
                    var presetsPermitidos = new[]
                    {
                        "/img/avatars/chef.svg",
                        "/img/avatars/burger.svg",
                        "/img/avatars/fuego.svg",
                        "/img/avatars/coctel.svg",
                        "/img/avatars/cafe.svg",
                        "/img/avatars/estrella.svg"
                    };

                    var presetNormalizado = avatarPreset.Trim();
                    if (presetsPermitidos.Contains(presetNormalizado))
                    {
                        usuario.FotoPerfil = presetNormalizado;
                        _usuarioRepo.Actualizar(usuario);
                        _usuarioRepo.GuardarCambios();

                        await RefrescarClaimsSesionAsync(usuario);

                        TempData["Mensaje"] = "¡Avatar gourmet asignado con éxito a tu cuenta!";
                        return RedirectToAction(nameof(Index));
                    }
                }

                TempData["Error"] = "Selecciona una fotografía de tu dispositivo o un avatar prediseñado.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al procesar la foto de perfil: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        private static bool EsImagenValida(IFormFile archivo)
        {
            var cabecera = new byte[12];
            using var stream = archivo.OpenReadStream();
            int leidos = stream.Read(cabecera, 0, cabecera.Length);
            if (leidos < 12) return false;

            bool jpg  = cabecera[0] == 0xFF && cabecera[1] == 0xD8 && cabecera[2] == 0xFF;
            bool png  = cabecera[0] == 0x89 && cabecera[1] == 0x50 && cabecera[2] == 0x4E && cabecera[3] == 0x47;
            bool gif  = cabecera[0] == 0x47 && cabecera[1] == 0x49 && cabecera[2] == 0x46 && cabecera[3] == 0x38;
            bool webp = cabecera[0] == 0x52 && cabecera[1] == 0x49 && cabecera[2] == 0x46 && cabecera[3] == 0x46
                     && cabecera[8] == 0x57 && cabecera[9] == 0x45 && cabecera[10] == 0x42 && cabecera[11] == 0x50;
            return jpg || png || gif || webp;
        }

        // ── Eliminar Foto de Perfil ───────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarFotoPerfil()
        {
            var nombre = UsuarioActual();
            var usuario = _usuarioRepo.ObtenerPorNombre(nombre);
            if (usuario == null) return RedirectToAction("Index", "Login");

            // Si es un archivo local subido, intentar eliminar del disco
            if (!string.IsNullOrEmpty(usuario.FotoPerfil) && usuario.FotoPerfil.StartsWith("/uploads/avatars/"))
            {
                var relativePath = usuario.FotoPerfil.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Path.Combine(_env.WebRootPath, relativePath);
                if (System.IO.File.Exists(fullPath))
                {
                    try { System.IO.File.Delete(fullPath); } catch { /* Ignore */ }
                }
            }

            usuario.FotoPerfil = null;
            _usuarioRepo.Actualizar(usuario);
            _usuarioRepo.GuardarCambios();

            await RefrescarClaimsSesionAsync(usuario);

            TempData["Mensaje"] = "Foto de perfil eliminada. Se mostrará tu inicial representativa.";
            return RedirectToAction(nameof(Index));
        }

        // ── Apartado de Tema: Escoger y Guardar Tema de la Cuenta ─────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActualizarTema(string tema)
        {
            var nombre = UsuarioActual();
            var usuario = _usuarioRepo.ObtenerPorNombre(nombre);
            if (usuario == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { ok = false, mensaje = "Sesión requerida." });
                return RedirectToAction("Index", "Login");
            }

            var temasValidos = new[] { "clasico", "dark", "dorado", "esmeralda", "sunset" };
            var temaElegido = (tema ?? "clasico").ToLowerInvariant().Trim();
            if (!temasValidos.Contains(temaElegido))
            {
                temaElegido = "clasico";
            }

            usuario.Tema = temaElegido;
            _usuarioRepo.Actualizar(usuario);
            _usuarioRepo.GuardarCambios();

            await RefrescarClaimsSesionAsync(usuario);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || (Request.Headers.Accept.ToString().Contains("application/json")))
            {
                return Json(new { ok = true, tema = temaElegido, mensaje = "Tema guardado exitosamente." });
            }

            TempData["Mensaje"] = "Tema visual guardado y aplicado a tu cuenta.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CambiarContraseña(string contrasenaActual, string nuevaContrasena, string confirmarContrasena)
        {
            var nombre = UsuarioActual();
            if (string.IsNullOrWhiteSpace(nuevaContrasena) || nuevaContrasena.Length < 6)
            {
                TempData["Error"] = "La nueva contraseña debe tener al menos 6 caracteres.";
                return RedirectToAction(nameof(Index));
            }

            if (nuevaContrasena != confirmarContrasena)
            {
                TempData["Error"] = "La nueva contraseña y su confirmación no coinciden.";
                return RedirectToAction(nameof(Index));
            }

            var (ok, mensaje) = _authService.CambiarContrasena(nombre, contrasenaActual, nuevaContrasena);
            if (!ok)
            {
                TempData["Error"] = mensaje;
                return RedirectToAction(nameof(Index));
            }

            _auditoriaService.Registrar(
                nombre,
                User.FindFirst(ClaimTypes.Role)?.Value,
                "CAMBIO_CONTRASEÑA",
                "Seguridad",
                $"El usuario '{nombre}' cambió su contraseña voluntariamente desde su perfil.",
                "Usuario",
                null,
                HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["Mensaje"] = "¡Contraseña actualizada exitosamente!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ActualizarDatosCliente(int? clienteId, string nombre, string telefono, string? correo)
        {
            var nombreUsuario = UsuarioActual();
            var usuario = _usuarioRepo.ObtenerPorNombre(nombreUsuario);
            if (usuario == null) return RedirectToAction("Index", "Login");

            Cliente? cliente = null;
            if (clienteId.HasValue && clienteId.Value > 0)
            {
                cliente = _clienteRepo.ObtenerPorId(clienteId.Value);
            }
            if (cliente == null)
            {
                cliente = _clienteRepo.ObtenerTodos().FirstOrDefault(c =>
                    c.Documento == usuario.NombreUsuario ||
                    (c.Correo != null && c.Correo == usuario.NombreUsuario) ||
                    c.Nombre == usuario.NombreUsuario)
                    ?? _clienteRepo.Buscar(usuario.NombreUsuario).FirstOrDefault();
            }

            if (cliente != null)
            {
                if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(telefono))
                {
                    TempData["Error"] = "El nombre y el teléfono son obligatorios.";
                    return RedirectToAction(nameof(Index));
                }

                cliente.Nombre = nombre.Trim();
                cliente.Telefono = telefono.Trim();
                cliente.Correo = correo?.Trim();
                _clienteRepo.Actualizar(cliente);
                _clienteRepo.GuardarCambios();

                _auditoriaService.Registrar(
                    nombreUsuario,
                    "Cliente",
                    "ACTUALIZACION_PERFIL",
                    "Clientes",
                    $"El cliente '{nombreUsuario}' actualizó sus datos de contacto.",
                    "Cliente",
                    cliente.Id,
                    HttpContext.Connection.RemoteIpAddress?.ToString());

                TempData["Mensaje"] = "Datos personales actualizados correctamente.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
