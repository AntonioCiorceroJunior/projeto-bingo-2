using System.Collections.Generic;
using System.Linq;
using BingoAdmin.Domain.Entities;
using BingoAdmin.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace BingoAdmin.UI.Services
{
    public class ComboService
    {
        private readonly BingoContext _context;

        public ComboService(BingoContext context)
        {
            _context = context;
        }

        public List<Bingo> GetBingos()
        {
            return _context.Bingos.OrderByDescending(b => b.DataInicioPrevista).ToList();
        }

        public List<Combo> GetCombos(int bingoId)
        {
            return _context.Combos
                .Include(c => c.Cartelas)
                .Where(c => c.BingoId == bingoId)
                .OrderBy(c => c.NumeroCombo)
                .ToList();
        }

        public void UpdateCombo(Combo combo)
        {
            // Ensure we are working with the tracked entity or attach it
            var existing = _context.Combos.Local.FirstOrDefault(c => c.Id == combo.Id);
            
            if (existing == null)
            {
                // Not in local cache, try to find it in DB
                existing = _context.Combos.Find(combo.Id);
            }
            
            if (existing != null)
            {
                // If it's a different instance (shouldn't be if loaded from same context), copy values
                if (!ReferenceEquals(existing, combo))
                {
                    _context.Entry(existing).CurrentValues.SetValues(combo);
                }
                
                // If it is the same instance, the values are already there.
                // We just need to ensure the state is Modified so EF generates the UPDATE.
                if (_context.Entry(existing).State == EntityState.Unchanged)
                {
                    _context.Entry(existing).State = EntityState.Modified;
                }

                // Auto-update confirmation date logic
                if (existing.Status == "Confirmado" && existing.DataConfirmacao == null)
                {
                    existing.DataConfirmacao = System.DateTime.Now;
                }

                _context.SaveChanges();
            }
        }

        public string GetAvailableCombosText(int bingoId)
        {
            var combos = _context.Combos
                .Where(c => c.BingoId == bingoId && c.Status == "Disponivel" && (c.NomeDono == null || c.NomeDono == ""))
                .OrderBy(c => c.NumeroCombo)
                .Select(c => c.NumeroCombo)
                .ToList();

            if (!combos.Any()) return "Não há combos disponíveis no momento.";

            return "Combos disponíveis:\n" + string.Join(", ", combos);
        }
    }
}
