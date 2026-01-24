namespace BingoAdmin.Domain.Entities
{
    public class BingoFrase
    {
        public int Id { get; set; }
        public string Texto { get; set; } = string.Empty;
        public bool FoiUsada { get; set; } = false;
        public string Tipo { get; set; } = "PorUmaBola"; // Para diferenciar tipos de frases no futuro
    }
}
