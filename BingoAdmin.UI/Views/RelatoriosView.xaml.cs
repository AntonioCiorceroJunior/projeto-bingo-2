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
        private readonly BingoContextService _bingoContext;

        public RelatoriosView()
        {
            InitializeComponent();
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;

            var services = ((App)Application.Current).Host.Services;
            _relatorioService = services.GetRequiredService<RelatorioService>();
            _comboService = services.GetRequiredService<ComboService>();
            _bingoContext = services.GetRequiredService<BingoContextService>();

            LoadBingos();
            _bingoContext.OnBingoChanged += OnGlobalBingoChanged;
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

            if (BingoSelector.Items.Count > 0) BingoSelector.SelectedIndex = 0;
        }

        private void BingoSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BingoSelector.SelectedItem is Bingo selectedBingo)
            {
                _bingoContext.SetCurrentBingo(selectedBingo.Id);
            }
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
