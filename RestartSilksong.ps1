param (
    [string]$SilksongPath
)

$process = Get-Process "Hollow Knight Silksong" -ErrorAction SilentlyContinue
if ($process) {
    taskkill /IM "Hollow Knight Silksong.exe"
    Start-Sleep -Seconds 3
}
Start-Process -FilePath "$SilksongPath\Hollow Knight Silksong.exe" -WindowStyle Normal
