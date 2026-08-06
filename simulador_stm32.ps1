# Simulador do STM32 em PowerShell (nao precisa instalar nada)
# Escreve leituras falsas numa porta COM virtual, no mesmo formato
# que o firmware real envia: "valor;filtroAtivo`r`n" (ex: "45.32;0")
#
# Uso:
#   .\simulador_stm32.ps1 -Porta COM8
#
# Se der erro de "execução de scripts desabilitada", rode antes (uma vez):
#   Set-ExecutionPolicy -Scope CurrentUser RemoteSigned

param(
    [Parameter(Mandatory=$true)]
    [string]$Porta
)

$baudRate = 115200
$periodoMs = 500  # mesmo periodo do firmware (PERIODO_ENVIO_MS = 500)

$port = New-Object System.IO.Ports.SerialPort $Porta, $baudRate, "None", 8, "One"

try {
    $port.Open()
    Write-Host "Simulador do STM32 rodando na porta $Porta ($baudRate bps)"
    Write-Host "Ctrl+C para parar.`n"
} catch {
    Write-Host "Erro ao abrir a porta $Porta`: $_"
    exit 1
}

$valor = 50.0
$filtroAtivo = 0

try {
    while ($true) {
        # simula variacao do potenciometro
        $delta = (Get-Random -Minimum -500 -Maximum 500) / 100.0
        $valor += $delta
        if ($valor -lt 0)   { $valor = 0 }
        if ($valor -gt 100) { $valor = 100 }

        $linha = "{0:F2};{1}" -f [double]$valor, $filtroAtivo
        $linha = $linha.Replace(",", ".")  # garante ponto decimal mesmo em Windows configurado em pt-BR
        $linha += "`r`n"
        $port.Write($linha)
        Write-Host "Enviado: $($linha.Trim())"

        Start-Sleep -Milliseconds $periodoMs
    }
} finally {
    $port.Close()
    Write-Host "Simulador encerrado."
}