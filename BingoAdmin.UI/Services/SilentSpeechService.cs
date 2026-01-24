using System.Threading.Tasks;

namespace BingoAdmin.UI.Services
{
    public class SilentSpeechService : ISpeechService
    {
        public bool IsEnabled { get; set; } = false;

        public void Speak(string text) { }
        public void SpeakBall(string letter, int number) { }
        public Task SpeakAsync(string text) => Task.CompletedTask;
        public void SpeakWinner(string winnerName, string comboNumero, string cartelaNumero) { }
        public void AnnouncePrize(string prizeName) { }
        public void SetVolume(int volume) { }
        public void SetRate(int rate) { }
        public void UpdateDrawInterval(double seconds) { }
        public void Stop() { }
    }
}
