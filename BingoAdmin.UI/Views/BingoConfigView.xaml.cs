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
        private readonly BingoContextService _bingoContext = null!;
        private int? _bingoEmEdicaoId = null;

        public ObservableCollection<RodadaConfigViewModel> RodadasConfig { get; set; } = new ObservableCollection<RodadaConfigViewModel>();

        public BingoConfigView()
        {
            InitializeComponent();
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;

            _bingoManagementService = ((App)Application.Current).Host.Services.GetRequiredService<BingoManagementService>();
            _padraoService = ((App)Application.Current).Host.Services.GetRequiredService<PadraoService>();
            _bingoContext = ((App)Application.Current).Host.Services.GetRequiredService<BingoContextService>();
            
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

        private void UpdateCombosVisibility()
        {
            if (PnlKitsPorCombo == null || LblQtdCombos == null) return;

            if (ChkUsarCombos.IsChecked == true)
            {
                PnlKitsPorCombo.Visibility = Visibility.Visible;
                LblQtdCombos.Text = "Quantidade de Combos";
            }
            else
            {
                PnlKitsPorCombo.Visibility = Visibility.Collapsed;
                LblQtdCombos.Text = "Quantidade de Kits";
            }
        }

        private void ChkUsarCombos_CheckedChanged(object sender, RoutedEventArgs e)
        {
            UpdateCombosVisibility();
        }

        private void TglModoJogo_Click(object sender, RoutedEventArgs e)
        {
             // Safety check for initialization
            if (TglModoJogo == null || ChkRodadasPersonalizado == null || TxtQtdRodadasManual == null || CmbQtdRodadas == null) return;

            bool isAcumulado = TglModoJogo.IsChecked == true;

            if (isAcumulado)
            {
                TglModoJogo.Content = "Prêmios Acumulados";
                // Force 1 round
                ChkRodadasPersonalizado.IsChecked = true; 
                TxtQtdRodadasManual.Text = "1";
                
                // Disable controls
                TxtQtdRodadasManual.IsEnabled = false;
                CmbQtdRodadas.IsEnabled = false;
                ChkRodadasPersonalizado.IsEnabled = false;
                
                if (RodadasConfig.Any())
                {
                    RodadasConfig[0].Descricao = "Rodada Única - Acumulado";
                    RodadasConfig[0].MaximoGanhadores = 0; // Config default logic needed elsewhere, but user wants max winners limit
                    RodadasConfig[0].ModoDinamico = true; // Acumulado usually implies dynamic patterns
                }
            }
            else
            {
                TglModoJogo.Content = "Bingo por Rodadas (Clássico)";
                // Restore
                if (TxtQtdRodadasManual != null) TxtQtdRodadasManual.IsEnabled = true;
                if (CmbQtdRodadas != null) CmbQtdRodadas.IsEnabled = true;
                if (ChkRodadasPersonalizado != null) ChkRodadasPersonalizado.IsEnabled = true;
            }
        }

        private async void GerarCombos_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarCampos(out int qtdCombos, out int kitsPorCombo, out int cartelasPorKit)) return;

            try
            {
                BtnGerar.IsEnabled = false;
                ProgressPanel.Visibility = Visibility.Visible;

                var progress = new Progress<string>(status =>
                {
                    StatusText.Text = status;
                });

                bool modoDinamicoGlobal = false;
                List<int> padroesIds = new List<int>();
                int modoJogo = (TglModoJogo.IsChecked == true) ? 1 : 0;

                // Convert ViewModel to DTO
                var rodadasDto = RodadasConfig.Select(r => new RodadaConfigDto
                {
                    Numero = r.Numero,
                    Descricao = r.Descricao,
                    TipoPremio = r.TipoPremio,
                    ModoDinamico = r.ModoDinamico,
                    PadroesIds = r.PadroesIds,
                    MaximoGanhadores = r.MaximoGanhadores,
                    TipoJogo = r.TipoJogo,
                    ModoDisputaPremios = r.ModoDisputaPremios,
                    Premios = r.Premios.Select(p => new PremioDto 
                    { 
                        Descricao = p.Descricao, 
                        Ordem = p.Ordem, 
                        Valor = p.Valor,
                        PadraoId = p.PadraoId
                    }).ToList()
                }).ToList();

                int newBingoId = await _bingoManagementService.CriarBingoAsync(
                    NomeBingoBox.Text, 
                    DataBingoPicker.SelectedDate.Value, 
                    qtdCombos, 
                    kitsPorCombo,
                    cartelasPorKit,
                    ChkUsarCombos.IsChecked ?? false,
                    rodadasDto,
                    modoDinamicoGlobal,
                    padroesIds,
                    modoJogo,
                    progress
                );

                MessageBox.Show("Bingo criado com sucesso!");
                LimparCampos();
                CarregarBingos(newBingoId); // Auto-select new bingo
                _bingoContext.SetCurrentBingo(newBingoId); // Set as global current bingo
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
            
            int qtdRodadas = RodadasConfig.Count;
            int modoJogo = (TglModoJogo.IsChecked == true) ? 1 : 0;

            try
            {
                var rodadasDto = RodadasConfig.Select(r => new RodadaConfigDto
                {
                    Numero = r.Numero,
                    Descricao = r.Descricao,
                    TipoPremio = r.TipoPremio,
                    ModoDinamico = r.ModoDinamico,
                    PadroesIds = r.PadroesIds,
                    MaximoGanhadores = r.MaximoGanhadores,
                    TipoJogo = r.TipoJogo,
                    ModoDisputaPremios = r.ModoDisputaPremios,
                    Premios = r.Premios.Select(p => new PremioDto 
                    { 
                        Descricao = p.Descricao, 
                        Ordem = p.Ordem, 
                        Valor = p.Valor,
                        PadraoId = p.PadraoId
                    }).ToList()
                }).ToList();

                await _bingoManagementService.AtualizarBingoAsync(
                    _bingoEmEdicaoId.Value,
                    NomeBingoBox.Text,
                    DataBingoPicker.SelectedDate.Value,
                    qtdRodadas,
                    rodadasDto,
                    modoJogo
                );

                MessageBox.Show("Bingo atualizado com sucesso!");
                LimparCampos();
                CarregarBingos(_bingoEmEdicaoId);
            }
            catch (Exception ex)
            {
                 var inner = ex.InnerException?.Message ?? "N/A";
                MessageBox.Show($"Erro ao atualizar bingo: {ex.Message}\nDetalhes: {inner}");
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
                TglModoJogo.IsChecked = (bingo.ModoJogo == 1);
                TglModoJogo_Click(TglModoJogo, null);
                
                ChkUsarCombos.IsChecked = bingo.TemCombos;
                UpdateCombosVisibility();

                QtdCombosBox.Text = bingo.QuantidadeCombos.ToString();
                KitsPorComboBox.Text = bingo.KitsPorCombo.ToString();
                CartelasPorKitBox.Text = bingo.CartelasPorKit.ToString();
                
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

                // Populate RodadasConfig from existing rounds
                RodadasConfig.Clear();
                foreach (var rodada in bingo.Rodadas.OrderBy(r => r.NumeroOrdem))
                {
                    var vm = new RodadaConfigViewModel
                    {
                        Numero = rodada.NumeroOrdem,
                        Descricao = rodada.Descricao,
                        TipoPremio = rodada.TipoPremio,
                        ModoDinamico = rodada.ModoPadroesDinamicos,
                        MaximoGanhadores = rodada.MaximoGanhadores,
                        TipoJogo = rodada.TipoJogo,
                        PadroesIds = rodada.RodadaPadroes.Select(rp => rp.PadraoId).ToList(),
                        ModoDisputaPremios = rodada.ModoDisputaPremios
                    };

                    if (rodada.Premios != null)
                    {
                        foreach (var p in rodada.Premios.OrderBy(x => x.Ordem))
                        {
                            vm.Premios.Add(new PremioViewModel
                            {
                                Descricao = p.Descricao,
                                Ordem = p.Ordem,
                                Valor = p.Valor,
                                PadraoId = p.PadraoId
                            });
                        }
                    }

                    RodadasConfig.Add(vm);
                }

                // Bloquear campos que não podem ser editados facilmente após criação (por enquanto)
                QtdCombosBox.IsEnabled = false;
                KitsPorComboBox.IsEnabled = false;
                CartelasPorKitBox.IsEnabled = false;

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

        private bool ValidarCampos(out int qtdCombos, out int kitsPorCombo, out int cartelasPorKit)
        {
            qtdCombos = 0;
            kitsPorCombo = 0;
            cartelasPorKit = 0;

            if (string.IsNullOrWhiteSpace(NomeBingoBox.Text) || 
                string.IsNullOrWhiteSpace(QtdCombosBox.Text) || 
                string.IsNullOrWhiteSpace(CartelasPorKitBox.Text))
            {
                MessageBox.Show("Preencha todos os campos obrigatórios.");
                return false;
            }

            if (ChkUsarCombos.IsChecked == true && string.IsNullOrWhiteSpace(KitsPorComboBox.Text))
            {
                MessageBox.Show("Informe a quantidade de kits por combo.");
                return false;
            }

            if (!int.TryParse(QtdCombosBox.Text, out qtdCombos) || qtdCombos <= 0)
            {
                MessageBox.Show("Quantidade principal deve ser um número positivo.", "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!int.TryParse(CartelasPorKitBox.Text, out cartelasPorKit) || cartelasPorKit <= 0)
            {
                 MessageBox.Show("Cartelas por Kit deve ser um número positivo.", "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                 return false;
            }

            if (ChkUsarCombos.IsChecked == true)
            {
                if (!int.TryParse(KitsPorComboBox.Text, out kitsPorCombo) || kitsPorCombo <= 0)
                {
                    MessageBox.Show("Kits por Combo deve ser um número positivo.", "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }
            else
            {
                kitsPorCombo = 1;
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
            TglModoJogo.IsChecked = false;
            TglModoJogo_Click(TglModoJogo, null);
            
            QtdCombosBox.Text = "";
            KitsPorComboBox.Text = "";
            CartelasPorKitBox.Text = "";
            DataBingoPicker.SelectedDate = null;
            
            // Reset rounds to default
            ChkRodadasPersonalizado.IsChecked = false;
            CmbQtdRodadas.SelectedIndex = 9; // 10 rounds
            
            // Reset config combos
            ChkUsarCombos.IsChecked = true;
            UpdateCombosVisibility();

            QtdCombosBox.IsEnabled = true;
            KitsPorComboBox.IsEnabled = true;
            CartelasPorKitBox.IsEnabled = true;

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

        private void ConfigurarPremiosRodada_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.DataContext is RodadaConfigViewModel vm)
                {
                    var padroes = _padraoService.ListarTodos();
                    var window = new PremiosConfigWindow(vm, padroes);
                    window.Owner = Application.Current.MainWindow;
                    if (window.ShowDialog() == true)
                    {
                        if (vm.Premios.Any())
                        {
                            vm.TipoPremio = string.Join(", ", vm.Premios.OrderBy(p => p.Ordem).Select(p => p.Descricao));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao abrir configuração de prêmios: {ex.Message}");
            }
        }
    }
}
