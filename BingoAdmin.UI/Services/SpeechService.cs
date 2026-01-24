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
            try 
            {
                _synthesizer = new SpeechSynthesizer();
                ConfigureVoice();
                _synthesizer.Volume = 100;
                _synthesizer.Rate = 4;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro ao inicializar TTS: {ex.Message}");
                _isEnabled = false;
                _synthesizer = null; // Mark as null so we verify before using
            }
            
            try
            {
                _tempAudioPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TempAudio");
                Directory.CreateDirectory(_tempAudioPath);
            }
            catch (Exception ex)
            {
                 System.Diagnostics.Debug.WriteLine($"Erro ao criar diretório temporário: {ex.Message}");
                 _isEnabled = false; // Disable if no IO access
            }
        }

        private void ConfigureVoice()
        {
            if (_synthesizer == null) return;
            try 
            {
                // Try to find a Portuguese voice
                var voices = _synthesizer.GetInstalledVoices();
                var portugueseVoice = voices.FirstOrDefault(v => v.VoiceInfo.Culture.Name.StartsWith("pt"));

                if (portugueseVoice != null)
                {
                    _synthesizer.SelectVoice(portugueseVoice.VoiceInfo.Name);
                }
            }
            catch { /* Ignore voice selection errors */ }
        }

        public void Speak(string text)
        {
            // Fire and forget wrapper
            _ = SpeakAsync(text);
        }

        public void SpeakBall(string letter, int number)
        {
             // Google TTS reads "N." as "Número". We substitute it for phonetic spelling.
             string spokenLetter = letter;
             string separator = ". "; // Default pause

             if (letter.Equals("N", StringComparison.OrdinalIgnoreCase))
             {
                 spokenLetter = "Eni";
                 separator = " "; // Remove pause for N
             }
             
             string text = $"{spokenLetter}{separator}{number}.";
             _ = SpeakAsync(text);
        }

        public async Task SpeakAsync(string text)
        {
            if (!IsEnabled || string.IsNullOrWhiteSpace(text)) return;
            StopAllAudio();
            
            // We need to wait for this to finish if possible, but PlayGoogleTts is fire-and-forget regarding 'playback' duration in current implementation.
            // However, we can use Synthesizer for blocking speech or try to calculate duration.
            // The best way for "pausing game" is if we can await the completion.
            // Getting duration from MP3 or TTS is tricky without external libs.
            // For now, let's use the Synthesizer which has events, OR just accept that we fire it.
            // But the user requested PAUSE. So we must know when it ends.
            // Windows TTS (_synthesizer) is easier to await using TaskCompletionSource.
            
            // Preference: High Quality Google TTS -> Fallback Windows.
            // If using Google TTS file playback via MCI, we can poll status.
            
            await PlayGoogleTts(text);
        }

        public void AnnouncePrize(string prizeName)
        {
            if (!IsEnabled || string.IsNullOrWhiteSpace(prizeName)) return;
            string text = $"Valendo {prizeName}";
            _ = SpeakAsync(text);
        }

        public void SpeakWinner(string winnerName, string comboNumero, string cartelaNumero)
        {
            if (!IsEnabled) return;
            StopAllAudio();
            // "Parabens funalo (Nome de quem ganhou), combo tal(numero do combo que ganhou), cartela tal (numero da cartela desse combo)"
            PlayGoogleTts($"Bingo! Parabéns {winnerName}, Combo {comboNumero}, Cartela {cartelaNumero}!");
        }

        private async Task PlayGoogleTts(string text)
        {
            try
            {
                string hash = GetMd5Hash(text);
                string filename = $"tts_{hash.Substring(0, 10)}.mp3"; // Shorten hash
                string filePath = Path.Combine(_tempAudioPath, filename);

                if (!File.Exists(filePath))
                {
                    string url = $"https://translate.google.com/translate_tts?ie=UTF-8&client=tw-ob&tl=pt-BR&q={Uri.EscapeDataString(text)}";
                    var data = await _httpClient.GetByteArrayAsync(url);
                    await File.WriteAllBytesAsync(filePath, data);
                }

                await PlayFileAsync(filePath);
            }
            catch
            {
                // Fallback to Windows TTS
                if (_synthesizer != null)
                {
                    await Task.Run(() => _synthesizer.Speak(text));
                }
            }
        }

        private async Task PlayFileAsync(string path)
        {
             mciSendString("close bingoAudio", null, 0, IntPtr.Zero);
             string commandOpen = $"open \"{path}\" type mpegvideo alias bingoAudio";
             mciSendString(commandOpen, null, 0, IntPtr.Zero);
             
             string commandVolume = $"setaudio bingoAudio volume to {_mciVolume}";
             mciSendString(commandVolume, null, 0, IntPtr.Zero);

             mciSendString("play bingoAudio", null, 0, IntPtr.Zero);
             
             // Poll for completion
             await Task.Run(async () => 
             {
                 StringBuilder sb = new StringBuilder(128);
                 while (true)
                 {
                     mciSendString("status bingoAudio mode", sb, 128, IntPtr.Zero);
                     if (sb.ToString() == "stopped") break;
                     await Task.Delay(100);
                 }
             });
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
            if (_synthesizer != null)
            {
                _synthesizer.SpeakAsyncCancelAll();
            }
            try 
            { 
                mciSendString("close bingoAudio", null, 0, IntPtr.Zero);
            } 
            catch { }
        }

        public void SetVolume(int volume)
        {
            int v = Math.Clamp(volume, 0, 100);
            if (_synthesizer != null)
            {
                _synthesizer.Volume = v;
            }
            _mciVolume = v * 10; // Map 0-100 to 0-1000
        }

        public void SetRate(int rate)
        {
            if (_synthesizer != null)
            {
                _synthesizer.Rate = Math.Clamp(rate, -10, 10);
            }
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
