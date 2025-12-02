using System;
using System.IO;

namespace BingoAdmin.UI.Services
{
    public class BingoContextService
    {
        public event Action<int> OnBingoChanged;
        public event Action OnBingoListUpdated; // New event for list synchronization
        public event Action OnRodadaReiniciada; // Evento global de reinício de rodada
        public int CurrentBingoId { get; private set; } = -1;
        private const string ConfigFile = "last_bingo.cfg";

        public BingoContextService()
        {
            LoadState();
        }

        public void NotifyRodadaReiniciada()
        {
            OnRodadaReiniciada?.Invoke();
        }

        public void NotifyBingoListUpdated()
        {
            OnBingoListUpdated?.Invoke();
        }

        public void SetCurrentBingo(int bingoId)
        {
            if (CurrentBingoId != bingoId)
            {
                CurrentBingoId = bingoId;
                SaveState(bingoId);
                OnBingoChanged?.Invoke(bingoId);
            }
        }

        private void SaveState(int bingoId)
        {
            try
            {
                File.WriteAllText(ConfigFile, bingoId.ToString());
            }
            catch { /* Ignore errors */ }
        }

        private void LoadState()
        {
            try
            {
                if (File.Exists(ConfigFile))
                {
                    var content = File.ReadAllText(ConfigFile);
                    if (int.TryParse(content, out int id))
                    {
                        CurrentBingoId = id;
                    }
                }
            }
            catch { /* Ignore errors */ }
        }
    }
}
