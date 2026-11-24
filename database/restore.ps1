param(
    [Parameter(Mandatory)][string]$File,
    [string]$Container = 'attendance-db',
    [switch]$Force
)
$ErrorActionPreference = 'Stop'

function Invoke-Checked([string]$what, [scriptblock]$command) {
    & $command
    if ($LASTEXITCODE -ne 0) { throw "$what завершилось с ошибкой (код $LASTEXITCODE)." }
}

$path = (Resolve-Path $File).Path
if (-not $Force) {
    $answer = Read-Host "Данные БД attendance будут заменены копией $path. Продолжить? (да/нет)"
    if ($answer -notin 'да', 'y', 'yes') { Write-Host 'Отменено.'; return }
}

Invoke-Checked 'docker cp' { docker cp $path "${Container}:/tmp/restore.dump" }
Invoke-Checked 'pg_restore' { docker exec $Container pg_restore -U postgres -d attendance --clean --if-exists --single-transaction /tmp/restore.dump }
docker exec $Container rm -f /tmp/restore.dump | Out-Null

Write-Host "База attendance восстановлена из $path"
