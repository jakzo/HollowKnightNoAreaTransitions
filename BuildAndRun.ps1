param (
    [string]$SilksongPath,
    [string]$Configuration = "Debug"
)

Write-Host "Building project..." -ForegroundColor Green
dotnet build --configuration $Configuration -p:StartGame=true

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "Build completed successfully!" -ForegroundColor Green

if (-not $SilksongPath) {
    $SilksongPath = "C:\Program Files (x86)\GOG Galaxy\Games\Hollow Knight Silksong"
}

Start-Sleep -Seconds 2

$logPath = "$SilksongPath\MelonLoader\Latest.log"

Write-Host "`nMonitoring MelonLoader logs: " -ForegroundColor Cyan
Write-Host "$logPath`n" -ForegroundColor Gray

Get-Content $logPath -Wait -Tail 500
