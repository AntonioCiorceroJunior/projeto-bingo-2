using System;
using System.Linq;
using BingoAdmin.Domain.Entities;
using BingoAdmin.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace BingoAdmin.UI.Services
{
    public class FraseService
    {
        private readonly BingoContext _context;
        private readonly Random _random = new Random();

        public FraseService(BingoContext context)
        {
            _context = context;
        }

        public string GetNextPorUmaBolaPhrase(int count)
        {
            try 
            {
                EnsureSeeded();

                var query = _context.BingoFrases.Where(f => f.Tipo == "PorUmaBola");
                
                // Check unused
                var unused = query.Where(f => !f.FoiUsada).ToList();

                if (unused.Count == 0)
                {
                    // Reset all
                    var all = query.ToList();
                    foreach (var f in all) f.FoiUsada = false;
                    _context.SaveChanges();
                    unused = all;
                }

                if (unused.Count == 0) return $"Atenção, {count} por uma bola!"; // Fallback

                // Select random
                var idx = _random.Next(unused.Count);
                var selected = unused[idx];

                // Mark used
                selected.FoiUsada = true;
                _context.SaveChanges();

                return string.Format(selected.Texto, count);
            }
            catch (Exception ex)
            {
                // Fallback in case of DB error
                return $"Atenção, {count} por uma bola!";
            }
        }

        private void EnsureSeeded()
        {
            if (!_context.BingoFrases.Any(f => f.Tipo == "PorUmaBola"))
            {
                var frases = new[]
                {
                    "Atenção: {0} por uma!",
                    "Olha as {0} na boa!",
                    "Temos {0} cartelas armadas!",
                    "Faltam só {0} cartelas!",
                    "Cuidado, {0} na espera!",
                    "Agora são {0} por uma bola!",
                    "Atenção, {0} cartelas na reta final!",
                    "Tem {0} por um fio!",
                    "Olha o bingo vindo para {0}!",
                    "{0} pedindo a boa!",
                    "Segura! {0} no quase!",
                    "{0} prontinhas pra bater!",
                    "Alerta de {0} na boa!",
                    "{0} cartelas no desespero!",
                    "Mais {0} na fila do gol!",
                    "{0} esperando o grito!",
                    "Tensão com {0} armadas!",
                    "{0} na boca do gol!",
                    "Vem bingo pra {0} aí!",
                    "{0} cartelas pedindo pedra!"
                };

                foreach (var t in frases)
                {
                    _context.BingoFrases.Add(new BingoFrase { Texto = t, Tipo = "PorUmaBola", FoiUsada = false });
                }
                _context.SaveChanges();
            }
        }
    }
}
