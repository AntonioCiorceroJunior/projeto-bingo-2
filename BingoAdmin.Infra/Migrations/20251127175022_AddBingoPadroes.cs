using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BingoAdmin.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddBingoPadroes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ModoPadroesDinamicos",
                table: "Bingos",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            /* 
            // Column already exists in DB
            migrationBuilder.AddColumn<decimal>(
                name: "ValorPorCombo",
                table: "Bingos",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
            */

            migrationBuilder.CreateTable(
                name: "BingoPadroes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BingoId = table.Column<int>(type: "INTEGER", nullable: false),
                    PadraoId = table.Column<int>(type: "INTEGER", nullable: false),
                    FoiSorteado = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BingoPadroes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BingoPadroes_Bingos_BingoId",
                        column: x => x.BingoId,
                        principalTable: "Bingos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BingoPadroes_Padroes_PadraoId",
                        column: x => x.PadraoId,
                        principalTable: "Padroes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            /*
            migrationBuilder.CreateTable(
                name: "DesempateItens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BingoId = table.Column<int>(type: "INTEGER", nullable: false),
                    RodadaId = table.Column<int>(type: "INTEGER", nullable: false),
                    CartelaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    Combo = table.Column<int>(type: "INTEGER", nullable: false),
                    CartelaNumero = table.Column<int>(type: "INTEGER", nullable: false),
                    PedraMaior = table.Column<int>(type: "INTEGER", nullable: false),
                    IsVencedor = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DesempateItens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Despesas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BingoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", nullable: false),
                    Valor = table.Column<decimal>(type: "TEXT", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Despesas", x => x.Id);
                });
            */

            migrationBuilder.CreateIndex(
                name: "IX_BingoPadroes_BingoId",
                table: "BingoPadroes",
                column: "BingoId");

            migrationBuilder.CreateIndex(
                name: "IX_BingoPadroes_PadraoId",
                table: "BingoPadroes",
                column: "PadraoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BingoPadroes");

            migrationBuilder.DropTable(
                name: "DesempateItens");

            migrationBuilder.DropTable(
                name: "Despesas");

            migrationBuilder.DropColumn(
                name: "ModoPadroesDinamicos",
                table: "Bingos");

            /*
            migrationBuilder.DropColumn(
                name: "ValorPorCombo",
                table: "Bingos");
            */
        }
    }
}
