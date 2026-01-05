using System.ComponentModel;
using BingoAdmin.UI.Services; // For GanhadorInfo if needed, or just use object for now if circular dep issues

namespace BingoAdmin.UI.ViewModels
{
    public class PedraMaiorItemViewModel : INotifyPropertyChanged
    {
        private int? _pedraSorteada;
        private bool _isWinner;

        public string Nome { get; set; } = string.Empty;
        public string ComboNumero { get; set; } = string.Empty;
        public string NumeroCartela { get; set; } = string.Empty;
        public string NomePadrao { get; set; } = string.Empty;
        
        // We can keep the original info loosely typed or reference the specific type if available globally
        public object? OriginalInfo { get; set; }

        public int? PedraSorteada
        {
            get => _pedraSorteada;
            set
            {
                _pedraSorteada = value;
                OnPropertyChanged(nameof(PedraSorteada));
            }
        }

        public bool IsWinner
        {
            get => _isWinner;
            set
            {
                _isWinner = value;
                OnPropertyChanged(nameof(IsWinner));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
