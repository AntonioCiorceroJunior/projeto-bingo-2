using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using BingoAdmin.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace BingoAdmin.UI
{
    public partial class MainWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;
        private const string StateFile = "uistate.json";

        public MainWindow(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;

            // Desabilita a navegação por Backspace no Frame
            NavigationCommands.BrowseBack.InputGestures.Clear();
            NavigationCommands.BrowseForward.InputGestures.Clear();
            
            this.Loaded += OnLoaded;
            this.Closing += OnClosing;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                File.AppendAllText("startup_log.txt", $"{DateTime.Now:HH:mm:ss} - MainWindow_Loaded started.\n");
                
                bool restored = false;
                if (File.Exists(StateFile))
                {
                    try
                    {
                        var json = File.ReadAllText(StateFile);
                        var state = JsonSerializer.Deserialize<UiState>(json);
                        if (state != null)
                        {
                            // Restore Window Position
                            if (state.Width > 0 && state.Height > 0)
                            {
                                this.Top = state.Top;
                                this.Left = state.Left;
                                this.Width = state.Width;
                                this.Height = state.Height;
                                this.WindowState = state.WindowState;
                            }
                        }
                    }
                    catch { }
                }

                // Always navigate to LoginView on startup
                File.AppendAllText("startup_log.txt", $"{DateTime.Now:HH:mm:ss} - Resolving LoginView...\n");
                var loginView = _serviceProvider.GetRequiredService<LoginView>();
                
                File.AppendAllText("startup_log.txt", $"{DateTime.Now:HH:mm:ss} - Navigating to LoginView...\n");
                MainFrame.Navigate(loginView);
                File.AppendAllText("startup_log.txt", $"{DateTime.Now:HH:mm:ss} - Navigation command issued.\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText("startup_log.txt", $"{DateTime.Now:HH:mm:ss} - ERROR IN MAINWINDOW_LOADED: {ex}\n");
                MessageBox.Show($"FATAL ERROR IN LOADED: {ex.Message}\n{ex.InnerException?.Message}", "Error");
            }
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            UiState state = new UiState();

            // Try to load existing state to preserve credentials
            if (File.Exists(StateFile))
            {
                try
                {
                    var existingJson = File.ReadAllText(StateFile);
                    var existingState = JsonSerializer.Deserialize<UiState>(existingJson);
                    if (existingState != null)
                    {
                        state = existingState;
                    }
                }
                catch { }
            }

            // Update Window settings
            state.Top = this.Top;
            state.Left = this.Left;
            state.Width = this.Width;
            state.Height = this.Height;
            state.WindowState = this.WindowState;
            state.LastView = MainFrame.Content?.GetType().Name ?? "";

            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(state, options);
                File.WriteAllText(StateFile, json);
            }
            catch { }
        }
    }

    public class UiState
    {
        public double Top { get; set; }
        public double Left { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public WindowState WindowState { get; set; }
        public string LastView { get; set; } = "";
        
        // Remember Me credentials
        public bool RememberMe { get; set; }
        public string SavedEmail { get; set; } = "";
        public string SavedPassword { get; set; } = ""; // Simple storage as requested
    }
}