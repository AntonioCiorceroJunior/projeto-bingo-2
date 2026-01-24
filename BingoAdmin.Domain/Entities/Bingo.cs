using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace BingoAdmin.Domain.Entities
{
    public class Bingo
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public DateTime DataInicioPrevista { get; set; }
        public int QuantidadeCombos { get; set; }
        
        public bool TemCombos { get; set; } = true;
        
        public int KitsPorCombo { get; set; } = 1;
        
        public int CartelasPorKit { get; set; } = 1;
        
        public int QuantidadeRodadas { get; set; }
        
        [NotMapped]
        public decimal ValorPorCombo { get; set; } // Novo campo para financeiro
        [NotMapped]
        public bool ModoPadroesDinamicos { get; set; } // Novo modo de jogo
        public int ModoJogo { get; set; } // 0 = Rodadas (Clássico), 1 = Acumulado (Todos contra todos)
        public string Status { get; set; } = "Rascunho"; // Rascunho, Ativo, Finalizado
        public int UsuarioCriadorId { get; set; }
        public Usuario? UsuarioCriador { get; set; }
        public List<Rodada> Rodadas { get; set; } = new();
        public List<Combo> Combos { get; set; } = new();
        public List<Premio> Premios { get; set; } = new();
        public List<BingoPadrao> BingoPadroes { get; set; } = new();
    }
}
