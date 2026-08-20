# FreshMart - Start All Services
# Run this from project root: .\start-all.ps1

$root = $PSScriptRoot

Write-Host ""
Write-Host "  FreshMart - Starting all services..." -ForegroundColor Cyan
Write-Host ""

# Kill any existing dotnet processes to avoid port conflicts
$existing = Get-Process dotnet -ErrorAction SilentlyContinue
if ($existing) {
    Write-Host "  Stopping existing dotnet processes..." -ForegroundColor Yellow
    $existing | Stop-Process -Force
    Start-Sleep -Seconds 2
}

# Start each service in a new terminal window
$services = @(
    @{ Name = "AuthService";    Path = "Microservices\AuthService";    Port = 5001 },
    @{ Name = "ProductService"; Path = "Microservices\ProductService"; Port = 5002 },
    @{ Name = "OrderService";   Path = "Microservices\OrderService";   Port = 5003 },
    @{ Name = "ApiGateway";     Path = "Microservices\ApiGateway";     Port = 8080 }
)

foreach ($svc in $services) {
    $fullPath = Join-Path $root $svc.Path
    Write-Host "  Starting $($svc.Name) on port $($svc.Port)..." -ForegroundColor Green
    Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$fullPath'; dotnet run --launch-profile http"
    Start-Sleep -Seconds 1
}

Write-Host ""
Write-Host "  All services started in separate windows." -ForegroundColor Cyan
Write-Host ""
Write-Host "  Waiting 30s for services to come up..." -ForegroundColor Yellow
Start-Sleep -Seconds 30

# Health check
Write-Host ""
Write-Host "  Health Check:" -ForegroundColor Cyan
Write-Host "  -------------------------------------" -ForegroundColor DarkGray

$allHealthy = $true

foreach ($svc in $services) {
    try {
        $r = Invoke-RestMethod "http://localhost:$($svc.Port)/health" -TimeoutSec 5
        Write-Host "  OK  $($svc.Name) [:$($svc.Port)] - $($r.status)" -ForegroundColor Green
    } catch {
        Write-Host "  FAIL  $($svc.Name) [:$($svc.Port)] - not responding" -ForegroundColor Red
        $allHealthy = $false
    }
}

Write-Host "  -------------------------------------" -ForegroundColor DarkGray
Write-Host ""

if ($allHealthy) {
    Write-Host "  All services are running!" -ForegroundColor Green
    Write-Host ""
    Write-Host "  Swagger UIs:" -ForegroundColor Cyan
    Write-Host "    Gateway   ->  http://localhost:8080/swagger" -ForegroundColor White
    Write-Host "    Auth      ->  http://localhost:5001/swagger" -ForegroundColor White
    Write-Host "    Products  ->  http://localhost:5002/swagger" -ForegroundColor White
    Write-Host "    Orders    ->  http://localhost:5003/swagger" -ForegroundColor White
} else {
    Write-Host "  Some services are not ready yet." -ForegroundColor Yellow
    Write-Host "  Wait a bit and check manually." -ForegroundColor Yellow
}

Write-Host ""
