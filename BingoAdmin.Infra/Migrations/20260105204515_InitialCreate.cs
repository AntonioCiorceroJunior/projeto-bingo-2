using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BingoAdmin.Infra.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DesempateItens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BingoId = table.Column<int>(type: "int", nullable: false),
                    RodadaId = table.Column<int>(type: "int", nullable: false),
                    CartelaId = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Combo = table.Column<int>(type: "int", nullable: false),
                    CartelaNumero = table.Column<int>(type: "int", nullable: false),
                    PedraMaior = table.Column<int>(type: "int", nullable: false),
                    IsVencedor = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DesempateItens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Despesas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BingoId = table.Column<int>(type: "int", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Despesas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Padroes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Mascara = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsPredefinido = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Padroes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SenhaHash = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bingos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataInicioPrevista = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QuantidadeCombos = table.Column<int>(type: "int", nullable: false),
                    CartelasPorCombo = table.Column<int>(type: "int", nullable: false),
                    QuantidadeRodadas = table.Column<int>(type: "int", nullable: false),
                    ValorPorCombo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ModoPadroesDinamicos = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsuarioCriadorId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bingos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bingos_Usuarios_UsuarioCriadorId",
                        column: x => x.UsuarioCriadorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BingoPadroes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BingoId = table.Column<int>(type: "int", nullable: false),
                    PadraoId = table.Column<int>(type: "int", nullable: false),
                    FoiSorteado = table.Column<bool>(type: "bit", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "Combos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BingoId = table.Column<int>(type: "int", nullable: false),
                    NumeroCombo = table.Column<int>(type: "int", nullable: false),
                    NomeDono = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Pagamento = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataConfirmacao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Observacoes = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Combos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Combos_Bingos_BingoId",
                        column: x => x.BingoId,
                        principalTable: "Bingos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Rodadas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BingoId = table.Column<int>(type: "int", nullable: false),
                    NumeroOrdem = table.Column<int>(type: "int", nullable: false),
                    TipoPremio = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PadraoId = table.Column<int>(type: "int", nullable: true),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EhRodadaExtra = table.Column<bool>(type: "bit", nullable: false),
                    ModoPadroesDinamicos = table.Column<bool>(type: "bit", nullable: false),
                    MaximoGanhadores = table.Column<int>(type: "int", nullable: true),
                    TipoJogo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rodadas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rodadas_Bingos_BingoId",
                        column: x => x.BingoId,
                        principalTable: "Bingos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Rodadas_Padroes_PadraoId",
                        column: x => x.PadraoId,
                        principalTable: "Padroes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Cartelas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BingoId = table.Column<int>(type: "int", nullable: false),
                    ComboId = table.Column<int>(type: "int", nullable: false),
                    NumeroCartelaNoCombo = table.Column<int>(type: "int", nullable: false),
                    GridNumeros = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HashUnico = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cartelas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cartelas_Combos_ComboId",
                        column: x => x.ComboId,
                        principalTable: "Combos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RodadaPadroes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RodadaId = table.Column<int>(type: "int", nullable: false),
                    PadraoId = table.Column<int>(type: "int", nullable: false),
                    FoiSorteado = table.Column<bool>(type: "bit", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "Sorteios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BingoId = table.Column<int>(type: "int", nullable: false),
                    RodadaId = table.Column<int>(type: "int", nullable: false),
                    DataHoraInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataHoraFim = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BolasSorteadas = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sorteios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sorteios_Rodadas_RodadaId",
                        column: x => x.RodadaId,
                        principalTable: "Rodadas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ganhadores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RodadaId = table.Column<int>(type: "int", nullable: false),
                    CartelaId = table.Column<int>(type: "int", nullable: false),
                    IsVencedorFinal = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ganhadores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ganhadores_Cartelas_CartelaId",
                        column: x => x.CartelaId,
                        principalTable: "Cartelas",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Ganhadores_Rodadas_RodadaId",
                        column: x => x.RodadaId,
                        principalTable: "Rodadas",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PedraMaiorSorteios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RodadaId = table.Column<int>(type: "int", nullable: false),
                    GanhadorId = table.Column<int>(type: "int", nullable: false),
                    NumeroSorteado = table.Column<int>(type: "int", nullable: false),
                    OrdemSorteio = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedraMaiorSorteios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedraMaiorSorteios_Ganhadores_GanhadorId",
                        column: x => x.GanhadorId,
                        principalTable: "Ganhadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BingoPadroes_BingoId",
                table: "BingoPadroes",
                column: "BingoId");

            migrationBuilder.CreateIndex(
                name: "IX_BingoPadroes_PadraoId",
                table: "BingoPadroes",
                column: "PadraoId");

            migrationBuilder.CreateIndex(
                name: "IX_Bingos_UsuarioCriadorId",
                table: "Bingos",
                column: "UsuarioCriadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Cartelas_ComboId",
                table: "Cartelas",
                column: "ComboId");

            migrationBuilder.CreateIndex(
                name: "IX_Combos_BingoId",
                table: "Combos",
                column: "BingoId");

            migrationBuilder.CreateIndex(
                name: "IX_Ganhadores_CartelaId",
                table: "Ganhadores",
                column: "CartelaId");

            migrationBuilder.CreateIndex(
                name: "IX_Ganhadores_RodadaId",
                table: "Ganhadores",
                column: "RodadaId");

            migrationBuilder.CreateIndex(
                name: "IX_PedraMaiorSorteios_GanhadorId",
                table: "PedraMaiorSorteios",
                column: "GanhadorId");

            migrationBuilder.CreateIndex(
                name: "IX_RodadaPadroes_PadraoId",
                table: "RodadaPadroes",
                column: "PadraoId");

            migrationBuilder.CreateIndex(
                name: "IX_RodadaPadroes_RodadaId",
                table: "RodadaPadroes",
                column: "RodadaId");

            migrationBuilder.CreateIndex(
                name: "IX_Rodadas_BingoId",
                table: "Rodadas",
                column: "BingoId");

            migrationBuilder.CreateIndex(
                name: "IX_Rodadas_PadraoId",
                table: "Rodadas",
                column: "PadraoId");

            migrationBuilder.CreateIndex(
                name: "IX_Sorteios_RodadaId",
                table: "Sorteios",
                column: "RodadaId");
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

            migrationBuilder.DropTable(
                name: "PedraMaiorSorteios");

            migrationBuilder.DropTable(
                name: "RodadaPadroes");

            migrationBuilder.DropTable(
                name: "Sorteios");

            migrationBuilder.DropTable(
                name: "Ganhadores");

            migrationBuilder.DropTable(
                name: "Cartelas");

            migrationBuilder.DropTable(
                name: "Rodadas");

            migrationBuilder.DropTable(
                name: "Combos");

            migrationBuilder.DropTable(
                name: "Padroes");

            migrationBuilder.DropTable(
                name: "Bingos");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
