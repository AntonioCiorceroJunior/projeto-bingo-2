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
        private readonly RelatorioService _relatorioService;
        private readonly BingoContextService _bingoContext;

        public ResultadosView()
        {
            InitializeComponent();
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;

            _comboService = ((App)Application.Current).Host.Services.GetRequiredService<ComboService>();
            _rodadaService = ((App)Application.Current).Host.Services.GetRequiredService<RodadaService>();
            _relatorioService = ((App)Application.Current).Host.Services.GetRequiredService<RelatorioService>();
            _bingoContext = ((App)Application.Current).Host.Services.GetRequiredService<BingoContextService>();

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

            if (bingos.Count > 0) BingoSelector.SelectedIndex = 0;
        }

        private void BingoSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BingoSelector.SelectedItem is Bingo bingo)
            {
                _bingoContext.SetCurrentBingo(bingo.Id);
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
