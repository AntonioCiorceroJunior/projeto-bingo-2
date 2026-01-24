using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BingoAdmin.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddKitsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cartelas_Combos_ComboId",
                table: "Cartelas");

            migrationBuilder.DropIndex(
                name: "IX_Cartelas_ComboId",
                table: "Cartelas");

            migrationBuilder.DropColumn(
                name: "CartelasPorCombo",
                table: "Bingos");

            migrationBuilder.DropColumn(
                name: "ModoPadroesDinamicos",
                table: "Bingos");

            migrationBuilder.DropColumn(
                name: "QuantidadeRodadas",
                table: "Bingos");

            migrationBuilder.DropColumn(
                name: "ValorPorCombo",
                table: "Bingos");

            migrationBuilder.RenameColumn(
                name: "NumeroCartelaNoCombo",
                table: "Cartelas",
                newName: "NumeroGlobal");

            migrationBuilder.RenameColumn(
                name: "ComboId",
                table: "Cartelas",
                newName: "NumeroCartelaNoKit");

            migrationBuilder.AddColumn<int>(
                name: "KitId",
                table: "Cartelas",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Kits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ComboId = table.Column<int>(type: "int", nullable: false),
                    NumeroKitNoCombo = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Kits_Combos_ComboId",
                        column: x => x.ComboId,
                        principalTable: "Combos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cartelas_KitId",
                table: "Cartelas",
                column: "KitId");

            migrationBuilder.CreateIndex(
                name: "IX_Kits_ComboId",
                table: "Kits",
                column: "ComboId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cartelas_Kits_KitId",
                table: "Cartelas",
                column: "KitId",
                principalTable: "Kits",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cartelas_Kits_KitId",
                table: "Cartelas");

            migrationBuilder.DropTable(
                name: "Kits");

            migrationBuilder.DropIndex(
                name: "IX_Cartelas_KitId",
                table: "Cartelas");

            migrationBuilder.DropColumn(
                name: "KitId",
                table: "Cartelas");

            migrationBuilder.RenameColumn(
                name: "NumeroGlobal",
                table: "Cartelas",
                newName: "NumeroCartelaNoCombo");

            migrationBuilder.RenameColumn(
                name: "NumeroCartelaNoKit",
                table: "Cartelas",
                newName: "ComboId");

            migrationBuilder.AddColumn<int>(
                name: "CartelasPorCombo",
                table: "Bingos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "ModoPadroesDinamicos",
                table: "Bingos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "QuantidadeRodadas",
                table: "Bingos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorPorCombo",
                table: "Bingos",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Cartelas_ComboId",
                table: "Cartelas",
                column: "ComboId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cartelas_Combos_ComboId",
                table: "Cartelas",
                column: "ComboId",
                principalTable: "Combos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
