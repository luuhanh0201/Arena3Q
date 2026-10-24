param([string]$Ffmpeg = 'C:/Program Files/Krita (x64)/bin/ffmpeg.exe')
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$videoRoot = Join-Path $projectRoot 'Assets/Arena3Q/Art/UI/Startup/Video'
$source = Join-Path $videoRoot 'OGG - in-game loop file.ogg'
$output = Join-Path $videoRoot 'BGM_From45s.wav'
if (-not (Test-Path -LiteralPath $source)) { throw 'Restore the original OGG from the supplied handoff first.' }
# Decode before atrim so the cut is sample-accurate, with no OGG packet preroll. Unity streams Vorbis at runtime.
& $Ffmpeg -hide_banner -y -i $source -af 'atrim=start=45,asetpts=PTS-STARTPTS' -c:a pcm_s16le $output
if ($LASTEXITCODE -ne 0) { throw 'Failed to trim background music.' }
