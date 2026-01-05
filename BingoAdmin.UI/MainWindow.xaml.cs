using System;
using System.IO;
using System.Text.Json;
using System.Windows;
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
            
            this.Loaded += OnLoaded;
            this.Closing += OnClosing;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
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

                        // View restoration disabled to enforce login
                        /*
                        if (!string.IsNullOrEmpty(state.LastView))
                        {
                            if (state.LastView == nameof(GameView))
                            {
                                var view = _serviceProvider.GetRequiredService<GameView>();
                                MainFrame.Navigate(view);
                                restored = true;
                            }
                            else if (state.LastView == nameof(DashboardView))
                            {
                                var view = _serviceProvider.GetRequiredService<DashboardView>();
                                MainFrame.Navigate(view);
                                restored = true;
                            }
                        }
                        */
                    }
                }
                catch { }
            }

            // Always navigate to LoginView on startup
            var loginView = _serviceProvider.GetRequiredService<LoginView>();
            MainFrame.Navigate(loginView);
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var state = new UiState
            {
                Top = this.Top,
                Left = this.Left,
                Width = this.Width,
                Height = this.Height,
                WindowState = this.WindowState,
                LastView = MainFrame.Content?.GetType().Name ?? ""
            };

            try
            {
                var json = JsonSerializer.Serialize(state);
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
    }
}