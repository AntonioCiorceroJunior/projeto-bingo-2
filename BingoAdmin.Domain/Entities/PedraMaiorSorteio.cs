namespace BingoAdmin.Domain.Entities
{
    public class PedraMaiorSorteio
    {
        public int Id { get; set; }
        public int RodadaId { get; set; }
        public int GanhadorId { get; set; }
        public Ganhador? Ganhador { get; set; }
        public int NumeroSorteado { get; set; } // 1-100
        public int OrdemSorteio { get; set; } // 1st draw, 2nd draw (tie-break)
    }
}
