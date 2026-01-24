using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BingoAdmin.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddPremiosAndGameModes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ModoDisputaPremios",
                table: "Rodadas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PremioId",
                table: "Ganhadores",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModoJogo",
                table: "Bingos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Premios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RodadaId = table.Column<int>(type: "int", nullable: true),
                    BingoId = table.Column<int>(type: "int", nullable: true),
                    PadraoId = table.Column<int>(type: "int", nullable: true),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ordem = table.Column<int>(type: "int", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Premios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Premios_Bingos_BingoId",
                        column: x => x.BingoId,
                        principalTable: "Bingos",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Premios_Padroes_PadraoId",
                        column: x => x.PadraoId,
                        principalTable: "Padroes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Premios_Rodadas_RodadaId",
                        column: x => x.RodadaId,
                        principalTable: "Rodadas",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ganhadores_PremioId",
                table: "Ganhadores",
                column: "PremioId");

            migrationBuilder.CreateIndex(
                name: "IX_Premios_BingoId",
                table: "Premios",
                column: "BingoId");

            migrationBuilder.CreateIndex(
                name: "IX_Premios_PadraoId",
                table: "Premios",
                column: "PadraoId");

            migrationBuilder.CreateIndex(
                name: "IX_Premios_RodadaId",
                table: "Premios",
                column: "RodadaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ganhadores_Premios_PremioId",
                table: "Ganhadores",
                column: "PremioId",
                principalTable: "Premios",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ganhadores_Premios_PremioId",
                table: "Ganhadores");

            migrationBuilder.DropTable(
                name: "Premios");

            migrationBuilder.DropIndex(
                name: "IX_Ganhadores_PremioId",
                table: "Ganhadores");

            migrationBuilder.DropColumn(
                name: "ModoDisputaPremios",
                table: "Rodadas");

            migrationBuilder.DropColumn(
                name: "PremioId",
                table: "Ganhadores");

            migrationBuilder.DropColumn(
                name: "ModoJogo",
                table: "Bingos");
        }
    }
}
