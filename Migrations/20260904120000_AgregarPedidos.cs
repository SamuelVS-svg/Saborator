using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ControlClientesRestaurante.Migrations
{
    public partial class AgregarPedidos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pedidos",
                columns: table => new
                {
                    Id            = table.Column<int>(nullable: false).Annotation("SqlServer:Identity","1, 1"),
                    Numero        = table.Column<string>(nullable: false),
                    Mesa          = table.Column<string>(maxLength: 50, nullable: false),
                    NombreCliente = table.Column<string>(maxLength: 100, nullable: true),
                    UsuarioMesero = table.Column<string>(nullable: false),
                    Estado        = table.Column<string>(nullable: false),
                    Notas         = table.Column<string>(maxLength: 300, nullable: true),
                    Total         = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FacturaId     = table.Column<int>(nullable: true),
                    FechaCreacion = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pedidos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pedidos_Facturas_FacturaId",
                        column: x => x.FacturaId,
                        principalTable: "Facturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "LineasPedido",
                columns: table => new
                {
                    Id             = table.Column<int>(nullable: false).Annotation("SqlServer:Identity","1, 1"),
                    PedidoId       = table.Column<int>(nullable: false),
                    ProductoId     = table.Column<int>(nullable: false),
                    NombreProducto = table.Column<string>(maxLength: 100, nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Cantidad       = table.Column<int>(nullable: false),
                    Notas          = table.Column<string>(nullable: true),
                    FechaCreacion  = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineasPedido", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LineasPedido_Pedidos_PedidoId",
                        column: x => x.PedidoId,
                        principalTable: "Pedidos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LineasPedido_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "IX_Pedidos_Numero",     table: "Pedidos",     column: "Numero", unique: true);
            migrationBuilder.CreateIndex(name: "IX_Pedidos_FacturaId",  table: "Pedidos",     column: "FacturaId");
            migrationBuilder.CreateIndex(name: "IX_LineasPedido_PedidoId",   table: "LineasPedido", column: "PedidoId");
            migrationBuilder.CreateIndex(name: "IX_LineasPedido_ProductoId", table: "LineasPedido", column: "ProductoId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "LineasPedido");
            migrationBuilder.DropTable(name: "Pedidos");
        }
    }
}
