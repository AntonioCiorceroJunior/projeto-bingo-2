using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BingoAdmin.UI.Views
{
    public class RodadaConfigViewModel : INotifyPropertyChanged
    {
        private int _numero;
        private string _descricao = string.Empty;
        private string _tipoPremio = string.Empty;
        private bool _modoDinamico;
        private List<int> _padroesIds = new();
        private int? _maximoGanhadores;
        private string _tipoJogo = "Padrao";

        public int Numero
        {
            get => _numero;
            set { _numero = value; OnPropertyChanged(); }
        }

        public string Descricao
        {
            get => _descricao;
            set { _descricao = value; OnPropertyChanged(); }
        }

        public string TipoPremio
        {
            get => _tipoPremio;
            set { _tipoPremio = value; OnPropertyChanged(); }
        }

        public bool ModoDinamico
        {
            get => _modoDinamico;
            set { _modoDinamico = value; OnPropertyChanged(); }
        }

        public int? MaximoGanhadores
        {
            get => _maximoGanhadores;
            set { _maximoGanhadores = value; OnPropertyChanged(); }
        }

        public string TipoJogo
        {
            get => _tipoJogo;
            set { _tipoJogo = value; OnPropertyChanged(); }
        }

        public List<int> PadroesIds
        {
            get => _padroesIds;
            set { _padroesIds = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
