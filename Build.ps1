param([string]$Output = (Join-Path $PSScriptRoot 'HexMapEditor.exe'), [string]$TestSource, [string]$EntryPoint = 'HexMapEditor.Program')
$ErrorActionPreference = 'Stop'
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$references = @('System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Web.Extensions.dll','System.Xaml.dll')
$arguments = @('/nologo','/optimize+',('/target:' + $(if ($TestSource) { 'exe' } else { 'winexe' })),('/main:' + $EntryPoint),('/out:' + $Output))
foreach ($reference in $references) { $arguments += '/reference:' + (Join-Path $framework $reference) }
foreach ($reference in @('PresentationFramework.dll','PresentationCore.dll','WindowsBase.dll','WindowsFormsIntegration.dll')) { $arguments += '/reference:' + (Join-Path $framework ('WPF\' + $reference)) }
$arguments += Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | ForEach-Object FullName
if ($TestSource) { $arguments += $TestSource }
& (Join-Path $framework 'csc.exe') @arguments
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
