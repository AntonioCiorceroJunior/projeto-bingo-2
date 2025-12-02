using Microsoft.EntityFrameworkCore;
using BingoAdmin.Domain.Entities;

namespace BingoAdmin.Infra.Data
{
    public class BingoContext : DbContext
    {
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Bingo> Bingos { get; set; }
        public DbSet<Rodada> Rodadas { get; set; }
        public DbSet<Padrao> Padroes { get; set; }
        public DbSet<Combo> Combos { get; set; }
        public DbSet<Cartela> Cartelas { get; set; }
        public DbSet<Sorteio> Sorteios { get; set; }
        public DbSet<Ganhador> Ganhadores { get; set; }
        public DbSet<PedraMaiorSorteio> PedraMaiorSorteios { get; set; }
        public DbSet<DesempateItem> DesempateItens { get; set; }
        public DbSet<Despesa> Despesas { get; set; }
        public DbSet<BingoPadrao> BingoPadroes { get; set; }
        public DbSet<RodadaPadrao> RodadaPadroes { get; set; }

        public BingoContext() { }

        public BingoContext(DbContextOptions<BingoContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=bingoadmin.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
