using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using ControlClientesRestaurante.Models;
using ControlClientesRestaurante.Services;
using ControlClientesRestaurante.Repositories;

namespace ControlClientesRestaurante.Controllers
{
    public class LoginController : Controller
    {
        private readonly AutenticacionService _auth;
        private readonly IUsuarioRepository   _usuarioRepo;
        private readonly AuditoriaService     _auditoriaService;
        private readonly IEmailService        _emailService;
        private readonly ITwilioService       _twilioService;
        private readonly IConfiguration       _configuration;
        private readonly Data.ApplicationDbContext _context;

        public LoginController(
            AutenticacionService auth, 
            IUsuarioRepository usuarioRepo, 
            AuditoriaService auditoriaService,
            IEmailService emailService,
            ITwilioService twilioService,
            IConfiguration configuration,
            Data.ApplicationDbContext context)
        {
            _auth             = auth;
            _usuarioRepo      = usuarioRepo;
            _auditoriaService = auditoriaService;
            _emailService     = emailService;
            _twilioService    = twilioService;
            _configuration    = configuration;
            _context          = context;
        }

        [HttpGet]
        public IActionResult Index(string? tab)
        {
            ViewBag.Tab = string.Equals(tab, "registro", StringComparison.OrdinalIgnoreCase) ? "registro" : "login";
            return View();
        }

        [HttpGet]
        public IActionResult Registro()
        {
            ViewBag.Tab = "registro";
            return View("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Index(string usuario, string? contraseña, [FromForm(Name = "password")] string? password = null)
        {
            var pass = !string.IsNullOrEmpty(contraseña) ? contraseña : (password ?? string.Empty);
            var resultado = _auth.ValidarCredenciales(usuario, pass);
            if (!resultado.Exitoso)
            {
                _auditoriaService.Registrar(
                    usuario,
                    "N/A",
                    "LOGIN_FALLIDO",
                    "Seguridad",
                    $"Intento de acceso fallido para el usuario '{usuario}'. Motivo: {resultado.Mensaje}",
                    "Usuario",
                    null,
                    HttpContext.Connection.RemoteIpAddress?.ToString());

                ViewBag.Error                 = resultado.Mensaje;
                ViewBag.Tab                   = "login";
                ViewBag.Usuario               = usuario;
                ViewBag.RequiereVerificacion  = resultado.RequiereVerificacionEmail;
                return View();
            }

            var usuarioEntity = resultado.Usuario!;
            var rol = resultado.Rol ?? "Cliente";
            var nombreMostrar = usuarioEntity.NombreUsuario ?? usuario;

            _auditoriaService.Registrar(
                usuarioEntity.NombreUsuario ?? usuario,
                rol,
                "LOGIN_EXITOSO",
                "Seguridad",
                $"Inicio de sesión exitoso con rol '{rol}'.",
                "Usuario",
                usuarioEntity.Id,
                HttpContext.Connection.RemoteIpAddress?.ToString());

            await IniciarSesionClaimsAsync(usuarioEntity, rol, nombreMostrar);

            // Redirección inteligente directa según el rol del usuario
            string destino = ObtenerRedireccionPorRol(rol);
            return Redirect(destino);
        }

        private string ObtenerRedireccionPorRol(string rol, string? returnUrl = null)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) && !returnUrl.Contains("/Login", StringComparison.OrdinalIgnoreCase))
                return returnUrl;

            return rol switch
            {
                "Administrador" => "/Home/Dashboard",
                "Cajero"        => "/Caja/Index",
                "Mesero"        => "/Pedido/Menu",
                "Cocinero"      => "/Pedido/Cocina",
                "Cliente"       => "/#menu",
                _               => "/Home/Dashboard"
            };
        }

        [HttpPost]
        public async Task<IActionResult> Registro(
            [FromForm] string  nombreCompleto,
            [FromForm] string? tipoDocumento,
            [FromForm] string  documento,
            [FromForm] DateTime? fechaNacimiento,
            [FromForm] string  telefono,
            [FromForm] string? direccion,
            [FromForm] string? canalVerificacion,
            [FromForm] string? correo,
            [FromForm] string  usuario,
            [FromForm] string? contraseña,
            [FromForm] string? confirmarContraseña,
            [FromForm] string? contrasena,
            [FromForm] string? confirmarContrasena)
        {
            var pass = !string.IsNullOrEmpty(contraseña) ? contraseña : (contrasena ?? "");
            var conf = !string.IsNullOrEmpty(confirmarContraseña) ? confirmarContraseña : (confirmarContrasena ?? "");

            // Anteponer tipo de documento si fue seleccionado y no viene con guión
            var docFinal = (documento ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(tipoDocumento) && !docFinal.Contains("-"))
            {
                docFinal = $"{tipoDocumento.Trim()}-{docFinal}";
            }

            var codigoOtp = Random.Shared.Next(100000, 999999).ToString("D6");
            var canal = string.Equals(canalVerificacion, "sms", StringComparison.OrdinalIgnoreCase) ? "sms" : "whatsapp";

            var (ok, mensaje, nuevoUsuario, cliente) = _auth.RegistrarCliente(
                nombreCompleto, docFinal, telefono, correo, usuario, pass, conf, fechaNacimiento, direccion, codigoOtp);

            if (!ok)
            {
                ViewBag.ErrorRegistro       = mensaje;
                ViewBag.Tab                 = "registro";
                ViewBag.RegNombreCompleto   = nombreCompleto;
                ViewBag.RegTipoDocumento    = tipoDocumento;
                ViewBag.RegDocumento        = documento;
                ViewBag.RegFechaNacimiento  = fechaNacimiento?.ToString("yyyy-MM-dd");
                ViewBag.RegTelefono         = telefono;
                ViewBag.RegDireccion        = direccion;
                ViewBag.RegCanal            = canal;
                ViewBag.RegCorreo           = correo;
                ViewBag.RegUsuario          = usuario;
                return View("Index");
            }

            // Registrar auditoría de cuenta creada
            _auditoriaService.Registrar(
                nuevoUsuario!.NombreUsuario,
                "Cliente",
                "REGISTRO_CLIENTE",
                "Autenticación",
                $"Cuenta comensal registrada para '{nuevoUsuario.NombreUsuario}' ({nuevoUsuario.Correo}). Pendiente de validación OTP vía {canal.ToUpper()}.",
                "Usuario",
                nuevoUsuario.Id,
                HttpContext.Connection.RemoteIpAddress?.ToString());

            // Enviar OTP real mediante Twilio por el canal seleccionado
            if (canal == "sms")
            {
                await _twilioService.EnviarOtpSmsAsync(telefono, codigoOtp);
            }
            else
            {
                await _twilioService.EnviarOtpWhatsAppAsync(telefono, codigoOtp);
            }

            // Enviar también respaldo por correo electrónico
            if (!string.IsNullOrWhiteSpace(nuevoUsuario.Correo))
            {
                await _emailService.EnviarOtpCuentaAsync(
                    nuevoUsuario.Correo!, 
                    cliente?.Nombre ?? nuevoUsuario.NombreUsuario, 
                    codigoOtp);
            }

            // Redirigir a la pantalla interactiva de verificación de código de 6 dígitos
            return RedirectToAction("RegistroExitoso", new { 
                email = nuevoUsuario.Correo, 
                usuario = nuevoUsuario.NombreUsuario, 
                telefono = telefono,
                canal = canal
            });
        }

