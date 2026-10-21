using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddEstadoEmpresa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Habilitada",
                table: "Empresas",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Habilitada",
                table: "Empresas");
        }
    }
}
