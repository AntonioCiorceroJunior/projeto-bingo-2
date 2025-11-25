using System;
using System.Windows;
using System.Windows.Controls;
using BingoAdmin.Domain.Entities;
using BingoAdmin.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace BingoAdmin.UI.Views
{
    public partial class RelatoriosView : UserControl
    {
        private readonly RelatorioService _relatorioService;
        private readonly ComboService _comboService;

        public RelatoriosView()
        {
            InitializeComponent();
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;

            var services = ((App)Application.Current).Host.Services;
            _relatorioService = services.GetRequiredService<RelatorioService>();
            _comboService = services.GetRequiredService<ComboService>();

            LoadBingos();
        }

        private void LoadBingos()
        {
            BingoSelector.ItemsSource = _comboService.GetBingos();
            if (BingoSelector.Items.Count > 0) BingoSelector.SelectedIndex = 0;
        }

        private void GerarRelatorio_Click(object sender, RoutedEventArgs e)
        {
            if (BingoSelector.SelectedItem is Bingo bingo)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "PDF file (*.pdf)|*.pdf",
                    FileName = $"Relatorio_{bingo.Nome}_{DateTime.Now:yyyyMMdd}.pdf"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        _relatorioService.GerarRelatorioFinal(bingo.Id, saveFileDialog.FileName);
                        MessageBox.Show("Relatório gerado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erro ao gerar relatório: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Selecione um bingo.");
            }
        }
    }
}
