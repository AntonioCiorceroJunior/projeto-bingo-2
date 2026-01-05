using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using BingoAdmin.Infra.Data;
using BingoAdmin.Domain.Services;
using BingoAdmin.UI.Services; // Added this
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace BingoAdmin.UI
{
    public partial class App : Application
    {
        public IHost Host { get; private set; }
        public IConfiguration Configuration { get; private set; }

        public App()
        {
            // Global exception handling
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var builder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            Configuration = builder.Build();

            // Debug Configuration
            // var providerDebug = Configuration["DatabaseProvider"];
            // MessageBox.Show($"Base Path: {basePath}\nProvider: {providerDebug}", "Debug Config");

            Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Register DbContext
                    services.AddDbContext<BingoContext>(options => 
                    {
                        var provider = Configuration["DatabaseProvider"] ?? "Sqlite";
                        var connectionString = Configuration.GetConnectionString(provider == "SqlServer" ? "AzureConnection" : "DefaultConnection");

                        if (provider == "SqlServer")
                        {
                            options.UseSqlServer(connectionString);
                        }
                        else
                        {
                            options.UseSqlite(connectionString ?? "Data Source=bingoadmin.db");
                        }
                    }, ServiceLifetime.Transient);

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
                    services.AddTransient<EmailService>();
                    services.AddTransient<PaymentService>();
                    
                    // Global Services
                    services.AddSingleton<FeedService>();
                    services.AddSingleton<BingoContextService>();
                    services.AddSingleton<GameStatusService>();
                    services.AddSingleton<UserSession>();
                    services.AddSingleton<ISpeechService, SpeechService>();

                    // Views
                    services.AddSingleton<MainWindow>();
                    services.AddTransient<Views.LoginView>();
                    services.AddTransient<Views.DashboardView>();
                    services.AddTransient<Views.UsuariosView>();
                    services.AddTransient<Views.FinanceiroView>();
                    services.AddTransient<Views.ResultadosView>();
                    services.AddTransient<Views.MiniGamesView>();
                })
                .Build();
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Erro não tratado (Dispatcher): {e.Exception.Message}\n\n{e.Exception.StackTrace}", "Erro Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                MessageBox.Show($"Erro não tratado (Domain): {ex.Message}\n\n{ex.StackTrace}", "Erro Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

                    // Ensure Admin User Exists
                    var usuarioService = scope.ServiceProvider.GetRequiredService<UsuarioService>();
                    usuarioService.EnsureAdminUser();

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

