using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ControlClientesRestaurante.Data;

#nullable disable

namespace ControlClientesRestaurante.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260905050000_AgregarMesas")]
    public partial class AgregarMesas : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Mesas'
                )
                BEGIN
                    CREATE TABLE [Mesas] (
                        [Id]           INT IDENTITY(1,1) NOT NULL,
                        [Numero]       INT           NOT NULL,
                        [Estado]       NVARCHAR(10)  NOT NULL DEFAULT 'Libre',
                        [PedidoId]     INT           NULL,
                        [FechaCreacion] DATETIME2    NOT NULL DEFAULT GETDATE(),
                        CONSTRAINT [PK_Mesas] PRIMARY KEY ([Id])
                    );
                END
            ");

            // Seed: 30 mesas
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM [Mesas])
                BEGIN
                    DECLARE @i INT = 1;
                    WHILE @i <= 30
                    BEGIN
                        INSERT INTO [Mesas] ([Numero],[Estado],[FechaCreacion])
                        VALUES (@i, 'Libre', GETDATE());
                        SET @i = @i + 1;
                    END
                END
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Mesas");
        }
    }
}
