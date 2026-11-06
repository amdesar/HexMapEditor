$ErrorActionPreference = 'Stop'
$run = Join-Path $PSScriptRoot ('_work\ui-030-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($run) | Out-Null
Copy-Item -LiteralPath (Join-Path (Split-Path -Parent $PSScriptRoot) 'HexMapEditor.exe') -Destination (Join-Path $run 'HexMapEditor.exe')
Add-Type -AssemblyName System.Drawing
$image = New-Object Drawing.Bitmap 96,48
$graphics = [Drawing.Graphics]::FromImage($image)
try { $graphics.Clear([Drawing.Color]::Transparent); $graphics.FillEllipse([Drawing.Brushes]::Gold,4,4,88,40); $image.Save((Join-Path $run 'Własna ikona.png'),[Drawing.Imaging.ImageFormat]::Png) }
finally { $graphics.Dispose(); $image.Dispose() }
Write-Output $run