        // ── Vista obligatoria de primer ingreso para cambiar contraseña ──
        [HttpGet]
        public IActionResult PrimerIngreso(string? usuario)
        {
            var u = usuario ?? HttpContext.Session.GetString("UsuarioPendienteCambio") ?? User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(u))
                return RedirectToAction("Index");

            ViewBag.Usuario = u;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PrimerIngreso(
            string usuario, string contrasenaActual, string nuevaContrasena, string confirmarContrasena)
        {
            var (ok, mensaje) = _auth.CambiarContrasenaObligatorio(
                usuario, contrasenaActual, nuevaContrasena, confirmarContrasena);

            if (!ok)
            {
                ViewBag.Error = mensaje;
                ViewBag.Usuario = usuario;
                return View();
            }

            var usuarioEntity = _usuarioRepo.ObtenerPorNombre(usuario);
            if (usuarioEntity == null)
                return RedirectToAction("Index");

            var rol = usuarioEntity.Rol?.NombreRol ?? "Cliente";
            await IniciarSesionClaimsAsync(usuarioEntity, rol, usuarioEntity.NombreUsuario);
            HttpContext.Session.Remove("UsuarioPendienteCambio");

            TempData["Mensaje"] = "¡Contraseña actualizada con éxito! Bienvenido al sistema.";

            if (rol == "Cocinero")
                return RedirectToAction("Cocina", "Pedido");
            if (rol == "Mesero" || rol == "Cliente")
                return RedirectToAction("Menu", "Pedido");
            if (rol == "Cajero")
                return RedirectToAction("Index", "Pedido");

            return RedirectToAction("Index", "Cliente");
        }

        // ── Cambiar rol (solo usuario "admin") ────────────────────────────
        [HttpPost]
        public async Task<IActionResult> CambiarRol(string nuevoRol)
        {
            var nombreUsuario = User.Identity?.Name ?? HttpContext.Session.GetString("Usuario");
            if (string.IsNullOrWhiteSpace(nombreUsuario))
                return RedirectToAction("Index");

            // Solo "admin" puede alternar roles
            if (nombreUsuario != "admin")
            {
                TempData["Error"] = "No tienes permiso para cambiar de rol.";
                return RedirectToAction("Index", "Cliente");
            }

            var rolesPermitidos = new[] { "Administrador", "Cajero", "Mesero", "Cocinero" };
            if (!rolesPermitidos.Contains(nuevoRol))
                return BadRequest("Rol no válido.");

            var u = _usuarioRepo.ObtenerPorNombre("admin");
            if (u != null)
            {
                await IniciarSesionClaimsAsync(u, nuevoRol, "Administrador");
            }

            TempData["Mensaje"] = $"Rol cambiado a: {nuevoRol}";

            // Redirigir a la pantalla principal del nuevo rol
            if (nuevoRol == "Cocinero")
                return RedirectToAction("Cocina", "Pedido");
            if (nuevoRol == "Mesero")
                return RedirectToAction("Menu", "Pedido");
            return RedirectToAction("Index", "Cliente");
        }

        // ── Cambiar contraseña (usuario autenticado) ──────────────────────
        [HttpPost]
        public IActionResult CambiarContrasena(
            string contrasenaActual, string nuevaContrasena, string confirmar)
        {
            var nombreUsuario = User.Identity?.Name ?? HttpContext.Session.GetString("Usuario");
            if (string.IsNullOrWhiteSpace(nombreUsuario))
                return RedirectToAction("Index");

            if (nuevaContrasena != confirmar)
            {
                TempData["ErrorContrasena"] = "Las contraseñas nuevas no coinciden.";
                return RedirectToAction("Index", "Cliente");
            }

            var (ok, mensaje) = _auth.CambiarContrasena(
                nombreUsuario, contrasenaActual, nuevaContrasena);

            if (ok)
                TempData["Mensaje"] = mensaje;
            else
                TempData["ErrorContrasena"] = mensaje;

            return RedirectToAction("Index", "Cliente");
        }

        // ── Acceso Denegado ───────────────────────────────────────────────
        [HttpGet]
        public IActionResult AccesoDenegado()
        {
            TempData["Error"] = "No tienes permisos suficientes para acceder a esa sección.";
            return RedirectToAction("Index", "Home");
        }

        // ── Login y Registro vía Modal AJAX ──────────────────────────────
        public class LoginAjaxDto
        {
            public string? Usuario { get; set; }
            public string? Contraseña { get; set; }
            public string? Contrasena { get; set; }
            public string? Password { get; set; }
            public string? ReturnUrl { get; set; }

            public string ObtenerPass() => !string.IsNullOrEmpty(Contraseña) ? Contraseña : (!string.IsNullOrEmpty(Contrasena) ? Contrasena : (Password ?? ""));
        }

