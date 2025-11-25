using System;
using System.Windows;
using System.Windows.Controls;
using BingoAdmin.Domain.Entities;
using BingoAdmin.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BingoAdmin.UI.Views
{
    public partial class RodadasConfigView : UserControl
    {
        private readonly RodadaService _rodadaService;
        private readonly ComboService _comboService; // Reusing to get Bingos
        private readonly PadraoService _padraoService;

        public RodadasConfigView()
        {
            InitializeComponent();
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;

            var services = ((App)Application.Current).Host.Services;
            _rodadaService = services.GetRequiredService<RodadaService>();
            _comboService = services.GetRequiredService<ComboService>();
            _padraoService = services.GetRequiredService<PadraoService>();

            LoadBingos();
            LoadPadroes();
        }

        private void LoadBingos()
        {
            BingoSelector.ItemsSource = _comboService.GetBingos();
            if (BingoSelector.Items.Count > 0) BingoSelector.SelectedIndex = 0;
        }

        private void LoadPadroes()
        {
            var padroes = _padraoService.GetPadroes();
            // Acessa a coluna pelo índice ou nome se o x:Name falhar
            if (RodadasGrid.Columns.Count > 3 && RodadasGrid.Columns[3] is DataGridComboBoxColumn comboCol)
            {
                comboCol.ItemsSource = padroes;
            }
        }

        private void BingoSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadRodadas();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadBingos();
        }

        private void LoadRodadas()
        {
            if (BingoSelector.SelectedItem is Bingo selectedBingo)
            {
                RodadasGrid.ItemsSource = _rodadaService.GetRodadas(selectedBingo.Id);
            }
            else
            {
                RodadasGrid.ItemsSource = null;
            }
        }

        private void RodadasGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            // Opcional: Salvar automaticamente ao terminar a edição da linha
            // Mas como o usuário pode querer editar várias e salvar depois, o botão "Salvar Alterações" é mais seguro.
            // Porém, para garantir que o objeto bound seja atualizado, o DataGrid faz isso por padrão.
        }

        private void SalvarTudo_Click(object sender, RoutedEventArgs e)
        {
            if (RodadasGrid.ItemsSource is System.Collections.IEnumerable items)
            {
                try
                {
                    foreach (var item in items)
                    {
                        if (item is Rodada rodada)
                        {
                            _rodadaService.AtualizarRodada(rodada);
                        }
                    }
                    MessageBox.Show("Todas as alterações foram salvas!");
                    LoadRodadas(); // Recarrega para garantir consistência
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao salvar: {ex.Message}");
                }
            }
        }
    }
}
