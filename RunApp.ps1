$ErrorActionPreference = "Stop"

Write-Host "=== BINGO ADMIN LAUNCHER ===" -ForegroundColor Cyan

# 1. Kill existing process
$processName = "BingoAdmin.UI"
$existing = Get-Process -Name $processName -ErrorAction SilentlyContinue
if ($existing) {
    Write-Host "Closing existing instance (PID: $($existing.Id))..." -ForegroundColor Yellow
    Stop-Process -Id $existing.Id -Force
    Start-Sleep -Milliseconds 500
}

# 2. Build
Write-Host "Building project..." -ForegroundColor Cyan
dotnet build BingoAdmin.UI/BingoAdmin.UI.csproj -v q --nologo

if ($LASTEXITCODE -eq 0) {
    # 3. Run
    Write-Host "Starting application..." -ForegroundColor Green
    $exePath = "BingoAdmin.UI\bin\Debug\net8.0-windows\BingoAdmin.UI.exe"
    
    if (Test-Path $exePath) {
        Start-Process $exePath
    } else {
        Write-Host "Executable not found at $exePath" -ForegroundColor Red
    }
} else {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}
