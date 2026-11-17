# Gera docs/BarberHub-Relatorio.pdf a partir de docs/relatorio.html usando o Edge (ou Chrome) em modo headless.
# Uso: powershell -ExecutionPolicy Bypass -File docs/gerar-pdf.ps1
$pasta = Split-Path -Parent $MyInvocation.MyCommand.Path
$html = Join-Path $pasta "relatorio.html"
$pdf = Join-Path $pasta "BarberHub-Relatorio.pdf"
$perfil = Join-Path $env:TEMP "barberhub-pdf"

$navegador = @(
    "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe",
    "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe",
    "$env:ProgramFiles\Google\Chrome\Application\chrome.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $navegador) { Write-Error "Edge ou Chrome nao encontrado."; exit 1 }

& $navegador --headless=new --disable-gpu --no-pdf-header-footer "--user-data-dir=$perfil" "--print-to-pdf=$pdf" "file:///$html" | Out-Null
Write-Host "PDF gerado em $pdf"
