$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$outputDirectory = Join-Path $PSScriptRoot ('_work\history-' + [Guid]::NewGuid().ToString('N'))
[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
$testExe = Join-Path $outputDirectory 'HexMapEditor.HistoryTests.exe'
try {
    & $compiler /nologo /target:exe /optimize+ /main:HexMapEditor.Tests.HistoryTests "/out:$testExe" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll (Join-Path $projectRoot 'HexMapEditor.cs') (Join-Path $PSScriptRoot 'HistoryTests.cs')
    if ($LASTEXITCODE -ne 0) { throw 'History test compilation failed.' }
    & $testExe
    if ($LASTEXITCODE -ne 0) { throw 'History tests failed.' }
} finally {
    if (Test-Path -LiteralPath $testExe) { Remove-Item -LiteralPath $testExe }
    [System.IO.Directory]::Delete($outputDirectory)
}
