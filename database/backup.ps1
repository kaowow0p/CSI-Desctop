# Резервная копия БД attendance из Docker-контейнера attendance-db.
# Формат custom (pg_dump -Fc): сжатый, восстанавливается выборочно через pg_restore.
# Копии старше KeepDays дней удаляются. Для ежедневного запуска — Планировщик заданий Windows:
#   powershell -ExecutionPolicy Bypass -File <путь>\database\backup.ps1
param(
    [string]$Container = 'attendance-db',
    [string]$OutDir = (Join-Path $PSScriptRoot '..\backups'),
    [int]$KeepDays = 14
)
$ErrorActionPreference = 'Stop'

function Invoke-Checked([string]$what, [scriptblock]$command) {
    & $command
    if ($LASTEXITCODE -ne 0) { throw "$what завершилось с ошибкой (код $LASTEXITCODE)." }
}

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$name = "attendance_$(Get-Date -Format 'yyyyMMdd_HHmm').dump"
$target = Join-Path $OutDir $name

# Дамп создаётся внутри контейнера и копируется через docker cp:
# перенаправление «>» в PowerShell портит двоичные данные.
Invoke-Checked 'pg_dump' { docker exec $Container pg_dump -U postgres -d attendance -Fc -f "/tmp/$name" }
Invoke-Checked 'Проверка копии (pg_restore --list)' { docker exec $Container pg_restore --list "/tmp/$name" | Out-Null }
Invoke-Checked 'docker cp' { docker cp "${Container}:/tmp/$name" $target }
docker exec $Container rm -f "/tmp/$name" | Out-Null

Get-ChildItem -Path $OutDir -Filter 'attendance_*.dump' |
    Where-Object LastWriteTime -lt (Get-Date).AddDays(-$KeepDays) |
    Remove-Item

Write-Host "Резервная копия создана: $((Resolve-Path $target).Path) ($([math]::Round((Get-Item $target).Length / 1KB)) КБ)"
