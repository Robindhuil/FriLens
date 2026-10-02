# Stiahne CSV logy FriLens z telefónu cez USB a k nim výpis logcatu.
#
#   powershell -ExecutionPolicy Bypass -File tools\pull-logs.ps1
#   powershell -ExecutionPolicy Bypass -File tools\pull-logs.ps1 -Out D:\logy -Logcat
#
# adb nie je v PATH; berie sa ten, ktorý prišiel s Unity. Telefón musí mať zapnuté
# ladenie cez USB a potvrdený tento počítač.

param(
    [string]$Out = (Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'Robin\unity\frilens\logs'),
    [switch]$Logcat
)

$ErrorActionPreference = 'Stop'

$adb = Get-ChildItem 'C:\Program Files\Unity\Hub\Editor\*\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe' -ErrorAction SilentlyContinue |
    Sort-Object FullName -Descending | Select-Object -First 1
if (-not $adb) {
    $cmd = Get-Command adb -ErrorAction SilentlyContinue
    if (-not $cmd) { throw 'adb sa nenašiel ani v Unity, ani v PATH.' }
    $adb = $cmd.Source
} else {
    $adb = $adb.FullName
}

$devices = & $adb devices | Select-Object -Skip 1 | Where-Object { $_ -match '\tdevice$' }
if (-not $devices) { throw 'Žiadny telefón. Zapni ladenie cez USB a potvrď počítač na displeji.' }

$remote = '/sdcard/Android/data/sk.uniza.fri.frilens/files'
New-Item -ItemType Directory -Force -Path $Out | Out-Null

$files = & $adb shell "ls $remote/frilens-*.csv 2>/dev/null" | ForEach-Object { $_.Trim() } | Where-Object { $_ }
if (-not $files) {
    Write-Host "Na telefóne nie sú žiadne logy v $remote."
} else {
    foreach ($file in $files) {
        $name = Split-Path $file -Leaf
        $target = Join-Path $Out $name
        if (Test-Path $target) {
            Write-Host "už je   $name"
            continue
        }
        & $adb pull $file $target | Out-Null
        Write-Host "stiahnutý $name"
    }
}

if ($Logcat) {
    # Buffer logcatu drží len posledné minúty až hodiny, takže sa oplatí ťahať hneď po teste.
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $logcat = Join-Path $Out "logcat-$stamp.txt"
    & $adb logcat -d -s Unity:V AndroidRuntime:E CRASH:E ARCore:W > $logcat
    Write-Host "logcat  $logcat"
}

Write-Host ""
Write-Host "Vyhodnotenie:"
Write-Host "  python tools\frilens_eval.py `"$Out\frilens-*.csv`""
