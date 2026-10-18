using ControlClientesRestaurante.Models;

namespace ControlClientesRestaurante.Services
{
    public interface IWompiService
    {
        /// <summary>
        /// Obtiene la configuración efectiva de Wompi considerando variables de entorno y modo local
        /// </summary>
        WompiSettings ObtenerConfiguracion(bool esLocal = false);

        /// <summary>
        /// Calcula la firma de integridad SHA-256 obligatoria para transacciones en Wompi
        /// Concatenación: referencia + montoEnCentavos + moneda + secretoIntegridad
        /// </summary>
        string CalcularFirmaIntegridad(string referencia, long montoEnCentavos, string moneda = "COP");

        /// <summary>
        /// Valida la firma criptográfica (checksum) del evento webhook transaction.updated enviado por Wompi
        /// </summary>
        bool ValidarChecksumEvento(string rawJson, string checksumRecibido, IEnumerable<string> properties, long? timestamp);

        /// <summary>
        /// Consulta el estado oficial de una transacción directamente con la API REST de Wompi usando la llave privada (Bearer)
        /// </summary>
        Task<WompiTransactionData?> ConsultarTransaccionAsync(string transaccionId);

        /// <summary>
        /// Prepara una sesión de pago protegida en backend con montos no alterables por el navegador
        /// </summary>
        Task<SesionWompiFirmadaResponse> CrearSesionPagoAsync(IniciarSesionWompiRequest? request, string baseUrl, bool esLocal);
    }
}
