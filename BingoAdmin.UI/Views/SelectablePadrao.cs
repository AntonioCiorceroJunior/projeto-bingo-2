using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BingoAdmin.UI.Views
{
    public class SelectablePadrao : INotifyPropertyChanged
    {
        private bool _isSelected;

        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Mascara { get; set; } = string.Empty;
        
        public bool IsSelected 
        { 
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
