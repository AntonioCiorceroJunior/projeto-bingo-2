using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BingoAdmin.Infra.Data;
using BingoAdmin.Domain.Services;
using BingoAdmin.UI.Services; // Added this
using Microsoft.EntityFrameworkCore;

namespace BingoAdmin.UI
{
    public partial class App : Application
    {
        public IHost Host { get; private set; }

        public App()
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Register DbContext as Transient to avoid Scope validation errors with Singletons
                    // and to ensure thread safety if services are used in parallel.
                    services.AddDbContext<BingoContext>(options => {}, ServiceLifetime.Transient);

                    // Services
                    services.AddTransient<BingoService>();
                    services.AddTransient<UsuarioService>();
                    services.AddTransient<BingoManagementService>();
                    services.AddTransient<ComboService>();
                    services.AddTransient<PdfService>();
                    services.AddTransient<PadraoService>();
                    services.AddTransient<RodadaService>();
                    services.AddTransient<GameService>();
                    services.AddTransient<DesempateService>();
                    services.AddTransient<RelatorioService>();
                    services.AddTransient<FinanceiroService>();
                    
                    // Global Services
                    services.AddSingleton<FeedService>();
                    services.AddSingleton<BingoContextService>();
                    services.AddSingleton<GameStatusService>();

                    // Views
                    services.AddSingleton<MainWindow>();
                    services.AddTransient<Views.LoginView>();
                    services.AddTransient<Views.DashboardView>();
                    services.AddTransient<Views.FinanceiroView>();
                    services.AddTransient<Views.ResultadosView>();
                    services.AddTransient<Views.MiniGamesView>();
                })
                .Build();
        }

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                await Host.StartAsync();

                // Seed Admin User
                using (var scope = Host.Services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<BingoContext>();
                    context.Database.Migrate(); // Aplica as migrações pendentes automaticamente

                    // Ensure ValorPorCombo column exists in Bingos table (manual schema update)
                    try 
                    {
                        context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Bingos"" ADD COLUMN ""ValorPorCombo"" TEXT NOT NULL DEFAULT '0';");
                    }
                    catch { /* Ignore if column already exists */ }

                    try
                    {
                        context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Bingos"" ADD COLUMN ""QuantidadeRodadas"" INTEGER NOT NULL DEFAULT 0;");
                    }
                    catch { /* Ignore if column already exists */ }

                    try
                    {
                        context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Rodadas"" ADD COLUMN ""ModoPadroesDinamicos"" INTEGER NOT NULL DEFAULT 0;");
                    }
                    catch { /* Ignore if column already exists */ }

                    try
                    {
                        context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Rodadas"" ADD COLUMN ""MaximoGanhadores"" INTEGER NULL;");
                    }
                    catch { /* Ignore if column already exists */ }

                    try
                    {
                        context.Database.ExecuteSqlRaw(@"ALTER TABLE ""Rodadas"" ADD COLUMN ""TipoJogo"" TEXT NOT NULL DEFAULT '';");
                    }
                    catch { /* Ignore if column already exists */ }

                    context.Database.ExecuteSqlRaw(@"
                        CREATE TABLE IF NOT EXISTS ""RodadaPadroes"" (
                            ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_RodadaPadroes"" PRIMARY KEY AUTOINCREMENT,
                            ""RodadaId"" INTEGER NOT NULL,
                            ""PadraoId"" INTEGER NOT NULL,
                            ""FoiSorteado"" INTEGER NOT NULL,
                            CONSTRAINT ""FK_RodadaPadroes_Rodadas_RodadaId"" FOREIGN KEY (""RodadaId"") REFERENCES ""Rodadas"" (""Id"") ON DELETE CASCADE,
                            CONSTRAINT ""FK_RodadaPadroes_Padroes_PadraoId"" FOREIGN KEY (""PadraoId"") REFERENCES ""Padroes"" (""Id"") ON DELETE CASCADE
                        );
                    ");

                    // Create DesempateItens table manually if not exists (since we can't run migrations easily)
                    // Drop table removed to persist data
                    // context.Database.ExecuteSqlRaw(@"DROP TABLE IF EXISTS ""DesempateItens"";");
                    
                    context.Database.ExecuteSqlRaw(@"
                        CREATE TABLE IF NOT EXISTS ""DesempateItens"" (
                            ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_DesempateItens"" PRIMARY KEY AUTOINCREMENT,
                            ""BingoId"" INTEGER NOT NULL DEFAULT 0,
                            ""RodadaId"" INTEGER NOT NULL,
                            ""CartelaId"" INTEGER NOT NULL,
                            ""Nome"" TEXT NOT NULL,
                            ""Combo"" INTEGER NOT NULL,
                            ""CartelaNumero"" INTEGER NOT NULL,
                            ""PedraMaior"" INTEGER NOT NULL,
                            ""IsVencedor"" INTEGER NOT NULL
                        );
                    ");

                    context.Database.ExecuteSqlRaw(@"
                        CREATE TABLE IF NOT EXISTS ""Despesas"" (
                            ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Despesas"" PRIMARY KEY AUTOINCREMENT,
                            ""BingoId"" INTEGER NOT NULL,
                            ""Descricao"" TEXT NOT NULL,
                            ""Valor"" TEXT NOT NULL,
                            ""Tipo"" TEXT NOT NULL
                        );
                    ");

                    // Garante que o usuário admin existe e a senha está correta
                    var adminUser = System.Linq.Enumerable.FirstOrDefault(context.Usuarios, u => u.Email == "admin");
                    if (adminUser == null)
                    {
                        adminUser = new BingoAdmin.Domain.Entities.Usuario
                        {
                            Nome = "Administrador",
                            Email = "admin",
                            SenhaHash = BCrypt.Net.BCrypt.HashPassword("admin")
                        };
                        context.Usuarios.Add(adminUser);
                    }
                    else
                    {
                        // Reseta a senha para garantir o acesso caso tenha sido alterada ou corrompida
                        adminUser.SenhaHash = BCrypt.Net.BCrypt.HashPassword("admin");
                    }
                    context.SaveChanges();

                    // Seed Padrões
                    var padraoService = scope.ServiceProvider.GetRequiredService<PadraoService>();
                    padraoService.SeedPadroesIniciais();
                }

                var mainWindow = Host.Services.GetRequiredService<MainWindow>();
                mainWindow.Show();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Erro fatal ao iniciar a aplicação: {ex.Message}\n\nDetalhes: {ex.InnerException?.Message}", "Erro de Inicialização", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private async void Application_Exit(object sender, ExitEventArgs e)
        {
            using (Host)
            {
                await Host.StopAsync();
            }
        }
    }
}

