using System.Speech.Synthesis;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace BingoAdmin.UI.Services
{
    public class SpeechService : ISpeechService, IDisposable
    {
        [DllImport("winmm.dll")]
        private static extern long mciSendString(string strCommand, StringBuilder strReturn, int iReturnLength, IntPtr hwndCallback);

        private readonly SpeechSynthesizer _synthesizer;
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly string _tempAudioPath;
        private bool _isEnabled = true;
        private double _currentInterval = 4.0;
        private int _mciVolume = 1000; // 0 to 1000

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                if (!_isEnabled)
                {
                    Stop();
                }
            }
        }

        public SpeechService()
        {
            _synthesizer = new SpeechSynthesizer();
            ConfigureVoice();
            _synthesizer.Volume = 100;
            _synthesizer.Rate = 4;
            
            _tempAudioPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TempAudio");
            Directory.CreateDirectory(_tempAudioPath);
        }

        private void ConfigureVoice()
        {
            // Try to find a Portuguese voice
            var voices = _synthesizer.GetInstalledVoices();
            var portugueseVoice = voices.FirstOrDefault(v => v.VoiceInfo.Culture.Name.StartsWith("pt"));

            if (portugueseVoice != null)
            {
                _synthesizer.SelectVoice(portugueseVoice.VoiceInfo.Name);
            }
        }

        public void Speak(string text)
        {
            if (!IsEnabled || string.IsNullOrWhiteSpace(text)) return;
            StopAllAudio();
            PlayGoogleTts(text);
        }

        public void SpeakBall(string letter, int number)
        {
            if (!IsEnabled) return;
            
            // Close any previous instance to ensure we can open the new one
            mciSendString("close bingoAudio", null, 0, IntPtr.Zero);

            // Try to play audio file first (e.g., "Audios/B5.mp3")
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string audioPath = Path.Combine(baseDir, "Audios", $"{letter}{number}.mp3");
            
            if (File.Exists(audioPath))
            {
                PlayFile(audioPath);
                return;
            }
            
            // Speak Letter and Number (e.g., "B 5")
            string textToSpeak = $"{letter} {number}";
            if (letter.Equals("O", StringComparison.OrdinalIgnoreCase))
            {
                textToSpeak = $"Ó {number}";
            }
            PlayGoogleTts(textToSpeak);
        }

        public void SpeakWinner(string winnerName)
        {
            if (!IsEnabled) return;
            StopAllAudio();
            PlayGoogleTts($"Bingo! Parabéns {winnerName}!");
        }

        private async void PlayGoogleTts(string text)
        {
            try
            {
                string hash = GetMd5Hash(text);
                string filename = $"tts_{hash}.mp3";
                string filePath = Path.Combine(_tempAudioPath, filename);

                if (!File.Exists(filePath))
                {
                    string url = $"https://translate.google.com/translate_tts?ie=UTF-8&client=tw-ob&tl=pt-BR&q={Uri.EscapeDataString(text)}";
                    var data = await _httpClient.GetByteArrayAsync(url);
                    await File.WriteAllBytesAsync(filePath, data);
                }

                PlayFile(filePath);
            }
            catch
            {
                // Fallback to Windows TTS if Google fails (offline, etc)
                _synthesizer.SpeakAsync(text);
            }
        }

        private void PlayFile(string path)
        {
             mciSendString("close bingoAudio", null, 0, IntPtr.Zero);
             string commandOpen = $"open \"{path}\" type mpegvideo alias bingoAudio";
             mciSendString(commandOpen, null, 0, IntPtr.Zero);
             
             string commandVolume = $"setaudio bingoAudio volume to {_mciVolume}";
             mciSendString(commandVolume, null, 0, IntPtr.Zero);

             mciSendString("play bingoAudio", null, 0, IntPtr.Zero);
        }

        private string GetMd5Hash(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        private void StopAllAudio()
        {
            _synthesizer.SpeakAsyncCancelAll();
            try 
            { 
                mciSendString("close bingoAudio", null, 0, IntPtr.Zero);
            } 
            catch { }
        }

        public void SetVolume(int volume)
        {
            int v = Math.Clamp(volume, 0, 100);
            _synthesizer.Volume = v;
            _mciVolume = v * 10; // Map 0-100 to 0-1000
        }

        public void SetRate(int rate)
        {
            _synthesizer.Rate = Math.Clamp(rate, -10, 10);
        }

        public void UpdateDrawInterval(double seconds)
        {
            _currentInterval = seconds;
        }

        public void Stop()
        {
            StopAllAudio();
        }

        public void Dispose()
        {
            _synthesizer?.Dispose();
            mciSendString("close bingoAudio", null, 0, IntPtr.Zero);
        }
    }
}