        public class RegistroAjaxDto
        {
            public string? NombreCompleto { get; set; }
            public string? TipoDocumento { get; set; }
            public string? Documento { get; set; }
            public string? FechaNacimiento { get; set; }
            public string? Telefono { get; set; }
            public string? Direccion { get; set; }
            public string? CanalVerificacion { get; set; } = "whatsapp";
            public string? Correo { get; set; }
            public string? Usuario { get; set; }
            public string? Contraseña { get; set; }
            public string? Contrasena { get; set; }
            public string? Password { get; set; }
            public string? ConfirmarContraseña { get; set; }
            public string? ConfirmarContrasena { get; set; }
            public string? ConfirmarPassword { get; set; }
            public string? ReturnUrl { get; set; }

            public string ObtenerPass() => !string.IsNullOrEmpty(Contraseña) ? Contraseña : (!string.IsNullOrEmpty(Contrasena) ? Contrasena : (Password ?? ""));
            public string ObtenerConfirmar() => !string.IsNullOrEmpty(ConfirmarContraseña) ? ConfirmarContraseña : (!string.IsNullOrEmpty(ConfirmarContrasena) ? ConfirmarContrasena : (ConfirmarPassword ?? ""));
        }

        public class VerificarOtpDto
        {
            public string? Usuario { get; set; }
            public string? Codigo { get; set; }
            public string? ReturnUrl { get; set; }
        }

