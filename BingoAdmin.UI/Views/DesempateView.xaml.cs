using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BingoAdmin.Domain.Entities;
using BingoAdmin.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BingoAdmin.UI.Views
{
    public partial class DesempateView : UserControl
    {
        private readonly DesempateService _desempateService;
        private readonly ComboService _comboService;
        private readonly RodadaService _rodadaService;
        private readonly GameService _gameService;
        private readonly BingoContextService _bingoContext;

        public DesempateView()
        {
            InitializeComponent();
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;

            var services = ((App)Application.Current).Host.Services;
            _desempateService = services.GetRequiredService<DesempateService>();
            _comboService = services.GetRequiredService<ComboService>();
            _rodadaService = services.GetRequiredService<RodadaService>();
            _gameService = services.GetRequiredService<GameService>();
            _bingoContext = services.GetRequiredService<BingoContextService>();

            _gameService.OnGanhadoresEncontrados += OnGanhadoresEncontrados;

            LoadBingos();
            _bingoContext.OnBingoChanged += OnGlobalBingoChanged;
        }

        private void OnGlobalBingoChanged(int bingoId)
        {
            if (BingoSelector.SelectedItem is Bingo current && current.Id == bingoId) return;

            var bingos = BingoSelector.ItemsSource as System.Collections.Generic.List<Bingo>;
            var target = System.Linq.Enumerable.FirstOrDefault(bingos, b => b.Id == bingoId);
            if (target != null)
            {
                BingoSelector.SelectedItem = target;
            }
        }

        private void OnGanhadoresEncontrados(System.Collections.Generic.List<GanhadorInfo> ganhadores)
        {
            Dispatcher.Invoke(() =>
            {
                LoadDesempates();
            });
        }

        private void BtnAtualizar_Click(object sender, RoutedEventArgs e)
        {
            LoadDesempates();
        }

        private void LoadBingos()
        {
            var bingos = _comboService.GetBingos();
            BingoSelector.ItemsSource = bingos;
            
            if (_bingoContext.CurrentBingoId != -1)
            {
                var target = System.Linq.Enumerable.FirstOrDefault(bingos, b => b.Id == _bingoContext.CurrentBingoId);
                if (target != null)
                {
                    BingoSelector.SelectedItem = target;
                    return;
                }
            }

            if (BingoSelector.Items.Count > 0) BingoSelector.SelectedIndex = 0;
        }

        private void BingoSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BingoSelector.SelectedItem is Bingo selectedBingo)
            {
                _bingoContext.SetCurrentBingo(selectedBingo.Id);
                LoadDesempates();
            }
        }

        private void LoadDesempates()
        {
            if (BingoSelector.SelectedItem is Bingo bingo)
            {
                // Carrega dados para obter número relativo da cartela (se necessário pelo GameService)
                _gameService.CarregarDadosBingo(bingo.Id);

                var infos = _desempateService.GetDesempatesDoBingo(bingo.Id);

                // Filter out rounds that don't have any Pedra Maior data (all zeros)
                infos = infos.Where(i => i.Itens.Any(item => item.PedraMaior > 0)).ToList();

                var viewModels = infos.Select(info => 
                {
                    var participantes = info.Itens.Select(item => 
                    {
                        return new DesempateParticipanteViewModel
                        {
                            Nome = item.Nome,
                            Combo = item.Combo,
                            CartelaNumero = item.CartelaNumero,
                            CartelaId = item.CartelaId, 
                            PedraMaior = item.PedraMaior,
                            IsGanhador = item.IsVencedor,
                            RodadaId = info.Rodada.Id,
                            Mascara = info.Rodada.Padrao?.Mascara ?? new string('1', 25)
                        };
                    }).ToList();

                    return new RodadaDesempateViewModel 
                    { 
                        HeaderText = $"{info.Rodada.NumeroOrdem}ª Rodada - {info.Rodada.TipoPremio} ({info.Itens.Count} Participantes)",
                        Participantes = participantes
                    };
                }).ToList();

                ListaDesempates.ItemsSource = viewModels;
            }
            else
            {
                ListaDesempates.ItemsSource = null;
            }
        }

        private void ListaParticipantes_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is DesempateParticipanteViewModel vm)
            {
                var cartela = _gameService.GetCartela(vm.CartelaId);
                if (cartela != null)
                {
                    // Get drawn numbers for THIS round, not the current game state
                    var sorteio = _desempateService.GetSorteioDaRodada(vm.RodadaId);
                    var sorteados = new System.Collections.Generic.HashSet<int>();
                    
                    if (sorteio != null && !string.IsNullOrEmpty(sorteio.BolasSorteadas))
                    {
                        var nums = sorteio.BolasSorteadas.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                         .Select(int.Parse);
                        foreach(var n in nums) sorteados.Add(n);
                    }

                    var win = new ConferenciaCartelaWindow(cartela, sorteados, vm.Mascara);
                    win.ShowDialog();
                }
            }
        }
    }

    public class RodadaDesempateViewModel
    {
        public string HeaderText { get; set; } = string.Empty;
        public System.Collections.Generic.List<DesempateParticipanteViewModel> Participantes { get; set; } = new();
    }

    public class DesempateParticipanteViewModel
    {
        public string Nome { get; set; } = string.Empty;
        public int Combo { get; set; }
        public int CartelaNumero { get; set; }
        public int CartelaId { get; set; }
        public int? PedraMaior { get; set; }
        public bool IsGanhador { get; set; }
        public int RodadaId { get; set; }
        public string Mascara { get; set; } = string.Empty;
        
        public string DisplayText => $"{Nome} | combo {Combo} | cartela {CartelaNumero} | numero pedra maior = {PedraMaior ?? 0}";
        public FontWeight FontWeight => IsGanhador ? FontWeights.Bold : FontWeights.Normal;
        public string GanhadorLabel => IsGanhador ? " < - Ganhador" : "";
    }
}
