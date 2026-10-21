using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace ControlClientesRestaurante.Services
{
    /// <summary>Generación de códigos y claves con un generador criptográficamente seguro.</summary>
    public static class GeneradorSeguro
    {
        /// <summary>Código OTP de 6 dígitos.</summary>
        public static string CodigoOtp() =>
            RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");

        /// <summary>Clave aleatoria larga para cuentas que ingresan por OTP o Google (nadie la escribe).</summary>
        public static string ClaveAleatoria(int longitud = 24)
        {
            const string alfabeto = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
            var chars = new char[longitud];
            for (int i = 0; i < chars.Length; i++)
                chars[i] = alfabeto[RandomNumberGenerator.GetInt32(alfabeto.Length)];
            return new string(chars) + "#9a";
        }
    }

    /// <summary>
    /// Límites en memoria contra fuerza bruta y abuso:
    /// bloqueo temporal tras varios intentos de login fallidos y topes de envío de OTP por teléfono.
    /// Singleton: el estado se reinicia al reiniciar la aplicación.
    /// </summary>
    public class LimitadorIntentos
    {
        private const int FallosPorUsuarioYOrigen = 5;   // mismo usuario desde la misma IP
        private const int FallosPorUsuario        = 15;  // mismo usuario desde cualquier IP
        private static readonly TimeSpan Ventana   = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan Bloqueo   = TimeSpan.FromMinutes(15);

        private sealed class Contador
        {
            public int Fallos;
            public DateTime Desde = DateTime.UtcNow;
            public DateTime? BloqueadoHasta;
        }

        private readonly ConcurrentDictionary<string, Contador> _login = new();
        private readonly ConcurrentDictionary<string, List<DateTime>> _eventos = new();

        // ── Login ──────────────────────────────────────────────────────────────
        private static string ClaveUsuario(string usuario) => "u:" + usuario.Trim().ToLowerInvariant();
        private static string ClaveUsuarioOrigen(string usuario, string ip) => ClaveUsuario(usuario) + "|" + ip;

        /// <summary>¿Está bloqueado este usuario (desde esta IP o en general)? Devuelve los minutos restantes.</summary>
        public bool LoginBloqueado(string usuario, string ip, out int minutosRestantes)
        {
            minutosRestantes = 0;
            var ahora = DateTime.UtcNow;
            foreach (var clave in new[] { ClaveUsuarioOrigen(usuario, ip), ClaveUsuario(usuario) })
            {
                if (_login.TryGetValue(clave, out var c) && c.BloqueadoHasta is DateTime hasta && hasta > ahora)
                {
                    minutosRestantes = Math.Max(minutosRestantes, (int)Math.Ceiling((hasta - ahora).TotalMinutes));
                }
            }
            return minutosRestantes > 0;
        }

        public void RegistrarFalloLogin(string usuario, string ip)
        {
            Registrar(ClaveUsuarioOrigen(usuario, ip), FallosPorUsuarioYOrigen);
            Registrar(ClaveUsuario(usuario), FallosPorUsuario);
        }

        public void LoginExitoso(string usuario, string ip)
        {
            _login.TryRemove(ClaveUsuarioOrigen(usuario, ip), out _);
            _login.TryRemove(ClaveUsuario(usuario), out _);
        }

        private void Registrar(string clave, int maximo)
        {
            var c = _login.GetOrAdd(clave, _ => new Contador());
            lock (c)
            {
                var ahora = DateTime.UtcNow;
                if (ahora - c.Desde > Ventana) { c.Fallos = 0; c.Desde = ahora; c.BloqueadoHasta = null; }
                c.Fallos++;
                if (c.Fallos >= maximo)
                {
                    c.BloqueadoHasta = ahora.Add(Bloqueo);
                    c.Fallos = 0;
                    c.Desde = ahora;
                }
            }
        }

        // ── Topes por ventana (envío de OTP, etc.) ─────────────────────────────
        /// <summary>Registra un evento y devuelve false si ya se alcanzó el máximo dentro de la ventana.</summary>
        public bool PermitirEnVentana(string clave, int maximo, TimeSpan ventana)
        {
            var lista = _eventos.GetOrAdd(clave, _ => new List<DateTime>());
            lock (lista)
            {
                var ahora = DateTime.UtcNow;
                lista.RemoveAll(t => ahora - t > ventana);
                if (lista.Count >= maximo) return false;
                lista.Add(ahora);
                return true;
            }
        }
    }
}
