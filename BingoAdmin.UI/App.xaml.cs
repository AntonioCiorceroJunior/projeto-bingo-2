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
            Log("App constructor started.");
            try 
            {
                // Global exception handling
                this.DispatcherUnhandledException += App_DispatcherUnhandledException;
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

                Log("Global handlers registered.");

                QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

                var basePath = AppDomain.CurrentDomain.BaseDirectory;
                Log($"Base Path: {basePath}");

                var builder = new ConfigurationBuilder()
                    .SetBasePath(basePath)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                
                Configuration = builder.Build();
                Log("Configuration built successfully.");

                Log("Building Host...");
                Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                    .ConfigureServices((context, services) =>
                    {
                        Log("Configuring Services...");
                        // Register DbContext
                        services.AddDbContext<BingoContext>(options => 
                        {
                            try
                            {
                                var provider = Configuration["DatabaseProvider"] ?? "Sqlite";
                                var connectionString = Configuration.GetConnectionString(provider == "SqlServer" ? "AzureConnection" : "DefaultConnection");
                                Log($"Configuring DB Provider: {provider}");

                                // FORCE SQLITE FOR DEBUGGING IF SQLSERVER FAILS
                                // provider = "Sqlite"; 
                                // connectionString = Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=bingoadmin.db";
                                // Log("FORCING SQLITE FOR DEBUGGING");

                                if (provider == "SqlServer")
                                {
                                    Log("Calling UseSqlServer...");
                                    options.UseSqlServer(connectionString);
                                    Log("UseSqlServer returned.");
                                }
                                else
                                {
                                    options.UseSqlite(connectionString ?? "Data Source=bingoadmin.db");
                                }
                                Log("DbContext configuration lambda finished.");
                            }
                            catch (Exception ex)
                            {
                                Log($"Error inside AddDbContext: {ex.Message}");
                                throw;
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
                        services.AddTransient<FraseService>();
                        
                        // Global Services
                        services.AddSingleton<FeedService>();
                        services.AddSingleton<BingoContextService>();
                        services.AddSingleton<GameStatusService>();
                        services.AddSingleton<UserSession>();
                        services.AddSingleton<ISpeechService, SpeechService>();
                        // services.AddSingleton<ISpeechService, SilentSpeechService>(); // Debugging

                        // Views
                        services.AddSingleton<MainWindow>();
                        services.AddTransient<Views.LoginView>();
                        services.AddTransient<Views.DashboardView>();
                        services.AddTransient<Views.UsuariosView>();
                        services.AddTransient<Views.FinanceiroView>();
                        services.AddTransient<Views.ResultadosView>();
                        services.AddTransient<Views.MiniGamesView>();
                        Log("Services configured.");
                    })
                    .Build();
                Log("Host built successfully.");
            }
            catch (Exception ex)
            {
                Log($"CRITICAL ERROR IN CONSTRUCTOR: {ex}");
                MessageBox.Show($"Erro Fatal no Construtor: {ex.Message}");
                throw;
            }
        }

        private static void Log(string message)
        {
            try
            {
                File.AppendAllText("startup_log.txt", $"{DateTime.Now:HH:mm:ss} - {message}{Environment.NewLine}");
            }
            catch { }
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log($"DispatcherUnhandledException: {e.Exception}");
            var ex = e.Exception;
            string msg = $"{ex.Message}";
            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
                msg += $"\n ---> {ex.Message}";
            }
            MessageBox.Show($"Erro não tratado (Dispatcher):\n\n{msg}\n\nStack:\n{e.Exception.StackTrace}", "Erro Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                Log($"CurrentDomain_UnhandledException: {ex}");
                string msg = $"{ex.Message}";
                var inner = ex;
                while (inner.InnerException != null)
                {
                    inner = inner.InnerException;
                    msg += $"\n ---> {inner.Message}";
                }
                MessageBox.Show($"Erro não tratado (Domain):\n\n{msg}\n\nStack:\n{ex.StackTrace}", "Erro Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                Log("Application_Startup executing...");
                await Host.StartAsync();
                Log("Host started.");

                // Seed Admin User
                using (var scope = Host.Services.CreateScope())
                {
                    Log("Scope created. Resolving BingoContext...");
                    var context = scope.ServiceProvider.GetRequiredService<BingoContext>();
                    
                    Log("Applying Migrations...");
                    if (context.Database.IsSqlServer())
                    {
                        context.Database.Migrate(); 
                        Log("Migrations applied.");
                    }
                    else
                    {
                        // Ensure DB exists first
                        bool created = context.Database.EnsureCreated();
                        Log($"Database ensured (SQLite). Created? {created}");
                        
                        // HOTFIX: Ensure 'PadraoId' column exists in 'Premios' table
                        // This handles the case where migration didn't run properly on existing DB
                        try
                        {
                            context.Database.ExecuteSqlRaw("ALTER TABLE Premios ADD COLUMN PadraoId INTEGER NULL;");
                            Log("HOTFIX: Added PadraoId column to Premios.");
                        }
                        catch 
                        { 
                            // Ignore if column already exists
                            Log("HOTFIX: PadraoId column likely already exists.");
                        }

                        // Hotfix 2: Index
                        try
                        {
                            context.Database.ExecuteSqlRaw("CREATE INDEX IX_Premios_PadraoId ON Premios (PadraoId);");
                        }
                        catch { /* Ignore */ }
                    }

                    // Ensure Admin User Exists
                    Log("Resolving UsuarioService...");
                    var usuarioService = scope.ServiceProvider.GetRequiredService<UsuarioService>();
                    
                    Log("Ensuring Admin User...");
                    usuarioService.EnsureAdminUser();

                    context.SaveChanges();

                    Log("Resolving PadraoService...");
                    var padraoService = scope.ServiceProvider.GetRequiredService<PadraoService>();
                    padraoService.SeedPadroesIniciais();
                    Log("Seed data completed.");
                }

                Log("Resolving MainWindow...");
                var mainWindow = Host.Services.GetRequiredService<MainWindow>();
                Log("Showing MainWindow...");
                mainWindow.Show();
            }
            catch (System.Exception ex)
            {
                Log($"STARTUP FATAL ERROR: {ex}");
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

