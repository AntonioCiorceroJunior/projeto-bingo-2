using System.Collections.Generic;
using System.Linq;
using BingoAdmin.Domain.Entities;
using BingoAdmin.Infra.Data;

namespace BingoAdmin.UI.Services
{
    public class PadraoService
    {
        private readonly BingoContext _context;

        public PadraoService(BingoContext context)
        {
            _context = context;
        }

        public void SeedPadroesIniciais()
        {
            var padroesIniciais = new List<Padrao>
            {
                new Padrao { Nome = "Cartela Cheia", Mascara = "1111111111111111111111111", IsPredefinido = true },
                new Padrao { Nome = "Linha Horizontal (Topo)", Mascara = "1111100000000000000000000", IsPredefinido = true },
                new Padrao { Nome = "Linha Horizontal (Meio)", Mascara = "0000000000111110000000000", IsPredefinido = true },
                new Padrao { Nome = "Linha Horizontal (Base)", Mascara = "0000000000000000000011111", IsPredefinido = true },
                new Padrao { Nome = "Linha Vertical (B)", Mascara = "1000010000100001000010000", IsPredefinido = true },
                new Padrao { Nome = "Linha Vertical (I)", Mascara = "0100001000010000100001000", IsPredefinido = true },
                new Padrao { Nome = "Linha Vertical (N)", Mascara = "0010000100001000010000100", IsPredefinido = true },
                new Padrao { Nome = "Linha Vertical (G)", Mascara = "0001000010000100001000010", IsPredefinido = true },
                new Padrao { Nome = "Linha Vertical (O)", Mascara = "0000100001000010000100001", IsPredefinido = true },
                new Padrao { Nome = "Diagonal Principal", Mascara = "1000001000001000001000001", IsPredefinido = true },
                new Padrao { Nome = "Diagonal Secundária", Mascara = "0000100010001000100010000", IsPredefinido = true },
                new Padrao { Nome = "4 Cantos", Mascara = "1000100000000000000010001", IsPredefinido = true },
                new Padrao { Nome = "X", Mascara = "1000101010001000101010001", IsPredefinido = true },
                new Padrao { Nome = "Cruz (Mais)", Mascara = "0010000100111110010000100", IsPredefinido = true },
                new Padrao { Nome = "Moldura Grande", Mascara = "1111110001100011000111111", IsPredefinido = true },
                new Padrao { Nome = "5 na Louca", Mascara = "RANDOM:5", IsPredefinido = true },
                new Padrao { Nome = "7 na Louca", Mascara = "RANDOM:7", IsPredefinido = true },
                new Padrao { Nome = "10 na Louca", Mascara = "RANDOM:10", IsPredefinido = true },
                new Padrao { Nome = "Pé Frio", Mascara = "1111111111111111111111111", IsPredefinido = true }
            };

            foreach (var p in padroesIniciais)
            {
                if (!_context.Padroes.Any(x => x.Nome == p.Nome))
                {
                    _context.Padroes.Add(p);
                }
            }
            
            _context.SaveChanges();
        }

        public List<Padrao> GetPadroes()
        {
            return _context.Padroes.OrderBy(p => p.Nome).ToList();
        }

        public void SalvarPadrao(string nome, string mascara)
        {
            var padrao = new Padrao
            {
                Nome = nome,
                Mascara = mascara,
                IsPredefinido = false
            };
            _context.Padroes.Add(padrao);
            _context.SaveChanges();
        }

        public void ExcluirPadrao(int id)
        {
            var padrao = _context.Padroes.Find(id);
            if (padrao != null && !padrao.IsPredefinido)
            {
                _context.Padroes.Remove(padrao);
                _context.SaveChanges();
            }
        }

        public List<Padrao> ListarTodos()
        {
            return _context.Padroes.ToList();
        }
    }
}
