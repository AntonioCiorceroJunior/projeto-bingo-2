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
        
        private DispatcherTimer _autoDrawTimer;
        private DispatcherTimer _countdownTimer; // For visual countdown
        private DateTime _nextDrawTime;
        private FlashboardWindow? _flashboardWindow;

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

            InitializeBoard();
            InitializeAutoDrawTimer();
            
            // Set DataContext to self so we can bind to properties
            this.DataContext = this;
            
            ListGanhadores.ItemsSource = Ganhadores;
            ListGanhadores.MouseDoubleClick += ListGanhadores_MouseDoubleClick;
            
            ListHistorico.ItemsSource = HistoricoSorteio;

            PatternSelector.ItemsSource = AvailablePatterns;
            LoadPatterns();

            LoadBingos();

            _gameService.OnNumeroSorteado += OnNumeroSorteado;
            _gameService.OnGanhadoresEncontrados += OnGanhadoresEncontrados;
            _gameService.OnRodadaEncerrada += OnRodadaEncerrada;
            _bingoContext.OnBingoChanged += OnGlobalBingoChanged;
            _bingoContext.OnBingoListUpdated += OnBingoListUpdated;
        }

        private void OnBingoListUpdated()
        {
            LoadBingos();
            
            if (!_gameStatusService.IsGameRunning)
            {
                 var bingos = BingoSelector.ItemsSource as List<Bingo>;
                 var newest = bingos?.OrderByDescending(b => b.Id).FirstOrDefault();
                 
                 if (newest != null && newest.Id != _bingoContext.CurrentBingoId)
                 {
                     BingoSelector.SelectedItem = newest;
                 }
            }
        }

        private void OnGlobalBingoChanged(int bingoId)
        {
            if (BingoSelector.SelectedItem is Bingo current && current.Id == bingoId) return;

            var bingos = BingoSelector.ItemsSource as List<Bingo>;
            var target = bingos?.FirstOrDefault(b => b.Id == bingoId);
            if (target != null)
            {
                BingoSelector.SelectedItem = target;
            }
        }

        private void InitializeAutoDrawTimer()
        {
            _autoDrawTimer = new DispatcherTimer();
            _autoDrawTimer.Tick += AutoDrawTimer_Tick;
            _autoDrawTimer.Interval = TimeSpan.FromSeconds(4); // Default

            _countdownTimer = new DispatcherTimer();
            _countdownTimer.Interval = TimeSpan.FromMilliseconds(100);
            _countdownTimer.Tick += CountdownTimer_Tick;
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
            TxtTgns.IsEnabled = false; // Lock interval while running
        }

        private void BtnPausarAuto_Click(object sender, RoutedEventArgs e)
        {
            StopAutoDraw();
        }

        private void StopAutoDraw()
        {
            _autoDrawTimer.Stop();
            _countdownTimer.Stop();
            _gameStatusService.IsAutoDrawActive = false;
            _gameStatusService.CurrentTimerText = "Pausado";
            _gameStatusService.CurrentTimerProgress = 0;

            BtnIniciarAuto.IsEnabled = true;
            BtnPausarAuto.IsEnabled = false;
            TxtTgns.IsEnabled = true;
        }

        private void BtnCompartilharHistorico_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Integração com WhatsApp em breve!", "Compartilhar", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void TxtTgns_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateTimerInterval();
        }

        private void UpdateTimerInterval()
        {
            if (_autoDrawTimer == null) return;

            if (int.TryParse(TxtTgns.Text, out int seconds) && seconds > 0)
            {
                _autoDrawTimer.Interval = TimeSpan.FromSeconds(seconds);
            }
        }


        private void LoadPatterns()
        {
            var padroes = _padraoService.GetPadroes();
            AvailablePatterns.Clear();
            foreach (var p in padroes) AvailablePatterns.Add(p);
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
            var bingos = _comboService.GetBingos();
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

        private void BingoSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BingoSelector.SelectedItem is Bingo selectedBingo)
            {
                _bingoContext.SetCurrentBingo(selectedBingo.Id);
                LoadRodadas(selectedBingo.Id);
            }
        }

        private void LoadRodadas(int bingoId)
        {
            var rodadas = _rodadaService.GetRodadas(bingoId);
            var displayList = rodadas.Select(r => new RodadaDisplay { Rodada = r }).ToList();
            
            RodadaSelector.ItemsSource = displayList;
            
            if (displayList.Count > 0) RodadaSelector.SelectedIndex = 0;
            else 
            {
                RodadaSelector.ItemsSource = null;
                BtnIniciar.IsEnabled = false;
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
            TxtUltimoNumero.Text = "--";
            OverlayEncerrada.Visibility = Visibility.Collapsed;
            BtnSortear.IsEnabled = true;
            StopAutoDraw();
            
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

        private void OnGanhadoresEncontrados(List<GanhadorInfo> ganhadores)
        {
            Dispatcher.Invoke(() =>
            {
                // Se houver ganhadores e o sorteio automático estiver rodando, PAUSA IMEDIATAMENTE
                if (_autoDrawTimer.IsEnabled)
                {
                    StopAutoDraw();
                    MessageBox.Show("Ganhador(es) encontrado(s)! O sorteio automático foi pausado.", "Bingo", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                if (ganhadores.Count > 1)
                {
                    // Sincroniza a tabela de desempate com os ganhadores atuais (limpa fantasmas)
                    if (RodadaSelector.SelectedItem is RodadaDisplay display)
                    {
                        _desempateService.SincronizarDesempate(display.Rodada.Id, ganhadores);
                    }

                    var window = new PedraMaiorWindow(ganhadores);
                    if (window.ShowDialog() == true)
                    {
                        // Salvar resultados do desempate
                        if (RodadaSelector.SelectedItem is RodadaDisplay currentDisplay)
                        {
                            var resultados = window.Items.Select(i => (
                                CartelaId: i.OriginalInfo.CartelaId,
                                NumeroSorteado: i.PedraSorteada ?? 0,
                                IsVencedor: i.IsWinner
                            )).ToList();
                            
                            _desempateService.SalvarSorteioPedraMaiorEmLote(currentDisplay.Rodada.Id, resultados);
                        }

                        var winnerItem = window.GetWinnerItem();
                        if (winnerItem != null)
                        {
                            string msg = $"GANHADOR: {winnerItem.Nome}, Combo {winnerItem.ComboNumero}, Cartela {winnerItem.NumeroCartela} - Pedra Maior: {winnerItem.PedraSorteada}";
                            if (!Ganhadores.Any(g => g.Texto == msg))
                            {
                                Ganhadores.Add(new GanhadorDisplay { Texto = msg, Info = winnerItem.OriginalInfo });
                                _feedService.AddMessage("Pedra Maior - Vencedor", $"Ganhador: {winnerItem.Nome}, Combo {winnerItem.ComboNumero}, Cartela {winnerItem.NumeroCartela} (Pedra: {winnerItem.PedraSorteada})", "Success");
                                MessageBox.Show(msg, "TEMOS UM VENCEDOR NO DESEMPATE!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            }
                        }
                    }
                }
                else if (ganhadores.Count == 1)
                {
                    var g = ganhadores[0];
                    string msg = $"BINGO! {g.NomeDono} (Combo {g.ComboNumero} - Cartela {g.NumeroCartela})";
                    if (!string.IsNullOrEmpty(g.NomePadrao))
                    {
                        msg += $" - Padrão: {g.NomePadrao}";
                    }

                    if (!Ganhadores.Any(gd => gd.Texto == msg))
                    {
                        Ganhadores.Add(new GanhadorDisplay { Texto = msg, Info = g });
                        MessageBox.Show(msg, "TEMOS UM GANHADOR!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                }
            });
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
                    RodadaSelector.SelectedIndex++;
                }
                else
                {
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
                        RodadaSelector.SelectedIndex++;
                    }
                    else
                    {
                        // Just update UI for current finished round
                        if (RodadaSelector.SelectedItem is RodadaDisplay display)
                        {
                            UpdateUIForSelectedRodada(display.Rodada);
                        }
                    }

                    MessageBox.Show("Rodada encerrada com sucesso!");
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
