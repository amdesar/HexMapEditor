param(
    [Parameter(Mandatory)][ValidateSet('scope')][string]$Case,
    [Parameter(Mandatory)][string]$Run,
    [Parameter(Mandatory)][string]$Setup,
    [Parameter(Mandatory)][int]$OtherPid
)
$ErrorActionPreference='Stop'
$work=(Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '_work')).Path.TrimEnd('\')+'\'
$area=(Resolve-Path -LiteralPath $Run).Path
$package=(Resolve-Path -LiteralPath $Setup).Path
function Assert([bool]$ok,[string]$error){if(-not $ok){throw $error}}
Assert ($area.StartsWith($work,[StringComparison]::OrdinalIgnoreCase) -and
    (Split-Path $area -Leaf) -match '^patch-0133-diagnosis(?:-[0-9a-f]+)?$') 'Wymagany wydzielony katalog próby.'
Assert ($package.StartsWith($area+'\',[StringComparison]::OrdinalIgnoreCase)) 'Wymagany izolowany Setup z katalogu próby.'
$program=Join-Path $area 'Program'
$exe=Join-Path $program 'HexMapEditor.exe'
$other=Get-Process -Id $OtherPid
Assert ($other.Path -eq (Join-Path $area 'OtherProgram/HexMapEditor.exe')) 'Wymagany rzeczywisty proces drugiej kopii próbnej.'
Assert ((Get-Item -LiteralPath $exe).VersionInfo.FileVersion -eq '0.13.2.0') 'Wymagana zainstalowana baza 0.13.2.'
$digest=(Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash
$common='/SP- /SILENT /SUPPRESSMSGBOXES /NORESTART /HMEUpdate=1 /HMEUpdateScope=user /HMEExpectedVersion=0.13.3 /HMESetupHash={0} /DIR="{1}"' -f $digest,$program
function FindWindow($process) {
    $until=[DateTime]::UtcNow.AddSeconds(10)
    do {
        $process.Refresh()
        if($process.HasExited){throw "Proces edytora zakończył się przed próbą."}
        if($process.MainWindowHandle -ne [IntPtr]::Zero){return}
        Start-Sleep -Milliseconds 120
    } while([DateTime]::UtcNow -lt $until)
    throw "Nie znaleziono okna próbnego edytora $($process.Id)."
}
$first=Start-Process -FilePath $exe -WorkingDirectory $program -WindowStyle Normal -PassThru
FindWindow $first
$blockedLog=Join-Path $area 'same-target.log'
$blocked=Start-Process -FilePath $package -ArgumentList ($common+' /LOG="'+$blockedLog+'"') -WindowStyle Hidden -PassThru
Assert ($blocked.WaitForExit(15000) -and $blocked.ExitCode -eq 7) 'Setup nie zatrzymał się na blokadzie tej instalacji.'
Assert ([bool](Select-String -LiteralPath $blockedLog -Pattern 'Sprawdzenie blokady EXE \(Windows (32|33)\)' -Quiet)) 'Brak kodu blokady współdzielenia.'
Assert ((Get-Item -LiteralPath $exe).VersionInfo.FileVersion -eq '0.13.2.0' -and -not $first.HasExited) 'Zmieniono zajęty EXE lub zamknięto proces.'
$resumeLog=Join-Path $area 'resume.log'
$update=Start-Process -FilePath $package -ArgumentList ($common+' /WaitForEditorPid='+$first.Id+' /LOG="'+$resumeLog+'"') -WindowStyle Hidden -PassThru
$until=[DateTime]::UtcNow.AddSeconds(10)
do {
    if((Test-Path -LiteralPath $resumeLog) -and (Select-String -LiteralPath $resumeLog -SimpleMatch ('PID '+$first.Id) -Quiet)){break}
    Start-Sleep -Milliseconds 150
} while([DateTime]::UtcNow -lt $until)
Assert ((Test-Path -LiteralPath $resumeLog) -and (Select-String -LiteralPath $resumeLog -SimpleMatch ('PID '+$first.Id) -Quiet)) 'Brak czekania na proces inicjujący.'
Assert (-not $update.HasExited -and -not $first.HasExited) 'Setup nie zaczekał na właściwy PID.'
Assert ($first.CloseMainWindow() -and $first.WaitForExit(10000)) 'Własny editor testowy nie zakończył pracy w zwykły sposób.'
Assert ($update.WaitForExit(20000) -and $update.ExitCode -eq 0) 'Upgrade nie zakończył się powodzeniem.'
Assert ((Get-Item -LiteralPath $exe).VersionInfo.FileVersion -eq '0.13.3.0' -and -not $other.HasExited) 'Zmieniono niewłaściwą kopię lub wersję.'
Assert ((Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash -eq $digest) 'Setup nie jest tą samą paczką.'
'scope PASS'
