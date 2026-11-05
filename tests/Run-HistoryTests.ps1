$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$outputDirectory = Join-Path $projectRoot 'bin'
[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
$testExe = Join-Path $outputDirectory 'HexMapEditor.HistoryTests.exe'
& $compiler /nologo /target:exe /optimize+ /main:HexMapEditor.Tests.HistoryTests "/out:$testExe" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll (Join-Path $projectRoot 'HexMapEditor.cs') (Join-Path $PSScriptRoot 'HistoryTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'History test compilation failed.' }
& $testExe
if ($LASTEXITCODE -ne 0) { throw 'History tests failed.' }
