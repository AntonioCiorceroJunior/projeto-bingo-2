using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BingoAdmin.UI.Services;

namespace BingoAdmin.UI.Views
{
    public partial class ConferenciaCartelaWindow : Window
    {
        public ConferenciaCartelaWindow(CachedCartela cartela, HashSet<int> numerosSorteados, string mascaraPadrao, string nomePadrao = "")
        {
            InitializeComponent();

            TxtTitulo.Text = $"CARTELA {cartela.NumeroCartela}";
            TxtDetalhes.Text = $"{cartela.Dono} | Combo {cartela.ComboNumero}";

            if (!string.IsNullOrEmpty(nomePadrao))
            {
                TxtPadrao.Text = $"Padrão Vencedor: {nomePadrao}";
                TxtPadrao.Visibility = Visibility.Visible;
            }

            PopulateGrid(cartela.Numeros, numerosSorteados, mascaraPadrao);
        }

        private void PopulateGrid(int[] numeros, HashSet<int> sorteados, string mascara)
        {
            // ...existing code...
            
            for (int i = 0; i < 25; i++)
            {
                int numero = numeros[i];
                int row = (i / 5) + 1; // +1 because row 0 is header
                int col = i % 5;
                
                bool fazParteDoPadrao;
                if (mascara.StartsWith("RANDOM:"))
                {
                    fazParteDoPadrao = true;
                }
                else
                {
                    fazParteDoPadrao = mascara.Length > i && mascara[i] == '1';
                }

                var border = new Border
                {
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(2),
                    CornerRadius = new CornerRadius(3)
                };

                var textBlock = new TextBlock
                {
                    Text = numero == 0 ? "FREE" : numero.ToString(),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 18,
                    FontWeight = FontWeights.Bold
                };

                if (numero == 0)
                {
                    // FREE space
                    border.Background = new SolidColorBrush(Color.FromRgb(245, 245, 220)); // Beige
                    textBlock.Foreground = Brushes.Black;
                }
                else if (sorteados.Contains(numero))
                {
                    if (fazParteDoPadrao)
                    {
                        // Sorteado e faz parte do padrão -> VERDE
                        border.Background = new SolidColorBrush(Color.FromRgb(46, 204, 113)); // Green
                        textBlock.Foreground = Brushes.White;
                    }
                    else
                    {
                        // Sorteado mas não faz parte do padrão -> VERMELHO
                        border.Background = new SolidColorBrush(Color.FromRgb(255, 68, 68)); // Red
                        textBlock.Foreground = Brushes.White;
                    }
                }
                else
                {
                    border.Background = Brushes.White;
                    textBlock.Foreground = Brushes.Black;
                }

                border.Child = textBlock;

                Grid.SetRow(border, row);
                Grid.SetColumn(border, col);
                GridNumeros.Children.Add(border);
            }
        }
    }
}
