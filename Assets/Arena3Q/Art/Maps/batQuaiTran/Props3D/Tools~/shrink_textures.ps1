# Battle-budget textures for the 3D map props: the Meshy 4K base colour and normal map -> 1024 JPG next to the
# decimated FBX (Props3D/<Prop>_BaseColor.jpg, <Prop>_Normal.jpg). Run from anywhere; sources stay local (gitignored).
param([int]$size = 1024, [int]$quality = 90)
Add-Type -AssemblyName System.Drawing
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$out = Join-Path $root 'Props3D'
$codec = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() | Where-Object { $_.MimeType -eq 'image/jpeg' }
$params = New-Object System.Drawing.Imaging.EncoderParameters 1
$params.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter ([System.Drawing.Imaging.Encoder]::Quality), ([long]$quality)
foreach ($dir in Get-ChildItem $root -Directory | Where-Object { Test-Path (Join-Path $_.FullName '*.fbx') }) {
  foreach ($kind in @(@{ suffix = '-texture.png'; name = 'BaseColor' }, @{ suffix = '-texture_normal.png'; name = 'Normal' })) {
    $src = Get-ChildItem $dir.FullName -Filter "*$($kind.suffix)" | Select-Object -First 1
    if (-not $src) { continue }
    $image = [System.Drawing.Image]::FromFile($src.FullName)
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp); $g.InterpolationMode = 'HighQualityBicubic'
    $g.DrawImage($image, 0, 0, $size, $size)
    $target = Join-Path $out "$($dir.Name)_$($kind.name).jpg"
    $bmp.Save($target, $codec, $params)
    $g.Dispose(); $bmp.Dispose(); $image.Dispose()
    "{0} {1:N0} KB" -f $target, ((Get-Item $target).Length / 1KB)
  }
}
