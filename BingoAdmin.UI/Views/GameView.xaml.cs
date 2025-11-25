using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

        public ObservableCollection<BoardNumber> ColumnB { get; set; } = new ObservableCollection<BoardNumber>();
        public ObservableCollection<BoardNumber> ColumnI { get; set; } = new ObservableCollection<BoardNumber>();
        public ObservableCollection<BoardNumber> ColumnN { get; set; } = new ObservableCollection<BoardNumber>();
        public ObservableCollection<BoardNumber> ColumnG { get; set; } = new ObservableCollection<BoardNumber>();
        public ObservableCollection<BoardNumber> ColumnO { get; set; } = new ObservableCollection<BoardNumber>();
        
        public ObservableCollection<GanhadorDisplay> Ganhadores { get; set; } = new ObservableCollection<GanhadorDisplay>();
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

            InitializeBoard();
            
            // Set DataContext to self so we can bind to properties
            this.DataContext = this;
            
            ListGanhadores.ItemsSource = Ganhadores;
            ListGanhadores.MouseDoubleClick += ListGanhadores_MouseDoubleClick;
            
            PatternSelector.ItemsSource = AvailablePatterns;
            LoadPatterns();

            LoadBingos();

            _gameService.OnNumeroSorteado += OnNumeroSorteado;
            _gameService.OnGanhadoresEncontrados += OnGanhadoresEncontrados;
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
                    var mascara = _gameService.GetMascaraAtual();
                    var win = new ConferenciaCartelaWindow(cartela, sorteados, mascara);
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

        private void LoadBingos()
        {
            var bingos = _comboService.GetBingos();
            BingoSelector.ItemsSource = bingos;
            if (bingos.Count > 0) BingoSelector.SelectedIndex = 0;
        }

        private void BingoSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BingoSelector.SelectedItem is Bingo selectedBingo)
            {
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
            TxtUltimoNumero.Text = "--";
            OverlayEncerrada.Visibility = Visibility.Collapsed;
            BtnSortear.IsEnabled = true;
            
            // Sync Pattern Selector
            if (rodada.Padrao != null)
            {
                PatternSelector.SelectedItem = AvailablePatterns.FirstOrDefault(p => p.Id == rodada.PadraoId);
            }
            else
            {
                PatternSelector.SelectedItem = null;
            }

            // Change background for Extra Round
            if (rodada.EhRodadaExtra)
            {
                GameArea.Background = new SolidColorBrush(Color.FromRgb(255, 250, 240)); // FloralWhite (Subtle)
                GameArea.BorderBrush = Brushes.OrangeRed;
                BtnExcluirRodada.Visibility = Visibility.Visible;
            }
            else
            {
                GameArea.Background = Brushes.White;
                GameArea.BorderBrush = Brushes.LightGray;
                BtnExcluirRodada.Visibility = Visibility.Collapsed;
            }

            // Check status
            if (rodada.Status == "NaoIniciada")
            {
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
                    OverlayEncerrada.Visibility = Visibility.Visible;
                    BtnSortear.IsEnabled = false;
                    BtnIniciar.Visibility = Visibility.Collapsed;
                    BtnEncerrar.Visibility = Visibility.Collapsed;
                    BtnReiniciar.Visibility = Visibility.Visible;
                }
                else // EmAndamento
                {
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
                                MessageBox.Show(msg, "TEMOS UM VENCEDOR NO DESEMPATE!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                            }
                        }
                    }
                }
                else if (ganhadores.Count == 1)
                {
                    var g = ganhadores[0];
                    string msg = $"BINGO! {g.NomeDono} (Combo {g.ComboNumero} - Cartela {g.NumeroCartela})";
                    if (!Ganhadores.Any(gd => gd.Texto == msg))
                    {
                        Ganhadores.Add(new GanhadorDisplay { Texto = msg, Info = g });
                        MessageBox.Show(msg, "TEMOS UM GANHADOR!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
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
                    }

                    // Reset UI
                    InitializeBoard();
                    Ganhadores.Clear();
                    TxtUltimoNumero.Text = "--";
                    
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
                OnPropertyChanged(nameof(Background));
                OnPropertyChanged(nameof(Foreground));
            }
        }

        public Brush Background => IsDrawn ? Brushes.Red : Brushes.White;
        public Brush Foreground => IsDrawn ? Brushes.White : Brushes.Black;

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
