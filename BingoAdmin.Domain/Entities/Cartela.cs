using System.ComponentModel.DataAnnotations.Schema;

namespace BingoAdmin.Domain.Entities
{
    public class Cartela
    {
        public int Id { get; set; }
        public int BingoId { get; set; }    
        
        public int? KitId { get; set; } // Changed to Kit
        
        public Kit? Kit { get; set; }
        
        public int NumeroCartelaNoKit { get; set; } // Renamed from NumeroCartelaNoCombo
        
        public int NumeroGlobal { get; set; } // Novo: Número único sequencial do bingo (001...999)
        
        // Grid 5x5 serialized. Comma separated numbers. 0 for free space.
        public string GridNumeros { get; set; } = string.Empty; 
        public string HashUnico { get; set; } = string.Empty;
    }
}
