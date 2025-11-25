using System.Collections.Generic;
using System.Windows;
using BingoAdmin.Domain.Entities;

namespace BingoAdmin.UI.Views
{
    public partial class SelecionarPadraoWindow : Window
    {
        public Padrao? SelectedPadrao { get; private set; }

        public SelecionarPadraoWindow(List<Padrao> padroes)
        {
            InitializeComponent();
            CmbPadroes.ItemsSource = padroes;
            if (padroes.Count > 0) CmbPadroes.SelectedIndex = 0;
        }

        private void BtnConfirmar_Click(object sender, RoutedEventArgs e)
        {
            if (CmbPadroes.SelectedItem is Padrao padrao)
            {
                SelectedPadrao = padrao;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Selecione um padrão.");
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
