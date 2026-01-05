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
            InitializeComponent();
            _feedService = feedService;
            _userSession = userSession;
            _gameStatusService = ((App)Application.Current).Host.Services.GetRequiredService<GameStatusService>();
            
            DataContext = this;

            // Welcome Message
            var userName = _userSession.CurrentUser?.Nome ?? "Usuário";
            _feedService.AddMessage("Sistema", $"Bem-vindo, {userName}!", "Info");

            // Toggle Admin Tab
            if (_userSession.IsAdmin)
            {
                AdminTab.Visibility = Visibility.Visible;
            }
            else
            {
                AdminTab.Visibility = Visibility.Collapsed;
            }
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
