namespace BingoAdmin.UI.Services
{
    public interface ISpeechService
    {
        bool IsEnabled { get; set; }
        void Speak(string text);
        void SpeakBall(string letter, int number);
        void SpeakWinner(string winnerName);
        void SetVolume(int volume);
        void SetRate(int rate);
        void UpdateDrawInterval(double seconds);
        void Stop();
    }
}
