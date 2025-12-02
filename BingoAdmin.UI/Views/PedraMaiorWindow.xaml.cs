using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using BingoAdmin.UI.Services;

namespace BingoAdmin.UI.Views
{
    public partial class PedraMaiorWindow : Window
    {
        public ObservableCollection<PedraMaiorItem> Items { get; set; } = new ObservableCollection<PedraMaiorItem>();
        private Random _random = new Random();

        public PedraMaiorWindow(List<GanhadorInfo> ganhadores)
        {
            InitializeComponent();
            GridGanhadores.ItemsSource = Items;

            foreach (var g in ganhadores)
            {
                Items.Add(new PedraMaiorItem
                {
                    Nome = g.NomeDono,
                    ComboNumero = g.ComboNumero,
                    NumeroCartela = g.NumeroCartela,
                    NomePadrao = g.NomePadrao,
                    OriginalInfo = g
                });
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
            
            // Force refresh if needed (though INotifyPropertyChanged handles it)
            // GridGanhadores.Items.Refresh();
        }

        private void BtnFechar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        public GanhadorInfo GetWinner()
        {
            return Items.FirstOrDefault(i => i.IsWinner)?.OriginalInfo;
        }

        public PedraMaiorItem GetWinnerItem()
        {
            return Items.FirstOrDefault(i => i.IsWinner);
        }
    }

    public class PedraMaiorItem : INotifyPropertyChanged
    {
        public string Nome { get; set; }
        public int ComboNumero { get; set; }
        public int NumeroCartela { get; set; }
        public string NomePadrao { get; set; } = string.Empty;
        
        private int? _pedraSorteada;
        public int? PedraSorteada 
        { 
            get => _pedraSorteada;
            set
            {
                _pedraSorteada = value;
                OnPropertyChanged(nameof(PedraSorteada));
            }
        }

        private bool _isWinner;
        public bool IsWinner 
        { 
            get => _isWinner;
            set
            {
                _isWinner = value;
                OnPropertyChanged(nameof(IsWinner));
            }
        }

        public GanhadorInfo OriginalInfo { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
