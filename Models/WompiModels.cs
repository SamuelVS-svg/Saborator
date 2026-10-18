namespace ControlClientesRestaurante.Models
{
    /// <summary>
    /// Solicitud del frontend para crear una sesión de pago firmada dinámicamente desde el servidor
    /// </summary>
    public class IniciarSesionWompiRequest
    {
        public string TipoOrigen { get; set; } = "Pedido"; // Pedido | Reserva | Directo
        public int? OrigenId { get; set; }
        public string? NombreCliente { get; set; }
        public string? Correo { get; set; }
        public string? Telefono { get; set; }
    }

    /// <summary>
    /// Datos firmados y no alterables devueltos al frontend para iniciar el widget de Wompi
    /// </summary>
    public class SesionWompiFirmadaResponse
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public string PublicKey { get; set; } = string.Empty;
        public string Currency { get; set; } = "COP";
        public long AmountInCents { get; set; }
        public decimal MontoPesos { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string IntegritySignature { get; set; } = string.Empty;
        public string RedirectUrl { get; set; } = string.Empty;
        public string Concepto { get; set; } = string.Empty;
        public CustomerDataWompi? CustomerData { get; set; }
    }

    public class CustomerDataWompi
    {
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? PhoneNumberPrefix { get; set; } = "+57";
        public string? LegalId { get; set; }
        public string? LegalIdType { get; set; } = "CC";
    }

    /// <summary>
    /// Respuesta de consulta de transacción en la API REST de Wompi
    /// </summary>
    public class WompiConsultaTransaccionResponse
    {
        public WompiTransactionData? Data { get; set; }
    }

    public class WompiTransactionData
    {
        public string Id { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public long AmountInCents { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string Currency { get; set; } = "COP";
        public string PaymentMethodType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // APPROVED, DECLINED, VOIDED, ERROR, PENDING
        public string? StatusMessage { get; set; }
        public Dictionary<string, object>? PaymentMethod { get; set; }
    }
}
