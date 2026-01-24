using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using BingoAdmin.Domain.Entities;
using BingoAdmin.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BingoAdmin.UI.Views
{
    public partial class GameView : UserControl
    {
        private readonly GameService _gameService = null!;
        private readonly ComboService _comboService = null!;
        private readonly RodadaService _rodadaService = null!;
        private readonly PadraoService _padraoService = null!;
        private readonly DesempateService _desempateService = null!;
        private readonly BingoContextService _bingoContext = null!;
        private readonly GameStatusService _gameStatusService = null!;
        private readonly FeedService _feedService = null!;
        private readonly ISpeechService _speechService = null!;
        private readonly FraseService _fraseService = null!;

        public GameStatusService GameStatus => _gameStatusService;
        
        private DispatcherTimer _autoDrawTimer;
        private DispatcherTimer _countdownTimer; // For visual countdown
        private DateTime _nextDrawTime;
        private FlashboardWindow? _flashboardWindow;
        private double _currentIntervalSeconds = 4.0;

        public ObservableCollection<BoardNumber> ColumnB { get; set; } = new ObservableCollection<BoardNumber>();
        public ObservableCollection<BoardNumber> ColumnI { get; set; } = new ObservableCollection<BoardNumber>();
        public ObservableCollection<BoardNumber> ColumnN { get; set; } = new ObservableCollection<BoardNumber>();
        public ObservableCollection<BoardNumber> ColumnG { get; set; } = new ObservableCollection<BoardNumber>();
        public ObservableCollection<BoardNumber> ColumnO { get; set; } = new ObservableCollection<BoardNumber>();
        
        public ObservableCollection<GanhadorDisplay> Ganhadores { get; set; } = new ObservableCollection<GanhadorDisplay>();
        public ObservableCollection<string> HistoricoSorteio { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<Padrao> AvailablePatterns { get; set; } = new ObservableCollection<Padrao>();

        public GameView()
        {
            InitializeComponent();
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;

            _gameService = ((App)Application.Current).Host.Services.GetRequiredService<GameService>();
            _comboService = ((App)Application.Current).Host.Services.GetRequiredService<ComboService>();
            _rodadaService = ((App)Application.Current).Host.Services.GetRequiredService<RodadaService>();
            _padraoService = ((App)Application.Current).Host.Services.GetRequiredService<PadraoService>();
            _desempateService = ((App)Application.Current).Host.Services.GetRequiredService<DesempateService>();
            _bingoContext = ((App)Application.Current).Host.Services.GetRequiredService<BingoContextService>();
            _gameStatusService = ((App)Application.Current).Host.Services.GetRequiredService<GameStatusService>();
            _feedService = ((App)Application.Current).Host.Services.GetRequiredService<FeedService>();
            _speechService = ((App)Application.Current).Host.Services.GetRequiredService<ISpeechService>();
            _fraseService = ((App)Application.Current).Host.Services.GetRequiredService<FraseService>();

            InitializeBoard();
            InitializeAutoDrawTimer();
            
            // Set DataContext to self so we can bind to properties
            this.DataContext = this;
            
            ChkLocucao.IsChecked = _speechService.IsEnabled;

            ListGanhadores.ItemsSource = Ganhadores;
            ListGanhadores.MouseDoubleClick += ListGanhadores_MouseDoubleClick;
            
            ListHistorico.ItemsSource = HistoricoSorteio;

            PatternSelector.ItemsSource = AvailablePatterns;
            LoadPatterns();

            LoadBingos();

            _gameService.OnNumeroSorteado += OnNumeroSorteado;
            _gameService.OnGanhadoresEncontrados += OnGanhadoresEncontrados;
            _gameService.OnRodadaEncerrada += OnRodadaEncerrada;
            _gameService.OnPorUmaBolaCountChanged += OnPorUmaBolaCountChanged;
            _bingoContext.OnBingoChanged += OnGlobalBingoChanged;
            _bingoContext.OnBingoListUpdated += OnBingoListUpdated;
        }

        private int _lastPorUmaBolaCount = -1;
        private bool _isProcessingWinners = false;
        private bool _cancelAutoResume = false;
        private bool _isTemporarilyPaused = false;

        private async void OnPorUmaBolaCountChanged(PorUmaBolaStats stats)
        {
            if (_isProcessingWinners) return;

            int totalWaiting = stats.TotalCartelasPorUma;

            await Dispatcher.InvokeAsync(async () =>
            {
                TxtTotalPorUma.Text = totalWaiting.ToString();
                
                // Show/Hide Panel
                PanelPorUmaBola.Visibility = totalWaiting > 0 ? Visibility.Visible : Visibility.Collapsed;

                // Update List of Waiting Numbers (Top 5 or less depending on waiting count)
                // If just 1 card waiting, show the number(s) it needs.
                // Logic requested: "show expected numbers... if 1 card, show the number waiting to win"
                
                var topNumbers = stats.NumerosMaisEsperados
                    .OrderByDescending(x => x.Value) // Most popular first
                    .ThenBy(x => x.Key) // Then number
                    .Take(5) // Limit to 5
                    .Select(x => $"{x.Key} ({x.Value})") // Display: 45 (1)
                    .ToList();
                
                ListNumerosEsperados.ItemsSource = topNumbers;


                bool countChanged = totalWaiting != _lastPorUmaBolaCount;
                _lastPorUmaBolaCount = totalWaiting;

                if (countChanged && totalWaiting > 0 && ChkLocucaoPorUmaBola.IsChecked == true)
                {
                    // Check Mode! (Accumulated Mode = 1 -> No "Por Uma Bola" Speech)
                    if (BingoSelector.SelectedItem is Bingo currentBingo && currentBingo.ModoJogo == 1)
                    {
                        return;
                    }

                    // Check if we just found a winner in this draw (Overlay would be visible)
                    if (_gameStatusService.IsWinnerOverlayVisible || _isProcessingWinners) return;

                    bool wasAutoDraw = _gameStatusService.IsAutoDrawActive;
                    if (wasAutoDraw) 
                    {
                        StopAutoDraw(true); // Pause Temporarily
                    }

                    // Wait 2.5s
                    await Task.Delay(2500);

                    // Re-check overlay after delay
                    if (_gameStatusService.IsWinnerOverlayVisible || _isProcessingWinners) return;

                    // Speak using rotation phrases (max 5s timeout)
                    // If total waiting is 1, keep the clear standard message or use rotation with logic?
                    // User asked to replace "Atenção agora temos X cartelas por uma" which is the plurality case.
                    
                    string text;
                    if (totalWaiting == 1)
                    {
                         text = "Atenção! Temos uma cartela por uma bola!";
                    }
                    else
                    {
                         // Use rotation service
                         text = _fraseService.GetNextPorUmaBolaPhrase(totalWaiting);
                    }
                        
                    var speechTask = _speechService.SpeakAsync(text);
                    var timeoutTask = Task.Delay(5000);
                    await Task.WhenAny(speechTask, timeoutTask);
                    
                    // Resume if it was auto
                    if (wasAutoDraw)
                    {
                        // Show countdown or status
                        _gameStatusService.CurrentTimerText = "Retomando...";

                        // Wait 3 seconds AFTER speech finishes (Reduced from 5s)
                        await Task.Delay(3000);
                        
                        // Check if a winner appeared during this time (unlikely but safe) AND if user didn't cancel
                        if (!_gameStatusService.IsWinnerOverlayVisible && !_isProcessingWinners && !_cancelAutoResume)
                        {
                            // 1. Reactivate Auto Mode
                            BtnIniciarAuto_Click(this, new RoutedEventArgs());

                            // 2. Draw IMMEDIATELY to resume flow
                            // Use Dispatcher to ensure it runs slightly after the UI update
                            await Dispatcher.InvokeAsync(() => 
                            {
                                if (BtnSortear.IsEnabled)
                                {
                                    BtnSortear_Click(this, new RoutedEventArgs());
                                    // Reset timer relative to this new draw
                                    _nextDrawTime = DateTime.Now.Add(_autoDrawTimer.Interval);
                                }
                            });
                        }
                    }
                }
            });
        }

        private async void OnGanhadoresEncontrados(List<GanhadorInfo> ganhadores)
        {
            // IMMEDIATELY BLOCK "Por Uma Bola" Logic
            _isProcessingWinners = true;

            await Dispatcher.InvokeAsync(async () =>
            {
                // Capture if was running automatically
                bool wasAutoDraw = _gameStatusService.IsAutoDrawActive || _isTemporarilyPaused;

                // Pause Game
                StopAutoDraw(true);

                // Wait for the ball speech to finish (approx 2.5s)
                await Task.Delay(2500);
                
                // 1. PEDRA MAIOR / EMPATE (Mais de 1 ganhador)
                if (ganhadores.Count > 1)
                {
                    // Speech: Atenção...
                    string speechText = $"Atenção, existem {ganhadores.Count} jogadores que bingaram! Por isso, o prêmio será decidido na pedra maior.";
                    
                    // Use safe timeout to avoid blocking UI if audio hangs
                    var st = _speechService.SpeakAsync(speechText);
                    var tt = Task.Delay(3000);
                    await Task.WhenAny(st, tt);

                    // Lógica de Desempate (Copiada do original)
                    if (RodadaSelector.SelectedItem is RodadaDisplay display)
                    {
                        _desempateService.SincronizarDesempate(display.Rodada.Id, ganhadores);
                    }

                    var window = new PedraMaiorWindow(ganhadores);
                    window.Owner = Window.GetWindow(this);
                    
                    // O ShowDialog bloqueia a UI thread, então o speech deve terminar antes ou continuar rodando?
                    // Como usamos 'await SpeakAsync' antes, o speech já terminou.
                    if (window.ShowDialog() == true)
                    {
                        if (RodadaSelector.SelectedItem is RodadaDisplay currentDisplay)
                        {
                            var resultados = window.Items.Select(i => (
                                CartelaId: ((GanhadorInfo)i.OriginalInfo!).CartelaId,
                                NumeroSorteado: i.PedraSorteada ?? 0,
                                IsVencedor: i.IsWinner
                            )).ToList();
                            
                            _desempateService.SalvarSorteioPedraMaiorEmLote(currentDisplay.Rodada.Id, resultados);
                        }

                        // 1. Coletar IDs dos Perdedores (Quem não venceu na Pedra Maior)
                        var perdedoresIds = window.Items
                            .Where(i => !i.IsWinner && i.OriginalInfo is GanhadorInfo)
                            .Select(i => ((GanhadorInfo)i.OriginalInfo!).CartelaId)
                            .ToList();

                        // 2. Coletar ID do Prêmio em disputa (Assumindo que todos disputaram o mesmo prêmio)
                        var winnerItem = window.GetWinnerItem();
                        int? premioIdContext = null;
                        if (winnerItem != null && winnerItem.OriginalInfo is GanhadorInfo wInfo)
                        {
                            premioIdContext = wInfo.PremioId;
                        }

                        // 3. Registrar Perdedores no Service (Para bani-los deste padrão)
                        if (perdedoresIds.Any() && premioIdContext.HasValue)
                        {
                            _gameService.RegistrarPerdedoresPedraMaior(perdedoresIds, premioIdContext.Value);
                        }

                        // Remover perdedores da lista visual
                        foreach (var item in window.Items)
                        {
                            if (!item.IsWinner && item.OriginalInfo is GanhadorInfo info)
                            {
                                _gameService.RemoverGanhador(info.CartelaId);
                            }
                        }

                        if (winnerItem != null)
                        {
                            var g = (GanhadorInfo)winnerItem.OriginalInfo!;
                            
                            // REGISTER WINNER IN BACKEND TO PREVENT RE-TRIGGER
                            _gameService.RegistrarGanhadorPedraMaior(g.CartelaId);

                            string msg = $"GANHADOR: {winnerItem.Nome}, Combo {winnerItem.ComboNumero}, Cartela {winnerItem.NumeroCartela} - Pedra Maior: {winnerItem.PedraSorteada}";
                            
                            if (!Ganhadores.Any(gd => gd.Info.CartelaId == g.CartelaId))
                            {
                                Ganhadores.Insert(0, new GanhadorDisplay { Texto = msg, Info = g });
                                _feedService.AddMessage("Pedra Maior - Vencedor", msg, "Success");
                            }

                            _gameStatusService.WinnerOverlayTitle = "VENCEDOR(A)!";
                            _gameStatusService.WinnerOverlayMessage = $"{winnerItem.Nome}\n{winnerItem.NomeKit}\nCartela: {winnerItem.NumeroCartela}\nPedra: {winnerItem.PedraSorteada}";
                            _gameStatusService.IsWinnerOverlayVisible = true;

                            // -- CUSTOM SPEECH LOGIC FOR ACCUMULATED MODE (applied globally as requested) --
                            string pmSpeechText = $"O vencedor na pedra maior é {winnerItem.Nome}, com a pedra {winnerItem.PedraSorteada}. ";
                            
                            // Check for pattern info in original info
                            string padrao = g.NomePadrao;
                            if (!string.IsNullOrEmpty(padrao)) pmSpeechText += $"Ganhou no padrão {padrao}. ";
                            
                            if (!string.IsNullOrEmpty(g.NomePremio)) pmSpeechText += $"Prêmio: {g.NomePremio}. ";

                            pmSpeechText += $"Combo {g.ComboNumero}, {g.NomeKit}, cartela {g.NumeroCartela}. ";
                            pmSpeechText += $"Série {g.CodigoValidacao}.";

                            var sp = _speechService.SpeakAsync(pmSpeechText);
                            // Wait for speech to FINISH completely (User Requirement)
                            await sp; 

                            // Initial pause after speech (4 seconds as requested)
                            await Task.Delay(4000);

                            if (wasAutoDraw)
                            {
                                // No extra wait needed here, we did the 4s pause above
                                _gameStatusService.IsWinnerOverlayVisible = false;
                                
                                // Reset auto status
                                _gameStatusService.IsAutoDrawActive = true;
                                _gameStatusService.CurrentTimerText = "Retomando...";
                                
                                // FORCE RESET SPEECH TRACKER SO IT SPEAKS AGAIN IF NEEDED
                                _lastPorUmaBolaCount = -1;
                                
                                // Release Block
                                _isProcessingWinners = false;

                                if (!_cancelAutoResume)
                                {
                                    // Restart
                                    BtnIniciarAuto_Click(this, new RoutedEventArgs());
                                    await Dispatcher.InvokeAsync(() => 
                                    {
                                        if (BtnSortear.IsEnabled)
                                        {
                                            BtnSortear_Click(this, new RoutedEventArgs());
                                            _nextDrawTime = DateTime.Now.Add(_autoDrawTimer.Interval);
                                        }
                                    });
                                }
                            }
                            else
                            {
                                MessageBox.Show(Window.GetWindow(this), msg, "VENCEDOR(A)!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                                _gameStatusService.IsWinnerOverlayVisible = false;
                                _isProcessingWinners = false;
                            }
                        }
                    }
                }
                // 2. GANHADOR ÚNICO
                else if (ganhadores.Count == 1)
                {
                    var g = ganhadores[0];
                    string msg = $"BINGO! {g.NomeDono} (Combo {g.ComboNumero} - Cartela {g.NumeroCartela}) - {g.NomeKit}";
                    
                    // -- CUSTOM SPEECH LOGIC --
                    string guSpeechText = $"Bingo! ";
                    
                    if (!string.IsNullOrEmpty(g.NomePadrao)) 
                    {
                        guSpeechText += $"Ganhador do prêmio de {g.NomePadrao}. ";
                    }
                    else
                    {
                        guSpeechText += $"Temos um ganhador! ";
                    }

                    if (!string.IsNullOrEmpty(g.NomePremio))
                    {
                        // Check if looks like a ranking or just append
                         guSpeechText += $"{g.NomePremio}. ";
                    }

                    guSpeechText += $"Parabéns {g.NomeDono}. ";
                    guSpeechText += $"Ganhou com o combo {g.ComboNumero}, {g.NomeKit}, cartela {g.NumeroCartela}. ";
                    // Make sure serial is read digit by digit or casually? Usually full number.
                    guSpeechText += $"Número de série: {g.CodigoValidacao}.";

                    if (!string.IsNullOrEmpty(g.NomePadrao)) msg += $" - Padrão: {g.NomePadrao}";

                    // Update UI Lists
                    if (!Ganhadores.Any(gd => gd.Info.CartelaId == g.CartelaId))
                    {
                        Ganhadores.Insert(0, new GanhadorDisplay { Texto = msg, Info = g });
                        _feedService.AddMessage("BINGO!", msg, "Success");
                    }

                    // Show Overlay
                    _gameStatusService.WinnerOverlayTitle = "GANHADOR(A)!";
                    _gameStatusService.WinnerOverlayMessage = $"{g.NomeDono}\n{g.NomeKit}\nCartela: {g.NumeroCartela}\nCombo: {g.ComboNumero}";
                    _gameStatusService.IsWinnerOverlayVisible = true;

                    // Speak (Wait for it)
                    var sp = _speechService.SpeakAsync(guSpeechText);
                    // Wait for speech to FINISH completely (User Requirement)
                    await sp; 

                    // Pause 4 seconds AFTER speech
                    await Task.Delay(4000);

                    if (wasAutoDraw)
                    {
                        // Clean up overlay and flags
                        _gameStatusService.IsWinnerOverlayVisible = false;
                        _lastPorUmaBolaCount = -1;
                        
                        // IMPORTANT: Release the processing block BEFORE attempting to resume
                        _isProcessingWinners = false;

                        if (!_cancelAutoResume)
                        {
                            // 1. Ensure Game is Running (Crucial for Accumulated Mode)
                            if (BingoSelector.SelectedItem is Bingo b && b.ModoJogo == 1)
                            {
                                _gameStatusService.IsGameRunning = true;
                                if (RodadaSelector.SelectedItem is RodadaDisplay rd) rd.Rodada.Status = "EmAndamento";
                            }

                             // Ensure game status is correct for resumption
                            _gameStatusService.IsAutoDrawActive = true;
                            _gameStatusService.CurrentTimerText = "Retomando...";

                            // 2. Restart Timer UI
                            BtnIniciarAuto_Click(this, new RoutedEventArgs());

                            // 3. Force Immediate Resumption Draw (Bypassing Button State Check)
                            await Dispatcher.InvokeAsync(() => 
                            { 
                                try 
                                {
                                    // Make sure button is enabled for visual consistency
                                    BtnSortear.IsEnabled = true; 
                                    
                                    // Call Service Directly to ensure action happens
                                    _gameService.SortearNumero();
                                    
                                    // Reset timer for next draw
                                    _nextDrawTime = DateTime.Now.Add(_autoDrawTimer.Interval);
                                }
                                catch (Exception ex)
                                {
                                    // Ignore if called too fast or round weird state
                                    // _feedService.AddMessage("Erro Resume", ex.Message, "Error");
                                }
                            });
                        }
                    }
                    else
                    {
                        MessageBox.Show(Window.GetWindow(this), msg, "GANHADOR(A)!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        _gameStatusService.IsWinnerOverlayVisible = false;
                        _isProcessingWinners = false;
                    }
                }
                
                // Only hide the game area if we are NOT resuming automatically.
                // If we are auto-resuming, the game continues.
                if (!wasAutoDraw)
                {
                    GameArea.Visibility = Visibility.Collapsed;
                    OverlayEncerrada.Visibility = Visibility.Visible;
                }
            });
        }

        private void InitializeAutoDrawTimer()
        {
            _autoDrawTimer = new DispatcherTimer();
            _autoDrawTimer.Tick += AutoDrawTimer_Tick;
            _autoDrawTimer.Interval = TimeSpan.FromSeconds(_currentIntervalSeconds);

            _countdownTimer = new DispatcherTimer();
            _countdownTimer.Interval = TimeSpan.FromMilliseconds(100);
            _countdownTimer.Tick += CountdownTimer_Tick;
        }

        private void OnGlobalBingoChanged(int bingoId)
        {
            LoadBingos();
        }

        private void OnBingoListUpdated()
        {
            LoadBingos();
        }

        private void CountdownTimer_Tick(object? sender, EventArgs e)
        {
            if (_gameStatusService.IsAutoDrawActive)
            {
                var remaining = _nextDrawTime - DateTime.Now;
                if (remaining.TotalSeconds < 0) remaining = TimeSpan.Zero;
                
                _gameStatusService.CurrentTimerText = $"{remaining.TotalSeconds:F1}s";
                
                // Calculate progress (assuming 4s interval or whatever is set)
                double total = _autoDrawTimer.Interval.TotalSeconds;
                if (total > 0)
                {
                    _gameStatusService.CurrentTimerProgress = (remaining.TotalSeconds / total) * 100;
                }
            }
            else
            {
                _gameStatusService.CurrentTimerText = "--";
                _gameStatusService.CurrentTimerProgress = 0;
            }
        }

        private void AutoDrawTimer_Tick(object? sender, EventArgs e)
        {
            if (BtnSortear.IsEnabled)
            {
                BtnSortear_Click(this, new RoutedEventArgs());
                _nextDrawTime = DateTime.Now.Add(_autoDrawTimer.Interval); // Reset for next tick
            }
            else
            {
                // Se o botão desabilitar (fim de jogo), para o timer
                StopAutoDraw();
            }
        }

        private void BtnIniciarAuto_Click(object sender, RoutedEventArgs e)
        {
            UpdateTimerInterval();
            _autoDrawTimer.Start();
            _nextDrawTime = DateTime.Now.Add(_autoDrawTimer.Interval);
            _countdownTimer.Start();
            
            _gameStatusService.IsAutoDrawActive = true;
            
            BtnIniciarAuto.IsEnabled = false;
            BtnPausarAuto.IsEnabled = true;
            BtnDecreaseInterval.IsEnabled = false;
            BtnIncreaseInterval.IsEnabled = false;
        }

        private void BtnPausarAuto_Click(object sender, RoutedEventArgs e)
        {
            StopAutoDraw(false);
        }

        private void ChkLocucao_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (_speechService != null)
            {
                _speechService.IsEnabled = ChkLocucao.IsChecked ?? false;
            }
        }

        private void StopAutoDraw(bool isTemporary = false)
        {
            _autoDrawTimer.Stop();
            _countdownTimer.Stop();
            _gameStatusService.IsAutoDrawActive = false;
            _gameStatusService.CurrentTimerText = "Pausado";
            _gameStatusService.CurrentTimerProgress = 0;

            if (isTemporary)
            {
                _isTemporarilyPaused = true;
                _cancelAutoResume = false;
                
                // Keep Pausar Enabled so user can Cancel the temporary wait
                BtnIniciarAuto.IsEnabled = false; 
                BtnPausarAuto.IsEnabled = true;
                BtnDecreaseInterval.IsEnabled = false;
                BtnIncreaseInterval.IsEnabled = false;
            }
            else
            {
                _isTemporarilyPaused = false;
                _cancelAutoResume = true; // Signal pending tasks to cancel

                BtnIniciarAuto.IsEnabled = true;
                BtnPausarAuto.IsEnabled = false;
                BtnDecreaseInterval.IsEnabled = true;
                BtnIncreaseInterval.IsEnabled = true;
            }
        }

        private void BtnCompartilharHistorico_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Integração com WhatsApp em breve!", "Compartilhar", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnDecreaseInterval_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIntervalSeconds > 2.5)
            {
                _currentIntervalSeconds -= 0.5;
                UpdateTimerInterval();
            }
        }

        private void BtnIncreaseInterval_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIntervalSeconds < 15.0)
            {
                _currentIntervalSeconds += 0.5;
                UpdateTimerInterval();
            }
        }

        private void UpdateTimerInterval()
        {
            TxtTgnsDisplay.Text = _currentIntervalSeconds.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);

            if (_autoDrawTimer != null)
            {
                _autoDrawTimer.Interval = TimeSpan.FromSeconds(_currentIntervalSeconds);
                _speechService.UpdateDrawInterval(_currentIntervalSeconds);
            }
        }


        private void LoadPatterns()
        {
            using (var scope = ((App)Application.Current).Host.Services.CreateScope())
            {
                var padraoService = scope.ServiceProvider.GetRequiredService<PadraoService>();
                var padroes = padraoService.GetPadroes();
                AvailablePatterns.Clear();
                foreach (var p in padroes) AvailablePatterns.Add(p);
            }
        }

        private void ListGanhadores_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ListGanhadores.SelectedItem is GanhadorDisplay display)
            {
                var cartela = _gameService.GetCartela(display.Info.CartelaId);
                if (cartela != null)
                {
                    var sorteados = _gameService.GetNumerosSorteados();
                    // Se tiver máscara específica do ganhador (modo dinâmico), usa ela. Senão, usa a atual global.
                    var mascara = !string.IsNullOrEmpty(display.Info.MascaraPadrao) 
                                  ? display.Info.MascaraPadrao 
                                  : _gameService.GetMascaraAtual();
                    
                    var win = new ConferenciaCartelaWindow(cartela, sorteados, mascara, display.Info.NomePadrao);
                    win.ShowDialog();
                }
            }
        }

        private void InitializeBoard()
        {
            ColumnB.Clear();
            ColumnI.Clear();
            ColumnN.Clear();
            ColumnG.Clear();
            ColumnO.Clear();

            for (int i = 1; i <= 15; i++) ColumnB.Add(new BoardNumber { Numero = i });
            for (int i = 16; i <= 30; i++) ColumnI.Add(new BoardNumber { Numero = i });
            for (int i = 31; i <= 45; i++) ColumnN.Add(new BoardNumber { Numero = i });
            for (int i = 46; i <= 60; i++) ColumnG.Add(new BoardNumber { Numero = i });
            for (int i = 61; i <= 75; i++) ColumnO.Add(new BoardNumber { Numero = i });
        }

        private void BtnOpenFlashboard_Click(object sender, RoutedEventArgs e)
        {
            if (_flashboardWindow == null || !_flashboardWindow.IsLoaded)
            {
                _flashboardWindow = new FlashboardWindow();
                _flashboardWindow.Closed += (s, args) => _flashboardWindow = null;
                _flashboardWindow.Show();
                
                // Sync current state
                SyncFlashboardState();
            }
            else
            {
                _flashboardWindow.Activate();
            }
        }

        private void SyncFlashboardState()
        {
            if (_flashboardWindow == null) return;

            // Sync Board
            foreach (var n in ColumnB.Where(x => x.IsDrawn)) _flashboardWindow.UpdateNumber(n.Numero);
            foreach (var n in ColumnI.Where(x => x.IsDrawn)) _flashboardWindow.UpdateNumber(n.Numero);
            foreach (var n in ColumnN.Where(x => x.IsDrawn)) _flashboardWindow.UpdateNumber(n.Numero);
            foreach (var n in ColumnG.Where(x => x.IsDrawn)) _flashboardWindow.UpdateNumber(n.Numero);
            foreach (var n in ColumnO.Where(x => x.IsDrawn)) _flashboardWindow.UpdateNumber(n.Numero);

            // Sync Last Number
            if (int.TryParse(TxtUltimoNumero.Text.Split('|').LastOrDefault()?.Trim(), out int lastNum))
            {
                _flashboardWindow.UpdateNumber(lastNum);
            }

            // Sync Pattern
            if (RodadaSelector.SelectedItem is RodadaDisplay display)
            {
                if (display.Rodada.ModoPadroesDinamicos)
                {
                     var padroesIds = _rodadaService.GetPadroesDaRodada(display.Rodada.Id);
                     var activePatterns = AvailablePatterns.Where(p => padroesIds.Contains(p.Id)).ToList();
                     _flashboardWindow.SetPatterns(activePatterns);
                }
                else
                {
                    var padrao = AvailablePatterns.FirstOrDefault(p => p.Id == display.Rodada.PadraoId);
                    _flashboardWindow.SetPattern(padrao);
                }
            }
        }

        private void LoadBingos()
        {
            using (var scope = ((App)Application.Current).Host.Services.CreateScope())
            {
                var comboService = scope.ServiceProvider.GetRequiredService<ComboService>();
                var bingos = comboService.GetBingos();
                BingoSelector.ItemsSource = bingos;
                
                if (_bingoContext.CurrentBingoId != -1)
                {
                    var target = bingos.FirstOrDefault(b => b.Id == _bingoContext.CurrentBingoId);
                    if (target != null)
                    {
                        BingoSelector.SelectedItem = target;
                        return;
                    }
                }

                if (bingos.Count > 0) BingoSelector.SelectedIndex = 0;
            }
        }

        private void BingoSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BingoSelector.SelectedItem is Bingo selectedBingo)
            {
                _bingoContext.SetCurrentBingo(selectedBingo.Id);
                LoadRodadas(selectedBingo.Id);
                UpdatePorUmaBolaVisibility(selectedBingo);
            }
        }

        private void UpdatePorUmaBolaVisibility(Bingo bingo)
        {
            if (bingo.ModoJogo == 1) // Acumulado
            {
                ChkLocucaoPorUmaBola.Visibility = Visibility.Collapsed;
                ChkLocucaoPorUmaBola.IsChecked = false;
            }
            else
            {
                ChkLocucaoPorUmaBola.Visibility = Visibility.Visible;
            }
        }

        private void LoadRodadas(int bingoId)
        {
            using (var scope = ((App)Application.Current).Host.Services.CreateScope())
            {
                var rodadaService = scope.ServiceProvider.GetRequiredService<RodadaService>();
                var rodadas = rodadaService.GetRodadas(bingoId);
                var displayList = rodadas.Select(r => new RodadaDisplay { Rodada = r }).ToList();
                
                RodadaSelector.ItemsSource = displayList;
                
                if (displayList.Count > 0) RodadaSelector.SelectedIndex = 0;
                else 
                {
                    RodadaSelector.ItemsSource = null;
                    BtnIniciar.IsEnabled = false;
                }
            }
        }

        private void BtnNovaRodadaExtra_Click(object sender, RoutedEventArgs e)
        {
            if (BingoSelector.SelectedItem is Bingo bingo)
            {
                try
                {
                    var padroes = _padraoService.GetPadroes();
                    var window = new SelecionarPadraoWindow(padroes);
                    if (window.ShowDialog() == true && window.SelectedPadrao != null)
                    {
                        _rodadaService.CriarRodadaExtra(bingo.Id, window.SelectedPadrao.Id);
                        LoadRodadas(bingo.Id);
                        
                        // Select the newly created round (last one)
                        if (RodadaSelector.Items.Count > 0)
                        {
                            RodadaSelector.SelectedIndex = RodadaSelector.Items.Count - 1;
                        }
                        
                        MessageBox.Show("Rodada extra criada com sucesso!");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao criar rodada extra: {ex.Message}");
                }
            }
        }

        private void RodadaSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RodadaSelector.SelectedItem is RodadaDisplay display)
            {
                UpdateUIForSelectedRodada(display.Rodada);
            }
            else
            {
                BtnIniciar.IsEnabled = false;
                GameArea.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateUIForSelectedRodada(Rodada rodada)
        {
            // Reset Visuals first
            InitializeBoard();
            Ganhadores.Clear();
            HistoricoSorteio.Clear();
            _lastPorUmaBolaCount = -1; // Reset tracker
            TxtUltimoNumero.Text = "--";
            OverlayEncerrada.Visibility = Visibility.Collapsed;
            BtnSortear.IsEnabled = true;
            StopAutoDraw();
            
            // Update Round Title on TV
            string tipo = rodada.TipoPremio;
            if (string.IsNullOrEmpty(tipo)) tipo = rodada.TipoJogo ?? "BINGO";
            
            _gameStatusService.CurrentRoundTitle = $"{rodada.NumeroOrdem}ª RODADA - {tipo.ToUpper()}";

            // Reset Flashboard
            _flashboardWindow?.ResetBoard();

            // Update Checkbox State
            ChkModoDinamicoRodada.IsChecked = rodada.ModoPadroesDinamicos;
            
            // Always enable checkbox to allow changes
            ChkModoDinamicoRodada.IsEnabled = true;

            // Sync Pattern Selector
            if (rodada.ModoPadroesDinamicos)
            {
                PatternSelector.Visibility = Visibility.Collapsed;
                BtnConfigurarPadroes.Visibility = Visibility.Visible;
                
                var padroesIds = _rodadaService.GetPadroesDaRodada(rodada.Id);
                BtnConfigurarPadroes.Content = $"Configurar Padrões ({padroesIds.Count} selecionados)";

                // Update Flashboard with multiple patterns
                var activePatterns = AvailablePatterns.Where(p => padroesIds.Contains(p.Id)).ToList();
                _flashboardWindow?.SetPatterns(activePatterns);
            }
            else
            {
                PatternSelector.Visibility = Visibility.Visible;
                BtnConfigurarPadroes.Visibility = Visibility.Collapsed;
                
                if (rodada.Padrao != null)
                {
                    var p = AvailablePatterns.FirstOrDefault(p => p.Id == rodada.PadraoId);
                    PatternSelector.SelectedItem = p;
                    _flashboardWindow?.SetPattern(p);
                }
                else
                {
                    PatternSelector.SelectedItem = null;
                    _flashboardWindow?.SetPattern(null);
                }
            }

            // Change background for Extra Round
            if (rodada.EhRodadaExtra)
            {
                GameArea.SetResourceReference(Border.BackgroundProperty, "ExtraRoundBackgroundBrush");
                GameArea.BorderBrush = Brushes.OrangeRed;
                BtnExcluirRodada.Visibility = Visibility.Visible;
            }
            else
            {
                GameArea.SetResourceReference(Border.BackgroundProperty, "CardBackgroundBrush");
                GameArea.SetResourceReference(Border.BorderBrushProperty, "BorderBrush");
                BtnExcluirRodada.Visibility = Visibility.Collapsed;
            }

            // Check status
            if (rodada.Status == "NaoIniciada")
            {
                _gameStatusService.IsGameRunning = false;
                GameArea.Visibility = Visibility.Collapsed;
                BtnIniciar.Visibility = Visibility.Visible;
                BtnIniciar.IsEnabled = true;
                BtnReiniciar.Visibility = Visibility.Collapsed;
                BtnEncerrar.Visibility = Visibility.Collapsed;
            }
            else
            {
                // If started or finished, load state
                GameArea.Visibility = Visibility.Visible;
                
                // Load data without "starting" logic (just view)
                _gameService.CarregarDadosBingo(rodada.BingoId);
                _gameService.IniciarRodada(rodada.Id); // This loads state into service
                _gameService.RefreshPorUmaBolaStats();
                
                var sorteados = _gameService.GetNumerosSorteados();
                
                // Populate History
                HistoricoSorteio.Clear();
                foreach(var n in sorteados.AsEnumerable().Reverse())
                {
                    HistoricoSorteio.Add($"{GetLetter(n)} | {n}");
                }
                
                // Disable dynamic mode checkbox if numbers have been drawn
                if (sorteados.Count > 0)
                {
                    ChkModoDinamicoRodada.IsEnabled = false;
                }

                foreach (var n in sorteados)
                {
                    UpdateBoard(n);
                    string letter = GetLetter(n);
                    TxtUltimoNumero.Text = $"{letter} | {n}";
                }

                var ganhadoresAtuais = _gameService.GetGanhadoresAtuais();
                foreach (var g in ganhadoresAtuais)
                {
                    string msg = $"BINGO! {g.NomeDono} (Combo {g.ComboNumero} - Cartela {g.NumeroCartela})";
                    if (!Ganhadores.Any(gd => gd.Texto == msg))
                    {
                        Ganhadores.Add(new GanhadorDisplay { Texto = msg, Info = g });
                    }
                }

                if (rodada.Status == "Encerrada")
                {
                    _gameStatusService.IsGameRunning = false;
                    OverlayEncerrada.Visibility = Visibility.Visible;
                    BtnSortear.IsEnabled = false;
                    BtnIniciar.Visibility = Visibility.Collapsed;
                    BtnEncerrar.Visibility = Visibility.Collapsed;
                    BtnReiniciar.Visibility = Visibility.Visible;
                }
                else // EmAndamento
                {
                    _gameStatusService.IsGameRunning = true;
                    BtnIniciar.Visibility = Visibility.Collapsed; // Already started
                    BtnEncerrar.Visibility = Visibility.Visible;
                    BtnReiniciar.Visibility = Visibility.Visible;
                }
            }
        }

        private void BtnIniciar_Click(object sender, RoutedEventArgs e)
        {
            if (BingoSelector.SelectedItem is Bingo bingo && RodadaSelector.SelectedItem is RodadaDisplay display)
            {
                try
                {
                    // Just ensure status is updated if needed, UI is already handled by SelectionChanged
                    _gameService.CarregarDadosBingo(bingo.Id);
                    _gameService.IniciarRodada(display.Rodada.Id);
                    
                    // Force status update in local object to reflect DB change
                    display.Rodada.Status = "EmAndamento";
                    _gameStatusService.IsGameRunning = true;
                    PanelPorUmaBola.Visibility = Visibility.Collapsed;
                    UpdateUIForSelectedRodada(display.Rodada);
                    
                    MessageBox.Show($"Rodada '{display.Rodada.NumeroOrdem}ª Rodada' iniciada!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao iniciar rodada: {ex.Message}");
                }
            }
        }

        private void BtnSortear_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _gameService.SortearNumero();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void OnNumeroSorteado(int numero)
        {
            Dispatcher.Invoke(() =>
            {
                string letter = GetLetter(numero);
                TxtUltimoNumero.Text = $"{letter} | {numero}";
                UpdateBoard(numero);
                
                // Add to history (newest first)
                HistoricoSorteio.Insert(0, $"{letter} | {numero}");
                
                // Update Flashboard
                _flashboardWindow?.UpdateNumber(numero);

                // Disable dynamic mode checkbox when a number is drawn
                ChkModoDinamicoRodada.IsEnabled = false;
            });
        }

        private string GetLetter(int n)
        {
            if (n <= 15) return "B";
            if (n <= 30) return "I";
            if (n <= 45) return "N";
            if (n <= 60) return "G";
            return "O";
        }

        private void UpdateBoard(int numero)
        {
            BoardNumber? item = null;
            if (numero <= 15) item = ColumnB.FirstOrDefault(b => b.Numero == numero);
            else if (numero <= 30) item = ColumnI.FirstOrDefault(b => b.Numero == numero);
            else if (numero <= 45) item = ColumnN.FirstOrDefault(b => b.Numero == numero);
            else if (numero <= 60) item = ColumnG.FirstOrDefault(b => b.Numero == numero);
            else item = ColumnO.FirstOrDefault(b => b.Numero == numero);

            if (item != null)
            {
                item.IsDrawn = true;
            }
        }





        private void OnRodadaEncerrada()
        {
            Dispatcher.Invoke(() =>
            {
                StopAutoDraw();
                
                // Update local object status
                if (RodadaSelector.SelectedItem is RodadaDisplay currentDisplay)
                {
                    currentDisplay.Rodada.Status = "Encerrada";
                    _gameStatusService.IsGameRunning = false;
                }
                
                // Auto-advance to next round if available
                if (RodadaSelector.SelectedIndex < RodadaSelector.Items.Count - 1)
                {
                    var nextIndex = RodadaSelector.SelectedIndex + 1;
                    if (RodadaSelector.Items[nextIndex] is RodadaDisplay nextRound)
                    {
                        _feedService.AddMessage("Início Rodada", $"{nextRound.Rodada.NumeroOrdem}ª Rodada", "RoundTitle");
                        _feedService.AddSeparator();
                    }
                    RodadaSelector.SelectedIndex++;
                }
                else
                {
                    _feedService.AddSeparator();
                    // Just update UI for current finished round
                    if (RodadaSelector.SelectedItem is RodadaDisplay display)
                    {
                        
                        UpdateUIForSelectedRodada(display.Rodada);
                    }
                }
            });
        }

        private void BtnEncerrar_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Tem certeza que deseja encerrar a rodada?", "Confirmar Encerramento", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    _gameService.EncerrarRodada();
                    
                    // Update local object status
                    if (RodadaSelector.SelectedItem is RodadaDisplay currentDisplay)
                    {
                        currentDisplay.Rodada.Status = "Encerrada";
                        _gameStatusService.IsGameRunning = false;
                    }
                    
                    // Auto-advance to next round if available
                    if (RodadaSelector.SelectedIndex < RodadaSelector.Items.Count - 1)
                    {
                        var nextIndex = RodadaSelector.SelectedIndex + 1;
                        if (RodadaSelector.Items[nextIndex] is RodadaDisplay nextRound)
                        {
                            _feedService.AddMessage("Início Rodada", $"{nextRound.Rodada.NumeroOrdem}ª Rodada", "RoundTitle");
                            _feedService.AddSeparator();
                        }
                        RodadaSelector.SelectedIndex++;
                    }
                    else
                    {
                        _feedService.AddSeparator();
                        // Just update UI for current finished round
                        if (RodadaSelector.SelectedItem is RodadaDisplay display)
                        {
                            UpdateUIForSelectedRodada(display.Rodada);
                        }
                    }                    MessageBox.Show("Rodada encerrada com sucesso!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao encerrar rodada: {ex.Message}");
                }
            }
        }

        private void BtnReiniciar_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Tem certeza que deseja reiniciar a rodada? Todos os números sorteados e ganhadores serão apagados.", "Confirmar Reinício", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    _gameService.ReiniciarRodada();
                    
                    // Update local object status
                    if (RodadaSelector.SelectedItem is RodadaDisplay currentDisplay)
                    {
                        currentDisplay.Rodada.Status = "EmAndamento";
                        _gameStatusService.IsGameRunning = true;
                    }

                    // Reset UI
                    InitializeBoard();
                    Ganhadores.Clear();
                    HistoricoSorteio.Clear();
                    TxtUltimoNumero.Text = "--";
                    PanelPorUmaBola.Visibility = Visibility.Collapsed;
                    StopAutoDraw();
                    
                    // Refresh UI state (buttons visibility etc)
                    if (RodadaSelector.SelectedItem is RodadaDisplay display)
                    {
                        UpdateUIForSelectedRodada(display.Rodada);
                    }
                    
                    MessageBox.Show("Rodada reiniciada com sucesso!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao reiniciar rodada: {ex.Message}");
                }
            }
        }

        private void BtnExcluirRodada_Click(object sender, RoutedEventArgs e)
        {
            if (RodadaSelector.SelectedItem is RodadaDisplay display && display.Rodada.EhRodadaExtra)
            {
                if (MessageBox.Show("Tem certeza que deseja excluir esta rodada extra?", "Confirmar Exclusão", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        _rodadaService.ExcluirRodada(display.Rodada.Id);
                        MessageBox.Show("Rodada extra excluída com sucesso!");
                        
                        // Reload rounds
                        if (BingoSelector.SelectedItem is Bingo bingo)
                        {
                            LoadRodadas(bingo.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erro ao excluir rodada: {ex.Message}");
                    }
                }
            }
        }

        private void PatternSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PatternSelector.SelectedItem is Padrao selectedPadrao && RodadaSelector.SelectedItem is RodadaDisplay display)
            {
                if (display.Rodada.PadraoId != selectedPadrao.Id)
                {
                    display.Rodada.PadraoId = selectedPadrao.Id;
                    display.Rodada.Padrao = selectedPadrao;
                    
                    _rodadaService.AtualizarRodada(display.Rodada);
                    
                    if (display.Rodada.Status != "NaoIniciada")
                    {
                         _gameService.AtualizarPadrao(selectedPadrao);
                    }
                    
                    // Refresh RodadaSelector to show new pattern name
                    RodadaSelector.Items.Refresh();
                }
            }
        }

        private void BtnConfigurarPadroes_Click(object sender, RoutedEventArgs e)
        {
            if (RodadaSelector.SelectedItem is RodadaDisplay display)
            {
                var rodada = display.Rodada;
                var padroesIds = _rodadaService.GetPadroesDaRodada(rodada.Id);
                var todosPadroes = _padraoService.GetPadroes();
                
                var window = new SelecionarPadroesWindow(todosPadroes, padroesIds);
                if (window.ShowDialog() == true)
                {
                    _rodadaService.SalvarPadroesDaRodada(rodada.Id, window.SelectedPadroesIds);
                    
                    BtnConfigurarPadroes.Content = $"Configurar Padrões ({window.SelectedPadroesIds.Count} selecionados)";
                    
                    if (rodada.Status == "EmAndamento")
                    {
                        _gameService.AtualizarPadroesDinamicos();
                    }
                }
            }
        }

        private void ChkModoDinamicoRodada_Click(object sender, RoutedEventArgs e)
        {
            if (RodadaSelector.SelectedItem is RodadaDisplay display)
            {
                var rodada = display.Rodada;
                bool novoEstado = ChkModoDinamicoRodada.IsChecked == true;
                
                rodada.ModoPadroesDinamicos = novoEstado;
                
                // Update in DB
                _rodadaService.AtualizarModoDinamico(rodada.Id, rodada.ModoPadroesDinamicos);
                
                // Refresh UI
                UpdateUIForSelectedRodada(rodada);
                
                // If running, update game service
                if (rodada.Status == "EmAndamento")
                {
                    _gameService.SetModoDinamico(novoEstado);
                }
            }
        }

        private void ChkModoUnico_Click(object sender, RoutedEventArgs e)
        {
            _gameService.ModoUnicoAtivo = ChkModoUnico.IsChecked == true;
        }
    }

    public class RodadaDisplay
    {
        public required Rodada Rodada { get; set; }
        public string DescricaoCompleta => $"{Rodada.NumeroOrdem}ª Rodada - {Rodada.TipoPremio} ({Rodada.Padrao?.Nome ?? "Sem Padrão"})";
        public Brush TextColor => Rodada.EhRodadaExtra ? Brushes.OrangeRed : Brushes.Black;
    }

    public class BoardNumber : System.ComponentModel.INotifyPropertyChanged
    {
        public int Numero { get; set; }
        private bool _isDrawn;
        public bool IsDrawn
        {
            get => _isDrawn;
            set
            {
                _isDrawn = value;
                OnPropertyChanged(nameof(IsDrawn));
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }

    public class GanhadorDisplay
    {
        public required string Texto { get; set; }
        public required GanhadorInfo Info { get; set; }
        public override string ToString() => Texto;
    }
}
