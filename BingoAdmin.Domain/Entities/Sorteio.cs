using System;
using System.Collections.Generic;

namespace BingoAdmin.Domain.Entities
{
    public class Sorteio
    {
        public int Id { get; set; }
        public int BingoId { get; set; }
        public int RodadaId { get; set; }
        public Rodada? Rodada { get; set; }
        public DateTime DataHoraInicio { get; set; }
        public DateTime? DataHoraFim { get; set; }
        // List of drawn numbers in order, comma separated
        public string BolasSorteadas { get; set; } = string.Empty; 
    }
}
