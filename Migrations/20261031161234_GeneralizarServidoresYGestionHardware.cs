using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiBackend.Migrations
{
    /// <inheritdoc />
    public partial class GeneralizarServidoresYGestionHardware : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "Servidores"
                SET "Tipo" = CASE
                    WHEN "Tipo" = 'Virtual' OR "ServidorProxmoxId" IS NOT NULL THEN 'Virtual'
                    ELSE 'Fisico'
                END;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Servidores_Servidores_ServidorProxmoxId",
                table: "Servidores");

            migrationBuilder.RenameColumn(
                name: "Tipo",
                table: "Servidores",
                newName: "Naturaleza");

            migrationBuilder.RenameColumn(
                name: "SistemaOperativo",
                table: "Servidores",
                newName: "Plataforma");

            migrationBuilder.RenameColumn(
                name: "ServidorProxmoxId",
                table: "Servidores",
                newName: "ServidorAnfitrionId");

            migrationBuilder.RenameIndex(
                name: "IX_Servidores_ServidorProxmoxId",
                table: "Servidores",
                newName: "IX_Servidores_ServidorAnfitrionId");

            migrationBuilder.AddColumn<string>(
                name: "IpGestionHardware",
                table: "Servidores",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoGestionHardware",
                table: "Servidores",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Servidores_Servidores_ServidorAnfitrionId",
                table: "Servidores",
                column: "ServidorAnfitrionId",
                principalTable: "Servidores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "Servidores"
                SET "Naturaleza" = CASE
                    WHEN "Naturaleza" = 'Virtual' THEN 'Virtual'
                    WHEN lower("Plataforma") LIKE '%proxmox%' THEN 'Proxmox'
                    ELSE 'Fisico'
                END;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Servidores_Servidores_ServidorAnfitrionId",
                table: "Servidores");

            migrationBuilder.DropColumn(
                name: "IpGestionHardware",
                table: "Servidores");

            migrationBuilder.DropColumn(
                name: "TipoGestionHardware",
                table: "Servidores");

            migrationBuilder.RenameColumn(
                name: "ServidorAnfitrionId",
                table: "Servidores",
                newName: "ServidorProxmoxId");

            migrationBuilder.RenameColumn(
                name: "Plataforma",
                table: "Servidores",
                newName: "SistemaOperativo");

            migrationBuilder.RenameColumn(
                name: "Naturaleza",
                table: "Servidores",
                newName: "Tipo");

            migrationBuilder.RenameIndex(
                name: "IX_Servidores_ServidorAnfitrionId",
                table: "Servidores",
                newName: "IX_Servidores_ServidorProxmoxId");

            migrationBuilder.AddForeignKey(
                name: "FK_Servidores_Servidores_ServidorProxmoxId",
                table: "Servidores",
                column: "ServidorProxmoxId",
                principalTable: "Servidores",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
