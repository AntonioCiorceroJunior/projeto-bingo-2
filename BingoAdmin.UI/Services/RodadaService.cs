using System.Collections.Generic;
using System.Linq;
using BingoAdmin.Domain.Entities;
using BingoAdmin.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace BingoAdmin.UI.Services
{
    public class RodadaService
    {
        private readonly BingoContext _context;

        public RodadaService(BingoContext context)
        {
            _context = context;
        }

        public List<Rodada> GetRodadas(int bingoId)
        {
            return _context.Rodadas
                .Include(r => r.Padrao)
                .Where(r => r.BingoId == bingoId)
                .OrderBy(r => r.NumeroOrdem)
                .ToList();
        }

        public void AdicionarRodada(int bingoId, int padraoId, string descricao, string tipoPremio)
        {
            var maxOrdem = _context.Rodadas
                .Where(r => r.BingoId == bingoId)
                .Max(r => (int?)r.NumeroOrdem) ?? 0;

            var rodada = new Rodada
            {
                BingoId = bingoId,
                PadraoId = padraoId,
                Descricao = descricao,
                TipoPremio = tipoPremio,
                NumeroOrdem = maxOrdem + 1,
                Status = "NaoIniciada",
                EhRodadaExtra = false
            };

            _context.Rodadas.Add(rodada);
            _context.SaveChanges();
        }

        public void AtualizarRodada(Rodada rodada)
        {
            var existing = _context.Rodadas.Find(rodada.Id);
            if (existing != null)
            {
                existing.Descricao = rodada.Descricao;
                existing.TipoPremio = rodada.TipoPremio;
                existing.PadraoId = rodada.PadraoId;
                _context.SaveChanges();
            }
        }

        public void ExcluirRodada(int id)
        {
            var rodada = _context.Rodadas.Find(id);
            if (rodada != null)
            {
                int bingoId = rodada.BingoId;
                _context.Rodadas.Remove(rodada);
                _context.SaveChanges();
                ReordenarSequencia(bingoId);
            }
        }

        public void MoverRodada(int id, bool paraCima)
        {
            var rodada = _context.Rodadas.Find(id);
            if (rodada == null) return;

            var outrasRodadas = _context.Rodadas
                .Where(r => r.BingoId == rodada.BingoId)
                .OrderBy(r => r.NumeroOrdem)
                .ToList();

            int index = outrasRodadas.FindIndex(r => r.Id == id);
            if (index == -1) return;

            if (paraCima && index > 0)
            {
                var anterior = outrasRodadas[index - 1];
                (rodada.NumeroOrdem, anterior.NumeroOrdem) = (anterior.NumeroOrdem, rodada.NumeroOrdem);
            }
            else if (!paraCima && index < outrasRodadas.Count - 1)
            {
                var proxima = outrasRodadas[index + 1];
                (rodada.NumeroOrdem, proxima.NumeroOrdem) = (proxima.NumeroOrdem, rodada.NumeroOrdem);
            }

            _context.SaveChanges();
        }

        private void ReordenarSequencia(int bingoId)
        {
            var rodadas = _context.Rodadas
                .Where(r => r.BingoId == bingoId)
                .OrderBy(r => r.NumeroOrdem)
                .ToList();

            for (int i = 0; i < rodadas.Count; i++)
            {
                rodadas[i].NumeroOrdem = i + 1;
            }
            _context.SaveChanges();
        }

        public void CriarRodadaExtra(int bingoId, int? padraoId = null)
        {
            var rodada = new Rodada
            {
                BingoId = bingoId,
                PadraoId = padraoId,
                Descricao = "Rodada Extra",
                TipoPremio = "Premio Extra",
                NumeroOrdem = GetProximaOrdem(bingoId),
                Status = "NaoIniciada",
                EhRodadaExtra = true
            };

            _context.Rodadas.Add(rodada);
            _context.SaveChanges();
        }

        private int GetProximaOrdem(int bingoId)
        {
            var rodadas = _context.Rodadas.Where(r => r.BingoId == bingoId).ToList();
            return rodadas.Any() ? rodadas.Max(r => r.NumeroOrdem) + 1 : 1;
        }
    }
}
