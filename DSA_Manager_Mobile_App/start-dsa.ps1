$ErrorActionPreference = "Stop"

$Root = $PSScriptRoot
$Backend = Join-Path $Root "backend"
$Frontend = Join-Path $Root "frontend"

function Require-Command([string]$Name, [string]$InstallHint) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "$Name не найден. $InstallHint"
    }
}

Require-Command "dotnet" "Установи .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0"
Require-Command "python" "Установи Python 3: https://www.python.org/downloads/"

if (-not (Test-Path (Join-Path $Backend "DSAManager.API.csproj"))) {
    throw "Не найден backend/DSAManager.API.csproj. Распакуй архив полностью и запусти скрипт из папки проекта."
}
if (-not (Test-Path (Join-Path $Frontend "index.html"))) {
    throw "Не найден frontend/index.html."
}

Write-Host "`n=== DSA Manager: проверка настроек ===" -ForegroundColor Cyan
Write-Host "Проверь backend/appsettings.Development.json:"
Write-Host "  ConnectionStrings:SqlServer — строка подключения к SQL Server"
Write-Host "  Jwt:Key — секрет не короче 32 байт"
Write-Host ""
$answer = Read-Host "База DSAManager уже создана, SQL-скрипт выполнен, настройки проверены? (y/n)"
if ($answer -notin @("y", "Y", "д", "Д")) {
    Write-Host "Сначала создай базу DSAManager и выполни database/DSA_Manager_SQL_Server_v1.sql в ней." -ForegroundColor Yellow
    Write-Host "Затем настрой backend/appsettings.Development.json и запусти скрипт повторно."
    exit 1
}

Write-Host "`n=== Восстановление пакетов ===" -ForegroundColor Cyan
Push-Location $Backend
try {
    dotnet restore
    if ($LASTEXITCODE -ne 0) { throw "dotnet restore завершился с ошибкой." }

    Write-Host "`n=== Сборка Backend ===" -ForegroundColor Cyan
    dotnet build --no-restore
    if ($LASTEXITCODE -ne 0) { throw "Сборка Backend завершилась с ошибкой. Пришли сюда полный текст ошибки." }

    Write-Host "`n=== Запуск Frontend ===" -ForegroundColor Cyan
    $frontendCommand = "Set-Location -LiteralPath '$Frontend'; python -m http.server 5500 --bind 0.0.0.0"
    Start-Process -FilePath "powershell.exe" -ArgumentList @("-NoExit", "-Command", $frontendCommand)

    Write-Host "`n=== Запуск Backend ===" -ForegroundColor Cyan
    Write-Host "Frontend: http://localhost:5500"
    Write-Host "Backend:  http://localhost:5000"
    Write-Host "Swagger:  http://localhost:5000/swagger"
    Write-Host "Health:   http://localhost:5000/api/health"
    $lanIPs = Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.IPAddress -notlike "127.*" -and $_.IPAddress -notlike "169.254.*" } | Select-Object -ExpandProperty IPAddress -Unique
    foreach ($ip in $lanIPs) { Write-Host "Телефон (та же Wi-Fi сеть): http://$ip`:5500"; Write-Host "API для телефона:           http://$ip`:5000" }
    Write-Host "`nОстановить Backend: Ctrl+C. Окно Frontend закрой отдельно." -ForegroundColor Yellow

    $env:ASPNETCORE_URLS = "http://0.0.0.0:5000"
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    dotnet run --no-build
    if ($LASTEXITCODE -ne 0) { throw "Backend завершился с ошибкой." }
}
finally {
    Pop-Location
}