        public class ReenviarOtpDto
        {
            public string? Usuario { get; set; }
            public string? Canal { get; set; } = "whatsapp";
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> LoginAjax([FromBody] LoginAjaxDto? dto)
        {
            string? usuario = dto?.Usuario;
            string? pass = dto?.ObtenerPass();

            if (Request.HasFormContentType)
            {
                if (string.IsNullOrEmpty(usuario))
                    usuario = Request.Form["usuario"].FirstOrDefault() ?? Request.Form["Usuario"].FirstOrDefault();
                if (string.IsNullOrEmpty(pass))
                    pass = Request.Form["contraseña"].FirstOrDefault() ?? Request.Form["contrasena"].FirstOrDefault() ?? Request.Form["password"].FirstOrDefault();
            }

            if (string.IsNullOrEmpty(usuario))
                usuario = Request.Query["usuario"].FirstOrDefault() ?? Request.Query["Usuario"].FirstOrDefault();
            if (string.IsNullOrEmpty(pass))
                pass = Request.Query["contraseña"].FirstOrDefault() ?? Request.Query["contrasena"].FirstOrDefault() ?? Request.Query["password"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrEmpty(pass))
                return Json(new { ok = false, mensaje = "Por favor ingresa usuario y contraseña." });

            var res = _auth.ValidarCredenciales(usuario, pass);
            if (!res.Exitoso)
                return Json(new { 
                    ok = false, 
                    mensaje = res.Mensaje, 
                    requiereVerificacion = res.RequiereVerificacionEmail,
                    usuario = usuario
                });

            var usuarioEntity = res.Usuario!;
            var rol = res.Rol ?? "Cliente";

            await IniciarSesionClaimsAsync(usuarioEntity, rol, usuarioEntity.NombreUsuario);

            string target = ObtenerRedireccionPorRol(rol, dto?.ReturnUrl);
            return Json(new { ok = true, mensaje = "Ingreso exitoso.", redirectUrl = target });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> RegistroAjax([FromBody] RegistroAjaxDto? dto)
        {
            if (dto == null && Request.HasFormContentType)
            {
                dto = new RegistroAjaxDto
                {
                    NombreCompleto    = Request.Form["nombreCompleto"].FirstOrDefault(),
                    TipoDocumento     = Request.Form["tipoDocumento"].FirstOrDefault(),
                    Documento         = Request.Form["documento"].FirstOrDefault(),
                    FechaNacimiento   = Request.Form["fechaNacimiento"].FirstOrDefault(),
                    Telefono          = Request.Form["telefono"].FirstOrDefault(),
                    Direccion         = Request.Form["direccion"].FirstOrDefault(),
                    CanalVerificacion = Request.Form["canalVerificacion"].FirstOrDefault() ?? "whatsapp",
                    Correo            = Request.Form["correo"].FirstOrDefault(),
                    Usuario           = Request.Form["usuario"].FirstOrDefault(),
                    Contraseña        = Request.Form["contraseña"].FirstOrDefault() ?? Request.Form["contrasena"].FirstOrDefault() ?? Request.Form["password"].FirstOrDefault(),
                    ConfirmarContraseña = Request.Form["confirmarContraseña"].FirstOrDefault() ?? Request.Form["confirmarContrasena"].FirstOrDefault() ?? Request.Form["confirmarPassword"].FirstOrDefault()
                };
            }

            if (dto == null) return Json(new { ok = false, mensaje = "Datos incompletos." });
            string pass = dto.ObtenerPass();
            string conf = dto.ObtenerConfirmar();

            var docFinal = (dto.Documento ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(dto.TipoDocumento) && !docFinal.Contains("-"))
            {
                docFinal = $"{dto.TipoDocumento.Trim()}-{docFinal}";
            }

            DateTime? fn = null;
            if (!string.IsNullOrWhiteSpace(dto.FechaNacimiento) && DateTime.TryParse(dto.FechaNacimiento, out var parsedFn))
            {
                fn = parsedFn;
            }

            var codigoOtp = Random.Shared.Next(100000, 999999).ToString("D6");
            var canal = string.Equals(dto.CanalVerificacion, "sms", StringComparison.OrdinalIgnoreCase) ? "sms" : "whatsapp";

            var (ok, mensaje, nuevoUsuario, cliente) = _auth.RegistrarCliente(
                dto.NombreCompleto, docFinal, dto.Telefono, dto.Correo,
                dto.Usuario, pass, conf, fn, dto.Direccion, codigoOtp);

            if (!ok)
                return Json(new { ok = false, mensaje });

            // Enviar OTP real mediante Twilio
            if (canal == "sms")
            {
                await _twilioService.EnviarOtpSmsAsync(dto.Telefono ?? "", codigoOtp);
            }
            else
            {
                await _twilioService.EnviarOtpWhatsAppAsync(dto.Telefono ?? "", codigoOtp);
            }

            // Respaldo por correo electrónico
            if (!string.IsNullOrWhiteSpace(nuevoUsuario!.Correo))
            {
                await _emailService.EnviarOtpCuentaAsync(
                    nuevoUsuario.Correo,
                    cliente?.Nombre ?? nuevoUsuario.NombreUsuario,
                    codigoOtp);
            }

            var redirectUrl = Url.Action("RegistroExitoso", "Login", new { 
                email = nuevoUsuario.Correo, 
                usuario = nuevoUsuario.NombreUsuario, 
                telefono = dto.Telefono,
                canal = canal
            });

            return Json(new { 
                ok = true, 
                requiereVerificacion = true, 
                mensaje = $"¡Cuenta creada! Código de 6 dígitos enviado por {canal.ToUpper()}.", 
                redirectUrl = redirectUrl 
            });
        }

        // ── Pantalla de Confirmación de Registro e Ingreso de Código OTP ───────
        [HttpGet]
        public IActionResult RegistroExitoso(string? email, string? usuario, string? telefono, string? canal)
        {
            ViewBag.Email    = email;
            ViewBag.Usuario  = usuario;
            ViewBag.Telefono = telefono;
            ViewBag.Canal    = string.Equals(canal, "sms", StringComparison.OrdinalIgnoreCase) ? "SMS" : "WhatsApp";
            return View();
        }

        // ── Validación automática mediante Código de 6 Dígitos (OTP) ──────
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> VerificarOtp([FromBody] VerificarOtpDto? dto)
        {
            string? usuarioIdentificador = dto?.Usuario;
            string? codigo = dto?.Codigo;

            if (Request.HasFormContentType)
            {
                usuarioIdentificador ??= Request.Form["usuario"].FirstOrDefault() ?? Request.Form["Usuario"].FirstOrDefault();
                codigo ??= Request.Form["codigo"].FirstOrDefault() ?? Request.Form["Codigo"].FirstOrDefault();
            }

            if (string.IsNullOrWhiteSpace(usuarioIdentificador) || string.IsNullOrWhiteSpace(codigo))
                return Json(new { ok = false, mensaje = "Por favor ingresa los 6 dígitos del código de verificación." });

            var (ok, mensaje, usuario) = _auth.VerificarCodigoOtp(usuarioIdentificador, codigo);
            if (!ok || usuario == null)
            {
                // Fallback de validación con Twilio Verify en vivo
                var cliente = _auth.ObtenerClientePorUsuario(usuarioIdentificador);
                var tel = cliente?.Telefono ?? usuarioIdentificador;
                if (!string.IsNullOrWhiteSpace(tel) && await _twilioService.VerificarCodigoTwilioAsync(tel, codigo))
                {
                    var usr = _usuarioRepo.ObtenerPorNombre(usuarioIdentificador) ?? _usuarioRepo.ObtenerPorCorreo(usuarioIdentificador);
                    if (usr != null)
                    {
                        usr.EmailVerificado = true;
                        usr.TokenVerificacionEmail = null;
                        usr.FechaExpiracionToken = null;
                        _usuarioRepo.Actualizar(usr);
                        _usuarioRepo.GuardarCambios();
                        ok = true;
                        usuario = usr;
                    }
                }
            }

            if (!ok || usuario == null)
            {
                return Json(new { ok = false, mensaje = mensaje ?? "Código incorrecto o vencido." });
            }

            _auditoriaService.Registrar(
                usuario.NombreUsuario,
                "Cliente",
                "CUENTA_VALIDADA_OTP",
                "Autenticación",
                $"Cuenta activada exitosamente con código de 6 dígitos para '{usuario.NombreUsuario}'.",
                "Usuario",
                usuario.Id,
                HttpContext.Connection.RemoteIpAddress?.ToString());

            var rol = usuario.Rol?.NombreRol ?? "Cliente";
            await IniciarSesionClaimsAsync(usuario, rol, usuario.NombreUsuario);

            string target = ObtenerRedireccionPorRol(rol, dto?.ReturnUrl);
            return Json(new { 
                ok = true, 
                mensaje = "¡Código verificado con éxito! Cuenta activada.", 
                redirectUrl = target 
            });
        }

        // ── Reenvío de Código OTP por SMS o WhatsApp ─────────────────────
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ReenviarOtp([FromBody] ReenviarOtpDto? dto)
        {
            string? usuarioIdentificador = dto?.Usuario;
            string? canalSol = dto?.Canal;

            if (Request.HasFormContentType)
            {
                usuarioIdentificador ??= Request.Form["usuario"].FirstOrDefault() ?? Request.Form["Usuario"].FirstOrDefault();
                canalSol ??= Request.Form["canal"].FirstOrDefault() ?? Request.Form["Canal"].FirstOrDefault();
            }

            if (string.IsNullOrWhiteSpace(usuarioIdentificador))
                return Json(new { ok = false, mensaje = "Identificador de usuario requerido." });

            var (ok, mensaje, nuevoCodigo, usuario, cliente) = _auth.GenerarNuevoCodigoOtp(usuarioIdentificador);
            if (!ok || usuario == null || nuevoCodigo == null)
            {
                return Json(new { ok = false, mensaje });
            }

            var canal = string.Equals(canalSol, "sms", StringComparison.OrdinalIgnoreCase) ? "sms" : "whatsapp";
            var tel = cliente?.Telefono ?? "";

            if (canal == "sms")
            {
                await _twilioService.EnviarOtpSmsAsync(tel, nuevoCodigo);
            }
            else
            {
                await _twilioService.EnviarOtpWhatsAppAsync(tel, nuevoCodigo);
            }

            if (!string.IsNullOrWhiteSpace(usuario.Correo))
            {
                await _emailService.EnviarOtpCuentaAsync(
                    usuario.Correo, 
                    cliente?.Nombre ?? usuario.NombreUsuario, 
                    nuevoCodigo);
            }

            _auditoriaService.Registrar(
                usuario.NombreUsuario,
                "Cliente",
                "REENVIO_OTP",
                "Autenticación",
                $"Nuevo código OTP generado y enviado vía {canal.ToUpper()} a '{tel}'.",
                "Usuario",
                usuario.Id,
                HttpContext.Connection.RemoteIpAddress?.ToString());

            return Json(new { 
                ok = true, 
                mensaje = $"Código de 6 dígitos reenviado por {canal.ToUpper()} a tu celular.", 
                canal = canal,
                expiraSegundos = 300 
            });
        }

        // ── Validación automática al hacer clic en el correo ──────────────
        [HttpGet]
        public async Task<IActionResult> VerificarCuenta(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                ViewBag.Exito = false;
                ViewBag.Mensaje = "El enlace de validación es inválido o no contiene un token.";
                return View();
            }

            var (ok, mensaje, usuario) = _auth.VerificarEmailToken(token);
            ViewBag.Exito   = ok;
            ViewBag.Mensaje = mensaje;
            ViewBag.Usuario = usuario?.NombreUsuario;

            if (ok && usuario != null)
            {
                _auditoriaService.Registrar(
                    usuario.NombreUsuario,
                    "Cliente",
                    "CUENTA_VALIDADA",
                    "Autenticación",
                    $"Cuenta validada por correo exitosamente para el usuario '{usuario.NombreUsuario}'.",
                    "Usuario",
                    usuario.Id,
                    HttpContext.Connection.RemoteIpAddress?.ToString());

                // Iniciar sesión automáticamente
                var rol = usuario.Rol?.NombreRol ?? "Cliente";
                await IniciarSesionClaimsAsync(usuario, rol, usuario.NombreUsuario);
            }

            return View();
        }

