using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BingoAdmin.Domain.Entities;

namespace BingoAdmin.UI.Views
{
    public partial class FlashboardWindow : Window, INotifyPropertyChanged
    {
        public ObservableCollection<BoardNumber> ColumnB { get; set; } = new ObservableCollection<BoardNumber>();
        public ObservableCollection<BoardNumber> ColumnI { get; set; } = new ObservableCollection<BoardNumber>();
        public ObservableCollection<BoardNumber> ColumnN { get; set; } = new ObservableCollection<BoardNumber>();
        public ObservableCollection<BoardNumber> ColumnG { get; set; } = new ObservableCollection<BoardNumber>();
        public ObservableCollection<BoardNumber> ColumnO { get; set; } = new ObservableCollection<BoardNumber>();

        public event PropertyChangedEventHandler? PropertyChanged;

        public FlashboardWindow()
        {
            InitializeComponent();
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

        public void SetPattern(Padrao? padrao)
        {
            PatternGrid.Children.Clear();
            if (padrao == null)
            {
                TxtPatternName.Text = "Nenhum / Livre";
                // Create empty grid
                for (int i = 0; i < 25; i++)
                {
                    var border = new Border
                    {
                        Background = Brushes.DarkGray,
                        Margin = new Thickness(1),
                        CornerRadius = new CornerRadius(2)
                    };
                    PatternGrid.Children.Add(border);
                }
                return;
            }

            TxtPatternName.Text = padrao.Nome;
            bool[] matrix = new bool[25];
            if (!string.IsNullOrEmpty(padrao.Mascara) && padrao.Mascara.Length == 25)
            {
                for (int i = 0; i < 25; i++)
                {
                    matrix[i] = padrao.Mascara[i] == '1';
                }
            }

            for (int i = 0; i < 25; i++)
            {
                var border = new Border
                {
                    Margin = new Thickness(1),
                    CornerRadius = new CornerRadius(2),
                    Background = matrix[i] ? Brushes.Red : Brushes.DarkGray
                };
                PatternGrid.Children.Add(border);
            }
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
}
