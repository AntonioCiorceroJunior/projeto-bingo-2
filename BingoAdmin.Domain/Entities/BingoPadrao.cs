namespace BingoAdmin.Domain.Entities
{
    public class BingoPadrao
    {
        public int Id { get; set; }
        public int BingoId { get; set; }
        public Bingo? Bingo { get; set; }
        public int PadraoId { get; set; }
        public Padrao? Padrao { get; set; }
        public bool FoiSorteado { get; set; }
    }
}
