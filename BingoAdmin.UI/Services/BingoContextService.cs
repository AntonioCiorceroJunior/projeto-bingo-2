using System;

namespace BingoAdmin.UI.Services
{
    public class BingoContextService
    {
        public event Action<int> OnBingoChanged;
        public int CurrentBingoId { get; private set; } = -1;

        public void SetCurrentBingo(int bingoId)
        {
            if (CurrentBingoId != bingoId)
            {
                CurrentBingoId = bingoId;
                OnBingoChanged?.Invoke(bingoId);
            }
        }
    }
}
