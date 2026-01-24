namespace BingoAdmin.Domain.Entities
{
    public class Premio
    {
        public int Id { get; set; }
        public int? RodadaId { get; set; }
        public Rodada? Rodada { get; set; }
        public int? BingoId { get; set; } // Para prêmios globais no modo acumulado
        public Bingo? Bingo { get; set; }
        public int? PadraoId { get; set; }
        public Padrao? Padrao { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public int Ordem { get; set; } // 1º Prêmio, 2º Prêmio...
        public decimal? Valor { get; set; }
        
        // Link reverso opcional (muitos sistemas preferem linkar Ganhador->Premio)
        // Aqui deixo apenas os dados do prêmio mesmo. O Ganhador apontará para o Prêmio.
    }
}
