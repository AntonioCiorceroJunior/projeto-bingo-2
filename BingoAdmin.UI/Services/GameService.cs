using System;
using System.Collections.Generic;
using System.Linq;
using BingoAdmin.Domain.Entities;
using BingoAdmin.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace BingoAdmin.UI.Services
{
    public class GameService
    {
        private readonly BingoContext _context;
        private List<CachedCartela> _cachedCartelas = new();
        private HashSet<int> _numerosSorteados = new();
        private HashSet<int> _ganhadoresIds = new();
        private Rodada? _rodadaAtual;
        private string _mascaraAtual = string.Empty;
        private List<BingoPadrao> _padroesDinamicosAtivos = new();
        private Random _random = new Random();
        private readonly FeedService _feedService;
        private readonly GameStatusService _gameStatusService;

        public event Action<int>? OnNumeroSorteado;
        public event Action<List<GanhadorInfo>>? OnGanhadoresEncontrados;
        public event Action<string>? OnPadraoDinamicoSorteado; // Evento para notificar UI

        public GameService(BingoContext context, FeedService feedService, GameStatusService gameStatusService)
        {
            _context = context;
            _feedService = feedService;
            _gameStatusService = gameStatusService;
        }

        public void CarregarDadosBingo(int bingoId)
        {
            _feedService.SwitchBingoContext(bingoId);
            // _feedService.AddMessage("Sistema", "Carregando dados do Bingo...", "Info"); // Reduced verbosity
            _cachedCartelas.Clear();
            var cartelas = _context.Cartelas
                .Include(c => c.Combo)
                .Where(c => c.BingoId == bingoId)
                .ToList();

            // Group by Combo to determine index
            var cartelasPorCombo = cartelas.GroupBy(c => c.ComboId);

            foreach (var group in cartelasPorCombo)
            {
                int index = 1;
                foreach (var c in group.OrderBy(x => x.Id)) // Assuming ID order is creation order
                {
                    var nums = c.GridNumeros.Split(',').Select(int.Parse).ToArray();
                    _cachedCartelas.Add(new CachedCartela
                    {
                        Id = c.Id,
                        ComboNumero = c.Combo?.NumeroCombo ?? 0,
                        NumeroCartela = index++,
                        Dono = c.Combo?.NomeDono ?? "Desconhecido",
                        Numeros = nums
                    });
                }
            }
        }

        public void IniciarRodada(int rodadaId)
        {
            // _feedService.AddMessage("Rodada", $"Iniciando rodada {rodadaId}...", "Info"); // Reduced verbosity as requested
            _gameStatusService.ClearRecentBalls();

            _rodadaAtual = _context.Rodadas
                .Include(r => r.Padrao)
                .Include(r => r.Bingo)
                .FirstOrDefault(r => r.Id == rodadaId);

            if (_rodadaAtual == null) throw new Exception("Rodada não encontrada");

            // Se não tiver padrão (ex: rodada extra), assume cartela cheia (tudo 1)
            _mascaraAtual = _rodadaAtual.Padrao?.Mascara ?? new string('1', 25);
            
            // Carregar padrões dinâmicos se necessário (Agora verifica a flag da RODADA)
            if (_rodadaAtual.ModoPadroesDinamicos)
            {
                // Tenta carregar padrões específicos da rodada
                var padroesRodada = _context.RodadaPadroes
                    .Include(rp => rp.Padrao)
                    .Where(rp => rp.RodadaId == _rodadaAtual.Id && !rp.FoiSorteado)
                    .Select(rp => new BingoPadrao 
                    { 
                        Id = rp.Id, // Note: This ID is from RodadaPadrao, but we map to BingoPadrao structure for compatibility or just use a common interface?
                        // Actually _padroesDinamicosAtivos is List<BingoPadrao>. We should change it to a generic or specific DTO.
                        // For now, let's map it manually or change the list type.
                        // Changing the list type is better.
                        Padrao = rp.Padrao,
                        PadraoId = rp.PadraoId,
                        FoiSorteado = rp.FoiSorteado,
                        BingoId = 0 // Dummy
                    })
                    .ToList();

                if (padroesRodada.Any())
                {
                    _padroesDinamicosAtivos = padroesRodada;
                }
                else
                {
                    // Fallback to global patterns if round patterns are empty (backward compatibility)
                    _padroesDinamicosAtivos = _context.BingoPadroes
                        .Include(bp => bp.Padrao)
                        .Where(bp => bp.BingoId == _rodadaAtual.BingoId && !bp.FoiSorteado)
                        .ToList();
                }
            }
            else
            {
                _padroesDinamicosAtivos.Clear();
            }

            _numerosSorteados.Clear();
            
            _ganhadoresIds = _context.Ganhadores
                .Where(g => g.RodadaId == rodadaId)
                .Select(g => g.CartelaId)
                .ToHashSet();
            
            // Carregar ou criar registro de Sorteio
            var sorteio = _context.Sorteios.FirstOrDefault(s => s.RodadaId == rodadaId);
            
            if (sorteio != null)
            {
                if (!string.IsNullOrEmpty(sorteio.BolasSorteadas))
                {
                    var nums = sorteio.BolasSorteadas.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                     .Select(int.Parse);
                    foreach(var n in nums) _numerosSorteados.Add(n);
                }
            }
            else
            {
                sorteio = new Sorteio
                {
                    RodadaId = rodadaId,
                    BingoId = _rodadaAtual.BingoId,
                    DataHoraInicio = DateTime.Now,
                    BolasSorteadas = ""
                };
                _context.Sorteios.Add(sorteio);
                _context.SaveChanges();
            }
            
            if (_rodadaAtual.Status == "NaoIniciada")
            {
                _rodadaAtual.Status = "EmAndamento";
                _context.SaveChanges();
            }
        }

        public void AtualizarPadrao(Padrao padrao)
        {
            if (_rodadaAtual != null)
            {
                _rodadaAtual.Padrao = padrao;
                _rodadaAtual.PadraoId = padrao.Id;
                _mascaraAtual = padrao.Mascara;
                
                // Re-verificar ganhadores com o novo padrão
                VerificarGanhadores();
            }
        }

        public void AtualizarPadroesDinamicos()
        {
            if (_rodadaAtual == null || !_rodadaAtual.ModoPadroesDinamicos) return;

            // Reload patterns from DB
            var padroesRodada = _context.RodadaPadroes
                .Include(rp => rp.Padrao)
                .Where(rp => rp.RodadaId == _rodadaAtual.Id && !rp.FoiSorteado)
                .Select(rp => new BingoPadrao 
                { 
                    Id = rp.Id, 
                    Padrao = rp.Padrao,
                    PadraoId = rp.PadraoId,
                    FoiSorteado = rp.FoiSorteado,
                    BingoId = 0 
                })
                .ToList();

            if (padroesRodada.Any())
            {
                _padroesDinamicosAtivos = padroesRodada;
            }
            else
            {
                // Fallback to global patterns
                _padroesDinamicosAtivos = _context.BingoPadroes
                    .Include(bp => bp.Padrao)
                    .Where(bp => bp.BingoId == _rodadaAtual.BingoId && !bp.FoiSorteado)
                    .ToList();
            }
            
            VerificarGanhadores();
        }

        public int SortearNumero()
        {
            if (_rodadaAtual == null) 
            {
                _feedService.AddMessage("Erro", "Tentativa de sorteio sem rodada iniciada.", "Error");
                throw new Exception("Nenhuma rodada iniciada.");
            }
            if (_numerosSorteados.Count >= 75) 
            {
                _feedService.AddMessage("Erro", "Todos os números já foram sorteados.", "Error");
                throw new Exception("Todos os números já foram sorteados.");
            }

            int numero;
            do
            {
                numero = _random.Next(1, 76);
            } while (_numerosSorteados.Contains(numero));

            _numerosSorteados.Add(numero);
            
            string letter = "";
            if (numero <= 15) letter = "B";
            else if (numero <= 30) letter = "I";
            else if (numero <= 45) letter = "N";
            else if (numero <= 60) letter = "G";
            else letter = "O";

            _feedService.AddMessage("Sorteio", $"{letter} | {numero}", "Info");
            _gameStatusService.AddRecentBall(numero);

            // Atualizar persistência
            var sorteio = _context.Sorteios.FirstOrDefault(s => s.RodadaId == _rodadaAtual.Id);
            if (sorteio != null)
            {
                var lista = _numerosSorteados.ToList();
                sorteio.BolasSorteadas = string.Join(",", lista);
                _context.SaveChanges();
            }

            OnNumeroSorteado?.Invoke(numero);
            VerificarGanhadores();

            return numero;
        }

        private void VerificarGanhadores()
        {
            var novosGanhadores = new List<GanhadorInfo>();
            var padroesSorteadosNestaVerificacao = new HashSet<int>();

            foreach (var cartela in _cachedCartelas)
            {
                if (_ganhadoresIds.Contains(cartela.Id)) continue;

                // Se modo dinâmico, verifica contra todos os padrões ativos
                if (_padroesDinamicosAtivos.Any())
                {
                    foreach (var bp in _padroesDinamicosAtivos)
                    {
                        // if (padroesSorteadosNestaVerificacao.Contains(bp.Id)) continue; // Permitir empate no mesmo padrão na mesma bola

                        if (bp.Padrao == null) continue;

                        if (VerificarMascara(cartela.Numeros, bp.Padrao.Mascara))
                        {
                            novosGanhadores.Add(new GanhadorInfo 
                            { 
                                CartelaId = cartela.Id, 
                                ComboNumero = cartela.ComboNumero,
                                NumeroCartela = cartela.NumeroCartela,
                                NomeDono = cartela.Dono,
                                NomePadrao = bp.Padrao.Nome // Adicionar info do padrão ganho
                            });
                            
                            // Marcar padrão como sorteado
                            bp.FoiSorteado = true;
                            
                            // Check if it's a RodadaPadrao (mapped manually) or BingoPadrao
                            // Since we mapped manually in IniciarRodada, the entity state tracking might be lost or confused.
                            // We need to update the correct table.
                            
                            if (bp.BingoId == 0) // It's a RodadaPadrao mapped
                            {
                                var rp = _context.RodadaPadroes.Find(bp.Id);
                                if (rp != null) 
                                {
                                    rp.FoiSorteado = true;
                                    _context.Entry(rp).State = EntityState.Modified;
                                }
                            }
                            else // It's a BingoPadrao (Global)
                            {
                                _context.Entry(bp).State = EntityState.Modified;
                            }

                            padroesSorteadosNestaVerificacao.Add(bp.Id);
                            OnPadraoDinamicoSorteado?.Invoke(bp.Padrao.Nome);
                        }
                    }
                }
                else
                {
                    // Modo clássico
                    if (VerificarMascara(cartela.Numeros, _mascaraAtual))
                    {
                        novosGanhadores.Add(new GanhadorInfo 
                        { 
                            CartelaId = cartela.Id, 
                            ComboNumero = cartela.ComboNumero,
                            NumeroCartela = cartela.NumeroCartela,
                            NomeDono = cartela.Dono 
                        });
                    }
                }
            }

            if (padroesSorteadosNestaVerificacao.Any())
            {
                _context.SaveChanges();
                // Remover da lista em memória
                _padroesDinamicosAtivos.RemoveAll(x => padroesSorteadosNestaVerificacao.Contains(x.Id));
            }

            if (novosGanhadores.Any())
            {
                foreach(var g in novosGanhadores) 
                {
                    _ganhadoresIds.Add(g.CartelaId);
                    _feedService.AddMessage("BINGO!", $"Ganhador: Combo {g.ComboNumero} - Cartela {g.NumeroCartela} ({g.NomePadrao})", "Success");
                }

                OnGanhadoresEncontrados?.Invoke(novosGanhadores);
                SalvarGanhadores(novosGanhadores);
            }
        }

        private bool VerificarCartela(int[] numerosCartela)
        {
            return VerificarMascara(numerosCartela, _mascaraAtual);
        }

        private bool VerificarMascara(int[] numerosCartela, string mascara)
        {
            // Check for "X na louca" pattern (RANDOM:X)
            if (mascara.StartsWith("RANDOM:"))
            {
                if (int.TryParse(mascara.Substring(7), out int requiredCount))
                {
                    int hitCount = 0;
                    for (int i = 0; i < 25; i++)
                    {
                        int numeroNaPosicao = numerosCartela[i];
                        // Count if it's a free space (0) or if the number has been drawn
                        if (numeroNaPosicao == 0 || _numerosSorteados.Contains(numeroNaPosicao))
                        {
                            hitCount++;
                        }
                    }
                    return hitCount >= requiredCount;
                }
            }

            for (int i = 0; i < 25; i++)
            {
                if (mascara.Length > i && mascara[i] == '1')
                {
                    int numeroNaPosicao = numerosCartela[i];
                    if (numeroNaPosicao != 0 && !_numerosSorteados.Contains(numeroNaPosicao))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private void SalvarGanhadores(List<GanhadorInfo> ganhadoresInfos)
        {
            if (_rodadaAtual == null) return;

            bool novosGanhadores = false;
            foreach (var g in ganhadoresInfos)
            {
                // Verifica se já não foi salvo
                bool jaSalvo = _context.Ganhadores.Any(x => x.RodadaId == _rodadaAtual.Id && x.CartelaId == g.CartelaId);
                if (!jaSalvo)
                {
                    _context.Ganhadores.Add(new Ganhador
                    {
                        RodadaId = _rodadaAtual.Id,
                        CartelaId = g.CartelaId,
                        IsVencedorFinal = false
                    });
                    novosGanhadores = true;
                }
            }

            if (novosGanhadores)
            {
                _context.SaveChanges();
            }
        }

        public void ReiniciarRodada()
        {
            if (_rodadaAtual == null) return;

            _numerosSorteados.Clear();
            _ganhadoresIds.Clear();

            var sorteio = _context.Sorteios.FirstOrDefault(s => s.RodadaId == _rodadaAtual.Id);
            if (sorteio != null)
            {
                sorteio.BolasSorteadas = "";
            }

            // Remove winners associated with this round
            var ganhadores = _context.Ganhadores.Where(g => g.RodadaId == _rodadaAtual.Id).ToList();
            if (ganhadores.Any())
            {
                _context.Ganhadores.RemoveRange(ganhadores);
            }

            // Se estiver encerrada, volta para EmAndamento
            if (_rodadaAtual.Status == "Encerrada")
            {
                _rodadaAtual.Status = "EmAndamento";
            }

            _context.SaveChanges();
        }

        public List<GanhadorInfo> GetGanhadoresAtuais()
        {
            if (_rodadaAtual == null) return new List<GanhadorInfo>();

            var ganhadores = new List<GanhadorInfo>();
            foreach (var id in _ganhadoresIds)
            {
                var cartela = _cachedCartelas.FirstOrDefault(c => c.Id == id);
                if (cartela != null)
                {
                    ganhadores.Add(new GanhadorInfo
                    {
                        CartelaId = cartela.Id,
                        ComboNumero = cartela.ComboNumero,
                        NumeroCartela = cartela.NumeroCartela,
                        NomeDono = cartela.Dono
                    });
                }
            }
            return ganhadores;
        }

        public void EncerrarRodada()
        {
            if (_rodadaAtual == null) return;
            _rodadaAtual.Status = "Encerrada";
            _context.SaveChanges();
        }

        public HashSet<int> GetNumerosSorteados() => _numerosSorteados;

        public string GetMascaraAtual() => _mascaraAtual;

        public CachedCartela? GetCartela(int cartelaId)
        {
            return _cachedCartelas.FirstOrDefault(c => c.Id == cartelaId);
        }

        public void SetModoDinamico(bool ativo)
        {
            if (_rodadaAtual != null)
            {
                _rodadaAtual.ModoPadroesDinamicos = ativo;
                if (ativo)
                {
                    AtualizarPadroesDinamicos();
                }
                else
                {
                    _padroesDinamicosAtivos.Clear();
                }
                VerificarGanhadores();
            }
        }
    }

    public class CachedCartela
    {
        public int Id { get; set; }
        public int ComboNumero { get; set; }
        public int NumeroCartela { get; set; }
        public string Dono { get; set; } = string.Empty;
        public int[] Numeros { get; set; } = Array.Empty<int>();
    }

    public class GanhadorInfo
    {
        public int CartelaId { get; set; }
        public int ComboNumero { get; set; }
        public int NumeroCartela { get; set; }
        public string NomeDono { get; set; } = string.Empty;
        public string NomePadrao { get; set; } = string.Empty; // Adicionado para armazenar o nome do padrão dinâmico, se aplicável
    }
}
