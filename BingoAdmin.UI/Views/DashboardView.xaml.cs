using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using BingoAdmin.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BingoAdmin.UI.Views
{
    public partial class DashboardView : Page
    {
        private readonly FeedService _feedService;
        private readonly GameStatusService _gameStatusService;
        private readonly UserSession _userSession;

        public ObservableCollection<FeedMessage> FeedMessages => _feedService.Messages;
        public GameStatusService GameStatus => _gameStatusService;

        public DashboardView(FeedService feedService, UserSession userSession)
        {
            try
            {
                InitializeComponent();
            }
            catch (System.Exception ex)
            {
                System.IO.File.AppendAllText("startup_log.txt", $"{System.DateTime.Now:HH:mm:ss} - CRASH IN DASHBOARD INITIALIZE_COMPONENT: {ex}\nInner: {ex.InnerException}\n");
                throw;
            }
            _feedService = feedService;
            _userSession = userSession;
            _gameStatusService = ((App)Application.Current).Host.Services.GetRequiredService<GameStatusService>();
            
            DataContext = this;
            
            _userSession.OnSessionChanged += UpdateSessionState;

            UpdateSessionState();
        }

        private void UpdateSessionState()
        {
            // Welcome Message & Banner
            var userName = _userSession.CurrentUser?.Nome ?? "Usuário";
            if (WelcomeText != null) WelcomeText.Text = $"Olá, {userName}";
            
            if (_userSession.IsImpersonating)
            {
                ImpersonationBanner.Visibility = Visibility.Visible;
                ImpersonationText.Text = $"Visualizando como: {userName}";
                
                // Hide admin tab when impersonating (to see what user sees)
                AdminTab.Visibility = Visibility.Collapsed;
                
                // Switch to first tab to prevent being stuck in hidden tab
                if (MainTabControl.SelectedItem == AdminTab)
                {
                    MainTabControl.SelectedIndex = 0;
                }
            }
            else
            {
                ImpersonationBanner.Visibility = Visibility.Collapsed;
                
                // Show admin tab if really admin
                // Use IsRealAdmin if available, or fallback to IsAdmin logic
                 if (_userSession.IsRealAdmin) 
                 {
                     AdminTab.Visibility = Visibility.Visible;
                 }
                 else
                 {
                     AdminTab.Visibility = Visibility.Collapsed;
                 }
            }
        }
        
        private void Logout_Click(object sender, RoutedEventArgs e)
        {
             if (MessageBox.Show("Deseja realmente sair?", "Sair", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
             {
                 _userSession.CurrentUser = null;
                 // Stop impersonating if active
                 if (_userSession.IsImpersonating) _userSession.StopImpersonation();

                 // Navigate back to Login
                 var loginView = ((App)Application.Current).Host.Services.GetRequiredService<LoginView>();
                 NavigationService.Navigate(loginView);
             }
        }

        private void ExitImpersonation_Click(object sender, RoutedEventArgs e)
        {
            _userSession.StopImpersonation();
            
            var bingoContextService = ((App)Application.Current).Host.Services.GetRequiredService<BingoContextService>();
            bingoContextService.NotifyBingoListUpdated();
            
            MessageBox.Show("Modo de visualização encerrado.", "Sistema", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnClearFeed_Click(object sender, RoutedEventArgs e)
        {
            _feedService.ClearCurrentView();
        }

        private void BtnReloadHistory_Click(object sender, RoutedEventArgs e)
        {
            _feedService.ReloadHistory();
        }

        private void FeedItem_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Grid grid && grid.DataContext is FeedMessage msg)
            {
                msg.IsExpanded = !msg.IsExpanded;
            }
        }
    }
}
