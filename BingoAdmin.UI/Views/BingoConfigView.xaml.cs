using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
        private readonly PadraoService _padraoService = null!;
        private int? _bingoEmEdicaoId = null;

        public ObservableCollection<RodadaConfigViewModel> RodadasConfig { get; set; } = new ObservableCollection<RodadaConfigViewModel>();

        public BingoConfigView()
        {
            InitializeComponent();
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;

            _bingoManagementService = ((App)Application.Current).Host.Services.GetRequiredService<BingoManagementService>();
            _padraoService = ((App)Application.Current).Host.Services.GetRequiredService<PadraoService>();
            
            GridRodadasConfig.ItemsSource = RodadasConfig;
            InitializeRoundsCombo();
            
            CarregarBingos();
            CarregarPadroes();
        }

        private void InitializeRoundsCombo()
        {
            CmbQtdRodadas.Items.Clear();
            for (int i = 1; i <= 30; i++)
            {
                CmbQtdRodadas.Items.Add(i);
            }
            // Default to manual mode
            ChkRodadasPersonalizado.IsChecked = true;
            TxtQtdRodadasManual.Text = "10";
        }

        private void CarregarPadroes()
        {
        }

        private void CmbQtdRodadas_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChkRodadasPersonalizado.IsChecked == false && CmbQtdRodadas.SelectedItem is int qtd)
            {
                UpdateRodadasConfig(qtd);
            }
        }

        private void ChkRodadasPersonalizado_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (ChkRodadasPersonalizado.IsChecked == true)
            {
                CmbQtdRodadas.Visibility = Visibility.Collapsed;
                TxtQtdRodadasManual.Visibility = Visibility.Visible;
                UpdateRodadasConfigFromManual();
            }
            else
            {
                CmbQtdRodadas.Visibility = Visibility.Visible;
                TxtQtdRodadasManual.Visibility = Visibility.Collapsed;
                if (CmbQtdRodadas.SelectedItem is int qtd)
                {
                    UpdateRodadasConfig(qtd);
                }
            }
        }

        private void TxtQtdRodadasManual_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ChkRodadasPersonalizado.IsChecked == true)
            {
                UpdateRodadasConfigFromManual();
            }
        }

        private void UpdateRodadasConfigFromManual()
        {
            if (int.TryParse(TxtQtdRodadasManual.Text, out int qtd) && qtd > 0 && qtd <= 100) // Limit 100 for safety
            {
                UpdateRodadasConfig(qtd);
            }
        }

        private void UpdateRodadasConfig(int qtd)
        {
            // Preserve existing configs if possible
            var existing = RodadasConfig.ToList();
            RodadasConfig.Clear();

            for (int i = 1; i <= qtd; i++)
            {
                var existingItem = existing.FirstOrDefault(x => x.Numero == i);
                if (existingItem != null)
                {
                    RodadasConfig.Add(existingItem);
                }
                else
                {
                    RodadasConfig.Add(new RodadaConfigViewModel 
                    { 
                        Numero = i, 
                        Descricao = "", 
                        ModoDinamico = false 
                    });
                }
            }
        }

        private async void CarregarBingos(int? selectedId = null)
        {
            try
            {
                var bingos = await _bingoManagementService.ListarBingosAsync();
                BingosGrid.ItemsSource = bingos;

                if (selectedId.HasValue)
                {
                    var selected = bingos.FirstOrDefault(b => b.Id == selectedId.Value);
                    if (selected != null)
                    {
                        BingosGrid.SelectedItem = selected;
                        BingosGrid.ScrollIntoView(selected);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar bingos: {ex.Message}");
            }
        }

        private async void GerarCombos_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarCampos(out int qtdCombos, out int cartelasPorCombo)) return;

            try
            {
                BtnGerar.IsEnabled = false;
                ProgressPanel.Visibility = Visibility.Visible;

                var progress = new Progress<string>(status =>
                {
                    StatusText.Text = status;
                });

                bool modoDinamicoGlobal = false; // Global mode removed
                List<int> padroesIds = new List<int>();

                // Convert ViewModel to DTO
                var rodadasDto = RodadasConfig.Select(r => new RodadaConfigDto
                {
                    Numero = r.Numero,
                    Descricao = r.Descricao,
                    ModoDinamico = r.ModoDinamico,
                    PadroesIds = r.PadroesIds
                }).ToList();

                int newBingoId = await _bingoManagementService.CriarBingoAsync(
                    NomeBingoBox.Text, 
                    DataBingoPicker.SelectedDate.Value, 
                    qtdCombos, 
                    cartelasPorCombo,
                    rodadasDto,
                    modoDinamicoGlobal,
                    padroesIds,
                    progress
                );

                MessageBox.Show("Bingo criado com sucesso!");
                LimparCampos();
                CarregarBingos(newBingoId); // Auto-select new bingo
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
            // Note: Update logic for rounds is complex, keeping simple for now or disabling round count update
            // For now, we just validate basic fields.
            int qtdRodadas = RodadasConfig.Count;

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
                CarregarBingos(_bingoEmEdicaoId);
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
                
                // Set rounds
                CmbQtdRodadas.SelectedItem = bingo.QuantidadeRodadas;
                if (bingo.QuantidadeRodadas > 30)
                {
                    ChkRodadasPersonalizado.IsChecked = true;
                    TxtQtdRodadasManual.Text = bingo.QuantidadeRodadas.ToString();
                }
                else
                {
                    ChkRodadasPersonalizado.IsChecked = false;
                    CmbQtdRodadas.SelectedItem = bingo.QuantidadeRodadas;
                }

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

        private bool ValidarCampos(out int qtdCombos, out int cartelasPorCombo)
        {
            qtdCombos = 0;
            cartelasPorCombo = 0;

            if (string.IsNullOrWhiteSpace(NomeBingoBox.Text) || 
                string.IsNullOrWhiteSpace(QtdCombosBox.Text) || 
                string.IsNullOrWhiteSpace(CartelasPorComboBox.Text))
            {
                MessageBox.Show("Preencha todos os campos.");
                return false;
            }

            if (!int.TryParse(QtdCombosBox.Text, out qtdCombos) || 
                !int.TryParse(CartelasPorComboBox.Text, out cartelasPorCombo))
            {
                MessageBox.Show("Quantidade de combos e cartelas devem ser números.");
                return false;
            }

            if (RodadasConfig.Count == 0)
            {
                MessageBox.Show("Defina a quantidade de rodadas.");
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
            DataBingoPicker.SelectedDate = null;
            
            // Reset rounds to default
            ChkRodadasPersonalizado.IsChecked = false;
            CmbQtdRodadas.SelectedIndex = 9; // 10 rounds

            QtdCombosBox.IsEnabled = true;
            CartelasPorComboBox.IsEnabled = true;

            BtnGerar.Visibility = Visibility.Visible;
            BtnAtualizar.Visibility = Visibility.Collapsed;
            BtnCancelar.Visibility = Visibility.Collapsed;
        }

        private void ConfigurarPadroesRodada_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.DataContext is RodadaConfigViewModel rodada)
                {
                    var padroes = _padraoService.ListarTodos();
                    
                    // Pass dynamic mode flag to window
                    var window = new SelecionarPadroesWindow(padroes, rodada.PadroesIds, rodada.ModoDinamico);
                    
                    if (window.ShowDialog() == true)
                    {
                        rodada.PadroesIds = window.SelectedPadroesIds;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao abrir configuração de padrões: {ex.Message}");
            }
        }
    }
}
