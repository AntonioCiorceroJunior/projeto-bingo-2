using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BingoAdmin.Domain.Entities;

namespace BingoAdmin.UI.Views
{
    public partial class PremiosConfigWindow : Window
    {
        private readonly RodadaConfigViewModel _viewModel;
        public List<Padrao> PadroesDisponiveis { get; private set; }

        public PremiosConfigWindow(RodadaConfigViewModel viewModel, List<Padrao> padroes)
        {
            InitializeComponent();
            _viewModel = viewModel;
            PadroesDisponiveis = padroes;
            
            CmbModoDisputa.SelectedIndex = _viewModel.ModoDisputaPremios;
            GridPremios.ItemsSource = _viewModel.Premios;
            
            // Set DataContext to allow binding validation
            this.DataContext = this;
        }

        private void AdicionarPremio_Click(object sender, RoutedEventArgs e)
        {
            int nextOrder = _viewModel.Premios.Any() ? _viewModel.Premios.Max(p => p.Ordem) + 1 : 1;
            _viewModel.Premios.Add(new PremioViewModel 
            { 
                Ordem = nextOrder, 
                Descricao = $"Prêmio {nextOrder}",
                Valor = 0
            });
        }

        private void RemoverPremio_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is PremioViewModel premio)
            {
                _viewModel.Premios.Remove(premio);
                // Reorder
                int order = 1;
                foreach(var p in _viewModel.Premios.OrderBy(x => x.Ordem))
                {
                    p.Ordem = order++;
                }
            }
        }

        private void Salvar_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ModoDisputaPremios = CmbModoDisputa.SelectedIndex;
            DialogResult = true;
            Close();
        }
    }
}