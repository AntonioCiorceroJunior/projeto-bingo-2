$outputDir = "c:\Users\ciorc\Desktop\projeto_bingo_2\BingoAdmin.UI\bin\Debug\net8.0-windows\Audios"
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

function Get-BingoLetter {
    param ($number)
    if ($number -le 15) { return "B" }
    if ($number -le 30) { return "I" }
    if ($number -le 45) { return "N" }
    if ($number -le 60) { return "G" }
    return "O"
}

Write-Host "Gerando áudios com voz do Google..." -ForegroundColor Cyan

for ($i = 1; $i -le 75; $i++) {
    $letter = Get-BingoLetter -number $i
    $text = "$letter $i"
    $filename = "$letter$i.wav" # Saving as wav extension but content might be mp3, SoundPlayer supports wav. 
    # Actually System.Media.SoundPlayer ONLY supports .wav (PCM). 
    # Google returns MP3.
    # We need a way to convert or use a player that supports MP3.
    # WPF MediaPlayer supports MP3.
    
    # Let's check if we can use MediaPlayer in the C# code instead of SoundPlayer.
    # SoundPlayer is very limited (WAV only).
    # MediaPlayer (System.Windows.Media) is better.
}
