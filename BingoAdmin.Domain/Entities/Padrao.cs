namespace BingoAdmin.Domain.Entities
{
    public class Padrao
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        // Matrix 5x5 serialized as string of 0s and 1s (length 25).
        public string Mascara { get; set; } = string.Empty; 
        public bool IsPredefinido { get; set; }
    }
}
