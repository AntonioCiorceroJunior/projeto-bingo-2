using System;

namespace BingoAdmin.Domain.Entities
{
    public class RodadaPadrao
    {
        public int Id { get; set; }
        public int RodadaId { get; set; }
        public Rodada? Rodada { get; set; }
        public int PadraoId { get; set; }
        public Padrao? Padrao { get; set; }
        public bool FoiSorteado { get; set; }
    }
}