        // ── Reenvío de correo de validación ──────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReenviarVerificacion(string usuarioOCorreo)
        {
            if (string.IsNullOrWhiteSpace(usuarioOCorreo))
            {
                TempData["Error"] = "Por favor ingresa tu usuario o correo electrónico.";
                return RedirectToAction("Index");
            }

            var (ok, mensaje, usuario) = _auth.GenerarNuevoTokenVerificacion(usuarioOCorreo);
            if (!ok || usuario == null)
            {
                TempData["Error"] = mensaje;
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(usuario.Correo))
            {
                TempData["Error"] = "No se encontró un correo electrónico asociado a la cuenta.";
                return RedirectToAction("Index");
            }

            var urlValidacion = Url.Action("VerificarCuenta", "Login", new { token = usuario.TokenVerificacionEmail }, Request.Scheme)
                ?? $"{Request.Scheme}://{Request.Host}/Login/VerificarCuenta?token={usuario.TokenVerificacionEmail}";

            await _emailService.EnviarVerificacionCuentaAsync(
                usuario.Correo,
                usuario.NombreUsuario,
                urlValidacion);

            TempData["Mensaje"] = $"Hemos reenviado el correo de validación a {usuario.Correo}.";
            return RedirectToAction("RegistroExitoso", new { 
                email = usuario.Correo, 
                usuario = usuario.NombreUsuario, 
                token = usuario.TokenVerificacionEmail 
            });
        }

        // ── Logout ────────────────────────────────────────────────────────
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // ── Endpoints REST para el Frontend React (/api/auth/*) ───────────
        public record ReactOtpRequestDto(string? phone, string? channel);
        public record ReactOtpVerifyDto(string? phone, string? code, bool isGuest, string? name);
        public record ReactGuestLoginDto(string? phone, string? name);

        [HttpPost("/api/auth/request-otp")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ApiRequestOtp([FromBody] ReactOtpRequestDto? dto)
        {
            var phone = new string((dto?.phone ?? "").Where(char.IsDigit).ToArray());
            if (phone.Length != 10)
                return BadRequest(new { success = false, error = "Número de celular inválido (debe tener 10 dígitos)." });

            var channel = string.Equals(dto?.channel, "whatsapp", StringComparison.OrdinalIgnoreCase) ? "whatsapp" : "sms";
            var code = Random.Shared.Next(100000, 999999).ToString("D6");

            HttpContext.Session.SetString($"ReactOtp_{phone}", code);

            var result = channel == "whatsapp"
                ? await _twilioService.EnviarOtpWhatsAppAsync(phone, code)
                : await _twilioService.EnviarOtpSmsAsync(phone, code);

            return Ok(new
            {
                success = true,
                message = result.ok
                    ? $"Código enviado por {channel.ToUpper()} al +57{phone}."
                    : $"Modo demostración activo: usa el código {code} para ingresar.",
                sid = result.sid
            });
        }

        [HttpPost("/api/auth/verify-otp")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ApiVerifyOtp([FromBody] ReactOtpVerifyDto? dto)
        {
            var phone = new string((dto?.phone ?? "").Where(char.IsDigit).ToArray());
            var code = (dto?.code ?? "").Trim();

            if (phone.Length != 10 || code.Length != 6)
                return BadRequest(new { success = false, error = "Datos de verificación incompletos." });

            var storedCode = HttpContext.Session.GetString($"ReactOtp_{phone}");
            var validLocal = !string.IsNullOrEmpty(storedCode) && storedCode == code;
            var validTwilio = !validLocal && await _twilioService.VerificarCodigoTwilioAsync(phone, code);

            if (!validLocal && !validTwilio)
                return BadRequest(new { success = false, error = "Código OTP incorrecto o expirado." });

            HttpContext.Session.Remove($"ReactOtp_{phone}");

            var userName = !string.IsNullOrWhiteSpace(dto?.name) ? dto.name.Trim() : $"Comensal {phone[^4..]}";
            var token = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{phone}:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"));

            return Ok(new
            {
                success = true,
                token,
                user = new
                {
                    phone = $"+57{phone}",
                    name = userName,
                    role = "Cliente",
                    verified = true
                }
            });
        }

        [HttpPost("/api/auth/guest-login")]
        [IgnoreAntiforgeryToken]
        public IActionResult ApiGuestLogin([FromBody] ReactGuestLoginDto? dto)
        {
            var phone = new string((dto?.phone ?? "3000000000").Where(char.IsDigit).ToArray());
            var userName = !string.IsNullOrWhiteSpace(dto?.name) ? dto.name.Trim() : "Invitado Saborator";
            var token = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"guest:{phone}:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"));

            return Ok(new
            {
                success = true,
                token,
                user = new
                {
                    phone = $"+57{phone}",
                    name = userName,
                    role = "Invitado",
                    verified = false
                }
            });
        }

