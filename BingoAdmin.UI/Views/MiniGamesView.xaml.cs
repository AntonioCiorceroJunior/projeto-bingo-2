using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BingoAdmin.UI.Views
{
    public partial class MiniGamesView : UserControl
    {
        private readonly List<string> _animais = new() 
        { 
            "Avestruz", "Águia", "Burro", "Borboleta", "Cachorro", 
            "Cabra", "Carneiro", "Camelo", "Cobra", "Coelho", 
            "Cavalo", "Elefante", "Galo", "Gato", "Jacaré", 
            "Leão", "Macaco", "Porco", "Pavão", "Peru", 
            "Touro", "Tigre", "Urso", "Veado", "Vaca",
            // Extras para completar 50
            "Formiga", "Abelha", "Baleia", "Besouro", "Búfalo", 
            "Canguru", "Castor", "Cisne", "Coruja", "Crocodilo", 
            "Esquilo", "Falcão", "Foca", "Girafa", "Golfinho", 
            "Gorila", "Hipopótamo", "Hiena", "Iguana", "Javali", 
            "Lagosta", "Leopardo", "Lobo", "Lontra", "Lula"
        };

        private List<string> _nomesFixos = new()
        {
            "Ana", "Bruno", "Carlos", "Daniela", "Eduardo", "Fernanda", "Gabriel", "Helena", "Igor", "Julia",
            "Karla", "Lucas", "Mariana", "Nicolas", "Olivia", "Pedro", "Quezia", "Rafael", "Sofia", "Thiago",
            "Ursula", "Vinicius", "Wagner", "Xavier", "Yasmin", "Zeca", "Alice", "Bernardo", "Caio", "Diana",
            "Elisa", "Felipe", "Gustavo", "Heitor", "Isabela", "Joao", "Kevin", "Larissa", "Miguel", "Natalia",
            "Otavio", "Paula", "Quiteria", "Ricardo", "Sara", "Tatiana", "Ubaldo", "Vitoria", "Wesley", "Ximena",
            "Yuri", "Zara", "Amanda", "Beatriz", "Camila", "Diego", "Elias", "Fabio", "Gisele", "Hugo",
            "Irene", "Jessica", "Kleber", "Lorena", "Marcelo", "Nicole", "Orlando", "Patricia", "Quenia", "Renato",
            "Simone", "Teresa", "Ulisses", "Vanessa", "William", "Xande", "Yara", "Zuleica", "Adriana", "Bianca",
            "Claudio", "Debora", "Erick", "Flavia", "Gilberto", "Hilda", "Ivan", "Janaína", "Katia", "Leonardo",
            "Monica", "Nelson", "Olavo", "Priscila", "Queiroz", "Roberto", "Sandra", "Tulio", "Ugo", "Valeria"
        };

        private Random _random = new Random();

        public ObservableCollection<GameItem> ItemsBichoCollection { get; set; } = new ObservableCollection<GameItem>();
        public ObservableCollection<GameItem> ItemsNomesCollection { get; set; } = new ObservableCollection<GameItem>();

        public MiniGamesView()
        {
            InitializeComponent();
            ItemsBicho.ItemsSource = ItemsBichoCollection;
            DrawItemsBicho.ItemsSource = ItemsBichoCollection;
            ItemsNomes.ItemsSource = ItemsNomesCollection;
            DrawItemsNomes.ItemsSource = ItemsNomesCollection;
        }

        // --- Jogo do Bicho ---

        private void BtnGerarBicho_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(TxtQtdBichos.Text, out int qtd) || qtd < 1 || qtd > 50)
            {
                MessageBox.Show("Por favor, insira uma quantidade entre 1 e 50.");
                return;
            }

            ItemsBichoCollection.Clear();
            
            for (int i = 0; i < qtd; i++)
            {
                string nomeAnimal = i < _animais.Count ? _animais[i] : $"Bicho {i + 1}";
                ItemsBichoCollection.Add(new GameItem { Name = $"{i + 1:00} - {nomeAnimal}" });
            }
        }

        private void BtnConfirmarBicho_Click(object sender, RoutedEventArgs e)
        {
            if (ItemsBichoCollection.Count == 0)
            {
                MessageBox.Show("Gere o jogo primeiro!");
                return;
            }
            SetupGrid_Bicho.Visibility = Visibility.Collapsed;
            DrawGrid_Bicho.Visibility = Visibility.Visible;
        }

        private void BtnVoltarBicho_Click(object sender, RoutedEventArgs e)
        {
            SetupGrid_Bicho.Visibility = Visibility.Visible;
            DrawGrid_Bicho.Visibility = Visibility.Collapsed;
        }

        private async void BtnRealizarSorteioBicho_Click(object sender, RoutedEventArgs e)
        {
            BtnRealizarSorteioBicho.IsEnabled = false;
            await RunDrawAnimation(ItemsBichoCollection);
            BtnRealizarSorteioBicho.IsEnabled = true;
        }

        // --- Jogo dos Nomes ---

        private void BtnGerarNomes_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(TxtQtdNomes.Text, out int qtd) || qtd < 1)
            {
                MessageBox.Show("Por favor, insira uma quantidade válida.");
                return;
            }

            if (qtd > _nomesFixos.Count)
            {
                MessageBox.Show($"Quantidade maior que o número de nomes disponíveis ({_nomesFixos.Count}).");
                return;
            }

            ItemsNomesCollection.Clear();

            // Embaralhar e pegar os primeiros N
            var nomesSorteados = _nomesFixos.OrderBy(x => _random.Next()).Take(qtd).ToList();

            foreach (var nome in nomesSorteados)
            {
                ItemsNomesCollection.Add(new GameItem { Name = nome });
            }
        }

        private void BtnAdicionarNome_Click(object sender, RoutedEventArgs e)
        {
            var novoNome = TxtNovoNome.Text.Trim();
            if (!string.IsNullOrEmpty(novoNome))
            {
                if (!_nomesFixos.Contains(novoNome))
                {
                    _nomesFixos.Add(novoNome);
                    MessageBox.Show($"Nome '{novoNome}' adicionado à lista!");
                    TxtNovoNome.Clear();
                }
                else
                {
                    MessageBox.Show("Este nome já está na lista.");
                }
            }
        }

        private void BtnConfirmarNomes_Click(object sender, RoutedEventArgs e)
        {
            if (ItemsNomesCollection.Count == 0)
            {
                MessageBox.Show("Gere o jogo primeiro!");
                return;
            }
            SetupGrid_Nomes.Visibility = Visibility.Collapsed;
            DrawGrid_Nomes.Visibility = Visibility.Visible;
        }

        private void BtnVoltarNomes_Click(object sender, RoutedEventArgs e)
        {
            SetupGrid_Nomes.Visibility = Visibility.Visible;
            DrawGrid_Nomes.Visibility = Visibility.Collapsed;
        }

        private async void BtnRealizarSorteioNomes_Click(object sender, RoutedEventArgs e)
        {
            BtnRealizarSorteioNomes.IsEnabled = false;
            await RunDrawAnimation(ItemsNomesCollection);
            BtnRealizarSorteioNomes.IsEnabled = true;
        }

        private async Task RunDrawAnimation(ObservableCollection<GameItem> items)
        {
            // Reset previous state
            foreach (var item in items)
            {
                item.IsHighlighted = false;
                item.IsWinner = false;
            }

            int totalDuration = 10000; // 10 seconds
            var startTime = DateTime.Now;
            int lastIndex = -1;

            while (true)
            {
                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                if (elapsed >= totalDuration) break;

                double progress = elapsed / totalDuration;
                int delay;

                // Curve: Slow start -> Fast middle -> Slow end
                if (progress < 0.2) // First 20%: 400ms -> 50ms
                {
                    double t = progress / 0.2;
                    delay = (int)(400 - (350 * t)); 
                }
                else if (progress > 0.7) // Last 30%: 50ms -> 600ms
                {
                    double t = (progress - 0.7) / 0.3;
                    delay = (int)(50 + (550 * t));
                }
                else // Middle: 50ms
                {
                    delay = 50;
                }

                // Ensure delay is at least 10ms
                if (delay < 10) delay = 10;

                // Pick random index different from last one
                int r;
                do
                {
                    r = _random.Next(items.Count);
                } while (r == lastIndex && items.Count > 1);
                
                lastIndex = r;

                items[r].IsHighlighted = true;
                await Task.Delay(delay);
                items[r].IsHighlighted = false;
            }

            // Pick Final Winner
            int winnerIndex = _random.Next(items.Count);
            items[winnerIndex].IsWinner = true;
        }
    }

    public class GameItem : INotifyPropertyChanged
    {
        private string _name;
        private string _buyerName;
        private bool _isHighlighted;
        private bool _isWinner;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string BuyerName
        {
            get => _buyerName;
            set { _buyerName = value; OnPropertyChanged(); }
        }

        public bool IsHighlighted
        {
            get => _isHighlighted;
            set { _isHighlighted = value; OnPropertyChanged(); }
        }

        public bool IsWinner
        {
            get => _isWinner;
            set { _isWinner = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
