using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BingoAdmin.Domain.Entities
{
    public class Kit
    {
        public int Id { get; set; }
        public int ComboId { get; set; }
        public Combo? Combo { get; set; }
        public int NumeroKitNoCombo { get; set; } // 1, 2, 3... within the Combo
        public List<Cartela> Cartelas { get; set; } = new();
    }
}
