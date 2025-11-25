namespace BingoAdmin.Domain.Entities
{
    public class DesempateItem
    {
        public int Id { get; set; }
        public int BingoId { get; set; }
        public int RodadaId { get; set; }
        public int CartelaId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int Combo { get; set; }
        public int CartelaNumero { get; set; }
        public int PedraMaior { get; set; }
        public bool IsVencedor { get; set; }
    }
}
