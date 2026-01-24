using System;
using System.Collections.Generic;
using System.Linq;
using BingoAdmin.Domain.Entities;

namespace BingoAdmin.Domain.Services
{
    public class BingoService
    {
        private readonly Random _random = new Random();

        public List<Combo> GerarCombos(int bingoId, int quantidadeCombos, int kitsPorCombo, int cartelasPorKit)
        {
            // Mantendo compatibilidade para chamadas simples, criando um HashSet novo
            return GerarLoteCombos(bingoId, 1, quantidadeCombos, kitsPorCombo, cartelasPorKit, new HashSet<string>());
        }

        public List<Combo> GerarLoteCombos(int bingoId, int startCombo, int quantidade, int kitsPorCombo, int cartelasPorKit, HashSet<string> hashesExistentes)
        {
            var combos = new List<Combo>();
            
            // 1. Calculate and Generate Global Numbers Pool
            // We need to know which numbers to assign.
            // Assumption: This method generates a continuous batch.
            // But wait, if we are generating a LOTE (batch), we might be appending to existing bingo.
            // However, the common use case is generating the whole bingo or a large chunk.
            
            // For now, let's assume we are generating indices sequentially based on what's being asked.
            // We need to know the starting global index if we were appending, but typically we generate all at once?
            // If the user asks for 10 combos, we generate (10 * kits * cartelas) global numbers.
            // But the requirement is: "distributed randomly per kit".
            
            int totalCartelasNoLote = quantidade * kitsPorCombo * cartelasPorKit;
            
            // We need a way to track global numbering across batches if we support batches.
            // But let's assume for this specific method scope, we are assigning numbers from a pool provided or created here.
            
            // To support random distribution ACROSS the whole bingo, we ideally should generate all cartelas first and then assign numbers?
            // OR, we simply generate a list of numbers for this batch and shuffle them.
            
            // Let's generate a list of IDs for this batch relative to a "startGlobalIndex" if we had one.
            // Since we don't have existing count passed in, we might assume startGlobalIndex = 1 if startCombo=1.
            // Calculating startGlobalIndex:
            int startGlobalIndex = ((startCombo - 1) * kitsPorCombo * cartelasPorKit) + 1;
            
            var globalNumbers = Enumerable.Range(startGlobalIndex, totalCartelasNoLote).ToList();
            
            // Shuffle the global numbers
            int n = globalNumbers.Count;
            while (n > 1)
            {
                n--;
                int k = _random.Next(n + 1);
                int value = globalNumbers[k];
                globalNumbers[k] = globalNumbers[n];
                globalNumbers[n] = value;
            }
            
            int currentGlobalNumberIndex = 0;

            for (int i = 0; i < quantidade; i++)
            {
                int numeroComboAtual = startCombo + i;
                var combo = new Combo
                {
                    BingoId = bingoId,
                    NumeroCombo = numeroComboAtual,
                    Status = "Disponivel",
                    Kits = new List<Kit>()
                };

                for (int k = 1; k <= kitsPorCombo; k++)
                {
                    var kit = new Kit
                    {
                         NumeroKitNoCombo = k,
                         Cartelas = new List<Cartela>()
                    };

                    for (int c = 1; c <= cartelasPorKit; c++)
                    {
                        // Generate cartela
                        Cartela cartela;
                        do
                        {
                            cartela = GerarUmaCartela(bingoId, c); 
                        } while (hashesExistentes.Contains(cartela.HashUnico));

                        // Assign shuffled global number
                        if (currentGlobalNumberIndex < globalNumbers.Count)
                        {
                            cartela.NumeroGlobal = globalNumbers[currentGlobalNumberIndex++];
                        }

                        hashesExistentes.Add(cartela.HashUnico);
                        cartela.Kit = kit;
                        kit.Cartelas.Add(cartela);
                    }
                    combo.Kits.Add(kit);
                }
                combos.Add(combo);
            }
            return combos;
        }

        private Cartela GerarUmaCartela(int bingoId, int numeroCartelaNoKit)
        {
            int[] b = GerarColuna(1, 15, 5);
            int[] i = GerarColuna(16, 30, 5);
            int[] n = GerarColuna(31, 45, 4); // 4 numbers, middle is free
            int[] g = GerarColuna(46, 60, 5);
            int[] o = GerarColuna(61, 75, 5);

            int[] grid = new int[25];
            
            // Col B
            grid[0] = b[0]; grid[5] = b[1]; grid[10] = b[2]; grid[15] = b[3]; grid[20] = b[4];
            // Col I
            grid[1] = i[0]; grid[6] = i[1]; grid[11] = i[2]; grid[16] = i[3]; grid[21] = i[4];
            // Col N
            grid[2] = n[0]; grid[7] = n[1]; grid[12] = 0;    grid[17] = n[2]; grid[22] = n[3];
            // Col G
            grid[3] = g[0]; grid[8] = g[1]; grid[13] = g[2]; grid[18] = g[3]; grid[23] = g[4];
            // Col O
            grid[4] = o[0]; grid[9] = o[1]; grid[14] = o[2]; grid[19] = o[3]; grid[24] = o[4];

            string gridString = string.Join(",", grid);
            
            return new Cartela
            {
                BingoId = bingoId,
                NumeroCartelaNoKit = numeroCartelaNoKit,
                GridNumeros = gridString,
                HashUnico = gridString
            };
        }

        private int[] GerarColuna(int min, int max, int count)
        {
            var numeros = new HashSet<int>();
            while (numeros.Count < count)
            {
                numeros.Add(_random.Next(min, max + 1));
            }
            // Returning random order
            return numeros.ToArray();
        }
    }
}
