using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlClientesRestaurante.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCodigoCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "Clientes",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Codigo",
                table: "Clientes",
                column: "Codigo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Clientes_Codigo",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "Clientes");
        }
    }
}
