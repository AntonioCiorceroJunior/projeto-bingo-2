using System;
using System.Collections.Generic;
using System.Linq;
using BingoAdmin.Domain.Entities;
using BingoAdmin.Infra.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BingoAdmin.UI.Services
{
    public class RelatorioService
    {
        private readonly BingoContext _context;

        public RelatorioService(BingoContext context)
        {
            _context = context;
        }

        public void GerarRelatorioFinal(int bingoId, string filePath)
        {
            var bingo = _context.Bingos.Find(bingoId);
            if (bingo == null) throw new Exception("Bingo não encontrado.");

            var rodadas = _context.Rodadas
                .Include(r => r.Padrao)
                .Include(r => r.Ganhadores)
                    .ThenInclude(g => g.Cartela)
                    .ThenInclude(c => c.Combo)
                .Where(r => r.BingoId == bingoId)
                .OrderBy(r => r.NumeroOrdem)
                .ToList();

            var totalCombos = _context.Combos.Count(c => c.BingoId == bingoId);
            var totalCartelas = _context.Cartelas.Count(c => c.BingoId == bingoId);

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .Text($"Relatório Final: {bingo.Nome}")
                        .SemiBold().FontSize(24).FontColor(Colors.Blue.Darken2);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            // Resumo
                            column.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(5).Text("Resumo do Evento").FontSize(16).Bold();
                            column.Item().Text($"Data: {DateTime.Now:dd/MM/yyyy HH:mm}");
                            column.Item().Text($"Total de Combos: {totalCombos}");
                            column.Item().Text($"Total de Cartelas: {totalCartelas}");
                            column.Item().Text($"Total de Rodadas: {rodadas.Count}");
                            column.Spacing(20);

                            // Detalhes das Rodadas
                            column.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(5).Text("Resultados por Rodada").FontSize(16).Bold();
                            
                            foreach (var rodada in rodadas)
                            {
                                column.Item().PaddingTop(10).Text($"Rodada #{rodada.NumeroOrdem}: {rodada.Descricao}").Bold().FontSize(14);
                                column.Item().Text($"Prêmio: {rodada.TipoPremio}");
                                column.Item().Text($"Padrão: {rodada.Padrao?.Nome ?? "N/A"}");
                                
                                if (rodada.Ganhadores.Any())
                                {
                                    column.Item().PaddingTop(5).Text("Ganhadores:").Underline();
                                    foreach (var ganhador in rodada.Ganhadores)
                                    {
                                        string status = ganhador.IsVencedorFinal ? " (VENCEDOR FINAL)" : "";
                                        string nome = ganhador.Cartela?.Combo?.NomeDono ?? "Desconhecido";
                                        int comboId = ganhador.Cartela?.ComboId ?? 0;
                                        
                                        column.Item().PaddingLeft(10).Text($"• {nome} - Combo {comboId} {status}");
                                    }
                                }
                                else
                                {
                                    column.Item().PaddingTop(5).Text("Nenhum ganhador registrado.").FontColor(Colors.Red.Medium);
                                }
                                
                                column.Item().PaddingVertical(5).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten3);
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Gerado pelo Bingo Admin - Página ");
                            x.CurrentPageNumber();
                        });
                });
            })
            .GeneratePdf(filePath);
        }

        public List<ResultadoGeralDto> GetResultadosGerais(int bingoId)
        {
            var resultados = new List<ResultadoGeralDto>();

            var rodadas = _context.Rodadas
                .Where(r => r.BingoId == bingoId)
                .OrderBy(r => r.NumeroOrdem)
                .ToList();

            foreach (var rodada in rodadas)
            {
                var ganhadores = _context.Ganhadores
                    .Where(g => g.RodadaId == rodada.Id)
                    .ToList();

                foreach (var g in ganhadores)
                {
                    var cartela = _context.Cartelas
                        .Include(c => c.Combo)
                        .FirstOrDefault(c => c.Id == g.CartelaId);

                    if (cartela != null)
                    {
                        resultados.Add(new ResultadoGeralDto
                        {
                            RodadaDescricao = $"{rodada.NumeroOrdem}ª Rodada - {rodada.Descricao}",
                            TipoPremio = rodada.TipoPremio,
                            NomeGanhador = cartela.Combo?.NomeDono ?? "Desconhecido",
                            ComboNumero = cartela.Combo?.NumeroCombo ?? 0,
                            CartelaNumero = cartela.NumeroCartelaNoCombo
                        });
                    }
                }
            }

            return resultados;
        }
    }

    public class ResultadoGeralDto
    {
        public string RodadaDescricao { get; set; } = string.Empty;
        public string TipoPremio { get; set; } = string.Empty;
        public string NomeGanhador { get; set; } = string.Empty;
        public int ComboNumero { get; set; }
        public int CartelaNumero { get; set; }
    }
}
