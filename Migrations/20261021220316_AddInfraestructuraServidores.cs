using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MiBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddInfraestructuraServidores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Servidores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmpresaId = table.Column<int>(type: "integer", nullable: false),
                    ServidorProxmoxId = table.Column<int>(type: "integer", nullable: true),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SistemaOperativo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    VersionSistemaOperativo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Nucleos = table.Column<int>(type: "integer", nullable: false),
                    RamGb = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servidores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Servidores_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Servidores_Servidores_ServidorProxmoxId",
                        column: x => x.ServidorProxmoxId,
                        principalTable: "Servidores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "InterfacesRed",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServidorId = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DireccionMac = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterfacesRed", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterfacesRed_Servidores_ServidorId",
                        column: x => x.ServidorId,
                        principalTable: "Servidores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DireccionesIp",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InterfazRedId = table.Column<int>(type: "integer", nullable: false),
                    Direccion = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DireccionesIp", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DireccionesIp_InterfacesRed_InterfazRedId",
                        column: x => x.InterfazRedId,
                        principalTable: "InterfacesRed",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DireccionesIp_InterfazRedId_Direccion",
                table: "DireccionesIp",
                columns: new[] { "InterfazRedId", "Direccion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InterfacesRed_ServidorId_Nombre",
                table: "InterfacesRed",
                columns: new[] { "ServidorId", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Servidores_EmpresaId",
                table: "Servidores",
                column: "EmpresaId");

            migrationBuilder.CreateIndex(
                name: "IX_Servidores_ServidorProxmoxId",
                table: "Servidores",
                column: "ServidorProxmoxId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DireccionesIp");

            migrationBuilder.DropTable(
                name: "InterfacesRed");

            migrationBuilder.DropTable(
                name: "Servidores");
        }
    }
}
