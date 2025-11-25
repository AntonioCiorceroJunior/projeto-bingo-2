using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using BingoAdmin.Domain.Entities;
using BingoAdmin.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BingoAdmin.UI.Views
{
    public partial class PadroesView : UserControl
    {
        private readonly PadraoService _padraoService;
        private ToggleButton[] _gridButtons = new ToggleButton[25];

        public PadroesView()
        {
            InitializeComponent();
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;

            _padraoService = ((App)Application.Current).Host.Services.GetRequiredService<PadraoService>();
            
            InitializeGrid();
            LoadPadroes();
        }

        private void InitializeGrid()
        {
            for (int i = 0; i < 25; i++)
            {
                var btn = new ToggleButton
                {
                    Content = "",
                    Margin = new Thickness(1),
                    Background = Brushes.White
                };
                
                // Style for checked state
                var style = new Style(typeof(ToggleButton));
                var trigger = new Trigger { Property = ToggleButton.IsCheckedProperty, Value = true };
                trigger.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Green));
                style.Triggers.Add(trigger);
                btn.Style = style;

                _gridButtons[i] = btn;
                GridEditor.Children.Add(btn);
            }
        }

        private void LoadPadroes()
        {
            _padraoService.SeedPadroesIniciais();
            PadroesList.ItemsSource = _padraoService.GetPadroes();
        }

        private void PadroesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PadroesList.SelectedItem is Padrao padrao)
            {
                NomePadraoBox.Text = padrao.Nome;
                ApplyMascaraToGrid(padrao.Mascara);
                
                BtnExcluir.Visibility = padrao.IsPredefinido ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private void ApplyMascaraToGrid(string mascara)
        {
            if (mascara.Length != 25) return;

            for (int i = 0; i < 25; i++)
            {
                _gridButtons[i].IsChecked = mascara[i] == '1';
            }
        }

        private string GetMascaraFromGrid()
        {
            char[] mascara = new char[25];
            for (int i = 0; i < 25; i++)
            {
                mascara[i] = _gridButtons[i].IsChecked == true ? '1' : '0';
            }
            return new string(mascara);
        }

        private void LimparGrid_Click(object sender, RoutedEventArgs e)
        {
            foreach (var btn in _gridButtons)
            {
                btn.IsChecked = false;
            }
            NomePadraoBox.Text = "";
            PadroesList.SelectedItem = null;
            BtnExcluir.Visibility = Visibility.Collapsed;
        }

        private void SalvarPadrao_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NomePadraoBox.Text))
            {
                MessageBox.Show("Digite um nome para o padrão.");
                return;
            }

            string mascara = GetMascaraFromGrid();
            if (!mascara.Contains('1'))
            {
                MessageBox.Show("Selecione pelo menos uma casa no grid.");
                return;
            }

            try
            {
                _padraoService.SalvarPadrao(NomePadraoBox.Text, mascara);
                MessageBox.Show("Padrão salvo!");
                LoadPadroes();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar: {ex.Message}");
            }
        }

        private void ExcluirPadrao_Click(object sender, RoutedEventArgs e)
        {
            if (PadroesList.SelectedItem is Padrao padrao)
            {
                if (MessageBox.Show($"Tem certeza que deseja excluir '{padrao.Nome}'?", "Confirmar", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _padraoService.ExcluirPadrao(padrao.Id);
                    LoadPadroes();
                    LimparGrid_Click(null, null);
                }
            }
        }
    }
}
