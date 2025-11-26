using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BingoAdmin.Domain.Entities;
using BingoAdmin.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BingoAdmin.UI.Views
{
    public partial class ResultadosView : UserControl
    {
        private readonly ComboService _comboService;
        private readonly RodadaService _rodadaService;

        // We need a service to get winners across all rounds. 
        // Since we don't have a dedicated "WinnerService", we can use RelatorioService or query context directly via a new method in GameService/RodadaService.
        // Let's assume we add a method to RelatorioService or create a simple query here for now via a new Service method.
        private readonly RelatorioService _relatorioService;

        public ResultadosView()
        {
            InitializeComponent();
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;

            _comboService = ((App)Application.Current).Host.Services.GetRequiredService<ComboService>();
            _rodadaService = ((App)Application.Current).Host.Services.GetRequiredService<RodadaService>();
            _relatorioService = ((App)Application.Current).Host.Services.GetRequiredService<RelatorioService>();

            LoadBingos();
        }

        private void LoadBingos()
        {
            var bingos = _comboService.GetBingos();
            BingoSelector.ItemsSource = bingos;
            if (bingos.Count > 0) BingoSelector.SelectedIndex = 0;
        }

        private void BingoSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BingoSelector.SelectedItem is Bingo bingo)
            {
                LoadResultados(bingo.Id);
            }
        }

        private void BtnAtualizar_Click(object sender, RoutedEventArgs e)
        {
            if (BingoSelector.SelectedItem is Bingo bingo) LoadResultados(bingo.Id);
        }

        private void LoadResultados(int bingoId)
        {
            // We need a DTO for the grid
            var resultados = _relatorioService.GetResultadosGerais(bingoId);
            GridResultados.ItemsSource = resultados;
        }
    }
}
