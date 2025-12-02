using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BingoAdmin.Domain.Entities;
using BingoAdmin.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BingoAdmin.UI.Views
{
    public partial class FlashboardWindow : Window, INotifyPropertyChanged
    {
        public ObservableCollection<BoardNumber> ColumnB { get; set; } = new ObservableCollection<BoardNumber>();
        public ObservableCollection<BoardNumber> ColumnI { get; set; } = new ObservableCollection<BoardNumber>();
        public ObservableCollection<BoardNumber> ColumnN { get; set; } = new ObservableCollection<BoardNumber>();
        public ObservableCollection<BoardNumber> ColumnG { get; set; } = new ObservableCollection<BoardNumber>();
        public ObservableCollection<BoardNumber> ColumnO { get; set; } = new ObservableCollection<BoardNumber>();

        private readonly FeedService _feedService;
        private readonly GameStatusService _gameStatusService;

        public ObservableCollection<FeedMessage> FeedMessages => _feedService.Messages;
        public GameStatusService GameStatus => _gameStatusService;
        public ObservableCollection<PatternDisplayViewModel> ActivePatterns { get; set; } = new ObservableCollection<PatternDisplayViewModel>();

        public event PropertyChangedEventHandler? PropertyChanged;

        public FlashboardWindow()
        {
            InitializeComponent();
            
            if (Application.Current is App app)
            {
                _feedService = app.Host.Services.GetRequiredService<FeedService>();
                _gameStatusService = app.Host.Services.GetRequiredService<GameStatusService>();
            }

            DataContext = this;
            InitializeBoard();
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

        public void UpdateNumber(int number)
        {
            BoardNumber? item = null;
            if (number <= 15) item = ColumnB.FirstOrDefault(b => b.Numero == number);
            else if (number <= 30) item = ColumnI.FirstOrDefault(b => b.Numero == number);
            else if (number <= 45) item = ColumnN.FirstOrDefault(b => b.Numero == number);
            else if (number <= 60) item = ColumnG.FirstOrDefault(b => b.Numero == number);
            else item = ColumnO.FirstOrDefault(b => b.Numero == number);

            if (item != null)
            {
                item.IsDrawn = true;
            }

            // Update Big Display
            string letter = GetLetter(number);
            TxtCurrentNumber.Text = $"{letter}-{number}";
        }

        public void ResetBoard()
        {
            InitializeBoard();
            TxtCurrentNumber.Text = "--";
        }

        public void SetPatterns(List<Padrao> padroes)
        {
            ActivePatterns.Clear();

            if (padroes == null || !padroes.Any())
            {
                // Add a "None" placeholder if desired, or just leave empty
                // For now, let's add a blank one to keep the UI consistent if that's what was there before
                ActivePatterns.Add(new PatternDisplayViewModel 
                { 
                    Name = "Nenhum / Livre", 
                    GridCells = Enumerable.Repeat((Brush)Brushes.DarkGray, 25).ToList() 
                });
                return;
            }

            foreach (var padrao in padroes)
            {
                var cells = new List<Brush>();
                if (!string.IsNullOrEmpty(padrao.Mascara) && padrao.Mascara.Length == 25)
                {
                    for (int i = 0; i < 25; i++)
                    {
                        cells.Add(padrao.Mascara[i] == '1' ? Brushes.Red : Brushes.DarkGray);
                    }
                }
                else
                {
                    cells.AddRange(Enumerable.Repeat(Brushes.DarkGray, 25));
                }

                ActivePatterns.Add(new PatternDisplayViewModel
                {
                    Name = padrao.Nome,
                    GridCells = cells
                });
            }
        }

        // Deprecated/Compatibility wrapper
        public void SetPattern(Padrao? padrao)
        {
            if (padrao == null) SetPatterns(new List<Padrao>());
            else SetPatterns(new List<Padrao> { padrao });
        }

        private string GetLetter(int n)
        {
            if (n <= 15) return "B";
            if (n <= 30) return "I";
            if (n <= 45) return "N";
            if (n <= 60) return "G";
            return "O";
        }
    }

    public class PatternDisplayViewModel
    {
        public string Name { get; set; } = string.Empty;
        public List<Brush> GridCells { get; set; } = new List<Brush>();
    }
}
