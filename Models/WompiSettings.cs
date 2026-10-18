namespace ControlClientesRestaurante.Models
{
    /// <summary>
    /// Configuración de la pasarela de pagos Wompi (Bancolombia)
    /// </summary>
    public class WompiSettings
    {
        /// <summary>
        /// Llave pública Wompi (pub_prod_... o pub_test_...)
        /// </summary>
        public string PublicKey { get; set; } = string.Empty;

        /// <summary>
        /// Llave privada Wompi para consultas autenticadas de servidor a servidor (prv_prod_... o prv_test_...)
        /// </summary>
        public string PrivateKey { get; set; } = string.Empty;

        /// <summary>
        /// Llave pública alternativa para pruebas locales en Sandbox
        /// </summary>
        public string SandboxPublicKey { get; set; } = "pub_test_Q5yDA9xoKdePium1amu0Bush320D4HfR";

        /// <summary>
        /// Llave privada alternativa para pruebas locales en Sandbox
        /// </summary>
        public string SandboxPrivateKey { get; set; } = "prv_test_V0n2v7u96K1F38sLz9G5j7Vb9mC2D4eF";

        /// <summary>
        /// Entorno de ejecución: "production" o "sandbox"
        /// </summary>
        public string Environment { get; set; } = "production";

        /// <summary>
        /// Moneda oficial para cobros (Por defecto COP)
        /// </summary>
        public string Currency { get; set; } = "COP";

        /// <summary>
        /// Secreto de integridad configurado en el panel de comercio para cálculo de SHA-256
        /// </summary>
        public string IntegritySecret { get; set; } = string.Empty;

        /// <summary>
        /// Secreto de eventos para validación criptográfica de webhooks
        /// </summary>
        public string EventSecret { get; set; } = string.Empty;

        /// <summary>
        /// URL pública del webhook registrada en el panel de Wompi
        /// </summary>
        public string WebhookUrl { get; set; } = "https://saborator.com/api/pagos/wompi-webhook";

        /// <summary>
        /// Retorna la URL base de la API REST de Wompi según el entorno activo
        /// </summary>
        public string ApiBaseUrl => Environment.Equals("sandbox", StringComparison.OrdinalIgnoreCase)
            ? "https://sandbox.wompi.co/v1"
            : "https://production.wompi.co/v1";

        /// <summary>
        /// Retorna la URL del CDN del widget oficial de Wompi
        /// </summary>
        public string WidgetUrl => "https://checkout.wompi.co/widget.js";
    }
}
