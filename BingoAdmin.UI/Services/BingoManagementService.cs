using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using BingoAdmin.Domain.Entities;
using BingoAdmin.Domain.Services;
using BingoAdmin.Infra.Data;

namespace BingoAdmin.UI.Services
{
    public class BingoManagementService
    {
        private readonly BingoContext _context;
        private readonly BingoService _bingoDomainService;

        public BingoManagementService(BingoContext context, BingoService bingoDomainService)
        {
            _context = context;
            _bingoDomainService = bingoDomainService;
        }

        public async Task CriarBingoAsync(string nome, DateTime data, int qtdCombos, int cartelasPorCombo, int qtdRodadas, IProgress<string> progress)
        {
            progress.Report("Iniciando criação do bingo...");

            var bingo = new Bingo
            {
                Nome = nome,
                DataInicioPrevista = data,
                QuantidadeCombos = qtdCombos,
                CartelasPorCombo = cartelasPorCombo,
                QuantidadeRodadas = qtdRodadas,
                Status = "Rascunho",
                UsuarioCriadorId = 1 // TODO: Pegar do usuário logado
            };

            _context.Bingos.Add(bingo);
            await _context.SaveChangesAsync();

            progress.Report("Gerando rodadas...");
            // Gerar rodadas automaticamente
            for (int i = 1; i <= qtdRodadas; i++)
            {
                var rodada = new Rodada
                {
                    BingoId = bingo.Id,
                    NumeroOrdem = i,
                    TipoPremio = "",
                    Descricao = "",
                    PadraoId = null, // Padrão em branco
                    Status = "NaoIniciada",
                    EhRodadaExtra = false
                };
                _context.Rodadas.Add(rodada);
            }
            await _context.SaveChangesAsync();

            progress.Report("Gerando cartelas e combos (isso pode demorar)...");

            // Run generation in background thread to not freeze UI
            var combos = await Task.Run(() => 
                _bingoDomainService.GerarCombos(bingo.Id, qtdCombos, cartelasPorCombo)
            );

            progress.Report($"Salvando {combos.Count} combos e {combos.Count * cartelasPorCombo} cartelas no banco...");

            _context.Combos.AddRange(combos);
            
            await _context.SaveChangesAsync();

            progress.Report("Concluído!");
        }

        public async Task AtualizarBingoAsync(int id, string nome, DateTime data, int qtdRodadas)
        {
            var bingo = await _context.Bingos.Include(b => b.Rodadas).FirstOrDefaultAsync(b => b.Id == id);
            if (bingo != null)
            {
                bingo.Nome = nome;
                bingo.DataInicioPrevista = data;
                
                // Ajustar quantidade de rodadas
                if (qtdRodadas != bingo.QuantidadeRodadas)
                {
                    bingo.QuantidadeRodadas = qtdRodadas;
                    
                    // Se aumentou, cria novas
                    int atuais = bingo.Rodadas.Count;
                    if (qtdRodadas > atuais)
                    {
                        for (int i = atuais + 1; i <= qtdRodadas; i++)
                        {
                            _context.Rodadas.Add(new Rodada
                            {
                                BingoId = bingo.Id,
                                NumeroOrdem = i,
                                TipoPremio = "",
                                Descricao = "",
                                PadraoId = null,
                                Status = "NaoIniciada"
                            });
                        }
                    }
                    // Se diminuiu, remove as últimas (apenas se não iniciadas)
                    else if (qtdRodadas < atuais)
                    {
                        var paraRemover = bingo.Rodadas
                            .OrderByDescending(r => r.NumeroOrdem)
                            .Take(atuais - qtdRodadas)
                            .ToList();
                        
                        _context.Rodadas.RemoveRange(paraRemover);
                    }
                }

                await _context.SaveChangesAsync();
            }
        }

        public async Task ExcluirBingoAsync(int id)
        {
            var bingo = await _context.Bingos.FindAsync(id);
            if (bingo != null)
            {
                _context.Bingos.Remove(bingo);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Bingo>> ListarBingosAsync()
        {
            return await _context.Bingos.OrderByDescending(b => b.DataInicioPrevista).ToListAsync();
        }
    }
}
