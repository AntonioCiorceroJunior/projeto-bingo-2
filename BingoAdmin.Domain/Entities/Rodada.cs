using System.Collections.Generic;

namespace BingoAdmin.Domain.Entities
{
    public class Rodada
    {
        public int Id { get; set; }
        public int BingoId { get; set; }
        public Bingo? Bingo { get; set; }
        public int NumeroOrdem { get; set; }
        public string TipoPremio { get; set; } = string.Empty;
        public int? PadraoId { get; set; } // Agora é anulável
        public Padrao? Padrao { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public bool EhRodadaExtra { get; set; }
        public string Status { get; set; } = "NaoIniciada"; // NaoIniciada, EmAndamento, Finalizada
        public List<Sorteio> Sorteios { get; set; } = new();
        public List<Ganhador> Ganhadores { get; set; } = new();
    }
}
