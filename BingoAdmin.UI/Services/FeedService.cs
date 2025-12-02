using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace BingoAdmin.UI.Services
{
    public class FeedMessage : System.ComponentModel.INotifyPropertyChanged
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public string Type { get; set; } // "Info", "Success", "Warning", "Error"
        public string FormattedTime => Timestamp.ToString("HH:mm:ss");

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsExpanded)));
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }

    public class FeedService
    {
        private Dictionary<int, List<FeedMessage>> _history = new Dictionary<int, List<FeedMessage>>();
        private int _currentBingoId = -1;

        public ObservableCollection<FeedMessage> Messages { get; private set; } = new ObservableCollection<FeedMessage>();

        public void ClearCurrentView()
        {
            if (System.Windows.Application.Current != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Messages.Clear();
                });
            }
        }

        public void ReloadHistory()
        {
            if (_currentBingoId != -1 && _history.ContainsKey(_currentBingoId))
            {
                if (System.Windows.Application.Current != null)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        Messages.Clear();
                        foreach (var msg in _history[_currentBingoId])
                        {
                            Messages.Add(msg);
                        }
                    });
                }
            }
        }

        public void SwitchBingoContext(int bingoId, string bingoName = "")
        {
            if (_currentBingoId == bingoId) return;

            // Save current messages to history
            if (_currentBingoId != -1)
            {
                _history[_currentBingoId] = new List<FeedMessage>(Messages);
            }

            _currentBingoId = bingoId;
            
            // Clear current view
            // Use Dispatcher to be safe, though this method might be called from UI thread usually
            if (System.Windows.Application.Current != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Messages.Clear();

                    // Restore history if exists
                    if (_history.ContainsKey(bingoId))
                    {
                        foreach (var msg in _history[bingoId])
                        {
                            Messages.Add(msg);
                        }
                    }
                    else
                    {
                        // Initialize empty history for new bingo
                        _history[bingoId] = new List<FeedMessage>();
                        // Optional: Add a welcome message
                        string name = !string.IsNullOrEmpty(bingoName) ? bingoName : $"Bingo {bingoId}";
                        AddMessage("Sistema", $"Feed conectado ao {name}", "Info");
                    }
                });
            }
        }

        public void AddMessage(string title, string message, string type = "Info")
        {
            // Ensure UI updates happen on the UI thread if called from background
            if (System.Windows.Application.Current != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Messages.Insert(0, new FeedMessage
                    {
                        Title = title,
                        Message = message,
                        Timestamp = DateTime.Now,
                        Type = type
                    });

                    // Keep only last 50 messages
                    if (Messages.Count > 50)
                    {
                        Messages.RemoveAt(Messages.Count - 1);
                    }
                });
            }
        }
    }
}
