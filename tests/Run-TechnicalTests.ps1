$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$outputDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ('HexMapEditor-tests-' + [Guid]::NewGuid().ToString('N'))
[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
$testExe = Join-Path $outputDirectory 'HexMapEditor.TechnicalTests.exe'
try {
    & $compiler /nologo /target:exe /optimize+ /main:HexMapEditor.Tests.TechnicalTests "/out:$testExe" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Web.Extensions.dll (Join-Path $projectRoot 'HexMapEditor.cs') (Join-Path $PSScriptRoot 'TechnicalTests.cs')
    if ($LASTEXITCODE -ne 0) { throw 'Technical test compilation failed.' }
    & $testExe
    if ($LASTEXITCODE -ne 0) { throw 'Technical tests failed.' }
} finally {
    if (Test-Path -LiteralPath $testExe) { Remove-Item -LiteralPath $testExe }
    [System.IO.Directory]::Delete($outputDirectory)
}
