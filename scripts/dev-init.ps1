# PowerShell script for Windows development environment initialization

param(
    [switch]$Clean = $false,
    [switch]$BuildOnly = $false,
    [switch]$Logs = $false
)

Write-Host "Claims Processing System - Development Initialization" -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Green

# Set error action preference
$ErrorActionPreference = "Stop"

# Navigate to project root
$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $projectRoot

if ($Clean) {
    Write-Host "Cleaning up existing containers and volumes..." -ForegroundColor Yellow
    docker-compose down -v --remove-orphans
    docker system prune -f
    Write-Host "Cleanup completed." -ForegroundColor Green
    if (-not $BuildOnly) {
        exit 0
    }
}

if ($Logs) {
    Write-Host "Showing container logs..." -ForegroundColor Yellow
    docker-compose logs -f
    exit 0
}

try {
    # Generate SSL certificates if they don't exist
    if (-not (Test-Path "nginx/ssl/cert.pem") -or -not (Test-Path "nginx/ssl/key.pem")) {
        Write-Host "Generating SSL certificates..." -ForegroundColor Yellow
        if (Get-Command "openssl" -ErrorAction SilentlyContinue) {
            & "$PSScriptRoot/generate-ssl.sh"
        } else {
            Write-Host "Warning: OpenSSL not found. SSL certificates not generated." -ForegroundColor Yellow
            Write-Host "You may need to install OpenSSL or run this from WSL/Git Bash" -ForegroundColor Yellow
        }
    }

    # Create storage directory
    if (-not (Test-Path "storage")) {
        New-Item -ItemType Directory -Path "storage" -Force | Out-Null
        Write-Host "Created storage directory." -ForegroundColor Green
    }

    # Build and start services
    Write-Host "Building and starting services..." -ForegroundColor Yellow
    docker-compose up --build -d

    # Wait for services to be healthy
    Write-Host "Waiting for services to start..." -ForegroundColor Yellow
    Start-Sleep -Seconds 10

    # Check service health
    Write-Host "Checking service health..." -ForegroundColor Yellow
    
    $services = @("postgres", "rabbitmq", "claims-processor", "doc-service", "api-gateway", "frontend", "nginx")
    
    foreach ($service in $services) {
        $status = docker-compose ps -q $service
        if ($status) {
            $running = docker inspect --format='{{.State.Running}}' $status 2>$null
            if ($running -eq "true") {
                Write-Host "✓ $service is running" -ForegroundColor Green
            } else {
                Write-Host "✗ $service is not running" -ForegroundColor Red
            }
        } else {
            Write-Host "✗ $service container not found" -ForegroundColor Red
        }
    }

    Write-Host "`nServices started successfully!" -ForegroundColor Green
    Write-Host "Frontend: https://localhost" -ForegroundColor Cyan
    Write-Host "API Gateway: https://localhost/api/health" -ForegroundColor Cyan
    Write-Host "RabbitMQ Management: http://localhost:15672 (claims/claims123)" -ForegroundColor Cyan
    Write-Host "`nDemo credentials:" -ForegroundColor Yellow
    Write-Host "  john.doe@example.com / password" -ForegroundColor Yellow
    Write-Host "  jane.smith@example.com / password" -ForegroundColor Yellow
    
    Write-Host "`nUseful commands:" -ForegroundColor Cyan
    Write-Host "  View logs: docker-compose logs -f [service-name]" -ForegroundColor White
    Write-Host "  Stop services: docker-compose down" -ForegroundColor White
    Write-Host "  Restart service: docker-compose restart [service-name]" -ForegroundColor White

} catch {
    Write-Host "Error occurred: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Check logs with: docker-compose logs" -ForegroundColor Yellow
    exit 1
}