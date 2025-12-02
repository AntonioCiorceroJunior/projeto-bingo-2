using System;
using System.Windows;
using System.Windows.Controls;
using BingoAdmin.Domain.Entities;
using BingoAdmin.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BingoAdmin.UI.Views
{
    public partial class FinanceiroView : UserControl
    {
        private readonly FinanceiroService _financeiroService;
        private readonly ComboService _comboService;
        private readonly BingoContextService _bingoContext;

        public FinanceiroView()
        {
            InitializeComponent();
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;

            _financeiroService = ((App)Application.Current).Host.Services.GetRequiredService<FinanceiroService>();
            _comboService = ((App)Application.Current).Host.Services.GetRequiredService<ComboService>();
            _bingoContext = ((App)Application.Current).Host.Services.GetRequiredService<BingoContextService>();

            LoadBingos();
            _bingoContext.OnBingoChanged += OnGlobalBingoChanged;
            _bingoContext.OnBingoListUpdated += LoadBingos;
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

            if (bingos.Count > 0) BingoSelector.SelectedIndex = 0;
        }

        private void BingoSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BingoSelector.SelectedItem is Bingo bingo)
            {
                _bingoContext.SetCurrentBingo(bingo.Id);
                TxtValorCombo.Text = _financeiroService.GetValorCombo(bingo.Id).ToString("F2");
                LoadData(bingo.Id);
            }
        }

        private void LoadData(int bingoId)
        {
            GridDespesas.ItemsSource = _financeiroService.GetDespesas(bingoId);
            UpdateTotals(bingoId);
        }

        private void UpdateTotals(int bingoId)
        {
            var (receita, despesas, lucro) = _financeiroService.CalcularTotais(bingoId);
            TxtReceita.Text = receita.ToString("C2");
            TxtDespesas.Text = despesas.ToString("C2");
            TxtLucro.Text = lucro.ToString("C2");
            
            if (lucro >= 0) TxtLucro.Foreground = System.Windows.Media.Brushes.Green;
            else TxtLucro.Foreground = System.Windows.Media.Brushes.Red;
        }

        private void BtnSalvarValor_Click(object sender, RoutedEventArgs e)
        {
            if (BingoSelector.SelectedItem is Bingo bingo && decimal.TryParse(TxtValorCombo.Text, out decimal valor))
            {
                _financeiroService.AtualizarValorCombo(bingo.Id, valor);
                UpdateTotals(bingo.Id);
                MessageBox.Show("Valor atualizado!");
            }
            else
            {
                MessageBox.Show("Valor inválido.");
            }
        }

        private void BtnAddDespesa_Click(object sender, RoutedEventArgs e)
        {
            if (BingoSelector.SelectedItem is Bingo bingo)
            {
                if (string.IsNullOrWhiteSpace(TxtDescricaoDespesa.Text) || !decimal.TryParse(TxtValorDespesa.Text, out decimal valor))
                {
                    MessageBox.Show("Preencha a descrição e um valor válido.");
                    return;
                }

                var despesa = new Despesa
                {
                    BingoId = bingo.Id,
                    Descricao = TxtDescricaoDespesa.Text,
                    Valor = valor,
                    Tipo = (CmbTipoDespesa.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Outros"
                };

                _financeiroService.AdicionarDespesa(despesa);
                
                // Clear inputs
                TxtDescricaoDespesa.Text = "";
                TxtValorDespesa.Text = "0,00";
                
                LoadData(bingo.Id);
            }
        }

        private void BtnExcluirDespesa_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Despesa despesa)
            {
                if (MessageBox.Show($"Excluir despesa '{despesa.Descricao}'?", "Confirmar", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    _financeiroService.RemoverDespesa(despesa.Id);
                    if (BingoSelector.SelectedItem is Bingo bingo) LoadData(bingo.Id);
                }
            }
        }

        private void BtnAtualizar_Click(object sender, RoutedEventArgs e)
        {
            if (BingoSelector.SelectedItem is Bingo bingo) UpdateTotals(bingo.Id);
        }

        private void TxtValor_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = true;
            if (sender is TextBox txt && int.TryParse(e.Text, out int digit))
            {
                string currentText = new string(txt.Text.Where(char.IsDigit).ToArray());
                if (long.TryParse(currentText, out long currentValue))
                {
                    long newValue = currentValue * 10 + digit;
                    txt.Text = (newValue / 100.0).ToString("N2");
                    txt.CaretIndex = txt.Text.Length;
                }
            }
        }

        private void TxtValor_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Back || e.Key == System.Windows.Input.Key.Delete)
            {
                e.Handled = true;
                if (sender is TextBox txt)
                {
                    string currentText = new string(txt.Text.Where(char.IsDigit).ToArray());
                    if (long.TryParse(currentText, out long currentValue))
                    {
                        long newValue = currentValue / 10;
                        txt.Text = (newValue / 100.0).ToString("N2");
                        txt.CaretIndex = txt.Text.Length;
                    }
                }
            }
        }
    }
}
