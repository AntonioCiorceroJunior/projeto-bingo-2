using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace BingoAdmin.UI.Services
{
    public class FeedMessage : INotifyPropertyChanged
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Type { get; set; } = "Info"; // "Info", "Success", "Warning", "Error"
        public string FormattedTime => Timestamp.ToString("HH:mm:ss");

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public class FeedService
    {
        // History: Key = BingoId, Value = List of messages
        private readonly Dictionary<int, List<FeedMessage>> _history = new();
        private int _currentBingoId = -1;

        public ObservableCollection<FeedMessage> Messages { get; } = new ObservableCollection<FeedMessage>();

        public void ClearCurrentView()
        {
            SafeInvoke(() => Messages.Clear());
        }

        public void ReloadHistory()
        {
            if (_currentBingoId != -1 && _history.ContainsKey(_currentBingoId))
            {
                SafeInvoke(() =>
                {
                    Messages.Clear();
                    // Re-add in correct order (newest first)
                    // Assuming history is stored newest-first
                    foreach (var msg in _history[_currentBingoId])
                    {
                        Messages.Add(msg);
                    }
                });
            }
        }

        public void SwitchBingoContext(int bingoId, string bingoName = "")
        {
            if (_currentBingoId == bingoId) return;

            _currentBingoId = bingoId;
            
            SafeInvoke(() =>
            {
                Messages.Clear();

                if (!_history.ContainsKey(bingoId))
                {
                    _history[bingoId] = new List<FeedMessage>();
                    string name = !string.IsNullOrEmpty(bingoName) ? bingoName : $"Bingo {bingoId}";
                    // Don't add directly here to avoid infinite loop or context issues, call internal helper if needed
                    // But AddMessage handles _currentBingoId check
                }

                // Restore history
                foreach (var msg in _history[bingoId])
                {
                    Messages.Add(msg);
                }
                
                if (Messages.Count == 0 && !string.IsNullOrEmpty(bingoName))
                {
                     AddMessage("Sistema", $"Feed conectado ao {bingoName}", "Info");
                }
            });
        }

        public void AddMessage(string title, string message, string type = "Info")
        {
            SafeInvoke(() =>
            {
                var msg = new FeedMessage
                {
                    Title = title,
                    Message = message,
                    Timestamp = DateTime.Now,
                    Type = type
                };

                // Add to View
                Messages.Insert(0, msg);

                // Trim View
                while (Messages.Count > 50)
                {
                    Messages.RemoveAt(Messages.Count - 1);
                }

                // Add to History (if context is active)
                if (_currentBingoId != -1)
                {
                    if (!_history.ContainsKey(_currentBingoId))
                    {
                        _history[_currentBingoId] = new List<FeedMessage>();
                    }

                    _history[_currentBingoId].Insert(0, msg);

                    // Trim History
                    if (_history[_currentBingoId].Count > 100)
                    {
                        _history[_currentBingoId].RemoveAt(_history[_currentBingoId].Count - 1);
                    }
                }
            });
        }

        private void SafeInvoke(Action action)
        {
            if (System.Windows.Application.Current != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(action);
            }
        }

        public void AddSeparator()
        {
            if (System.Windows.Application.Current != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var msg = new FeedMessage
                    {
                        Title = "",
                        Message = "",
                        Type = "Separator",
                        Timestamp = DateTime.Now
                    };
                    Messages.Insert(0, msg);
                    
                    // Update History
                    if (_currentBingoId != -1)
                    {
                        if (!_history.ContainsKey(_currentBingoId))
                        {
                            _history[_currentBingoId] = new List<FeedMessage>();
                        }
                        _history[_currentBingoId].Insert(0, msg);
                    }
                });
            }
        }
    }
}