        // ── Helper centralizado para generar Claims y Cookie ─────────────
        private async Task IniciarSesionClaimsAsync(Usuario usuario, string rol, string nombreMostrar)
        {
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
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Sincronizar Session para compatibilidad retroactiva
            HttpContext.Session.SetString("Usuario", usuario.NombreUsuario);
            HttpContext.Session.SetString("UsuarioNombre", nombreMostrar);
            HttpContext.Session.SetString("Rol", rol);
            HttpContext.Session.SetString("FotoPerfil", usuario.FotoPerfil ?? "");
            HttpContext.Session.SetString("Tema", usuario.Tema ?? "clasico");
        }

        public class SocialAuthDto
        {
            public string? Provider { get; set; }
            public string? Email { get; set; }
            public string? Name { get; set; }
            public string? SocialId { get; set; }
            public string? ReturnUrl { get; set; }
        }

        public class CompletarRegistroSocialDto
        {
            public string? Nombre { get; set; }
            public string? Apellido { get; set; }
            public string? Correo { get; set; }
            public string? PrefijoPais { get; set; }
            public string? Telefono { get; set; }
            public string? Password { get; set; }
            public bool AceptaTerminos { get; set; }
            public bool AceptaPoliticas { get; set; }
            public bool AceptaPromociones { get; set; }
            public string? Provider { get; set; }
            public string? SocialId { get; set; }
            public string? ReturnUrl { get; set; }
        }

        [HttpPost("/Login/SocialAuth")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SocialAuth([FromBody] SocialAuthDto? dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Email))
            {
                return BadRequest(new { ok = false, mensaje = "Datos de autenticación social incompletos." });
            }

            var provider = string.IsNullOrWhiteSpace(dto.Provider) ? "Google" : dto.Provider.Trim();
            var res = _auth.ValidarOIniciarSocial(provider, dto.Email, dto.Name, dto.SocialId);

            if (!res.ok)
            {
                return BadRequest(new { ok = false, mensaje = res.mensaje });
            }

            if (res.esNuevo)
            {
                var partesNombre = (dto.Name ?? "").Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                var nombre = partesNombre.Length > 0 ? partesNombre[0] : "";
                var apellido = partesNombre.Length > 1 ? partesNombre[1] : "";

                return Ok(new
                {
                    ok = true,
                    isNew = true,
                    prefill = new
                    {
                        nombre,
                        apellido,
                        correo = dto.Email.Trim().ToLowerInvariant(),
                        provider,
                        socialId = dto.SocialId
                    }
                });
            }

            var usuarioEntity = res.usuario!;
            var rol = usuarioEntity.Rol?.NombreRol ?? "Cliente";
            var nombreMostrar = res.cliente?.Nombre ?? usuarioEntity.NombreUsuario;

            await IniciarSesionClaimsAsync(usuarioEntity, rol, nombreMostrar);

            _auditoriaService.Registrar(usuarioEntity.NombreUsuario, rol, "LOGIN_SOCIAL", $"Inicio de sesión exitoso con {provider}.", "Seguridad");

            return Ok(new
            {
                ok = true,
                isNew = false,
                mensaje = $"¡Bienvenido de nuevo, {nombreMostrar}!",
                redirectUrl = dto.ReturnUrl ?? "/"
            });
        }

        public class GoogleDirectAuthDto
        {
            public string? Email { get; set; }
            public string? Name { get; set; }
            public string? Credential { get; set; }
            public string? ReturnUrl { get; set; }
        }

        public class EnviarOtpWhatsAppDto
        {
            public string? Telefono { get; set; }
        }

        public class VerificarOtpWhatsAppDto
        {
            public string? Telefono { get; set; }
            public string? Codigo { get; set; }
            public string? Nombre { get; set; }
            public string? ReturnUrl { get; set; }
        }

        [HttpGet("/Login/GoogleConfig")]
        public IActionResult GoogleConfig()
        {
            var clientId = _configuration["Authentication:Google:ClientId"] 
                        ?? _configuration["Google:ClientId"] 
                        ?? Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") 
                        ?? "";
            return Json(new { hasClientId = !string.IsNullOrWhiteSpace(clientId), clientId });
        }

