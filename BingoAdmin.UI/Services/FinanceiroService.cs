using System.Collections.Generic;
using System.Linq;
using BingoAdmin.Domain.Entities;
using BingoAdmin.Infra.Data;

namespace BingoAdmin.UI.Services
{
    public class FinanceiroService
    {
        private readonly BingoContext _context;

        public FinanceiroService(BingoContext context)
        {
            _context = context;
        }

        public void AtualizarValorCombo(int bingoId, decimal valor)
        {
            var bingo = _context.Bingos.Find(bingoId);
            if (bingo != null)
            {
                bingo.ValorPorCombo = valor;
                _context.SaveChanges();
            }
        }

        public decimal GetValorCombo(int bingoId)
        {
            var bingo = _context.Bingos.Find(bingoId);
            return bingo?.ValorPorCombo ?? 0;
        }

        public List<Despesa> GetDespesas(int bingoId)
        {
            return _context.Despesas.Where(d => d.BingoId == bingoId).ToList();
        }

        public void AdicionarDespesa(Despesa despesa)
        {
            _context.Despesas.Add(despesa);
            _context.SaveChanges();
        }

        public void RemoverDespesa(int despesaId)
        {
            var despesa = _context.Despesas.Find(despesaId);
            if (despesa != null)
            {
                _context.Despesas.Remove(despesa);
                _context.SaveChanges();
            }
        }

        public (decimal Receita, decimal Despesas, decimal Lucro) CalcularTotais(int bingoId)
        {
            var bingo = _context.Bingos.Find(bingoId);
            if (bingo == null) return (0, 0, 0);

            // Receita: Combos vendidos (com dono) * Valor do Combo
            // Consideramos "vendido" se tiver dono, independente se está "Pago" ou não para previsão,
            // mas para fluxo de caixa real deveríamos filtrar por Pagamento == "Pago".
            // O usuário pediu "quanto ele vai lucrar", então previsão (todos com dono) é interessante,
            // mas vamos filtrar por "Pago" para ser mais conservador ou mostrar os dois?
            // Vamos simplificar: Receita Potencial (Todos com dono) vs Receita Real (Pagos).
            // Por enquanto, vamos usar Receita Real (Pagos).
            
            int combosPagos = _context.Combos.Count(c => c.BingoId == bingoId && !string.IsNullOrEmpty(c.NomeDono) && c.Pagamento == "Pago");
            decimal receita = combosPagos * bingo.ValorPorCombo;

            // Fix for SQLite limitation: "SQLite cannot apply aggregate operator 'Sum' on expressions of type 'decimal'"
            // We bring the data to memory (client-side) before summing.
            var despesasLista = _context.Despesas.Where(d => d.BingoId == bingoId).ToList();
            decimal despesas = despesasLista.Sum(d => d.Valor);

            return (receita, despesas, receita - despesas);
        }
    }
}
