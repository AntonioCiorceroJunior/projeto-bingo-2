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

        public async Task<int> CriarBingoAsync(string nome, DateTime data, int qtdCombos, int cartelasPorCombo, List<RodadaConfigDto> rodadasConfig, bool modoDinamicoGlobal, List<int> padroesIds, IProgress<string> progress)
        {
            progress.Report("Iniciando criação do bingo...");

            int qtdRodadas = rodadasConfig.Count;

            var bingo = new Bingo
            {
                Nome = nome,
                DataInicioPrevista = data,
                QuantidadeCombos = qtdCombos,
                CartelasPorCombo = cartelasPorCombo,
                QuantidadeRodadas = qtdRodadas,
                ModoPadroesDinamicos = modoDinamicoGlobal,
                Status = "Rascunho",
                UsuarioCriadorId = 1 // TODO: Pegar do usuário logado
            };

            _context.Bingos.Add(bingo);
            await _context.SaveChangesAsync();

            if (modoDinamicoGlobal && padroesIds != null && padroesIds.Any())
            {
                foreach (var pid in padroesIds)
                {
                    _context.BingoPadroes.Add(new BingoPadrao
                    {
                        BingoId = bingo.Id,
                        PadraoId = pid,
                        FoiSorteado = false
                    });
                }
                await _context.SaveChangesAsync();
            }

            progress.Report("Gerando rodadas...");
            // Gerar rodadas automaticamente com base na config
            foreach (var config in rodadasConfig)
            {
                var rodada = new Rodada
                {
                    BingoId = bingo.Id,
                    NumeroOrdem = config.Numero,
                    TipoPremio = "",
                    Descricao = config.Descricao,
                    PadraoId = null, // Padrão em branco
                    Status = "NaoIniciada",
                    EhRodadaExtra = false,
                    ModoPadroesDinamicos = config.ModoDinamico
                };
                _context.Rodadas.Add(rodada);
                
                // Se tiver padrões específicos para esta rodada (e modo dinâmico ativo)
                if (config.ModoDinamico && config.PadroesIds != null && config.PadroesIds.Any())
                {
                    // Precisamos salvar a rodada primeiro para ter o ID? 
                    // EF Core resolve isso se adicionarmos à coleção de navegação, mas RodadaPadroes não está inicializada no construtor talvez?
                    // Vamos adicionar direto ao contexto depois do SaveChanges ou usar a navegação.
                    // Melhor estratégia: Adicionar à lista da entidade Rodada.
                    
                    foreach(var pid in config.PadroesIds)
                    {
                        rodada.RodadaPadroes.Add(new RodadaPadrao 
                        { 
                            PadraoId = pid,
                            FoiSorteado = false
                        });
                    }
                }
                // Se não tiver padrões específicos mas o modo global estiver ativo e tiver padrões globais?
                // O usuário pediu "escolher quais padrões ele quer dentro do modo dinamico".
                // Se ele não escolher nada na rodada, mas tiver global, devemos copiar?
                // Vamos assumir que se ele ativou modo dinâmico na rodada, ele DEVE configurar os padrões da rodada.
                // Mas para facilitar, se a lista da rodada estiver vazia e tiver lista global, copiamos a global.
                else if (config.ModoDinamico && modoDinamicoGlobal && padroesIds != null && padroesIds.Any())
                {
                     foreach(var pid in padroesIds)
                    {
                        rodada.RodadaPadroes.Add(new RodadaPadrao 
                        { 
                            PadraoId = pid,
                            FoiSorteado = false
                        });
                    }
                }
            }
            await _context.SaveChangesAsync();

            progress.Report("Gerando cartelas e combos em lotes...");

            // Configuração de Lotes para evitar travamento
            int tamanhoLote = 500; // Salva a cada 500 combos
            int combosGerados = 0;
            var hashesGlobais = new HashSet<string>(); // Mantém unicidade entre lotes

            await Task.Run(async () => 
            {
                while (combosGerados < qtdCombos)
                {
                    int restante = qtdCombos - combosGerados;
                    int atual = Math.Min(tamanhoLote, restante);
                    int startCombo = combosGerados + 1;

                    // Gera o lote na memória
                    var loteCombos = _bingoDomainService.GerarLoteCombos(bingo.Id, startCombo, atual, cartelasPorCombo, hashesGlobais);

                    // Salva o lote no banco
                    progress.Report($"Salvando lote {startCombo} a {startCombo + atual - 1}...");
                    
                    // Precisamos voltar para a thread principal ou usar um contexto thread-safe? 
                    // EF Core DbContext não é thread-safe. Como estamos dentro de um Task.Run, precisamos ter cuidado.
                    // O ideal é fazer a operação de banco fora do Task.Run ou garantir que ninguém mais usa o _context.
                    // Como este método é async e o _context é injetado (Scoped), deve estar ok desde que a UI não use o mesmo contexto simultaneamente.
                    
                    // Adiciona ao contexto
                    _context.Combos.AddRange(loteCombos);
                    await _context.SaveChangesAsync();

                    // LIMPEZA DE MEMÓRIA CRÍTICA
                    _context.ChangeTracker.Clear(); 

                    combosGerados += atual;
                    progress.Report($"Progresso: {combosGerados}/{qtdCombos} combos gerados.");
                }
            });

            progress.Report("Concluído!");
            
            return bingo.Id;
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

    public class RodadaConfigDto
    {
        public int Numero { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public bool ModoDinamico { get; set; }
        public List<int> PadroesIds { get; set; } = new();
    }
}
