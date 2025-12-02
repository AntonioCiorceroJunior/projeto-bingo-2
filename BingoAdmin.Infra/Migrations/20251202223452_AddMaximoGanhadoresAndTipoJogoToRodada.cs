using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BingoAdmin.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddMaximoGanhadoresAndTipoJogoToRodada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaximoGanhadores",
                table: "Rodadas",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ModoPadroesDinamicos",
                table: "Rodadas",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TipoJogo",
                table: "Rodadas",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "RodadaPadroes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RodadaId = table.Column<int>(type: "INTEGER", nullable: false),
                    PadraoId = table.Column<int>(type: "INTEGER", nullable: false),
                    FoiSorteado = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RodadaPadroes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RodadaPadroes_Padroes_PadraoId",
                        column: x => x.PadraoId,
                        principalTable: "Padroes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RodadaPadroes_Rodadas_RodadaId",
                        column: x => x.RodadaId,
                        principalTable: "Rodadas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RodadaPadroes_PadraoId",
                table: "RodadaPadroes",
                column: "PadraoId");

            migrationBuilder.CreateIndex(
                name: "IX_RodadaPadroes_RodadaId",
                table: "RodadaPadroes",
                column: "RodadaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RodadaPadroes");

            migrationBuilder.DropColumn(
                name: "MaximoGanhadores",
                table: "Rodadas");

            migrationBuilder.DropColumn(
                name: "ModoPadroesDinamicos",
                table: "Rodadas");

            migrationBuilder.DropColumn(
                name: "TipoJogo",
                table: "Rodadas");
        }
    }
}
