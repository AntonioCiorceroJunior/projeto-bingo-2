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
    
    # Ajuste de pronúncia para a letra O (evitar som de "u")
    if ($letter -eq "O") {
        $text = "Ó $i"
    }
    
    # URL do Google TTS (Endpoint não oficial, mas funcional para uso leve)
    # tl=pt-BR define o idioma
    # q=Texto define o texto
    $url = "https://translate.google.com/translate_tts?ie=UTF-8&tl=pt-BR&client=tw-ob&q=$text"
    
    $filename = "$outputDir\$letter$i.mp3"
    
    try {
        Invoke-WebRequest -Uri $url -OutFile $filename -UserAgent "Mozilla/5.0"
        Write-Host "Gerado: $letter $i" -ForegroundColor Green
        Start-Sleep -Milliseconds 200 # Pausa para não bloquear o IP
    }
    catch {
        Write-Host "Erro ao gerar $letter $i : $_" -ForegroundColor Red
    }
}

Write-Host "Concluído! Os arquivos estão na pasta Audios." -ForegroundColor Cyan
