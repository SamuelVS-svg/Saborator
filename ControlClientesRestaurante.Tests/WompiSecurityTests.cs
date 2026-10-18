using System.Security.Cryptography;
using System.Text;
using ControlClientesRestaurante.Models;
using ControlClientesRestaurante.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ControlClientesRestaurante.Tests
{
    public class WompiSecurityTests
    {
        private WompiService CrearServicio(string integritySecret = "secreto_integridad_restaurante", string eventSecret = "secreto_evento_restaurante")
        {
            var options = Options.Create(new WompiSettings
            {
                PublicKey = "pub_prod_3LfjIbKhTNWppn3OIYoG3aVNYUzmWUnw",
                PrivateKey = "prv_prod_mgVvckn3K4JcmX0l98R9gg2YwBjEFrkW",
                Environment = "production",
                Currency = "COP",
                IntegritySecret = integritySecret,
                EventSecret = eventSecret
            });

            // Usamos NullLogger y null context para pruebas de criptografía pura
            return new WompiService(options, null!, null!, NullLogger<WompiService>.Instance);
        }

        [Fact]
        public void CalcularFirmaIntegridad_GeneraHashSha256Valido()
        {
            // Arrange
            var secreto = "mi_secreto_super_seguro";
            var servicio = CrearServicio(integritySecret: secreto);
            var referencia = "PED-0012";
            long montoCentavos = 8500000; // $85.000 COP
            var moneda = "COP";

            // Cálculo manual esperado: SHA256("PED-00128500000COPmi_secreto_super_seguro")
            var cadenaEsperada = $"{referencia}{montoCentavos}{moneda}{secreto}";
            var hashEsperado = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cadenaEsperada))).ToLowerInvariant();

            // Act
            var hashGenerado = servicio.CalcularFirmaIntegridad(referencia, montoCentavos, moneda);

            // Assert
            Assert.NotNull(hashGenerado);
            Assert.Equal(64, hashGenerado.Length); // 64 caracteres hexadecimales (256 bits)
            Assert.Equal(hashEsperado, hashGenerado);
        }

        [Fact]
        public void CalcularFirmaIntegridad_DetectaAlteracionDeMonto_PrevieneFraudeFrontend()
        {
            // Arrange
            var servicio = CrearServicio();
            var referencia = "PED-0099";
            long montoOriginal = 15000000; // $150.000 COP
            long montoAlterado = 10000;    // Atacante intenta pagar $100 COP

            // Act
            var hashOriginal = servicio.CalcularFirmaIntegridad(referencia, montoOriginal, "COP");
            var hashAlterado = servicio.CalcularFirmaIntegridad(referencia, montoAlterado, "COP");

            // Assert: Los hashes deben ser completamente diferentes
            Assert.NotEqual(hashOriginal, hashAlterado);
        }

        [Fact]
        public void CalcularFirmaIntegridad_DetectaAlteracionDeReferencia()
        {
            // Arrange
            var servicio = CrearServicio();
            long monto = 5000000;

            // Act
            var hashRef1 = servicio.CalcularFirmaIntegridad("PED-100", monto, "COP");
            var hashRef2 = servicio.CalcularFirmaIntegridad("PED-101", monto, "COP");

            // Assert
            Assert.NotEqual(hashRef1, hashRef2);
        }

        [Fact]
        public void ValidarChecksumEvento_VerificaPayloadWompiCorrectamente()
        {
            // Arrange
            var eventSecret = "wompi_event_secret_valido";
            var servicio = CrearServicio(eventSecret: eventSecret);

            var txId = "12345-67890-test";
            var status = "APPROVED";
            var amountInCents = 4500000;
            long timestamp = 1712890000;

            // Simular cálculo oficial de Wompi: Concatenar valores de propiedades + timestamp + eventSecret
            var cadenaParaChecksum = $"{txId}{status}{amountInCents}{timestamp}{eventSecret}";
            var checksumOficial = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cadenaParaChecksum))).ToLowerInvariant();

            var rawJson = $@"{{
                ""event"": ""transaction.updated"",
                ""data"": {{
                    ""transaction"": {{
                        ""id"": ""{txId}"",
                        ""status"": ""{status}"",
                        ""amount_in_cents"": {amountInCents}
                    }}
                }},
                ""timestamp"": {timestamp},
                ""signature"": {{
                    ""properties"": [""transaction.id"", ""transaction.status"", ""transaction.amount_in_cents""],
                    ""checksum"": ""{checksumOficial}""
                }}
            }}";

            var properties = new[] { "transaction.id", "transaction.status", "transaction.amount_in_cents" };

            // Act
            var esValido = servicio.ValidarChecksumEvento(rawJson, checksumOficial, properties, timestamp);

            // Assert
            Assert.True(esValido);
        }

        [Fact]
        public void ValidarChecksumEvento_RechazaFirmaFalsificada()
        {
            // Arrange
            var servicio = CrearServicio(eventSecret: "secreto_real");

            var rawJson = @"{
                ""event"": ""transaction.updated"",
                ""data"": { ""transaction"": { ""id"": ""tx-999"", ""status"": ""APPROVED"" } },
                ""timestamp"": 1712890000
            }";

            var checksumFalso = "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff";
            var properties = new[] { "transaction.id", "transaction.status" };

            // Act
            var esValido = servicio.ValidarChecksumEvento(rawJson, checksumFalso, properties, 1712890000);

            // Assert
            Assert.False(esValido);
        }

        [Fact]
        public void ObtenerConfiguracion_PriorizaVariablesDeEntorno()
        {
            // Arrange
            var servicio = CrearServicio();
            var llavePruebaEnv = "pub_prod_VARIABLE_ENTORNO_TEST";
            Environment.SetEnvironmentVariable("WOMPI_PUBLIC_KEY", llavePruebaEnv);

            try
            {
                // Act
                var config = servicio.ObtenerConfiguracion(esLocal: false);

                // Assert
                Assert.Equal(llavePruebaEnv, config.PublicKey);
            }
            finally
            {
                // Limpiar variable de entorno para no afectar otras pruebas
                Environment.SetEnvironmentVariable("WOMPI_PUBLIC_KEY", null);
            }
        }
    }
}
