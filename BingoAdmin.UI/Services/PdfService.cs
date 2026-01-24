using System.Collections.Generic;
using System.IO;
using System.Linq;
using BingoAdmin.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BingoAdmin.UI.Services
{
    public class PdfService
    {
        public void GenerateComboPdf(Combo combo, string bingoName, string filePath)
        {
            // Prepare items with Kit context
            var renderItems = new List<(Cartela Cartela, int KitNumero)>();

            if (combo.Kits != null)
            {
               foreach(var kit in combo.Kits.OrderBy(k => k.NumeroKitNoCombo))
               {
                   if (kit.Cartelas != null)
                   {
                        foreach(var cartela in kit.Cartelas.OrderBy(c => c.NumeroCartelaNoKit))
                        {
                            renderItems.Add((cartela, kit.NumeroKitNoCombo));
                        }
                   }
               }
            }

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .Column(c =>
                        {
                            c.Item().Text($"Bingo: {bingoName}").SemiBold().FontSize(20).FontColor(Colors.Black);
                            c.Item().Text($"Combo #{combo.NumeroCombo} - {combo.NomeDono}").FontSize(16).FontColor(Colors.Grey.Darken2);
                        });

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            int batchSize = 4;
                            for (int i = 0; i < renderItems.Count; i += batchSize)
                            {
                                var chunk = renderItems.Skip(i).Take(batchSize).ToList();

                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                    });

                                    foreach (var item in chunk)
                                    {
                                        table.Cell().Padding(10).Element(e => RenderCartela(e, item.Cartela, combo.NumeroCombo, item.KitNumero, combo.NomeDono, item.Cartela.NumeroGlobal));
                                    }
                                });

                                if (i + batchSize < renderItems.Count)
                                {
                                    column.Item().PageBreak();
                                }
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Página ");
                            x.CurrentPageNumber();
                        });
                });
            })
            .GeneratePdf(filePath);
        }

        private void RenderCartela(IContainer container, Cartela cartela, int comboNumero, int kitNumero, string nomeDono, int numeroCartelaGlobal)
        {
            var blueColor = "#5DADE2";
            var grayColor = "#CCCCCC";
            var redColor = Colors.Red.Darken2; // Red Dark for Global Number

            container
                .ShowEntire()
                .Border(1)
                .BorderColor(Colors.Grey.Lighten1)
                .Background(Colors.White)
                .Padding(10)
                .Column(column =>
                {
                    // Header - Stacked to avoid overlap
                    column.Item().PaddingBottom(0).AlignRight().Text($"Nº {numeroCartelaGlobal:D4}")
                        .FontSize(10).Bold().FontColor(redColor);

                    column.Item().PaddingBottom(5).AlignCenter().Text(text => 
                    {
                        text.Span($"CARTELA {cartela.NumeroCartelaNoKit}").FontSize(14).Black().ExtraBold();
                        text.Span($" | KIT {kitNumero} | COMBO {comboNumero}").FontSize(14).Black().NormalWeight();
                    });
                    
                    // Owner Name
                    column.Item().PaddingBottom(5).AlignCenter().Text($"{nomeDono}")
                        .FontSize(12).FontColor(Colors.Grey.Darken2);

                    column.Item().Table(grid =>
                    {
                        grid.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn();
                            cols.RelativeColumn();
                            cols.RelativeColumn();
                            cols.RelativeColumn();
                            cols.RelativeColumn();
                        });

                        // Headers
                        var headers = new[] { "B", "I", "N", "G", "O" };
                        foreach (var h in headers)
                        {
                            grid.Cell().Padding(2).Element(c => 
                                c.AlignCenter()
                                 .AlignMiddle()
                                 .Text(h)
                                 .FontSize(16)
                                 .Bold()
                                 .FontColor(blueColor)
                            );
                        }

                        var numbers = cartela.GridNumeros.Split(',').Select(int.Parse).ToArray();

                        for (int row = 0; row < 5; row++)
                        {
                            for (int col = 0; col < 5; col++)
                            {
                                int index = row * 5 + col;
                                int number = numbers[index];
                                bool isFree = number == 0;
                                string text = isFree ? "FREE" : number.ToString();
                                
                                grid.Cell().Padding(2).Element(c => 
                                {
                                    var bg = isFree ? Color.FromHex("#F5F5DC") : Colors.White; // Beige for FREE
                                    
                                    c.Border(1)
                                     .BorderColor(grayColor)
                                     .Background(bg)
                                     .Height(35)
                                     .AlignCenter()
                                     .AlignMiddle()
                                     .Text(text)
                                     .FontSize(14)
                                     .Bold()
                                     .FontColor(Colors.Black);
                                });
                            }
                        }
                    });
                });
        }
    }
}
