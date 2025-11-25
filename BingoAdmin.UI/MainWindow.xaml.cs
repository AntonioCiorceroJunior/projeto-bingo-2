using System.Windows;
using BingoAdmin.UI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace BingoAdmin.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            // Resolve LoginView from DI
            // var loginView = serviceProvider.GetRequiredService<LoginView>();
            // MainFrame.Navigate(loginView);

            // Bypass login for testing
            var dashboardView = serviceProvider.GetRequiredService<DashboardView>();
            MainFrame.Navigate(dashboardView);
        }
    }
}