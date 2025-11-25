using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BingoAdmin.Infra.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSchema_v3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rodadas_Padroes_PadraoId",
                table: "Rodadas");

            migrationBuilder.AlterColumn<int>(
                name: "PadraoId",
                table: "Rodadas",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<string>(
                name: "Pagamento",
                table: "Combos",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "QuantidadeRodadas",
                table: "Bingos",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_Rodadas_Padroes_PadraoId",
                table: "Rodadas",
                column: "PadraoId",
                principalTable: "Padroes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rodadas_Padroes_PadraoId",
                table: "Rodadas");

            migrationBuilder.DropColumn(
                name: "Pagamento",
                table: "Combos");

            migrationBuilder.DropColumn(
                name: "QuantidadeRodadas",
                table: "Bingos");

            migrationBuilder.AlterColumn<int>(
                name: "PadraoId",
                table: "Rodadas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Rodadas_Padroes_PadraoId",
                table: "Rodadas",
                column: "PadraoId",
                principalTable: "Padroes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
