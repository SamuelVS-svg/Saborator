using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ControlClientesRestaurante.Data;

#nullable disable

namespace ControlClientesRestaurante.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260905040000_AgregarProveedores")]
    public partial class AgregarProveedores : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_NAME = 'Proveedores'
                )
                BEGIN
                    CREATE TABLE [Proveedores] (
                        [Id]           INT IDENTITY(1,1) NOT NULL,
                        [Nombre]       NVARCHAR(100)  NOT NULL,
                        [Contacto]     NVARCHAR(100)  NULL,
                        [Telefono]     NVARCHAR(20)   NULL,
                        [Correo]       NVARCHAR(100)  NULL,
                        [Direccion]    NVARCHAR(200)  NULL,
                        [Nit]          NVARCHAR(20)   NULL,
                        [Categoria]    NVARCHAR(100)  NULL,
                        [Notas]        NVARCHAR(300)  NULL,
                        [Activo]       BIT            NOT NULL DEFAULT 1,
                        [FechaCreacion] DATETIME2     NOT NULL DEFAULT GETDATE(),
                        CONSTRAINT [PK_Proveedores] PRIMARY KEY ([Id])
                    );
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Proveedores");
        }
    }
}
