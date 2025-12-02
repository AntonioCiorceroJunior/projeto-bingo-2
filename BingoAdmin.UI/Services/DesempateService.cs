using System;
using System.Collections.Generic;
using System.Linq;
using BingoAdmin.Domain.Entities;
using BingoAdmin.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace BingoAdmin.UI.Services
{
    public class DesempateService
    {
        private readonly BingoContext _context;
        private readonly FeedService _feedService;
        private Random _random = new Random();

        public DesempateService(BingoContext context, FeedService feedService)
        {
            _context = context;
            _feedService = feedService;
        }

        public List<Ganhador> GetGanhadoresDaRodada(int rodadaId)
        {
            return _context.Ganhadores
                .Include(g => g.Cartela)
                .ThenInclude(c => c.Combo)
                .Where(g => g.RodadaId == rodadaId)
                .ToList();
        }

        public List<PedraMaiorSorteio> GetHistoricoSorteios(int rodadaId)
        {
            return _context.PedraMaiorSorteios
                .Include(p => p.Ganhador)
                .ThenInclude(g => g.Cartela)
                .ThenInclude(c => c.Combo)
                .Where(p => p.RodadaId == rodadaId)
                .OrderBy(p => p.OrdemSorteio)
                .ToList();
        }

        public int RealizarSorteio(int rodadaId, int ganhadorId)
        {
            // Verifica qual é a "rodada" de desempate atual para este ganhador
            int ordem = _context.PedraMaiorSorteios
                .Count(p => p.RodadaId == rodadaId && p.GanhadorId == ganhadorId) + 1;

            int numero = _random.Next(1, 101);

            var sorteio = new PedraMaiorSorteio
            {
                RodadaId = rodadaId,
                GanhadorId = ganhadorId,
                NumeroSorteado = numero,
                OrdemSorteio = ordem
            };

            _context.PedraMaiorSorteios.Add(sorteio);
            _context.SaveChanges();

            _feedService.AddMessage("Pedra Maior", $"Sorteio: {numero} (Ganhador ID: {ganhadorId})", "Info");

            return numero;
        }

        public void DefinirVencedorFinal(int ganhadorId)
        {
            var ganhador = _context.Ganhadores.Find(ganhadorId);
            if (ganhador != null)
            {
                // Remove flag de outros vencedores da mesma rodada
                var outros = _context.Ganhadores.Where(g => g.RodadaId == ganhador.RodadaId).ToList();
                foreach (var g in outros)
                {
                    g.IsVencedorFinal = false;
                }

                ganhador.IsVencedorFinal = true;
                _context.SaveChanges();
            }
        }

        public List<RodadaDesempateInfo> GetRodadasComDesempate(int bingoId)
        {
            var rodadas = _context.Rodadas
                .Include(r => r.Padrao)
                .Include(r => r.Ganhadores)
                    .ThenInclude(g => g.Cartela)
                        .ThenInclude(c => c.Combo)
                .Where(r => r.BingoId == bingoId)
                .ToList();

            var result = new List<RodadaDesempateInfo>();

            foreach (var rodada in rodadas)
            {
                if (rodada.Ganhadores.Count > 1)
                {
                    var historico = _context.PedraMaiorSorteios
                        .Include(p => p.Ganhador)
                        .ThenInclude(g => g.Cartela)
                        .ThenInclude(c => c.Combo)
                        .Where(p => p.RodadaId == rodada.Id)
                        .OrderBy(p => p.OrdemSorteio)
                        .ToList();

                    result.Add(new RodadaDesempateInfo
                    {
                        Rodada = rodada,
                        Ganhadores = rodada.Ganhadores,
                        Historico = historico
                    });
                }
            }

            return result;
        }

        public void SalvarSorteioPedraMaiorEmLote(int rodadaId, List<(int CartelaId, int NumeroSorteado, bool IsVencedor)> resultados)
        {
            // Atualiza tabela específica de Desempate (Snapshot)
            var itens = _context.DesempateItens.Where(d => d.RodadaId == rodadaId).ToList();
            foreach (var res in resultados)
            {
                var item = itens.FirstOrDefault(i => i.CartelaId == res.CartelaId);
                if (item != null)
                {
                    item.PedraMaior = res.NumeroSorteado;
                    item.IsVencedor = res.IsVencedor;
                }
            }

            // Limpa sorteios anteriores dessa rodada para evitar duplicidade se re-sortear
            var existentes = _context.PedraMaiorSorteios.Where(p => p.RodadaId == rodadaId).ToList();
            if (existentes.Any())
            {
                _context.PedraMaiorSorteios.RemoveRange(existentes);
            }

            // Busca os ganhadores (Ganhador entity) baseados no CartelaId e RodadaId
            var ganhadores = _context.Ganhadores
                .Where(g => g.RodadaId == rodadaId)
                .ToList();

            foreach (var res in resultados)
            {
                var ganhador = ganhadores.FirstOrDefault(g => g.CartelaId == res.CartelaId);
                if (ganhador != null)
                {
                    // Salva o sorteio
                    _context.PedraMaiorSorteios.Add(new PedraMaiorSorteio
                    {
                        RodadaId = rodadaId,
                        GanhadorId = ganhador.Id,
                        NumeroSorteado = res.NumeroSorteado,
                        OrdemSorteio = 1 // Lote único
                    });

                    // Atualiza flag de vencedor
                    ganhador.IsVencedorFinal = res.IsVencedor;
                }
            }

            _context.SaveChanges();
        }

        public void SincronizarDesempate(int rodadaId, List<GanhadorInfo> ganhadores)
        {
            // Busca o BingoId da rodada
            var rodada = _context.Rodadas.FirstOrDefault(r => r.Id == rodadaId);
            int bingoId = rodada?.BingoId ?? 0;

            // Limpa itens existentes para garantir que a lista reflete apenas os ganhadores atuais (sem fantasmas)
            var existentes = _context.DesempateItens.Where(d => d.RodadaId == rodadaId).ToList();
            if (existentes.Any())
            {
                _context.DesempateItens.RemoveRange(existentes);
            }

            foreach (var g in ganhadores)
            {
                _context.DesempateItens.Add(new DesempateItem
                {
                    BingoId = bingoId,
                    RodadaId = rodadaId,
                    CartelaId = g.CartelaId,
                    Nome = g.NomeDono,
                    Combo = g.ComboNumero,
                    CartelaNumero = g.NumeroCartela,
                    PedraMaior = 0,
                    IsVencedor = false
                });
            }
            
            _context.SaveChanges();
        }

        public List<DesempateItem> GetItensDesempate(int rodadaId)
        {
            return _context.DesempateItens
                .Where(d => d.RodadaId == rodadaId)
                .OrderByDescending(d => d.PedraMaior)
                .ToList();
        }

        public Sorteio? GetSorteioDaRodada(int rodadaId)
        {
            return _context.Sorteios.FirstOrDefault(s => s.RodadaId == rodadaId);
        }

        public List<RodadaDesempateViewModelInfo> GetDesempatesDoBingo(int bingoId)
        {
            var rodadas = _context.Rodadas
                .Include(r => r.Padrao)
                .Where(r => r.BingoId == bingoId)
                .OrderBy(r => r.NumeroOrdem)
                .ToList();

            var result = new List<RodadaDesempateViewModelInfo>();

            foreach (var rodada in rodadas)
            {
                var itens = _context.DesempateItens
                    .Where(d => d.RodadaId == rodada.Id)
                    .OrderByDescending(d => d.PedraMaior)
                    .ToList();

                // Se não houver itens, ou se todos os itens tiverem PedraMaior == 0 (indicando possível falha ou dados não sincronizados)
                // mas houver histórico de sorteio legado, tentamos migrar novamente.
                bool precisaMigrar = !itens.Any();
                if (!precisaMigrar && itens.All(i => i.PedraMaior == 0))
                {
                    var temHistoricoLegado = _context.PedraMaiorSorteios.Any(p => p.RodadaId == rodada.Id);
                    if (temHistoricoLegado)
                    {
                        // Remove itens zerados para re-importar do legado
                        _context.DesempateItens.RemoveRange(itens);
                        _context.SaveChanges();
                        precisaMigrar = true;
                    }
                }

                if (precisaMigrar)
                {
                    // Tenta migrar/carregar do modelo antigo
                    itens = MigrarOuCarregarLegado(rodada);
                }

                if (itens.Any())
                {
                    result.Add(new RodadaDesempateViewModelInfo
                    {
                        Rodada = rodada,
                        Itens = itens
                    });
                }
            }

            return result;
        }

        private List<DesempateItem> MigrarOuCarregarLegado(Rodada rodada)
        {
            var ganhadores = _context.Ganhadores
                .Include(g => g.Cartela)
                    .ThenInclude(c => c!.Combo)
                .Where(g => g.RodadaId == rodada.Id)
                .ToList();

            if (ganhadores.Count <= 1) return new List<DesempateItem>();

            var sorteios = _context.PedraMaiorSorteios
                .Where(p => p.RodadaId == rodada.Id)
                .ToList();

            var novosItens = new List<DesempateItem>();

            // Recalcula indices das cartelas para este bingo
            var cartelasBingo = _context.Cartelas
                .Where(c => c.BingoId == rodada.BingoId)
                .OrderBy(c => c.ComboId).ThenBy(c => c.Id)
                .Select(c => new { c.Id, c.ComboId })
                .ToList();
                
            var cartelaMap = new Dictionary<int, int>();
            var combos = cartelasBingo.GroupBy(c => c.ComboId);
            foreach(var grp in combos)
            {
                int idx = 1;
                foreach(var c in grp)
                {
                    cartelaMap[c.Id] = idx++;
                }
            }

            foreach (var g in ganhadores)
            {
                var sorteio = sorteios.FirstOrDefault(s => s.GanhadorId == g.Id);
                var numeroCartela = cartelaMap.ContainsKey(g.CartelaId) ? cartelaMap[g.CartelaId] : 0;

                var item = new DesempateItem
                {
                    BingoId = rodada.BingoId,
                    RodadaId = rodada.Id,
                    CartelaId = g.CartelaId,
                    Nome = g.Cartela?.Combo?.NomeDono ?? "Desconhecido",
                    Combo = g.Cartela?.Combo?.NumeroCombo ?? 0,
                    CartelaNumero = numeroCartela,
                    PedraMaior = sorteio?.NumeroSorteado ?? 0,
                    IsVencedor = g.IsVencedorFinal
                };
                novosItens.Add(item);
                _context.DesempateItens.Add(item);
            }

            if (novosItens.Any())
            {
                _context.SaveChanges();
            }

            return novosItens.OrderByDescending(i => i.PedraMaior).ToList();
        }
    }

    public class RodadaDesempateInfo
    {
        public required Rodada Rodada { get; set; }
        public required List<Ganhador> Ganhadores { get; set; }
        public required List<PedraMaiorSorteio> Historico { get; set; }
    }

    public class RodadaDesempateViewModelInfo
    {
        public required Rodada Rodada { get; set; }
        public required List<DesempateItem> Itens { get; set; }
    }
}
