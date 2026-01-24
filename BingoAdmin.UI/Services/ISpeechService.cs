namespace BingoAdmin.UI.Services
{
    public interface ISpeechService
    {
        bool IsEnabled { get; set; }
        void Speak(string text);
        void SpeakBall(string letter, int number);
        Task SpeakAsync(string text); // Changed to Task and specific method
        void SpeakWinner(string winnerName, string comboNumero, string cartelaNumero); // Updated signature
        void AnnouncePrize(string prizeName);
        void SetVolume(int volume);
        void SetRate(int rate);
        void UpdateDrawInterval(double seconds);
        void Stop();
    }
}
