param([string]$Ffmpeg = 'C:/Program Files/Krita (x64)/bin/ffmpeg.exe')
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$videoRoot = Join-Path $projectRoot 'Assets/Arena3Q/Art/UI/Startup/Video'
$source = Join-Path $videoRoot 'Arena3Q_Splash_fire_sound.mp4'
$output = Join-Path $videoRoot 'Arena3Q_Splash_loud.mp4'
if (-not (Test-Path -LiteralPath $source)) { throw 'Restore the original splash from the supplied handoff first.' }
# Preserve the source and every decoded video frame. The limiter protects peaks after the 2x audio gain.
& $Ffmpeg -hide_banner -y -i $source -c:v copy -bsf:v 'h264_metadata=colour_primaries=1:transfer_characteristics=1:matrix_coefficients=1' -af 'volume=2,alimiter=limit=0.89:level=false:latency=true' -c:a aac -b:a 224k -movflags +faststart $output
if ($LASTEXITCODE -ne 0) { throw 'Failed to prepare louder intro audio.' }
