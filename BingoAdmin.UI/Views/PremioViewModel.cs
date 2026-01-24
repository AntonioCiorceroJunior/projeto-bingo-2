using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BingoAdmin.UI.Views
{
    public class PremioViewModel : INotifyPropertyChanged
    {
        private string _descricao = string.Empty;
        private int _ordem;
        private decimal? _valor;
        private int? _padraoId;

        public string Descricao
        {
            get => _descricao;
            set { _descricao = value; OnPropertyChanged(); }
        }

        public int Ordem
        {
            get => _ordem;
            set { _ordem = value; OnPropertyChanged(); }
        }
        
        public decimal? Valor
        {
            get => _valor;
            set { _valor = value; OnPropertyChanged(); }
        }

        public int? PadraoId
        {
            get => _padraoId;
            set { _padraoId = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
