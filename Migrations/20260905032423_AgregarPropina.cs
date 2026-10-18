using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlClientesRestaurante.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPropina : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // La columna puede ya existir si fue creada manualmente — se agrega solo si no está
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = 'Facturas' AND COLUMN_NAME = 'Propina'
                )
                BEGIN
                    ALTER TABLE [Facturas] ADD [Propina] decimal(18,2) NOT NULL DEFAULT 0.0;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Propina",
                table: "Facturas");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_Nombre",
                table: "Productos",
                column: "Nombre");
        }
    }
}
