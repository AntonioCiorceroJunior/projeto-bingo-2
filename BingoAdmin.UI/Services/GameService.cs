using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly BingoContextService _bingoContextService;
        private readonly ISpeechService _speechService;
        private readonly UserSession _userSession;
        private string _lastAnnouncedPrize = string.Empty;
        
        // Armazena perdedores da pedra maior por rodada e padrão: Key="RodadaId_PadraoId", Value=Set<CartelaId>
        private Dictionary<string, HashSet<int>> _perdedoresPorPadrao = new();

        public bool ModoUnicoAtivo { get; set; } = false;

        public event Action<int>? OnNumeroSorteado;
        public event Action<List<GanhadorInfo>>? OnGanhadoresEncontrados;
        public event Action<string>? OnPadraoDinamicoSorteado; // Evento para notificar UI
        public event Action? OnRodadaReiniciada; // Evento para notificar UI que a rodada foi reiniciada
        public event Action? OnRodadaEncerrada; // Evento para notificar UI que a rodada foi encerrada automaticamente
        public event Action<PorUmaBolaStats>? OnPorUmaBolaCountChanged; // Changed to full object for stats display

        public GameService(BingoContext context, FeedService feedService, GameStatusService gameStatusService, BingoContextService bingoContextService, ISpeechService speechService, UserSession userSession)
        {
            _context = context;
            _feedService = feedService;
            _gameStatusService = gameStatusService;
            _bingoContextService = bingoContextService;
            _speechService = speechService;
            _userSession = userSession;
        }

        public void CarregarDadosBingo(int bingoId)
        {
            // _feedService.SwitchBingoContext(bingoId); // Moved down
            // _feedService.AddMessage("Sistema", "Carregando dados do Bingo...", "Info"); // Reduced verbosity
            _cachedCartelas.Clear();
            
            var query = _context.Bingos.AsQueryable();
            // if (!_userSession.IsAdmin)
            {
               var userId = _userSession.CurrentUser?.Id ?? 0;
               query = query.Where(b => b.UsuarioCriadorId == userId);
            }

            var bingo = query.FirstOrDefault(b => b.Id == bingoId);
            
            if (bingo == null)
            {
                // Se o bingo não existe ou não pertence ao usuário
                throw new Exception("Bingo não encontrado ou acesso negado.");
            }

            if (bingo != null)
            {
                _feedService.SwitchBingoContext(bingoId, bingo.Nome);
                _gameStatusService.CurrentBingoTitle = bingo.Nome;
                _gameStatusService.CurrentRoundTitle = "Aguardando Início";
                
                // Tentar carregar a última rodada ativa ou a primeira não iniciada para mostrar contexto
                var lastActiveRound = _context.Rodadas
                    .Where(r => r.BingoId == bingoId && r.Status != "NaoIniciada")
                    .OrderByDescending(r => r.Id)
                    .FirstOrDefault();

                if (lastActiveRound != null)
                {
                    _gameStatusService.CurrentRoundTitle = $"{lastActiveRound.NumeroOrdem}ª Rodada";
                    
                    // Carregar sorteios dessa rodada para o painel lateral
                    var sorteio = _context.Sorteios.FirstOrDefault(s => s.RodadaId == lastActiveRound.Id);
                    if (sorteio != null && !string.IsNullOrEmpty(sorteio.BolasSorteadas))
                    {
                        _gameStatusService.ClearRecentBalls();
                        var nums = sorteio.BolasSorteadas.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                         .Select(int.Parse)
                                         .TakeLast(20); // Pegar os últimos 20 na ordem original para inserção correta (pilha)
                        
                        foreach(var n in nums)
                        {
                            _gameStatusService.AddRecentBall(n);
                        }
                    }
                }
            }

            var cartelas = _context.Cartelas
                .Include(c => c.Kit)
                .ThenInclude(k => k.Combo)
                .Where(c => c.BingoId == bingoId)
                // Filter: Only participam combos Confirmados (que implicam Pagamento = Pago)
                .Where(c => c.Kit.Combo.Status == "Confirmado")
                .ToList();

            // Group by Combo to determine index
            var cartelasPorCombo = cartelas.GroupBy(c => c.Kit?.ComboId ?? 0);

            foreach (var group in cartelasPorCombo)
            {
                int index = 1;
                foreach (var c in group.OrderBy(x => x.Id)) // Assuming ID order is creation order
                {
                    var nums = c.GridNumeros.Split(',').Select(int.Parse).ToArray();
                    _cachedCartelas.Add(new CachedCartela
                    {
                        Id = c.Id,
                        ComboNumero = c.Kit?.Combo?.NumeroCombo ?? 0,
                        NumeroCartela = index++,
                        Dono = c.Kit?.Combo?.NomeDono ?? "Desconhecido",
                        NomeKit = c.Kit != null ? $"Kit {c.Kit.NumeroKitNoCombo}" : "Kit Padrão",
                        CodigoValidacao = c.NumeroGlobal.ToString(), // Usando NumeroGlobal como Série
                        Numeros = nums
                    });
                }
            }
        }

        public void RegistrarGanhadorPedraMaior(int cartelaId)
        {
            var cartela = _cachedCartelas.FirstOrDefault(c => c.Id == cartelaId);
            if (cartela == null) return;

            // Adicionar à lista de ganhadores processados em memória
            if (!_ganhadoresIds.Contains(cartelaId))
            {
                _ganhadoresIds.Add(cartelaId);
            }

            // Salvar no banco como vencedor final
            if (_rodadaAtual != null)
            {
                var jaSalvo = _context.Ganhadores.Any(x => x.RodadaId == _rodadaAtual.Id && x.CartelaId == cartelaId);
                if (!jaSalvo)
                {
                    _context.Ganhadores.Add(new Ganhador
                    {
                        RodadaId = _rodadaAtual.Id,
                        CartelaId = cartelaId,
                        IsVencedorFinal = true
                    });
                    _context.SaveChanges();
                }
                else
                {
                    // Update existing if needed (e.g. from false to true)
                    var g = _context.Ganhadores.First(x => x.RodadaId == _rodadaAtual.Id && x.CartelaId == cartelaId);
                    g.IsVencedorFinal = true;
                    _context.SaveChanges();
                }
            }
        }

        public void RegistrarPerdedoresPedraMaior(List<int> cartelaIds, int premioId)
        {
            if (_rodadaAtual == null) return;

            // Busca o Padrão associado ao prêmio
            var premio = _context.Premios.Find(premioId);
            if (premio == null || premio.PadraoId == null) return;
            
            int padraoId = premio.PadraoId.Value;
            string key = $"{_rodadaAtual.Id}_{padraoId}";

            if (!_perdedoresPorPadrao.ContainsKey(key))
            {
                _perdedoresPorPadrao[key] = new HashSet<int>();
            }

            foreach (var cid in cartelaIds)
            {
                _perdedoresPorPadrao[key].Add(cid);
            }
            
            _feedService.AddMessage("Sistema", $"{cartelaIds.Count} cartela(s) eliminada(s) do padrão {premio.Padrao?.Nome} após disputa de Pedra Maior.", "Info");
        }

        public void IniciarRodada(int rodadaId)
        {
            _lastAnnouncedPrize = string.Empty;
            _perdedoresPorPadrao.Clear();
            // _feedService.AddMessage("Rodada", $"Iniciando rodada {rodadaId}...", "Info"); // Reduced verbosity as requested
            _gameStatusService.ClearRecentBalls();

            _rodadaAtual = _context.Rodadas
                .Include(r => r.Padrao)
                .Include(r => r.Bingo)
                .Include(r => r.Premios).ThenInclude(p => p.Padrao)
                .FirstOrDefault(r => r.Id == rodadaId);

            if (_rodadaAtual == null) throw new Exception("Rodada não encontrada");

            _gameStatusService.CurrentRoundTitle = $"{_rodadaAtual.NumeroOrdem}ª Rodada";

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
                    foreach(var n in nums) 
                    {
                        _numerosSorteados.Add(n);
                        // Não adicionamos ao GameStatusService aqui porque o ClearRecentBalls() já limpou
                        // e queremos adicionar na ordem correta se necessário, mas geralmente IniciarRodada
                        // é para começar a jogar, então o histórico visual começa vazio ou carrega tudo?
                        // Se a rodada já estava em andamento, devemos restaurar o visual também.
                        _gameStatusService.AddRecentBall(n);
                    }
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

            UpdateCurrentPrizeInfo();
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

            // Check if round is already finished (e.g. by max winners)
            if (_rodadaAtual.Status == "Encerrada")
            {
                _feedService.AddMessage("Aviso", "A rodada já está encerrada.", "Warning");
                throw new Exception("A rodada já está encerrada.");
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

            // _feedService.AddMessage("Sorteio", $"{letter} | {numero}", "Info");
            _gameStatusService.AddRecentBall(numero);
            _speechService.SpeakBall(letter, numero);

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

            // Calcular estatísticas "Por Uma Bola" após verificar ganhadores
            var stats = CalcularStatsPorUmaBola();
            OnPorUmaBolaCountChanged?.Invoke(stats);

            return numero;
        }


        // Logic removed - moved to UI


        private void VerificarGanhadores()
        {
            if (_rodadaAtual != null)
            {
                if (_rodadaAtual.TipoJogo == "PeFrio")
                {
                    VerificarGanhadoresPeFrio();
                }
                else if (_rodadaAtual.Bingo?.ModoJogo == 1) // Modo Acumulado
                {
                    VerificarGanhadoresAcumulado();
                }
                else // Modo Clássico
                {
                    VerificarGanhadoresPadrao();
                }
            }

            // Check Max Winners (Only for Classic, as Accumulated handles it via Prizes)
            if (_rodadaAtual != null && _rodadaAtual.Bingo?.ModoJogo != 1 && _rodadaAtual.MaximoGanhadores.HasValue && _rodadaAtual.MaximoGanhadores > 0)
            {
                // Re-fetch winners count because _ganhadoresIds might have been updated
                if (_ganhadoresIds.Count >= _rodadaAtual.MaximoGanhadores.Value)
                {
                    if (_rodadaAtual.Status != "Encerrada")
                    {
                        _rodadaAtual.Status = "Encerrada";
                        _context.SaveChanges();
                        _feedService.AddMessage("Sistema", "Limite de ganhadores atingido. Rodada encerrada automaticamente.", "Warning");
                        OnRodadaEncerrada?.Invoke();
                    }
                }
            }
        }

        private void VerificarGanhadoresAcumulado()
        {
            if (_rodadaAtual == null) return;

            var novosGanhadores = new List<GanhadorInfo>();
            var hasPrizes = _rodadaAtual.Premios != null && _rodadaAtual.Premios.Any();
            
            // DEBUG: DIAGNOSTICO DE RODADA
            if (_numerosSorteados.Count > 0 && _numerosSorteados.Count % 10 == 0) // Loga a cada 10 bolas para não floodar
            {
                 // _feedService.AddMessage("DEBUG", $"Validando {_rodadaAtual.Premios?.Count ?? 0} prêmios no modo ACUMULADO.", "Info");
            }

            if (!hasPrizes) return;

            // 1. Obter todos os prêmios da rodada, agrupados por PADRÃO
            var premiosPorPadrao = _rodadaAtual.Premios
                .GroupBy(p => p.PadraoId ?? 0)
                .ToDictionary(g => g.Key, g => g.OrderBy(p => p.Ordem).ToList());

            // 2. Para cada padrão, descobrir qual é o "Prêmio da Vez"
            foreach (var kvp in premiosPorPadrao)
            {
                int padraoId = kvp.Key;
                var premiosDoPadrao = kvp.Value;
                
                if (padraoId == 0) continue; 
                
                string keyPerdedores = $"{_rodadaAtual.Id}_{padraoId}";
                HashSet<int> perdedoresDestePadrao = _perdedoresPorPadrao.ContainsKey(keyPerdedores) 
                    ? _perdedoresPorPadrao[keyPerdedores] 
                    : new HashSet<int>();

                // Descobre quais prêmios DESSA FILA já foram pagos
                var premiosPagosIds = _context.Ganhadores
                    .Where(g => g.RodadaId == _rodadaAtual.Id && g.Premio != null && g.Premio.PadraoId == padraoId)
                    .Select(g => g.PremioId!.Value)
                    .Distinct()
                    .ToHashSet();
                
                // Pega o primeiro prêmio da fila que AINDA NÃO FOI PAGO
                var premioDaVez = premiosDoPadrao.FirstOrDefault(p => !premiosPagosIds.Contains(p.Id));
                
                if (premioDaVez == null) continue;

                // DIAGNOSTICO CRITICO: Verificar Máscara
                string mascara;
                string nomePadrao;

                if (premioDaVez.Padrao != null)
                {
                    mascara = premioDaVez.Padrao.Mascara;
                    nomePadrao = premioDaVez.Padrao.Nome;
                }
                else
                {
                    // SE ENTRAR AQUI, É O PROBLEMA: O prêmio existe mas perdeu o link com o padrão
                    mascara = new string('1', 25);
                    nomePadrao = "Padrão Desconhecido (Fallback Cheia)";
                    
                    if (_numerosSorteados.Count % 5 == 0) // Log de alerta ocasional
                    {
                        _feedService.AddMessage("ALERTA", $"Prêmio '{premioDaVez.Descricao}' está sem Padrão vinculado! Usando Cartela Cheia.", "Warning");
                    }
                }

                foreach (var cartela in _cachedCartelas)
                {
                    if (perdedoresDestePadrao.Contains(cartela.Id)) continue;
                    
                    // Se já ganhou QUALQUER coisa na rodada, tá fora.
                    if (_ganhadoresIds.Contains(cartela.Id)) continue;

                    if (VerificarMascara(cartela.Numeros, mascara))
                    {
                        var info = CreateGanhadorInfo(cartela, nomePadrao, mascara);
                        info.PremioId = premioDaVez.Id;
                        info.NomePremio = premioDaVez.Descricao;
                        info.ValorPremio = premioDaVez.Valor;
                        
                        novosGanhadores.Add(info);
                    }
                }
            }


            if (novosGanhadores.Any())
            {
                // Agrupa e processa (pode haver ganhadores de Quina e Cheia simultaneamente)
                var groups = novosGanhadores.GroupBy(g => g.PremioId);
                foreach(var group in groups)
                {
                    ProcessarNovosGanhadores(group.ToList());
                }
            }
        }


        private void VerificarGanhadoresPeFrio()
        {
            if (_cachedCartelas.Count > 3000)
            {
                _feedService.AddMessage("Erro", "Modo Pé Frio disponível apenas para até 3000 cartelas.", "Error");
                return;
            }

            var survivors = new List<CachedCartela>();
            var justEliminated = new List<CachedCartela>();
            int lastDrawn = _numerosSorteados.LastOrDefault();

            foreach (var cartela in _cachedCartelas)
            {
                if (_ganhadoresIds.Contains(cartela.Id)) continue;

                int hits = 0;
                bool hitByLast = false;
                
                foreach(var n in cartela.Numeros)
                {
                    if (n != 0 && _numerosSorteados.Contains(n))
                    {
                        hits++;
                        if (n == lastDrawn) hitByLast = true;
                    }
                }

                if (hits == 0)
                {
                    survivors.Add(cartela);
                }
                else if (hits == 1 && hitByLast)
                {
                    justEliminated.Add(cartela);
                }
            }

            var novosGanhadores = new List<GanhadorInfo>();

            if (survivors.Count == 1)
            {
                // We have a winner!
                var winner = survivors.First();
                novosGanhadores.Add(new GanhadorInfo 
                { 
                    CartelaId = winner.Id, 
                    ComboNumero = winner.ComboNumero, 
                    NumeroCartela = winner.NumeroCartela, 
                    NomeDono = winner.Dono,
                    NomeKit = winner.NomeKit,
                    NomePadrao = "Pé Frio (Invicto)"
                });
            }
            else if (survivors.Count == 0 && justEliminated.Any())
            {
                // Everyone hit something. The winners are the ones who lasted longest (just eliminated).
                foreach(var c in justEliminated)
                {
                    novosGanhadores.Add(new GanhadorInfo 
                    { 
                        CartelaId = c.Id, 
                        ComboNumero = c.ComboNumero, 
                        NumeroCartela = c.NumeroCartela, 
                        NomeDono = c.Dono,
                        NomeKit = c.NomeKit,
                        NomePadrao = "Pé Frio (Último a marcar)"
                    });
                }
            }
            else if (survivors.Count > 1 && _numerosSorteados.Count >= 74)
            {
                 // Tie breaker at the end
                 foreach(var c in survivors)
                {
                    novosGanhadores.Add(new GanhadorInfo 
                    { 
                        CartelaId = c.Id, 
                        ComboNumero = c.ComboNumero, 
                        NumeroCartela = c.NumeroCartela, 
                        NomeDono = c.Dono,
                        NomeKit = c.NomeKit,
                        NomePadrao = "Pé Frio (Empate Final)"
                    });
                }
            }

            if (novosGanhadores.Any())
            {
                ProcessarNovosGanhadores(novosGanhadores);
            }
        }

        private void VerificarGanhadoresPadrao()
        {
            var novosGanhadores = new List<GanhadorInfo>();
            var padroesSorteadosNestaVerificacao = new HashSet<int>();

            // 1. Identify Target Rules
            var hasDynamicPatterns = _padroesDinamicosAtivos.Any();
            var hasPrizes = _rodadaAtual?.Premios != null && _rodadaAtual.Premios.Any();
            
            List<Premio> targetPrizes = new List<Premio>();
            if (hasPrizes)
            {
                // Check against DB for prizes already won this round
                var wonPrizeIds = _context.Ganhadores
                    .Where(g => g.RodadaId == _rodadaAtual!.Id && g.PremioId.HasValue)
                    .Select(g => g.PremioId!.Value)
                    .ToHashSet();

                var availablePrizes = _rodadaAtual!.Premios
                    .Where(p => !wonPrizeIds.Contains(p.Id))
                    .OrderBy(p => p.Ordem)
                    .ToList();

                if (_rodadaAtual.Bingo?.ModoJogo == 1) // Accumulated
                {
                    targetPrizes = availablePrizes; // Check all active prizes
                }
                else // Standard (Sequential)
                {
                    if (availablePrizes.Any())
                        targetPrizes.Add(availablePrizes.First()); // Check only the next prize
                }
            }

            // 2. Iterate Cartelas
            foreach (var cartela in _cachedCartelas)
            {
                bool alreadyWonInRound = _ganhadoresIds.Contains(cartela.Id);

                // --- Dynamic Patterns Logic ---
                if (hasDynamicPatterns && !alreadyWonInRound)
                {
                    foreach (var bp in _padroesDinamicosAtivos)
                    {
                        // if (padroesSorteadosNestaVerificacao.Contains(bp.Id)) continue; 

                        if (bp.Padrao == null) continue;

                        if (VerificarMascara(cartela.Numeros, bp.Padrao.Mascara))
                        {
                            novosGanhadores.Add(CreateGanhadorInfo(cartela, "Dinâmico: " + bp.Padrao.Nome, bp.Padrao.Mascara));
                            
                            bp.FoiSorteado = true;
                            
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
                // --- Prizes Logic ---
                else if (hasPrizes)
                {
                    foreach (var prize in targetPrizes)
                    {
                         // In Standard Mode, if card already won, skip. In Accumulated, allow win if different prize (implicit by targetPrizes check if strictly handled, but safer to allow loop)
                         if (_rodadaAtual!.Bingo?.ModoJogo == 0 && alreadyWonInRound) continue;
                         
                         string mask = prize.Padrao?.Mascara ?? new string('1', 25);
                         if (VerificarMascara(cartela.Numeros, mask))
                         {
                              var prizeName = $"{prize.Ordem}º Prêmio - {prize.Descricao}";
                              var info = CreateGanhadorInfo(cartela, prizeName, mask);
                              info.PremioId = prize.Id;
                              info.NomePremio = prize.Descricao;
                              info.ValorPremio = prize.Valor;
                              novosGanhadores.Add(info);
                         }
                    }
                }
                // --- Legacy Logic ---
                else
                {
                    // Modo clássico standard
                    if (!alreadyWonInRound && VerificarMascara(cartela.Numeros, _mascaraAtual))
                    {
                        novosGanhadores.Add(CreateGanhadorInfo(cartela, "Bingo!", _mascaraAtual));
                    }
                }
            }

            if (padroesSorteadosNestaVerificacao.Any())
            {
               _context.SaveChanges();
               if (ModoUnicoAtivo) _padroesDinamicosAtivos.RemoveAll(x => padroesSorteadosNestaVerificacao.Contains(x.Id));
            }

            if (novosGanhadores.Any())
            {
                // If we have winners for multiple prizes, process strictly per prize
                var groups = novosGanhadores.GroupBy(g => g.PremioId);
                foreach(var group in groups)
                {
                    ProcessarNovosGanhadores(group.ToList());
                }
            }
        }

        private GanhadorInfo CreateGanhadorInfo(CachedCartela cartela, string nomePadrao, string mascara)
        {
             return new GanhadorInfo 
             { 
                CartelaId = cartela.Id, 
                ComboNumero = cartela.ComboNumero,
                NumeroCartela = cartela.NumeroCartela,
                NomeDono = cartela.Dono,
                NomeKit = cartela.NomeKit,
                CodigoValidacao = cartela.CodigoValidacao,
                NomePadrao = nomePadrao,
                MascaraPadrao = mascara
             };
        }

        private void ProcessarNovosGanhadores(List<GanhadorInfo> novosGanhadores)
        {
            // Determine Tie-Breaker Logic
            // If Eliminate mode (1) OR (Legacy mode and MaxWinners=1)
            bool eliminateMode = _rodadaAtual?.ModoDisputaPremios == 1 || (_rodadaAtual?.MaximoGanhadores == 1 && (_rodadaAtual?.Premios == null || !_rodadaAtual.Premios.Any()));
            
            // Check count
            if (eliminateMode && novosGanhadores.Count > 1)
            {
                // Configurar Pedra Maior
                var partsOrdenados = novosGanhadores
                    .OrderBy(g => g.NomeDono)
                    .Select(g => new ViewModels.PedraMaiorItemViewModel
                    {
                         Nome = g.NomeDono,
                         ComboNumero = g.ComboNumero.ToString(),
                         NumeroCartela = g.NumeroCartela.ToString(),
                         NomeKit = g.NomeKit,
                         OriginalInfo = g,
                         PedraSorteada = 0,
                         IsWinner = false
                    })
                    .ToList();
                
                _gameStatusService.PedraMaiorParticipants.Clear();
                foreach(var part in partsOrdenados)
                {
                    _gameStatusService.PedraMaiorParticipants.Add(part);
                }

                _gameStatusService.IsPedraMaiorActive = true;
                
                // Notifica UI sobre empates para pausar o jogo e Tocar o audio apropriado
                OnGanhadoresEncontrados?.Invoke(novosGanhadores);
                
                return; 
            }
            
            // Caso Vencedor(es) Válido(s) (não Pedra Maior ou Distribuído)
            foreach(var g in novosGanhadores) 
            {
                _ganhadoresIds.Add(g.CartelaId);
                string prizeTxt = string.IsNullOrEmpty(g.NomePremio) ? "" : $" ({g.NomePremio})";
                _feedService.AddMessage("BINGO!", $"Ganhador: {g.NomeDono}, Combo {g.ComboNumero}, Cartela {g.NumeroCartela}{prizeTxt}", "Success");
            }

            OnGanhadoresEncontrados?.Invoke(novosGanhadores);
            SalvarGanhadores(novosGanhadores);
            UpdateCurrentPrizeInfo();
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
                bool jaSalvo;
                if (g.PremioId.HasValue)
                {
                     jaSalvo = _context.Ganhadores.Any(x => x.RodadaId == _rodadaAtual.Id && x.CartelaId == g.CartelaId && x.PremioId == g.PremioId);
                }
                else
                {
                     jaSalvo = _context.Ganhadores.Any(x => x.RodadaId == _rodadaAtual.Id && x.CartelaId == g.CartelaId && x.PremioId == null);
                }

                if (!jaSalvo)
                {
                    _context.Ganhadores.Add(new Ganhador
                    {
                        RodadaId = _rodadaAtual.Id,
                        CartelaId = g.CartelaId,
                        IsVencedorFinal = false,
                        PremioId = g.PremioId
                    });
                    novosGanhadores = true;
                }
            }

            if (novosGanhadores)
            {
                _context.SaveChanges();
            }
        }

        public void RefreshPorUmaBolaStats()
        {
            if (_rodadaAtual == null) return;
            var stats = CalcularStatsPorUmaBola();
            OnPorUmaBolaCountChanged?.Invoke(stats);
        }

        public PorUmaBolaStats CalcularStatsPorUmaBola()
        {
            var stats = new PorUmaBolaStats();
            
            foreach (var cartela in _cachedCartelas)
            {
                if (_ganhadoresIds.Contains(cartela.Id)) continue;

                bool cardIsOneAway = false;
                var waitingNumbersForThisCard = new HashSet<int>();

                if (_padroesDinamicosAtivos.Any())
                {
                    foreach (var bp in _padroesDinamicosAtivos)
                    {
                        if (bp.Padrao == null) continue;
                        var (missingCount, missingNums) = GetMissingNumbers(cartela.Numeros, bp.Padrao.Mascara);
                        if (missingCount == 1)
                        {
                            cardIsOneAway = true;
                            foreach(var n in missingNums) waitingNumbersForThisCard.Add(n);
                        }
                    }
                }
                else
                {
                    var (missingCount, missingNums) = GetMissingNumbers(cartela.Numeros, _mascaraAtual);
                    if (missingCount == 1)
                    {
                        cardIsOneAway = true;
                        foreach(var n in missingNums) waitingNumbersForThisCard.Add(n);
                    }
                }

                if (cardIsOneAway)
                {
                    stats.TotalCartelasPorUma++;
                    foreach(var n in waitingNumbersForThisCard)
                    {
                        if (!stats.NumerosMaisEsperados.ContainsKey(n))
                            stats.NumerosMaisEsperados[n] = 0;
                        stats.NumerosMaisEsperados[n]++;
                    }
                }
            }
            
            return stats;
        }

        private (int count, List<int> numbers) GetMissingNumbers(int[] numerosCartela, string mascara)
        {
             var missingNumbers = new List<int>();
             // Check for "X na louca" pattern (RANDOM:X)
            if (mascara.StartsWith("RANDOM:"))
            {
                if (int.TryParse(mascara.Substring(7), out int requiredCount))
                {
                    int hitCount = 0;
                    var undrawnNumbers = new List<int>();
                    
                    for (int i = 0; i < 25; i++)
                    {
                        int numeroNaPosicao = numerosCartela[i];
                        if (numeroNaPosicao == 0) 
                        {
                            hitCount++; // Free space counts as hit
                        }
                        else if (_numerosSorteados.Contains(numeroNaPosicao))
                        {
                            hitCount++;
                        }
                        else
                        {
                            undrawnNumbers.Add(numeroNaPosicao);
                        }
                    }
                    
                    int missing = Math.Max(0, requiredCount - hitCount);
                    
                    if (missing == 1)
                    {
                        return (1, undrawnNumbers);
                    }
                    return (missing, new List<int>());
                }
            }

            for (int i = 0; i < 25; i++)
            {
                if (mascara.Length > i && mascara[i] == '1')
                {
                    int numeroNaPosicao = numerosCartela[i];
                    if (numeroNaPosicao != 0 && !_numerosSorteados.Contains(numeroNaPosicao))
                    {
                        missingNumbers.Add(numeroNaPosicao);
                    }
                }
            }
            return (missingNumbers.Count, missingNumbers);
        }

        public void ReiniciarRodada()
        {
            if (_rodadaAtual == null) return;

            _lastAnnouncedPrize = string.Empty;
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

            // Remove PedraMaior history (Desempate)
            var desempates = _context.PedraMaiorSorteios.Where(p => p.RodadaId == _rodadaAtual.Id).ToList();
            if (desempates.Any())
            {
                _context.PedraMaiorSorteios.RemoveRange(desempates);
            }

            // Remove DesempateItens (Desempate Novo)
            var desempateItens = _context.DesempateItens.Where(d => d.RodadaId == _rodadaAtual.Id).ToList();
            if (desempateItens.Any())
            {
                _context.DesempateItens.RemoveRange(desempateItens);
            }

            // Se estiver encerrada, volta para EmAndamento
            if (_rodadaAtual.Status == "Encerrada")
            {
                _rodadaAtual.Status = "EmAndamento";
            }

            _context.SaveChanges();
            
            _feedService.AddMessage("Sistema", $"Rodada {_rodadaAtual.NumeroOrdem} reiniciada. Histórico e sorteios limpos.", "Warning");
            OnRodadaReiniciada?.Invoke();
            _bingoContextService.NotifyRodadaReiniciada();
        }

        public void RemoverGanhador(int cartelaId)
        {
            if (_ganhadoresIds.Contains(cartelaId))
            {
                _ganhadoresIds.Remove(cartelaId);
            }

            if (_rodadaAtual != null)
            {
                var ganhadorDb = _context.Ganhadores.FirstOrDefault(g => g.RodadaId == _rodadaAtual.Id && g.CartelaId == cartelaId);
                if (ganhadorDb != null)
                {
                    _context.Ganhadores.Remove(ganhadorDb);
                    _context.SaveChanges();
                }
            }
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
                        NomeDono = cartela.Dono,
                        NomeKit = cartela.NomeKit // Copiar
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

        private void UpdateCurrentPrizeInfo()
        {
            if (_rodadaAtual == null) return;

            string desc = "";
            string val = "";
            string pat = "";

            if (_rodadaAtual.Premios != null && _rodadaAtual.Premios.Any())
            {
                var wonPrizeIds = _context.Ganhadores
                     .Where(g => g.RodadaId == _rodadaAtual.Id && g.PremioId.HasValue)
                     .Select(g => g.PremioId!.Value)
                     .ToHashSet();

                var availablePrizes = _rodadaAtual.Premios
                    .Where(p => !wonPrizeIds.Contains(p.Id))
                    .OrderBy(p => p.Ordem)
                    .ToList();

                int modoJogo = _rodadaAtual.Bingo?.ModoJogo ?? 0;

                if (modoJogo == 1) // Accumulated
                {
                    if (availablePrizes.Any())
                    {
                        desc = "ACUMULADO";
                        if (availablePrizes.Count == 1)
                        {
                            var p = availablePrizes.First();
                            desc = p.Descricao;
                            val = p.Valor.HasValue ? p.Valor.Value.ToString("C2") : "";
                            pat = p.Padrao?.Nome ?? "";
                        }
                        else
                        {
                            desc = $"ACUMULADO ({availablePrizes.Count} Prêmios)";
                            val = string.Join(" | ", availablePrizes.Select(p => p.Descricao));
                            pat = "Múltiplos Padrões";
                        }
                    }
                    else
                    {
                        desc = "Rodada Finalizada";
                        val = "";
                        pat = "Todos prêmios sorteados";
                    }
                }
                else // Standard/Sequential (0)
                {
                    var nextPrize = availablePrizes.FirstOrDefault();
                    if (nextPrize != null)
                    {
                        desc = nextPrize.Descricao;
                        val = nextPrize.Valor.HasValue ? nextPrize.Valor.Value.ToString("C2") : "";
                        pat = nextPrize.Padrao?.Nome ?? "Padrão Definido";
                    }
                    else
                    {
                        desc = "Rodada Finalizada";
                        val = "Todos prêmios sorteados";
                        pat = "";
                    }
                }
            }
            else
            {
                desc = _rodadaAtual.Descricao;
                if (string.IsNullOrEmpty(desc)) desc = "Rodada " + _rodadaAtual.NumeroOrdem;

                if (_rodadaAtual.Padrao != null)
                {
                    pat = _rodadaAtual.Padrao.Nome;
                    val = "Sem Prêmios Definidos";
                }
                else
                {
                    pat = "Cartela Cheia (Padrão)";
                    val = "";
                }
            }

            _gameStatusService.CurrentPrizeDescription = desc;
            _gameStatusService.CurrentPrizeValue = val;
            _gameStatusService.CurrentPrizePatternName = pat;

            // Anunciar prêmio se mudou
            if (desc != _lastAnnouncedPrize && !string.IsNullOrEmpty(desc) && desc != "Rodada Finalizada" && !desc.StartsWith("ACUMULADO"))
            {
                 _speechService.AnnouncePrize(desc);
                 _lastAnnouncedPrize = desc;
            }
        }

    }

    public class CachedCartela
    {
        public int Id { get; set; }
        public int ComboNumero { get; set; }
        public int NumeroCartela { get; set; }
        public string Dono { get; set; } = string.Empty;
        public string NomeKit { get; set; } = string.Empty; // New Field
        public string CodigoValidacao { get; set; } = string.Empty; // Serial/Hash
        public int[] Numeros { get; set; } = Array.Empty<int>();
    }

    public class GanhadorInfo
    {
        public int CartelaId { get; set; }
        public int ComboNumero { get; set; }
        public int NumeroCartela { get; set; }
        public string NomeDono { get; set; } = string.Empty;
        public string NomeKit { get; set; } = string.Empty; // New Field
        public string CodigoValidacao { get; set; } = string.Empty; // Added Serial Number
        public string NomePadrao { get; set; } = string.Empty; // Adicionado para armazenar o nome do padrão dinâmico, se aplicável
        public string MascaraPadrao { get; set; } = string.Empty; // Adicionado para armazenar a máscara do padrão vencedor
        public int? PremioId { get; set; }
        public string NomePremio { get; set; } = string.Empty;
        public decimal? ValorPremio { get; set; }
        
        // Helper para UI
        public string NumeroCartelaFormatado => $"{NumeroCartela}"; 
    }

    public class PorUmaBolaStats
    {
        public int TotalCartelasPorUma { get; set; }
        public Dictionary<int, int> NumerosMaisEsperados { get; set; } = new();
    }
}
