using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BingoAdmin.Domain.Entities;

namespace BingoAdmin.UI.Views
{
    public partial class SelecionarPadroesWindow : Window
    {
        public List<int> SelectedPadroesIds { get; private set; } = new List<int>();
        private List<SelectablePadrao> _items;

        public SelecionarPadroesWindow(List<SelectablePadrao> items)
        {
            InitializeComponent();
            _items = items;
            LstPadroes.ItemsSource = _items;
        }

        private bool _isDynamicMode;

        public SelecionarPadroesWindow(List<Padrao> allPatterns, List<int> selectedIds, bool isDynamicMode = true)
        {
            InitializeComponent();
            _isDynamicMode = isDynamicMode;
            var ids = selectedIds ?? new List<int>();
            
            _items = allPatterns.Select(p => new SelectablePadrao
            {
                Id = p.Id,
                Nome = p.Nome,
                Mascara = p.Mascara,
                IsSelected = ids.Contains(p.Id)
            }).ToList();
            
            LstPadroes.ItemsSource = _items;
            
            // If not dynamic, hide "Select All" and maybe enforce single selection logic
            if (!_isDynamicMode)
            {
                ChkSelectAll.Visibility = Visibility.Collapsed;
            }
        }

        private void ChkSelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (!_isDynamicMode) return;
            
            bool isChecked = ChkSelectAll.IsChecked == true;
            foreach (var item in _items)
            {
                item.IsSelected = isChecked;
            }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            var selected = _items.Where(x => x.IsSelected).ToList();
            
            if (!_isDynamicMode && selected.Count > 1)
            {
                MessageBox.Show("No modo padrão (não dinâmico), selecione apenas 1 padrão.");
                return;
            }
            
            if (selected.Count == 0)
            {
                 MessageBox.Show("Selecione pelo menos um padrão.");
                 return;
            }

            SelectedPadroesIds = selected.Select(x => x.Id).ToList();
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ItemCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (!_isDynamicMode && sender is CheckBox chk && chk.DataContext is SelectablePadrao clickedItem)
            {
                if (chk.IsChecked == true)
                {
                    // Uncheck others
                    foreach (var item in _items)
                    {
                        if (item != clickedItem)
                        {
                            item.IsSelected = false;
                        }
                    }
                }
            }
        }
    }
}