using System;
using System.Collections.Generic;
using System.Linq;
using BingoAdmin.Domain.Entities;
using BingoAdmin.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace BingoAdmin.UI.Services
{
    public class GameService
    {
        private readonly BingoContext _context;
        private List<CachedCartela> _cachedCartelas = new();
        private HashSet<int> _numerosSorteados = new();
        private HashSet<int> _ganhadoresIds = new();
        private Rodada? _rodadaAtual;
        private string _mascaraAtual = string.Empty;
        private Random _random = new Random();

        public event Action<int>? OnNumeroSorteado;
        public event Action<List<GanhadorInfo>>? OnGanhadoresEncontrados;

        public GameService(BingoContext context)
        {
            _context = context;
        }

        public void CarregarDadosBingo(int bingoId)
        {
            _cachedCartelas.Clear();
            var cartelas = _context.Cartelas
                .Include(c => c.Combo)
                .Where(c => c.BingoId == bingoId)
                .ToList();

            // Group by Combo to determine index
            var cartelasPorCombo = cartelas.GroupBy(c => c.ComboId);

            foreach (var group in cartelasPorCombo)
            {
                int index = 1;
                foreach (var c in group.OrderBy(x => x.Id)) // Assuming ID order is creation order
                {
                    var nums = c.GridNumeros.Split(',').Select(int.Parse).ToArray();
                    _cachedCartelas.Add(new CachedCartela
                    {
                        Id = c.Id,
                        ComboNumero = c.Combo?.NumeroCombo ?? 0,
                        NumeroCartela = index++,
                        Dono = c.Combo?.NomeDono ?? "Desconhecido",
                        Numeros = nums
                    });
                }
            }
        }

        public void IniciarRodada(int rodadaId)
        {
            _rodadaAtual = _context.Rodadas
                .Include(r => r.Padrao)
                .FirstOrDefault(r => r.Id == rodadaId);

            if (_rodadaAtual == null) throw new Exception("Rodada não encontrada");

            // Se não tiver padrão (ex: rodada extra), assume cartela cheia (tudo 1)
            _mascaraAtual = _rodadaAtual.Padrao?.Mascara ?? new string('1', 25);
            _numerosSorteados.Clear();
            
            _ganhadoresIds = _context.Ganhadores
                .Where(g => g.RodadaId == rodadaId)
                .Select(g => g.CartelaId)
                .ToHashSet();
            
            // Carregar ou criar registro de Sorteio
            var sorteio = _context.Sorteios.FirstOrDefault(s => s.RodadaId == rodadaId);
            
            if (sorteio != null)
            {
                if (!string.IsNullOrEmpty(sorteio.BolasSorteadas))
                {
                    var nums = sorteio.BolasSorteadas.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                     .Select(int.Parse);
                    foreach(var n in nums) _numerosSorteados.Add(n);
                }
            }
            else
            {
                sorteio = new Sorteio
                {
                    RodadaId = rodadaId,
                    BingoId = _rodadaAtual.BingoId,
                    DataHoraInicio = DateTime.Now,
                    BolasSorteadas = ""
                };
                _context.Sorteios.Add(sorteio);
                _context.SaveChanges();
            }
            
            if (_rodadaAtual.Status == "NaoIniciada")
            {
                _rodadaAtual.Status = "EmAndamento";
                _context.SaveChanges();
            }
        }

        public void AtualizarPadrao(Padrao padrao)
        {
            if (_rodadaAtual != null)
            {
                _rodadaAtual.Padrao = padrao;
                _rodadaAtual.PadraoId = padrao.Id;
                _mascaraAtual = padrao.Mascara;
                
                // Re-verificar ganhadores com o novo padrão
                VerificarGanhadores();
            }
        }

        public int SortearNumero()
        {
            if (_rodadaAtual == null) throw new Exception("Nenhuma rodada iniciada.");
            if (_numerosSorteados.Count >= 75) throw new Exception("Todos os números já foram sorteados.");

            int numero;
            do
            {
                numero = _random.Next(1, 76);
            } while (_numerosSorteados.Contains(numero));

            _numerosSorteados.Add(numero);

            // Atualizar persistência
            var sorteio = _context.Sorteios.FirstOrDefault(s => s.RodadaId == _rodadaAtual.Id);
            if (sorteio != null)
            {
                var lista = _numerosSorteados.ToList();
                sorteio.BolasSorteadas = string.Join(",", lista);
                _context.SaveChanges();
            }

            OnNumeroSorteado?.Invoke(numero);
            VerificarGanhadores();

            return numero;
        }

        private void VerificarGanhadores()
        {
            var novosGanhadores = new List<GanhadorInfo>();

            foreach (var cartela in _cachedCartelas)
            {
                if (_ganhadoresIds.Contains(cartela.Id)) continue;

                if (VerificarCartela(cartela.Numeros))
                {
                    novosGanhadores.Add(new GanhadorInfo 
                    { 
                        CartelaId = cartela.Id, 
                        ComboNumero = cartela.ComboNumero,
                        NumeroCartela = cartela.NumeroCartela,
                        NomeDono = cartela.Dono 
                    });
                }
            }

            if (novosGanhadores.Any())
            {
                foreach(var g in novosGanhadores) _ganhadoresIds.Add(g.CartelaId);

                OnGanhadoresEncontrados?.Invoke(novosGanhadores);
                SalvarGanhadores(novosGanhadores);
            }
        }

        private bool VerificarCartela(int[] numerosCartela)
        {
            // Check for "X na louca" pattern (RANDOM:X)
            if (_mascaraAtual.StartsWith("RANDOM:"))
            {
                if (int.TryParse(_mascaraAtual.Substring(7), out int requiredCount))
                {
                    int hitCount = 0;
                    for (int i = 0; i < 25; i++)
                    {
                        int numeroNaPosicao = numerosCartela[i];
                        // Count if it's a free space (0) or if the number has been drawn
                        if (numeroNaPosicao == 0 || _numerosSorteados.Contains(numeroNaPosicao))
                        {
                            hitCount++;
                        }
                    }
                    return hitCount >= requiredCount;
                }
            }

            for (int i = 0; i < 25; i++)
            {
                if (_mascaraAtual.Length > i && _mascaraAtual[i] == '1')
                {
                    int numeroNaPosicao = numerosCartela[i];
                    // 0 é considerado espaço livre (já marcado)
                    if (numeroNaPosicao != 0 && !_numerosSorteados.Contains(numeroNaPosicao))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private void SalvarGanhadores(List<GanhadorInfo> ganhadoresInfos)
        {
            if (_rodadaAtual == null) return;

            bool novosGanhadores = false;
            foreach (var g in ganhadoresInfos)
            {
                // Verifica se já não foi salvo
                bool jaSalvo = _context.Ganhadores.Any(x => x.RodadaId == _rodadaAtual.Id && x.CartelaId == g.CartelaId);
                if (!jaSalvo)
                {
                    _context.Ganhadores.Add(new Ganhador
                    {
                        RodadaId = _rodadaAtual.Id,
                        CartelaId = g.CartelaId,
                        IsVencedorFinal = false
                    });
                    novosGanhadores = true;
                }
            }

            if (novosGanhadores)
            {
                _context.SaveChanges();
            }
        }

        public void ReiniciarRodada()
        {
            if (_rodadaAtual == null) return;

            _numerosSorteados.Clear();
            _ganhadoresIds.Clear();

            var sorteio = _context.Sorteios.FirstOrDefault(s => s.RodadaId == _rodadaAtual.Id);
            if (sorteio != null)
            {
                sorteio.BolasSorteadas = "";
            }

            // Remove winners associated with this round
            var ganhadores = _context.Ganhadores.Where(g => g.RodadaId == _rodadaAtual.Id).ToList();
            if (ganhadores.Any())
            {
                _context.Ganhadores.RemoveRange(ganhadores);
            }

            // Se estiver encerrada, volta para EmAndamento
            if (_rodadaAtual.Status == "Encerrada")
            {
                _rodadaAtual.Status = "EmAndamento";
            }

            _context.SaveChanges();
        }

        public List<GanhadorInfo> GetGanhadoresAtuais()
        {
            if (_rodadaAtual == null) return new List<GanhadorInfo>();

            var ganhadores = new List<GanhadorInfo>();
            foreach (var id in _ganhadoresIds)
            {
                var cartela = _cachedCartelas.FirstOrDefault(c => c.Id == id);
                if (cartela != null)
                {
                    ganhadores.Add(new GanhadorInfo
                    {
                        CartelaId = cartela.Id,
                        ComboNumero = cartela.ComboNumero,
                        NumeroCartela = cartela.NumeroCartela,
                        NomeDono = cartela.Dono
                    });
                }
            }
            return ganhadores;
        }

        public void EncerrarRodada()
        {
            if (_rodadaAtual == null) return;
            _rodadaAtual.Status = "Encerrada";
            _context.SaveChanges();
        }

        public HashSet<int> GetNumerosSorteados() => _numerosSorteados;

        public string GetMascaraAtual() => _mascaraAtual;

        public CachedCartela? GetCartela(int cartelaId)
        {
            return _cachedCartelas.FirstOrDefault(c => c.Id == cartelaId);
        }
    }

    public class CachedCartela
    {
        public int Id { get; set; }
        public int ComboNumero { get; set; }
        public int NumeroCartela { get; set; }
        public string Dono { get; set; } = string.Empty;
        public int[] Numeros { get; set; } = Array.Empty<int>();
    }

    public class GanhadorInfo
    {
        public int CartelaId { get; set; }
        public int ComboNumero { get; set; }
        public int NumeroCartela { get; set; }
        public string NomeDono { get; set; } = string.Empty;
    }
}
