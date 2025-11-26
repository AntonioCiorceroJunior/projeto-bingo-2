namespace BingoAdmin.Domain.Entities
{
    public class Despesa
    {
        public int Id { get; set; }
        public int BingoId { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public string Tipo { get; set; } = "Operacional"; // Operacional, Premio, Outros
    }
}
