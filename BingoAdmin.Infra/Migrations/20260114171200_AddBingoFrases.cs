using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BingoAdmin.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddBingoFrases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BingoFrases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Texto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FoiUsada = table.Column<bool>(type: "bit", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BingoFrases", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BingoFrases");

            migrationBuilder.DropColumn(
                name: "CartelasPorKit",
                table: "Bingos");

            migrationBuilder.DropColumn(
                name: "KitsPorCombo",
                table: "Bingos");

            migrationBuilder.DropColumn(
                name: "QuantidadeRodadas",
                table: "Bingos");

            migrationBuilder.DropColumn(
                name: "TemCombos",
                table: "Bingos");
        }
    }
}
