using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BingoAdmin.Domain.Entities;
using BingoAdmin.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BingoAdmin.UI.Views
{
    public partial class CombosView : UserControl
    {
        private readonly ComboService _comboService;
        private readonly PdfService _pdfService;
        private readonly BingoContextService _bingoContext;
        private readonly HashSet<int> _unlockedCombos = new HashSet<int>();
        
        private List<Combo> _allCombos = new();
        private string _currentFilter = "Total";

        public CombosView()
        {
            InitializeComponent();
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)) return;

            _comboService = ((App)Application.Current).Host.Services.GetRequiredService<ComboService>();
            _pdfService = ((App)Application.Current).Host.Services.GetRequiredService<PdfService>();
            _bingoContext = ((App)Application.Current).Host.Services.GetRequiredService<BingoContextService>();

            LoadBingos();
            _bingoContext.OnBingoChanged += OnGlobalBingoChanged;
            _bingoContext.OnBingoListUpdated += LoadBingos;
        }

        private void OnGlobalBingoChanged(int bingoId)
        {
            if (BingoSelector.SelectedItem is Bingo current && current.Id == bingoId) return;

            var bingos = BingoSelector.ItemsSource as List<Bingo>;
            var target = bingos?.FirstOrDefault(b => b.Id == bingoId);
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
                var target = bingos.FirstOrDefault(b => b.Id == _bingoContext.CurrentBingoId);
                if (target != null)
                {
                    BingoSelector.SelectedItem = target;
                    return;
                }
            }

            if (bingos.Count > 0)
            {
                BingoSelector.SelectedIndex = 0;
            }
        }

        private void BingoSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BingoSelector.SelectedItem is Bingo selectedBingo)
            {
                _bingoContext.SetCurrentBingo(selectedBingo.Id);
                LoadCombos();
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // Recarrega a lista de bingos (para pegar novos criados)
            LoadBingos();
            
            // Se a seleção não mudou (manteve o mesmo ID), forçamos o recarregamento dos combos
            // pois o SelectionChanged pode não disparar se o objeto for considerado "igual" ou se restaurarmos a seleção rapidamente
            if (BingoSelector.SelectedItem != null)
            {
                LoadCombos();
            }
        }

        private void LoadCombos()
        {
            if (BingoSelector.SelectedItem is Bingo selectedBingo)
            {
                _allCombos = _comboService.GetCombos(selectedBingo.Id);
                _unlockedCombos.Clear(); // Reset unlocked state when reloading
                
                UpdateCounts();
                ApplyFilter(_currentFilter);
            }
        }

        private void UpdateCounts()
        {
            int total = _allCombos.Count;
            int disponiveis = _allCombos.Count(c => string.IsNullOrEmpty(c.NomeDono));
            int confirmados = _allCombos.Count(c => !string.IsNullOrEmpty(c.NomeDono) && c.Pagamento == "Pago");
            int pendentes = _allCombos.Count(c => !string.IsNullOrEmpty(c.NomeDono) && c.Pagamento != "Pago");

            BtnFilterDisponivel.Content = $"Disponíveis: {disponiveis}";
            BtnFilterPendente.Content = $"Pendentes: {pendentes}";
            BtnFilterConfirmado.Content = $"Confirmados: {confirmados}";
            BtnFilterTotal.Content = $"Total: {total}";
        }

        private void Filter_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string filter)
            {
                ApplyFilter(filter);
            }
        }

        private void ApplyFilter(string filter)
        {
            _currentFilter = filter;
            IEnumerable<Combo> filtered = _allCombos;

            // Reset Button Styles
            var defaultBrush = new SolidColorBrush(Color.FromRgb(221, 221, 221)); // #DDDDDD
            
            // Define colors for active states
            var yellowBrush = new SolidColorBrush(Colors.Yellow);
            var orangeBrush = new SolidColorBrush(Colors.Orange);
            var greenBrush = new SolidColorBrush(Colors.LightGreen);
            var blueBrush = new SolidColorBrush(Color.FromRgb(173, 216, 230)); // LightBlue for Total

            BtnFilterDisponivel.Background = defaultBrush;
            BtnFilterPendente.Background = defaultBrush;
            BtnFilterConfirmado.Background = defaultBrush;
            BtnFilterTotal.Background = defaultBrush;

            switch (filter)
            {
                case "Disponivel":
                    filtered = _allCombos.Where(c => string.IsNullOrEmpty(c.NomeDono));
                    BtnFilterDisponivel.Background = yellowBrush;
                    BtnCopy.Background = yellowBrush;
                    BtnCopy.Content = "Copiar Lista de Disponíveis (WhatsApp)";
                    break;
                case "Pendente":
                    filtered = _allCombos.Where(c => !string.IsNullOrEmpty(c.NomeDono) && c.Pagamento != "Pago");
                    BtnFilterPendente.Background = orangeBrush;
                    BtnCopy.Background = orangeBrush;
                    BtnCopy.Content = "Copiar Lista de Pendentes (WhatsApp)";
                    break;
                case "Confirmado":
                    filtered = _allCombos.Where(c => !string.IsNullOrEmpty(c.NomeDono) && c.Pagamento == "Pago");
                    BtnFilterConfirmado.Background = greenBrush;
                    BtnCopy.Background = greenBrush;
                    BtnCopy.Content = "Copiar Lista de Confirmados (WhatsApp)";
                    break;
                case "Total":
                default:
                    filtered = _allCombos;
                    BtnFilterTotal.Background = blueBrush;
                    BtnCopy.Background = blueBrush;
                    BtnCopy.Content = "Copiar Lista Total (WhatsApp)";
                    break;
            }

            CombosGrid.ItemsSource = filtered.ToList();
        }

        private void CombosGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if (e.Row.Item is Combo combo)
            {
                // Se tiver Dono (salvo) e não estiver desbloqueado, cancela a edição
                if (!string.IsNullOrEmpty(combo.NomeDono) && !_unlockedCombos.Contains(combo.Id))
                {
                    e.Cancel = true;
                }
            }
        }

        private void EditRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Combo combo)
            {
                // Desbloqueia a linha para edição
                if (!_unlockedCombos.Contains(combo.Id))
                {
                    _unlockedCombos.Add(combo.Id);
                    MessageBox.Show($"Edição habilitada para o Combo {combo.NumeroCombo}.");
                }
            }
        }

        private void SaveRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Combo combo)
            {
                try
                {
                    // Tenta commitar a edição
                    CombosGrid.CommitEdit();
                    CombosGrid.CommitEdit(DataGridEditingUnit.Row, true);
                    
                    // Se ainda estiver editando, cancela para liberar o Refresh
                    CombosGrid.CancelEdit();
                    CombosGrid.CancelEdit(DataGridEditingUnit.Row);

                    _comboService.UpdateCombo(combo);
                    MessageBox.Show($"Combo {combo.NumeroCombo} atualizado!");
                    
                    // Se salvou e tem dono, remove do desbloqueio para travar novamente
                    if (!string.IsNullOrEmpty(combo.NomeDono))
                    {
                        _unlockedCombos.Remove(combo.Id);
                    }
                    
                    // Atualiza contagens e re-aplica filtro
                    UpdateCounts();
                    ApplyFilter(_currentFilter);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao salvar: {ex.Message}");
                }
            }
        }

        private void GeneratePdf_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Combo combo && BingoSelector.SelectedItem is Bingo selectedBingo)
            {
                try
                {
                    var dlg = new Microsoft.Win32.SaveFileDialog
                    {
                        FileName = $"Combo_{combo.NumeroCombo}_{selectedBingo.Nome}.pdf",
                        DefaultExt = ".pdf",
                        Filter = "PDF documents (.pdf)|*.pdf"
                    };

                    if (dlg.ShowDialog() == true)
                    {
                        _pdfService.GenerateComboPdf(combo, selectedBingo.Nome, dlg.FileName);
                        MessageBox.Show("PDF gerado com sucesso!");
                        
                        // Open the file
                        var p = new System.Diagnostics.Process();
                        p.StartInfo = new System.Diagnostics.ProcessStartInfo(dlg.FileName)
                        {
                            UseShellExecute = true
                        };
                        p.Start();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao gerar PDF: {ex.Message}");
                }
            }
        }

        private void CopyWhatsapp_Click(object sender, RoutedEventArgs e)
        {
            if (BingoSelector.SelectedItem is Bingo selectedBingo)
            {
                var sb = new StringBuilder();
                
                // Adiciona mensagem personalizada
                if (!string.IsNullOrWhiteSpace(TxtMensagem.Text))
                {
                    sb.AppendLine(TxtMensagem.Text);
                    sb.AppendLine();
                }

                // Pega a lista filtrada atual (que está no Grid)
                var items = CombosGrid.ItemsSource as List<Combo>;
                if (items == null) return;

                foreach (var c in items)
                {
                    string line = $"Combo {c.NumeroCombo} - ";
                    
                    if (!string.IsNullOrEmpty(c.NomeDono))
                    {
                        line += c.NomeDono;
                        
                        // Se for lista de pendentes, adiciona sufixo
                        if (_currentFilter == "Pendente")
                        {
                            line += " - pendente";
                        }
                    }
                    
                    sb.AppendLine(line);
                }

                Clipboard.SetText(sb.ToString());
                MessageBox.Show($"Lista ({_currentFilter}) copiada para a área de transferência!");
            }
        }
    }
}
