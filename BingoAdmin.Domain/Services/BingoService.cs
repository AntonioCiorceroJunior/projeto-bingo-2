using System;
using System.Collections.Generic;
using System.Linq;
using BingoAdmin.Domain.Entities;

namespace BingoAdmin.Domain.Services
{
    public class BingoService
    {
        private readonly Random _random = new Random();

        public List<Combo> GerarCombos(int bingoId, int quantidadeCombos, int cartelasPorCombo)
        {
            var combos = new List<Combo>();
            var hashesExistentes = new HashSet<string>();

            for (int i = 1; i <= quantidadeCombos; i++)
            {
                var combo = new Combo
                {
                    BingoId = bingoId,
                    NumeroCombo = i,
                    Status = "Disponivel",
                    Cartelas = new List<Cartela>()
                };

                for (int j = 1; j <= cartelasPorCombo; j++)
                {
                    Cartela cartela;
                    do
                    {
                        cartela = GerarUmaCartela(bingoId, 0, j); // ComboId will be set by EF when adding to list
                    } while (hashesExistentes.Contains(cartela.HashUnico));

                    hashesExistentes.Add(cartela.HashUnico);
                    cartela.NumeroCartelaNoCombo = j;
                    combo.Cartelas.Add(cartela);
                }
                combos.Add(combo);
            }

            return combos;
        }

        private Cartela GerarUmaCartela(int bingoId, int comboId, int numeroCartela)
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
                ComboId = comboId,
                NumeroCartelaNoCombo = numeroCartela,
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
