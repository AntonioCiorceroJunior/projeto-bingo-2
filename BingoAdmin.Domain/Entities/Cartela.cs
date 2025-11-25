namespace BingoAdmin.Domain.Entities
{
    public class Cartela
    {
        public int Id { get; set; }
        public int BingoId { get; set; }
        public int ComboId { get; set; }
        public Combo? Combo { get; set; }
        public int NumeroCartelaNoCombo { get; set; }
        // Grid 5x5 serialized. Comma separated numbers. 0 for free space.
        public string GridNumeros { get; set; } = string.Empty; 
        public string HashUnico { get; set; } = string.Empty;
    }
}
