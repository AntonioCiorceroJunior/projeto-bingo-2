using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace BingoAdmin.UI.Services
{
    public class GameStatusService : INotifyPropertyChanged
    {
        private string _currentTimerText = "--";
        private double _currentTimerProgress = 0;
        private string _nextRoundTimerText = "--";
        private bool _isAutoDrawActive = false;

        public ObservableCollection<string> RecentBalls { get; } = new ObservableCollection<string>();

        public string CurrentTimerText
        {
            get => _currentTimerText;
            set
            {
                _currentTimerText = value;
                OnPropertyChanged(nameof(CurrentTimerText));
            }
        }

        public double CurrentTimerProgress
        {
            get => _currentTimerProgress;
            set
            {
                _currentTimerProgress = value;
                OnPropertyChanged(nameof(CurrentTimerProgress));
            }
        }

        public string NextRoundTimerText
        {
            get => _nextRoundTimerText;
            set
            {
                _nextRoundTimerText = value;
                OnPropertyChanged(nameof(NextRoundTimerText));
            }
        }

        public bool IsAutoDrawActive
        {
            get => _isAutoDrawActive;
            set
            {
                _isAutoDrawActive = value;
                OnPropertyChanged(nameof(IsAutoDrawActive));
            }
        }

        public void AddRecentBall(int number)
        {
            string letter = "";
            if (number <= 15) letter = "B";
            else if (number <= 30) letter = "I";
            else if (number <= 45) letter = "N";
            else if (number <= 60) letter = "G";
            else letter = "O";

            string formatted = $"{letter} | {number}";

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                RecentBalls.Insert(0, formatted);
                // Keep only last 20 items to avoid memory issues in long games
                if (RecentBalls.Count > 20)
                {
                    RecentBalls.RemoveAt(RecentBalls.Count - 1);
                }
            });
        }

        public void ClearRecentBalls()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                RecentBalls.Clear();
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
