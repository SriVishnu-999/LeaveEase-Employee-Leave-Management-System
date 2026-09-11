$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "Starting LeaveEase API..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$root\backend\LeaveEase.Api'; dotnet restore; dotnet run"

Write-Host "Starting LeaveEase React app..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$root\frontend'; npm install; npm run dev"

Write-Host "LeaveEase will be available at http://localhost:5173" -ForegroundColor Green
