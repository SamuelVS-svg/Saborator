using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ControlClientesRestaurante.Services
{
    public interface IEmailService
    {
        Task<bool> EnviarCorreoAsync(string destinatario, string asunto, string cuerpoHtml);
        Task<bool> EnviarVerificacionCuentaAsync(string correo, string nombreCliente, string urlValidacion);
        Task<bool> EnviarOtpCuentaAsync(string correo, string nombreCliente, string codigoOtp);
        Task<bool> EnviarConfirmacionReservaAsync(string correo, string nombreCliente, string codigoReserva, DateTime fecha, string hora, int mesa, int personas, decimal? depositoPagado);
        Task<bool> EnviarComprobantePagoAsync(string correo, string nombreCliente, string refPago, decimal monto, string metodo, string concepto, string? numeroPedido, string? numeroFactura);
        Task<bool> EnviarConfirmacionPedidoAsync(string correo, string nombreCliente, string numeroPedido, string mesaODireccion, decimal total, List<(string Producto, int Cantidad, decimal Subtotal)> items);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<bool> EnviarCorreoAsync(string destinatario, string asunto, string cuerpoHtml)
        {
            if (string.IsNullOrWhiteSpace(destinatario)) return false;

            var servidor = _config["Smtp:Servidor"] ?? "smtp.gmail.com";
            var puerto = int.TryParse(_config["Smtp:Puerto"], out int p) ? p : 587;
            var habilitarSsl = bool.TryParse(_config["Smtp:HabilitarSsl"], out bool ssl) ? ssl : true;
            var usuario = _config["Smtp:Usuario"] ?? "";
            var contrasena = _config["Smtp:Contrasena"] ?? "";
            var remitente = _config["Smtp:Remitente"] ?? "notificaciones@saborator.com";
            var nombreRemitente = _config["Smtp:NombreRemitente"] ?? "Saborator Restaurante Gourmet";

            // Si no hay credenciales configuradas, registrar en modo simulación (resiliente)
            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(contrasena))
            {
                _logger.LogInformation("[EmailService - Simulación]: Correo enviado a {Destinatario} | Asunto: {Asunto}", destinatario, asunto);
                return true;
            }

            try
            {
                using var client = new SmtpClient(servidor, puerto)
                {
                    EnableSsl = habilitarSsl,
                    Credentials = new NetworkCredential(usuario, contrasena),
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 8000
                };

                using var mensaje = new MailMessage
                {
                    From = new MailAddress(remitente, nombreRemitente),
                    Subject = asunto,
                    Body = cuerpoHtml,
                    IsBodyHtml = true
                };

                mensaje.To.Add(destinatario);

                await client.SendMailAsync(mensaje);
                _logger.LogInformation("[EmailService - Éxito]: Correo enviado a {Destinatario}", destinatario);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[EmailService - Fallback]: No se pudo entregar el correo a {Destinatario}. Verifique credenciales SMTP.", destinatario);
                return false;
            }
        }

        public async Task<bool> EnviarVerificacionCuentaAsync(string correo, string nombreCliente, string urlValidacion)
        {
            if (string.IsNullOrWhiteSpace(correo)) return false;

            string html = $@"
<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='utf-8'/>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'/>
    <title>Activa y Valida tu Cuenta — Saborator</title>
    <style>
        body {{ font-family: 'Helvetica Neue', Arial, sans-serif; background-color: #F5EFE6; margin: 0; padding: 24px; color: #1C1416; }}
        .card {{ max-width: 580px; margin: 0 auto; background: #FFFDF9; border: 1px solid #E3D8C8; border-radius: 16px; overflow: hidden; box-shadow: 0 8px 30px rgba(0,0,0,0.08); }}
        .header {{ background: #6B1E2E; color: #FFFDF9; padding: 32px 25px; text-align: center; border-bottom: 3px solid #C8973A; }}
        .header h1 {{ margin: 0; font-size: 26px; font-weight: 800; letter-spacing: 1px; }}
        .header p {{ margin: 6px 0 0; color: #D4C2A5; font-size: 13px; text-transform: uppercase; letter-spacing: 1.5px; }}
        .body {{ padding: 32px 28px; line-height: 1.6; font-size: 15px; color: #2B2124; }}
        .welcome {{ font-size: 20px; font-weight: 700; color: #6B1E2E; margin-top: 0; margin-bottom: 12px; }}
        .btn-wrap {{ text-align: center; margin: 32px 0; }}
        .btn-validate {{ display: inline-block; background: linear-gradient(135deg, #C8973A 0%, #A67524 100%); color: #FFFDF9 !important; text-decoration: none; font-size: 16px; font-weight: 700; padding: 15px 36px; border-radius: 50px; box-shadow: 0 4px 15px rgba(200, 151, 58, 0.4); text-transform: uppercase; letter-spacing: 0.5px; }}
        .info-box {{ background: #F8F3EC; border-left: 4px solid #C8973A; border-radius: 8px; padding: 16px 20px; margin: 24px 0; font-size: 13.5px; color: #5B4E53; }}
        .alt-link {{ word-break: break-all; font-size: 12px; color: #8A5A10; background: #EFE6D8; padding: 10px 14px; border-radius: 6px; font-family: monospace; display: block; margin-top: 8px; }}
        .footer {{ background: #FAF6F0; padding: 20px 25px; text-align: center; font-size: 12px; color: #7A6F73; border-top: 1px solid #E3D8C8; }}
    </style>
</head>
<body>
    <div class='card'>
        <div class='header'>
            <h1>SABORATOR</h1>
            <p>Alta Cocina &amp; Salón Exclusivo</p>
        </div>
        <div class='body'>
            <div class='welcome'>¡Bienvenido/a, {nombreCliente}!</div>
            <p>Gracias por registrarte en el portal oficial de <strong>Saborator</strong>. Hemos creado tu cuenta de comensal mayor de edad para que puedas reservar mesas exclusivas, ordenar platos gourmet y seguir tus consumos en tiempo real.</p>
            <p>Para garantizar la seguridad de tu perfil y activar tu acceso inmediato, por favor confirma tu dirección de correo electrónico pulsando el botón a continuación:</p>
            
            <div class='btn-wrap'>
                <a href='{urlValidacion}' class='btn-validate' target='_blank'>
                    ✓ Validar mi Cuenta
                </a>
            </div>

            <div class='info-box'>
                <strong>Importante:</strong> Este enlace de verificación es único y tiene una vigencia de <strong>24 horas</strong>. Si no reconoces esta solicitud de registro, puedes descartar este mensaje con total tranquilidad.
            </div>

            <p style='font-size:13px; color:#6E6468; margin-top:24px;'>
                Si tienes problemas con el botón, copia y pega la siguiente dirección web en tu navegador:
                <span class='alt-link'>{urlValidacion}</span>
            </p>
        </div>
        <div class='footer'>
            &copy; {DateTime.Now.Year} Saborator Alta Cocina S.A.S. · NIT 901.847.219-4<br/>
            Carrera 4 Sur # 50D-107, Barranquilla, Atlántico · Teléfono: +57 322 525 4289
        </div>
    </div>
</body>
</html>";

            return await EnviarCorreoAsync(correo, "Activa y Valida tu Cuenta — Saborator Alta Cocina", html);
        }

        public async Task<bool> EnviarOtpCuentaAsync(string correo, string nombreCliente, string codigoOtp)
        {
            if (string.IsNullOrWhiteSpace(correo)) return false;

            string html = $@"
<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='utf-8'/>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'/>
    <title>Código de Verificación — Saborator</title>
    <style>
        body {{ font-family: 'Helvetica Neue', Arial, sans-serif; background-color: #F5EFE6; margin: 0; padding: 24px; color: #1C1416; }}
        .card {{ max-width: 580px; margin: 0 auto; background: #FFFDF9; border: 1px solid #E3D8C8; border-radius: 16px; overflow: hidden; box-shadow: 0 8px 30px rgba(0,0,0,0.08); }}
        .header {{ background: #6B1E2E; color: #FFFDF9; padding: 32px 25px; text-align: center; border-bottom: 3px solid #C8973A; }}
        .header h1 {{ margin: 0; font-size: 26px; font-weight: 800; letter-spacing: 1px; }}
        .header p {{ margin: 6px 0 0; color: #D4C2A5; font-size: 13px; text-transform: uppercase; letter-spacing: 1.5px; }}
        .body {{ padding: 32px 28px; line-height: 1.6; font-size: 15px; color: #2B2124; }}
        .welcome {{ font-size: 20px; font-weight: 700; color: #6B1E2E; margin-top: 0; margin-bottom: 12px; }}
        .otp-container {{ text-align: center; margin: 28px 0; }}
        .otp-code {{ display: inline-block; background: #FAF6F0; border: 2px dashed #C8973A; color: #6B1E2E; font-size: 38px; font-weight: 900; letter-spacing: 12px; padding: 16px 28px; border-radius: 12px; box-shadow: 0 4px 15px rgba(200, 151, 58, 0.15); font-family: 'Courier New', monospace; }}
        .info-box {{ background: #F8F3EC; border-left: 4px solid #C8973A; border-radius: 8px; padding: 16px 20px; margin: 24px 0; font-size: 13.5px; color: #5B4E53; }}
        .footer {{ background: #FAF6F0; padding: 20px 25px; text-align: center; font-size: 12px; color: #7A6F73; border-top: 1px solid #E3D8C8; }}
    </style>
</head>
<body>
    <div class='card'>
        <div class='header'>
            <h1>SABORATOR</h1>
            <p>Comidas Rápidas &amp; Alta Cocina · Soledad</p>
        </div>
        <div class='body'>
            <div class='welcome'>¡Hola, {nombreCliente}!</div>
            <p>Para activar tu cuenta y comenzar a ordenar tus pedidos gourmet o domicilios, ingresa el siguiente código de verificación de 6 dígitos en tu pantalla:</p>
            
            <div class='otp-container'>
                <div class='otp-code'>{codigoOtp}</div>
            </div>

            <div class='info-box'>
                <strong>Validez:</strong> Este código expira en <strong>5 minutos</strong>. El sistema validará tu cuenta automáticamente una vez ingreses este código en el formulario. Nunca compartas este código con nadie.
            </div>

            <p style='font-size:13px; color:#6E6468; margin-top:20px;'>
                Si no solicitaste este código, puedes ignorar este correo de forma segura.
            </p>
        </div>
        <div class='footer'>
            &copy; {DateTime.Now.Year} Saborator Soledad · Comidas Rápidas &amp; Domicilios<br/>
            Soledad, Atlántico · WhatsApp Oficial de Pedidos
        </div>
    </div>
</body>
</html>";

            return await EnviarCorreoAsync(correo, $"Tu código de verificación Saborator es: {codigoOtp}", html);
        }

        public async Task<bool> EnviarConfirmacionReservaAsync(
            string correo, string nombreCliente, string codigoReserva, DateTime fecha, string hora, int mesa, int personas, decimal? depositoPagado)
        {
            if (string.IsNullOrWhiteSpace(correo)) return false;

            string depositoHtml = depositoPagado.HasValue && depositoPagado.Value > 0
                ? $"<p style='color:#2A7348;font-weight:700;margin:4px 0;'>✓ Depósito de garantía recibido: ${(depositoPagado.Value).ToString("N0", ControlClientesRestaurante.Models.Formato.Co)} COP</p>"
                : "";

            string html = $@"
<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='utf-8'/>
    <style>
        body {{ font-family: 'Helvetica Neue', Arial, sans-serif; background-color: #F5EFE6; margin: 0; padding: 20px; color: #1C1416; }}
        .card {{ max-width: 600px; margin: 0 auto; background: #FFFDF9; border: 1px solid #E3D8C8; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.06); }}
        .header {{ background: #6B1E2E; color: #FFFDF9; padding: 30px 25px; text-align: center; border-bottom: 2px solid #C8973A; }}
        .header h1 {{ margin: 0; font-size: 26px; font-weight: 700; letter-spacing: -0.5px; }}
        .header p {{ margin: 6px 0 0; color: #D4C2A5; font-size: 13px; text-transform: uppercase; letter-spacing: 1px; }}
        .body {{ padding: 28px 25px; line-height: 1.6; font-size: 15px; }}
        .box {{ background: #EFE6D8; border-radius: 12px; padding: 18px; margin: 20px 0; border: 1px solid #E3D8C8; }}
        .code {{ font-size: 24px; font-family: monospace; font-weight: 800; color: #6B1E2E; text-align: center; margin: 8px 0; letter-spacing: 2px; }}
        .row {{ display: flex; justify-content: space-between; margin-bottom: 8px; font-size: 14px; }}
        .footer {{ background: #FAF6F0; padding: 18px 25px; text-align: center; font-size: 12px; color: #6E6468; border-top: 1px solid #E3D8C8; }}
    </style>
</head>
<body>
    <div class='card'>
        <div class='header'>
            <h1>SABORATOR</h1>
            <p>Alta Cocina &amp; Salón Exclusivo</p>
        </div>
        <div class='body'>
            <h2 style='color:#6B1E2E;margin-top:0;'>¡Tu reserva está confirmada, {nombreCliente}!</h2>
            <p>Nos complace asegurar tu velada culinaria. Nuestro equipo gastronómico ya está preparando los detalles de tu mesa.</p>
            <div class='box'>
                <div style='text-align:center;font-size:12px;text-transform:uppercase;color:#8A5A10;font-weight:700;'>Código de Reserva</div>
                <div class='code'>{codigoReserva}</div>
                <hr style='border:none;border-top:1px dashed #D4C6B2;margin:14px 0;'/>
                <div class='row'><span>Fecha:</span><strong>{fecha:dddd, dd 'de' MMMM 'de' yyyy}</strong></div>
                <div class='row'><span>Hora del turno:</span><strong>{hora}</strong></div>
                <div class='row'><span>Mesa asignada:</span><strong>Mesa #{mesa}</strong></div>
                <div class='row'><span>Comensales:</span><strong>{personas} personas</strong></div>
                {depositoHtml}
            </div>
            <p style='font-size:13px;color:#5E5357;'>Sede Central: Carrera 4 Sur # 50D-107, Barranquilla, Atlántico.<br/>Llegada recomendada: 10 minutos antes de tu turno.</p>
        </div>
        <div class='footer'>
            &copy; {DateTime.Now.Year} Saborator Alta Cocina S.A.S. · Concierge: +57 322 525 4289
        </div>
    </div>
</body>
</html>";

            return await EnviarCorreoAsync(correo, $"Confirmación de Reserva #{codigoReserva} — Saborator Alta Cocina", html);
        }

        public async Task<bool> EnviarComprobantePagoAsync(
            string correo, string nombreCliente, string refPago, decimal monto, string metodo, string concepto, string? numeroPedido, string? numeroFactura)
        {
            if (string.IsNullOrWhiteSpace(correo)) return false;

            string infoAdicional = "";
            if (!string.IsNullOrWhiteSpace(numeroFactura))
                infoAdicional += $"<div class='row'><span>Factura Electrónica:</span><strong>{numeroFactura}</strong></div>";
            if (!string.IsNullOrWhiteSpace(numeroPedido))
                infoAdicional += $"<div class='row'><span>Número de Pedido:</span><strong>{numeroPedido}</strong></div>";

            string html = $@"
<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='utf-8'/>
    <style>
        body {{ font-family: 'Helvetica Neue', Arial, sans-serif; background-color: #F5EFE6; margin: 0; padding: 20px; color: #1C1416; }}
        .card {{ max-width: 600px; margin: 0 auto; background: #FFFDF9; border: 1px solid #E3D8C8; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.06); }}
        .header {{ background: #6B1E2E; color: #FFFDF9; padding: 28px 25px; text-align: center; border-bottom: 2px solid #C8973A; }}
        .header h1 {{ margin: 0; font-size: 24px; font-weight: 700; }}
        .body {{ padding: 26px 25px; line-height: 1.6; font-size: 15px; }}
        .monto {{ font-size: 32px; font-weight: 800; color: #6B1E2E; text-align: center; margin: 12px 0 20px; }}
        .box {{ background: #EFE6D8; border-radius: 12px; padding: 18px; margin: 15px 0; border: 1px solid #E3D8C8; }}
        .row {{ display: flex; justify-content: space-between; margin-bottom: 8px; font-size: 14px; }}
        .footer {{ background: #FAF6F0; padding: 16px 25px; text-align: center; font-size: 12px; color: #6E6468; border-top: 1px solid #E3D8C8; }}
    </style>
</head>
<body>
    <div class='card'>
        <div class='header'>
            <h1>SABORATOR</h1>
            <p style='margin:4px 0 0;color:#D4C2A5;font-size:12px;text-transform:uppercase;'>Comprobante de Pago Electrónico</p>
        </div>
        <div class='body'>
            <div style='text-align:center;color:#2A7348;font-weight:700;'>✓ Transacción Aprobada Exitosamente</div>
            <div class='monto'>${(monto).ToString("N0", ControlClientesRestaurante.Models.Formato.Co)} COP</div>
            <p>Estimado/a <strong>{nombreCliente}</strong>, hemos recibido tu pago a través de la pasarela de pagos segura Wompi.</p>
            <div class='box'>
                <div class='row'><span>Referencia:</span><strong style='font-family:monospace;'>{refPago}</strong></div>
                <div class='row'><span>Concepto:</span><strong>{concepto}</strong></div>
                <div class='row'><span>Medio de Pago:</span><strong>{metodo}</strong></div>
                <div class='row'><span>Fecha y Hora:</span><strong>{DateTime.Now:dd/MM/yyyy HH:mm}</strong></div>
                {infoAdicional}
            </div>
            <p style='font-size:13px;color:#5E5357;'>Este documento certifica el pago formal de tu consumo gastronómico en Saborator.</p>
        </div>
        <div class='footer'>
            &copy; {DateTime.Now.Year} Saborator Alta Cocina S.A.S. · NIT 901.847.219-4
        </div>
    </div>
</body>
</html>";

            return await EnviarCorreoAsync(correo, $"Recibo de Pago Aprobado Ref: {refPago} — Saborator", html);
        }

        public async Task<bool> EnviarConfirmacionPedidoAsync(
            string correo, string nombreCliente, string numeroPedido, string mesaODireccion, decimal total, List<(string Producto, int Cantidad, decimal Subtotal)> items)
        {
            if (string.IsNullOrWhiteSpace(correo)) return false;

            var itemsHtml = string.Join("", items.Select(i =>
                $"<tr style='border-bottom:1px solid #E3D8C8;'><td style='padding:8px 0;'>{i.Cantidad}x {i.Producto}</td><td style='text-align:right;padding:8px 0;'><strong>${(i.Subtotal).ToString("N0", ControlClientesRestaurante.Models.Formato.Co)}</strong></td></tr>"));

            string html = $@"
<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='utf-8'/>
    <style>
        body {{ font-family: 'Helvetica Neue', Arial, sans-serif; background-color: #F5EFE6; margin: 0; padding: 20px; color: #1C1416; }}
        .card {{ max-width: 600px; margin: 0 auto; background: #FFFDF9; border: 1px solid #E3D8C8; border-radius: 16px; overflow: hidden; }}
        .header {{ background: #6B1E2E; color: #FFFDF9; padding: 24px; text-align: center; border-bottom: 2px solid #C8973A; }}
        .body {{ padding: 24px; font-size: 15px; }}
        .footer {{ background: #FAF6F0; padding: 16px; text-align: center; font-size: 12px; color: #6E6468; }}
    </style>
</head>
<body>
    <div class='card'>
        <div class='header'>
            <h1 style='margin:0;'>SABORATOR</h1>
            <p style='margin:4px 0 0;color:#D4C2A5;font-size:12px;'>Comanda &amp; Despacho Gastronómico</p>
        </div>
        <div class='body'>
            <h2 style='color:#6B1E2E;margin-top:0;'>¡Pedido en preparación, {nombreCliente}!</h2>
            <p>Tu orden <strong>#{numeroPedido}</strong> ha entrado a la cocina de autor. Destino: <strong>{mesaODireccion}</strong>.</p>
            <table style='width:100%;border-collapse:collapse;margin:16px 0;font-size:14px;'>
                {itemsHtml}
                <tr style='font-size:16px;'><td style='padding:12px 0;color:#6B1E2E;'><strong>Total General:</strong></td><td style='text-align:right;padding:12px 0;color:#6B1E2E;'><strong>${(total).ToString("N0", ControlClientesRestaurante.Models.Formato.Co)} COP</strong></td></tr>
            </table>
        </div>
        <div class='footer'>
            &copy; {DateTime.Now.Year} Saborator Alta Cocina S.A.S.
        </div>
    </div>
</body>
</html>";

            return await EnviarCorreoAsync(correo, $"Orden #{numeroPedido} en Cocina — Saborator", html);
        }
    }
}
