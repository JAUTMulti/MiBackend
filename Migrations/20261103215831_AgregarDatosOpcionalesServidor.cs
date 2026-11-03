using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiBackend.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDatosOpcionalesServidor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DatosOpcionales",
                table: "Servidores",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DatosOpcionales",
                table: "Servidores");
        }
    }
}
