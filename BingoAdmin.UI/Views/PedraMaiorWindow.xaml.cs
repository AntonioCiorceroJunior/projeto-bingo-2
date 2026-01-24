using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using BingoAdmin.UI.Services;
using BingoAdmin.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BingoAdmin.UI.Views
{
    public partial class PedraMaiorWindow : Window
    {
        public ObservableCollection<PedraMaiorItemViewModel> Items { get; set; } = new ObservableCollection<PedraMaiorItemViewModel>();
        private Random _random = new Random();
        private GameStatusService? _gameStatusService;

        public PedraMaiorWindow(List<GanhadorInfo> ganhadores)
        {
            InitializeComponent();
            
            if (Application.Current is App app)
            {
                _gameStatusService = app.Host.Services.GetService<GameStatusService>();
            }

            GridGanhadores.ItemsSource = Items;

            // Initialize Items
            foreach (var g in ganhadores)
            {
                Items.Add(new PedraMaiorItemViewModel
                {
                    Nome = g.NomeDono,
                    ComboNumero = g.ComboNumero.ToString(),
                    NumeroCartela = g.NumeroCartela.ToString(),
                    NomePadrao = g.NomePadrao,
                    NomeKit = g.NomeKit,
                    OriginalInfo = g
                });
            }

            // Sync with GameStatusService
            if (_gameStatusService != null)
            {
                _gameStatusService.PedraMaiorParticipants.Clear();
                foreach (var item in Items)
                {
                    _gameStatusService.PedraMaiorParticipants.Add(item);
                }
                _gameStatusService.IsPedraMaiorActive = true;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (_gameStatusService != null)
            {
                _gameStatusService.IsPedraMaiorActive = false;
                _gameStatusService.PedraMaiorParticipants.Clear();
            }
        }

        private async void BtnSortear_Click(object sender, RoutedEventArgs e)
        {
            BtnSortear.IsEnabled = false;
            ChkModoSuspense.IsEnabled = false;

            // Sortear números de 1 a 100
            var usedNumbers = new HashSet<int>();
            bool suspense = ChkModoSuspense.IsChecked == true;
            
            foreach (var item in Items)
            {
                int num;
                do
                {
                    num = _random.Next(1, 101);
                } while (usedNumbers.Contains(num));
                
                usedNumbers.Add(num);
                item.PedraSorteada = num;

                if (suspense)
                {
                    await Task.Delay(2000);
                }
            }

            // Determinar vencedor
            var maxPedra = Items.Max(i => i.PedraSorteada);
            var winner = Items.First(i => i.PedraSorteada == maxPedra);
            
            foreach (var item in Items)
            {
                item.IsWinner = item == winner;
            }

            // Atualizar UI
            TitleText.Text = "PEDRA MAIOR - RESULTADO";
            TxtResultado.Text = $"GANHADOR: {winner.Nome} - Pedra {winner.PedraSorteada}";
            
            BtnSortear.Visibility = Visibility.Collapsed;
            BtnFechar.Visibility = Visibility.Visible;
        }

        private void BtnFechar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        public GanhadorInfo? GetWinner()
        {
            return Items.FirstOrDefault(i => i.IsWinner)?.OriginalInfo as GanhadorInfo;
        }

        public PedraMaiorItemViewModel? GetWinnerItem()
        {
            return Items.FirstOrDefault(i => i.IsWinner);
        }
    }
}
