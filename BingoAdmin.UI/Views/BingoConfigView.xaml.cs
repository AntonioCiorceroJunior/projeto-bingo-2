using System;
using System.Windows;
using System.Windows.Controls;
using BingoAdmin.Domain.Entities;
using BingoAdmin.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BingoAdmin.UI.Views
{
    public partial class BingoConfigView : UserControl
    {
        private readonly BingoManagementService _bingoManagementService = null!;
        private int? _bingoEmEdicaoId = null;

        public BingoConfigView()
        {
            InitializeComponent();
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;

            _bingoManagementService = ((App)Application.Current).Host.Services.GetRequiredService<BingoManagementService>();
            CarregarBingos();
        }

        private async void CarregarBingos()
        {
            try
            {
                var bingos = await _bingoManagementService.ListarBingosAsync();
                BingosGrid.ItemsSource = bingos;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar bingos: {ex.Message}");
            }
        }

        private async void GerarCombos_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarCampos(out int qtdCombos, out int cartelasPorCombo, out int qtdRodadas)) return;

            try
            {
                BtnGerar.IsEnabled = false;
                ProgressPanel.Visibility = Visibility.Visible;

                var progress = new Progress<string>(status =>
                {
                    StatusText.Text = status;
                });

                await _bingoManagementService.CriarBingoAsync(
                    NomeBingoBox.Text, 
                    DataBingoPicker.SelectedDate.Value, 
                    qtdCombos, 
                    cartelasPorCombo,
                    qtdRodadas,
                    progress
                );

                MessageBox.Show("Bingo criado com sucesso!");
                LimparCampos();
                CarregarBingos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao criar bingo: {ex.Message}");
            }
            finally
            {
                BtnGerar.IsEnabled = true;
                ProgressPanel.Visibility = Visibility.Collapsed;
            }
        }

        private async void Atualizar_Click(object sender, RoutedEventArgs e)
        {
            if (_bingoEmEdicaoId == null) return;
            if (!ValidarCampos(out int qtdCombos, out int cartelasPorCombo, out int qtdRodadas)) return;

            try
            {
                await _bingoManagementService.AtualizarBingoAsync(
                    _bingoEmEdicaoId.Value,
                    NomeBingoBox.Text,
                    DataBingoPicker.SelectedDate.Value,
                    qtdRodadas
                );

                MessageBox.Show("Bingo atualizado com sucesso!");
                LimparCampos();
                CarregarBingos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao atualizar bingo: {ex.Message}");
            }
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            LimparCampos();
        }

        private void EditarBingo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Bingo bingo)
            {
                _bingoEmEdicaoId = bingo.Id;
                NomeBingoBox.Text = bingo.Nome;
                DataBingoPicker.SelectedDate = bingo.DataInicioPrevista;
                QtdCombosBox.Text = bingo.QuantidadeCombos.ToString();
                CartelasPorComboBox.Text = bingo.CartelasPorCombo.ToString();
                QtdRodadasBox.Text = bingo.QuantidadeRodadas.ToString();

                // Bloquear campos que não podem ser editados facilmente após criação (por enquanto)
                QtdCombosBox.IsEnabled = false;
                CartelasPorComboBox.IsEnabled = false;

                BtnGerar.Visibility = Visibility.Collapsed;
                BtnAtualizar.Visibility = Visibility.Visible;
                BtnCancelar.Visibility = Visibility.Visible;
            }
        }

        private async void ExcluirBingo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Bingo bingo)
            {
                if (MessageBox.Show($"Tem certeza que deseja excluir o bingo '{bingo.Nome}'?", "Confirmar Exclusão", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _bingoManagementService.ExcluirBingoAsync(bingo.Id);
                        CarregarBingos();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erro ao excluir bingo: {ex.Message}");
                    }
                }
            }
        }

        private bool ValidarCampos(out int qtdCombos, out int cartelasPorCombo, out int qtdRodadas)
        {
            qtdCombos = 0;
            cartelasPorCombo = 0;
            qtdRodadas = 0;

            if (string.IsNullOrWhiteSpace(NomeBingoBox.Text) || 
                string.IsNullOrWhiteSpace(QtdCombosBox.Text) || 
                string.IsNullOrWhiteSpace(CartelasPorComboBox.Text) ||
                string.IsNullOrWhiteSpace(QtdRodadasBox.Text))
            {
                MessageBox.Show("Preencha todos os campos.");
                return false;
            }

            if (!int.TryParse(QtdCombosBox.Text, out qtdCombos) || 
                !int.TryParse(CartelasPorComboBox.Text, out cartelasPorCombo) ||
                !int.TryParse(QtdRodadasBox.Text, out qtdRodadas))
            {
                MessageBox.Show("Quantidade de combos, cartelas e rodadas devem ser números.");
                return false;
            }

            if (DataBingoPicker.SelectedDate == null)
            {
                MessageBox.Show("Selecione uma data.");
                return false;
            }

            return true;
        }

        private void LimparCampos()
        {
            _bingoEmEdicaoId = null;
            NomeBingoBox.Text = "";
            QtdCombosBox.Text = "";
            CartelasPorComboBox.Text = "";
            QtdRodadasBox.Text = "";
            DataBingoPicker.SelectedDate = null;

            QtdCombosBox.IsEnabled = true;
            CartelasPorComboBox.IsEnabled = true;

            BtnGerar.Visibility = Visibility.Visible;
            BtnAtualizar.Visibility = Visibility.Collapsed;
            BtnCancelar.Visibility = Visibility.Collapsed;
        }
    }
}
