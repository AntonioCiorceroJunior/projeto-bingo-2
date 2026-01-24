namespace BingoAdmin.Domain.Entities
{
    public class Ganhador
    {
        public int Id { get; set; }
        public int RodadaId { get; set; }
        public Rodada? Rodada { get; set; }
        public int CartelaId { get; set; }
        public Cartela? Cartela { get; set; }
        public bool IsVencedorFinal { get; set; } // In case of tie-break
        public int? PremioId { get; set; } // Qual prêmio específico levou
        public Premio? Premio { get; set; }
    }
}
