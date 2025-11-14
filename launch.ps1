# Launcher.ps1
# Stop all child processes on Ctrl+C
$DOTNET_PROCS = @()

# Ctrl+C handler
$cancelEvent = {
    Write-Host "Stopping all services..."
    
    # Stop .NET apps
    foreach ($proc in $DOTNET_PROCS) {
        if (!$proc.HasExited) {
            Write-Host "Stopping .NET process $($proc.Id)"
            $proc.Kill()
        }
    }

    # Stop Docker Compose containers
    Write-Host "Stopping Docker Compose..."
    docker compose -f .\forecast-service\compose.yml down

    exit
}

# Register Ctrl+C handler
$null = Register-EngineEvent PowerShell.Exiting -Action $cancelEvent
$null = Register-EngineEvent Console_CancelKeyPress -Action $cancelEvent

# Start .NET apps in background
$proc1 = Start-Process "dotnet" -ArgumentList "run --project .\CentralAPIDashboard\Dashboard" -PassThru
$DOTNET_PROCS += $proc1

$proc2 = Start-Process "dotnet" -ArgumentList "run --project .\CentralAPIDashboard\API" -PassThru
$DOTNET_PROCS += $proc2

# Change this URL to your actual Dashboard URL/port
$dashboardUrl = "http://localhost:5169"
$dashboardHost = "localhost"
$dashboardPort = 5169

Write-Host "Waiting for Dashboard to start on port $dashboardPort ..."

while (-not (Test-NetConnection -ComputerName $dashboardHost -Port $dashboardPort -InformationLevel Quiet)) {
    Start-Sleep -Seconds 1
}

Write-Host "Dashboard is running. Opening browser..."
Start-Process $dashboardUrl

# Build Docker image
docker build -t forecast-python:latest .\forecast-service

# Start Docker Compose in detached mode
docker compose -f .\forecast-service\compose.yml up -d

# Wait for .NET apps to exit
foreach ($proc in $DOTNET_PROCS) {
    $proc.WaitForExit()
}

