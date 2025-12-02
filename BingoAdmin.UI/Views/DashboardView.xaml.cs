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

        public ObservableCollection<FeedMessage> FeedMessages => _feedService.Messages;
        public GameStatusService GameStatus => _gameStatusService;

        public DashboardView(FeedService feedService)
        {
            InitializeComponent();
            _feedService = feedService;
            _gameStatusService = ((App)Application.Current).Host.Services.GetRequiredService<GameStatusService>();
            
            DataContext = this;

            // Add a welcome message
            _feedService.AddMessage("Sistema", "Bem-vindo ao Bingo Admin 2.0", "Info");
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