        [HttpPost("/Login/GoogleCallback")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> GoogleCallback([FromForm] string? credential, [FromQuery] string? returnUrl = null)
        {
            string? token = credential;

            // Si viene por JSON en el body (invocación fetch AJAX)
            if (string.IsNullOrWhiteSpace(token) && Request.HasJsonContentType())
            {
                try
                {
                    using var reader = new System.IO.StreamReader(Request.Body);
                    var rawBody = await reader.ReadToEndAsync();
                    if (!string.IsNullOrWhiteSpace(rawBody))
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(rawBody);
                        if (doc.RootElement.TryGetProperty("credential", out var cElem)) token = cElem.GetString();
                        else if (doc.RootElement.TryGetProperty("Credential", out var cElem2)) token = cElem2.GetString();

                        if (string.IsNullOrWhiteSpace(returnUrl) && doc.RootElement.TryGetProperty("returnUrl", out var rElem))
                            returnUrl = rElem.GetString();
                    }
                }
                catch { }
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                if (Request.HasJsonContentType() || Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return BadRequest(new { ok = false, mensaje = "No se recibió la credencial oficial de Google (JWT)." });

                TempData["ErrorLogin"] = "No se recibió la credencial oficial de Google.";
                return Redirect("/#menu");
            }

            string? email = null;
            string? name = null;
            string? picture = null;
            string? sub = null;

            try
            {
                var parts = token.Split('.');
                if (parts.Length >= 2)
                {
                    var payload = parts[1];
                    payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
                    var jsonBytes = Convert.FromBase64String(payload.Replace('-', '+').Replace('_', '/'));
                    var json = System.Text.Encoding.UTF8.GetString(jsonBytes);
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("email", out var eElem)) email = eElem.GetString();
                    if (doc.RootElement.TryGetProperty("name", out var nElem)) name = nElem.GetString();
                    if (doc.RootElement.TryGetProperty("picture", out var pElem)) picture = pElem.GetString();
                    if (doc.RootElement.TryGetProperty("sub", out var sElem)) sub = sElem.GetString();
                }
            }
            catch (Exception ex)
            {
                if (Request.HasJsonContentType() || Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return BadRequest(new { ok = false, mensaje = $"Error al procesar credencial de Google: {ex.Message}" });

                TempData["ErrorLogin"] = "Token de autenticación de Google inválido.";
                return Redirect("/#menu");
            }

            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                if (Request.HasJsonContentType() || Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return BadRequest(new { ok = false, mensaje = "La cuenta de Google seleccionada no proporcionó un correo válido." });

                TempData["ErrorLogin"] = "La cuenta de Google no contiene un correo válido.";
                return Redirect("/#menu");
            }

            var correoLimpio = email.Trim().ToLowerInvariant();
            var nombreFinal = !string.IsNullOrWhiteSpace(name) ? name.Trim() : email.Split('@')[0];

            // 1. Buscar o crear cliente y usuario en SQL Server
            var usuario = _usuarioRepo.ObtenerPorCorreo(correoLimpio);
            Cliente? cliente = _context.Clientes.FirstOrDefault(c => c.Correo == correoLimpio);

            if (usuario == null)
            {
                var rolCliente = _context.Roles.FirstOrDefault(r => r.NombreRol == "Cliente");
                if (rolCliente == null)
                {
                    rolCliente = new Rol { NombreRol = "Cliente" };
                    _context.Roles.Add(rolCliente);
                    _context.SaveChanges();
                }

                var userBase = new string(nombreFinal.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
                if (string.IsNullOrWhiteSpace(userBase)) userBase = correoLimpio.Split('@')[0];
                var userFinal = userBase;
                int c = 1;
                while (_usuarioRepo.ExisteNombre(userFinal))
                {
                    userFinal = $"{userBase}{c}";
                    c++;
                }

                var docGen = $"CC-{Random.Shared.Next(10000000, 99999999)}";

                if (cliente == null)
                {
                    cliente = new Cliente
                    {
                        Nombre = nombreFinal,
                        Documento = docGen,
                        Telefono = "+57 3000000000",
                        Correo = correoLimpio,
                        FechaNacimiento = DateTime.Today.AddYears(-25),
                        Activo = true,
                        Codigo = Guid.NewGuid().ToString("N")
                    };
                    _context.Clientes.Add(cliente);
                    _context.SaveChanges();
                }

                usuario = new Usuario
                {
                    NombreUsuario = userFinal,
                    ContraseñaHash = BCrypt.Net.BCrypt.HashPassword($"Google#{Guid.NewGuid():N}!"),
                    RolId = rolCliente.Id,
                    Estado = true,
                    Correo = correoLimpio,
                    EmailVerificado = true,
                    DebeCambiarContraseña = false,
                    FotoPerfil = picture,
                    Tema = "clasico"
                };
                _context.Usuarios.Add(usuario);
                _context.SaveChanges();
            }
            else if (!string.IsNullOrWhiteSpace(picture) && string.IsNullOrWhiteSpace(usuario.FotoPerfil))
            {
                usuario.FotoPerfil = picture;
                _context.SaveChanges();
            }

            if (!usuario.Estado)
            {
                if (Request.HasJsonContentType() || Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return BadRequest(new { ok = false, mensaje = "Esta cuenta se encuentra inactiva. Contacte al personal del Club Saborator." });

                TempData["ErrorLogin"] = "Esta cuenta se encuentra inactiva.";
                return Redirect("/#menu");
            }

            var nombreMostrar = cliente?.Nombre ?? usuario.NombreUsuario;
            var rol = usuario.Rol?.NombreRol ?? "Cliente";

            await IniciarSesionClaimsAsync(usuario, rol, nombreMostrar);

            _auditoriaService.Registrar(
                usuario.NombreUsuario,
                rol,
                "LOGIN_EXITOSO",
                "Seguridad",
                $"Inicio de sesión exitoso con Google Identity Services ({correoLimpio}) en Club Saborator.",
                "Usuario",
                usuario.Id,
                HttpContext.Connection.RemoteIpAddress?.ToString());

            var destino = !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/#menu";

            if (Request.HasJsonContentType() || Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Ok(new
                {
                    ok = true,
                    mensaje = $"¡Bienvenido al Club Saborator, {nombreMostrar}!",
                    redirectUrl = destino,
                    email = correoLimpio,
                    name = nombreMostrar,
                    picture
                });
            }

            return Redirect(destino);
        }

        [HttpPost("/Login/GoogleDirectAuth")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> GoogleDirectAuth([FromBody] GoogleDirectAuthDto? dto)
        {
            if (dto != null && !string.IsNullOrWhiteSpace(dto.Credential))
            {
                return await GoogleCallback(dto.Credential, dto.ReturnUrl);
            }

            return BadRequest(new { ok = false, mensaje = "Credencial oficial de Google requerida." });
        }

        [HttpPost("/Login/EnviarOtpWhatsApp")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> EnviarOtpWhatsApp([FromBody] EnviarOtpWhatsAppDto? dto)
        {
            var phone = new string((dto?.Telefono ?? "").Where(char.IsDigit).ToArray());
            if (phone.Length != 10)
                return BadRequest(new { ok = false, mensaje = "Ingresa un número de celular de 10 dígitos (ej. 300 123 4567)." });

            var code = Random.Shared.Next(100000, 999999).ToString("D6");
            HttpContext.Session.SetString($"ClubOtp_{phone}", code);

            var sendResult = await _twilioService.EnviarOtpWhatsAppAsync(phone, code);

            return Ok(new
            {
                ok = true,
                mensaje = sendResult.ok
                    ? $"Código de 6 dígitos enviado por WhatsApp al +57 {phone}."
                    : $"Código generado exitosamente. (Modo prueba: {code})",
                demoCode = code
            });
        }

        [HttpPost("/Login/VerificarOtpWhatsApp")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> VerificarOtpWhatsApp([FromBody] VerificarOtpWhatsAppDto? dto)
        {
            var phone = new string((dto?.Telefono ?? "").Where(char.IsDigit).ToArray());
            var code = (dto?.Codigo ?? "").Trim();

            if (phone.Length != 10 || code.Length != 6)
                return BadRequest(new { ok = false, mensaje = "Digita el número de celular y los 6 dígitos del código." });

            var storedCode = HttpContext.Session.GetString($"ClubOtp_{phone}");
            var validLocal = !string.IsNullOrEmpty(storedCode) && storedCode == code;
            var validTwilio = !validLocal && await _twilioService.VerificarCodigoTwilioAsync(phone, code);

            if (!validLocal && !validTwilio)
                return BadRequest(new { ok = false, mensaje = "Código OTP incorrecto o expirado. Solicita uno nuevo." });

            HttpContext.Session.Remove($"ClubOtp_{phone}");

            var formattedPhone = $"+57 {phone}";
            var cliente = _context.Clientes.FirstOrDefault(c => c.Telefono.Contains(phone));
            var userNameCandidate = $"comensal_{phone[^4..]}";
            var usuario = _usuarioRepo.ObtenerPorNombre(userNameCandidate) ??
                          _context.Usuarios.FirstOrDefault(u => u.Correo == $"{phone}@saborator.club");

            if (usuario == null)
            {
                var rolCliente = _context.Roles.FirstOrDefault(r => r.NombreRol == "Cliente");
                if (rolCliente == null)
                {
                    rolCliente = new Rol { NombreRol = "Cliente" };
                    _context.Roles.Add(rolCliente);
                    _context.SaveChanges();
                }

                var nombreComensal = !string.IsNullOrWhiteSpace(dto?.Nombre) ? dto.Nombre.Trim() : $"Comensal Saborator ({phone[^4..]})";

                if (cliente == null)
                {
                    cliente = new Cliente
                    {
                        Nombre = nombreComensal,
                        Documento = $"CC-{phone[^8..]}",
                        Telefono = formattedPhone,
                        Correo = $"{phone}@saborator.club",
                        FechaNacimiento = DateTime.Today.AddYears(-25),
                        Activo = true,
                        Codigo = Guid.NewGuid().ToString("N")
                    };
                    _context.Clientes.Add(cliente);
                    _context.SaveChanges();
                }

                usuario = new Usuario
                {
                    NombreUsuario = userNameCandidate,
                    ContraseñaHash = BCrypt.Net.BCrypt.HashPassword($"Otp#{code}!"),
                    RolId = rolCliente.Id,
                    Estado = true,
                    Correo = $"{phone}@saborator.club",
                    EmailVerificado = true,
                    DebeCambiarContraseña = false,
                    Tema = "clasico"
                };
                _context.Usuarios.Add(usuario);
                _context.SaveChanges();
            }

            var nombreMostrar = cliente?.Nombre ?? usuario.NombreUsuario;
            await IniciarSesionClaimsAsync(usuario, "Cliente", nombreMostrar);

            _auditoriaService.Registrar(usuario.NombreUsuario, "Cliente", "LOGIN_EXITOSO", $"Ingreso verificado por WhatsApp (+57 {phone}) al Club Saborator.", "Seguridad");

            return Ok(new
            {
                ok = true,
                mensaje = $"¡Bienvenido al Club Saborator, {nombreMostrar}!",
                redirectUrl = dto?.ReturnUrl ?? "/#menu"
            });
        }

        [HttpPost("/Login/CompletarRegistroSocial")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> CompletarRegistroSocial([FromBody] CompletarRegistroSocialDto? dto)
        {
            if (dto == null)
            {
                return BadRequest(new { ok = false, mensaje = "Datos de formulario inválidos." });
            }

            if (!dto.AceptaTerminos)
            {
                return BadRequest(new { ok = false, mensaje = "Debes aceptar los Términos y condiciones del servicio." });
            }

            if (!dto.AceptaPoliticas)
            {
                return BadRequest(new { ok = false, mensaje = "Debes aceptar las Políticas de privacidad y tratamiento de datos personales." });
            }

            if (string.IsNullOrWhiteSpace(dto.Nombre))
            {
                return BadRequest(new { ok = false, mensaje = "Por favor ingresa tu nombre." });
            }

            if (string.IsNullOrWhiteSpace(dto.Correo) || !dto.Correo.Contains("@"))
            {
                return BadRequest(new { ok = false, mensaje = "Por favor ingresa un correo electrónico válido." });
            }

            if (string.IsNullOrWhiteSpace(dto.Telefono))
            {
                return BadRequest(new { ok = false, mensaje = "Por favor ingresa tu número de celular." });
            }

            var res = _auth.CompletarRegistroSocial(
                dto.Nombre,
                dto.Apellido ?? "",
                dto.Correo,
                dto.Telefono,
                dto.PrefijoPais ?? "+57",
                dto.AceptaPromociones,
                dto.Password
            );

            if (!res.ok)
            {
                return BadRequest(new { ok = false, mensaje = res.mensaje });
            }

            var usuarioEntity = res.usuario!;
            var nombreMostrar = res.cliente?.Nombre ?? usuarioEntity.NombreUsuario;

            await IniciarSesionClaimsAsync(usuarioEntity, "Cliente", nombreMostrar);

            _auditoriaService.Registrar(usuarioEntity.NombreUsuario, "Cliente", "REGISTRO_SOCIAL_COMPLETO", $"Registro gourmet completado exitosamente vía {dto.Provider ?? "Social/Directo"}.", "Seguridad");

            return Ok(new
            {
                ok = true,
                mensaje = "¡Tu cuenta ha sido creada exitosamente! Bienvenido a Saborator.",
                redirectUrl = dto.ReturnUrl ?? "/"
            });
        }
    }
}
