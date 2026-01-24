using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BingoAdmin.Domain.Entities
{
    public class Combo
    {
        public int Id { get; set; }
        public int BingoId { get; set; }
        public Bingo? Bingo { get; set; }
        public int NumeroCombo { get; set; }
        public string NomeDono { get; set; } = string.Empty;
        public string Status { get; set; } = "Disponivel"; // Disponivel, Reservado, Confirmado
        public string Pagamento { get; set; } = "-----"; // -----, Pendente, Pago
        public DateTime? DataConfirmacao { get; set; }
        public string Observacoes { get; set; } = string.Empty;
        
        public List<Kit> Kits { get; set; } = new();
    }
}
