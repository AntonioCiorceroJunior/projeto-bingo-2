using System;
using System.Collections.Generic;

namespace BingoAdmin.Domain.Entities
{
    public class Bingo
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public DateTime DataInicioPrevista { get; set; }
        public int QuantidadeCombos { get; set; }
        public int CartelasPorCombo { get; set; }
        public int QuantidadeRodadas { get; set; } // Nova propriedade
        public decimal ValorPorCombo { get; set; } // Novo campo para financeiro
        public bool ModoPadroesDinamicos { get; set; } // Novo modo de jogo
        public string Status { get; set; } = "Rascunho"; // Rascunho, Ativo, Finalizado
        public int UsuarioCriadorId { get; set; }
        public Usuario? UsuarioCriador { get; set; }
        public List<Rodada> Rodadas { get; set; } = new();
        public List<Combo> Combos { get; set; } = new();
        public List<BingoPadrao> BingoPadroes { get; set; } = new();
    }
}
