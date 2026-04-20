# ============================================
# SCRIPT KHOI DONG HE THONG TOURISM APP
# Chay moi sang sau khi mo may
# Right-click > Run with PowerShell
# ============================================

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  KHOI DONG HE THONG TOURISM APP" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# --- BUOC 1: Kiem tra IP WiFi ---
Write-Host "[1/4] Kiem tra IP WiFi..." -ForegroundColor Yellow
$wifiIp = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.InterfaceAlias -eq "Wi-Fi" }).IPAddress

if ($wifiIp) {
    Write-Host "  WiFi IP: $wifiIp" -ForegroundColor Green
} else {
    Write-Host "  KHONG TIM THAY WIFI! Hay ket noi WiFi truoc." -ForegroundColor Red
    Read-Host "Nhan Enter de thoat"
    exit
}

# Kiem tra IP co thay doi khong
$savedIp = "192.168.1.14"
if ($wifiIp -ne $savedIp) {
    Write-Host "  CANH BAO: IP da doi tu $savedIp -> $wifiIp" -ForegroundColor Red
    Write-Host "  Ban can sua IP trong app Settings hoac MauiProgram.cs" -ForegroundColor Red
    Write-Host ""
}

# --- BUOC 2: Khoi dong WebApplication2 (API Server) ---
Write-Host "[2/4] Khoi dong API Server (port 5216)..." -ForegroundColor Yellow

$serverRunning = $false
try {
    $r = Invoke-WebRequest -Uri "http://localhost:5216/swagger/index.html" -UseBasicParsing -TimeoutSec 3 -ErrorAction Stop
    $serverRunning = $true
    Write-Host "  Server DA CHAY san!" -ForegroundColor Green
} catch {
    Write-Host "  Dang khoi dong server..." -ForegroundColor Cyan
    $serverPath = Join-Path $PSScriptRoot "WebApplication2"
    Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$serverPath'; dotnet run" -WindowStyle Normal
    
    # Doi server khoi dong
    $timeout = 30
    $started = $false
    for ($i = 0; $i -lt $timeout; $i++) {
        Start-Sleep -Seconds 1
        try {
            $r = Invoke-WebRequest -Uri "http://localhost:5216/swagger/index.html" -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
            $started = $true
            break
        } catch { }
        Write-Host "  Cho server khoi dong... ($i/$timeout giay)" -ForegroundColor Gray -NoNewline
        Write-Host "`r" -NoNewline
    }
    
    if ($started) {
        Write-Host "  Server KHOI DONG THANH CONG!                    " -ForegroundColor Green
    } else {
        Write-Host "  Server CHUA SAN SANG (co the can them thoi gian)" -ForegroundColor Yellow
    }
}

# --- BUOC 3: Khoi dong ngrok ---
Write-Host "[3/4] Khoi dong ngrok tunnel..." -ForegroundColor Yellow

$ngrokRunning = $false
$ngrokUrl = ""
try {
    $r = Invoke-WebRequest -Uri "http://127.0.0.1:4040/api/tunnels" -UseBasicParsing -ErrorAction Stop
    $json = $r.Content | ConvertFrom-Json
    $ngrokUrl = $json.tunnels[0].public_url
    $ngrokRunning = $true
    Write-Host "  ngrok DA CHAY: $ngrokUrl" -ForegroundColor Green
} catch {
    # Kiem tra ngrok co duoc cai dat khong
    $ngrokPath = Get-Command ngrok -ErrorAction SilentlyContinue
    if (-not $ngrokPath) {
        Write-Host "  NGROK CHUA CAI DAT!" -ForegroundColor Red
        Write-Host "  Tai tu: https://ngrok.com/download" -ForegroundColor Red
    } else {
        Write-Host "  Dang khoi dong ngrok..." -ForegroundColor Cyan
        Start-Process powershell -ArgumentList "-NoExit", "-Command", "ngrok http 5216" -WindowStyle Normal
        
        # Doi ngrok khoi dong
        Start-Sleep -Seconds 5
        try {
            $r = Invoke-WebRequest -Uri "http://127.0.0.1:4040/api/tunnels" -UseBasicParsing -ErrorAction Stop
            $json = $r.Content | ConvertFrom-Json
            $ngrokUrl = $json.tunnels[0].public_url
            $ngrokRunning = $true
            Write-Host "  ngrok KHOI DONG THANH CONG!" -ForegroundColor Green
        } catch {
            Write-Host "  ngrok CHUA SAN SANG (kiem tra terminal ngrok)" -ForegroundColor Yellow
        }
    }
}

# --- BUOC 4: Hien thi thong tin ---
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  KET QUA KHOI DONG" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  WiFi IP:     $wifiIp" -ForegroundColor White
Write-Host "  Server:      http://localhost:5216" -ForegroundColor White
if ($ngrokUrl) {
    Write-Host "  ngrok URL:   $ngrokUrl" -ForegroundColor White
    Write-Host ""
    Write-Host "  Webhook URL cho SePay:" -ForegroundColor Yellow
    Write-Host "  $ngrokUrl/api/payments/webhook" -ForegroundColor Green
    
    # Copy vao clipboard
    "$ngrokUrl/api/payments/webhook" | Set-Clipboard
    Write-Host ""
    Write-Host "  (Da copy Webhook URL vao clipboard)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "  VIEC CAN LAM THU CONG:" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""

$step = 1
if ($wifiIp -ne $savedIp) {
    Write-Host "  $step. Mo app > Settings > Sua Server IP thanh: $wifiIp" -ForegroundColor White
    Write-Host "     Roi TAT APP va MO LAI" -ForegroundColor Gray
    $step++
}

if ($ngrokUrl) {
    Write-Host "  $step. Vao my.sepay.vn > Webhook > Dan URL:" -ForegroundColor White
    Write-Host "     $ngrokUrl/api/payments/webhook" -ForegroundColor Green
    Write-Host "     Nhan LUU" -ForegroundColor Gray
    $step++
}

if ($step -eq 1) {
    Write-Host "  Khong can lam gi them! Moi thu da san sang." -ForegroundColor Green
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  XONG! Co the dung app binh thuong." -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Read-Host "Nhan Enter de dong cua so nay"
