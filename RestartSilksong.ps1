param (
    [string]$SilksongPath
)

$process = Get-Process "Hollow Knight Silksong" -ErrorAction SilentlyContinue
if ($process) {
    Write-Host "Stopping Hollow Knight Silksong..."
    taskkill /IM "Hollow Knight Silksong.exe"
    
    # Poll every second for up to 10 seconds to ensure the process has exited
    $timeout = 10
    $elapsed = 0
    
    do {
        Start-Sleep -Seconds 1
        $elapsed++
        $process = Get-Process "Hollow Knight Silksong" -ErrorAction SilentlyContinue
        
        if ($process) {
            Write-Host "Waiting for Silksong to exit... ($elapsed/$timeout seconds)"
        } else {
            Write-Host "Silksong has exited successfully."
            break
        }
    } while ($elapsed -lt $timeout)
    
    if ($process) {
        Write-Error "Failed to stop Silksong after $timeout seconds. Aborting."
        exit 1
    }
}
Start-Process -FilePath "$SilksongPath\Hollow Knight Silksong.exe" -WindowStyle Normal
