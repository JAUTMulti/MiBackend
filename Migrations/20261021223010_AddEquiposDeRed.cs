using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MiBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddEquiposDeRed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DispositivosRed",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmpresaId = table.Column<int>(type: "integer", nullable: false),
                    SedeId = table.Column<int>(type: "integer", nullable: true),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Fabricante = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Modelo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NumeroSerie = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    VersionFirmware = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispositivosRed", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispositivosRed_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DispositivosRed_Sedes_SedeId",
                        column: x => x.SedeId,
                        principalTable: "Sedes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "InterfacesDispositivosRed",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DispositivoRedId = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DireccionMac = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterfacesDispositivosRed", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterfacesDispositivosRed_DispositivosRed_DispositivoRedId",
                        column: x => x.DispositivoRedId,
                        principalTable: "DispositivosRed",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IpsDispositivosRed",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InterfazDispositivoRedId = table.Column<int>(type: "integer", nullable: false),
                    Direccion = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IpsDispositivosRed", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IpsDispositivosRed_InterfacesDispositivosRed_InterfazDispos~",
                        column: x => x.InterfazDispositivoRedId,
                        principalTable: "InterfacesDispositivosRed",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DispositivosRed_EmpresaId_Nombre",
                table: "DispositivosRed",
                columns: new[] { "EmpresaId", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DispositivosRed_SedeId",
                table: "DispositivosRed",
                column: "SedeId");

            migrationBuilder.CreateIndex(
                name: "IX_InterfacesDispositivosRed_DispositivoRedId_Nombre",
                table: "InterfacesDispositivosRed",
                columns: new[] { "DispositivoRedId", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IpsDispositivosRed_InterfazDispositivoRedId_Direccion",
                table: "IpsDispositivosRed",
                columns: new[] { "InterfazDispositivoRedId", "Direccion" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IpsDispositivosRed");

            migrationBuilder.DropTable(
                name: "InterfacesDispositivosRed");

            migrationBuilder.DropTable(
                name: "DispositivosRed");
        }
    }
}
