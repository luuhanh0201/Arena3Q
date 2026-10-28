Add-Type -AssemblyName System.Drawing
$jobs = Get-Content (Join-Path $PSScriptRoot 'prompts.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$result = @()
foreach ($job in $jobs) {
    $path = Join-Path $PSScriptRoot ($job.id + '.png')
    if (!(Test-Path -LiteralPath $path)) { throw "Missing: $path" }
    $img = [Drawing.Bitmap]::new($path)
    try {
        $amin = 255; $amax = 0; $clear = 0; $partial = 0; $opaque = 0; $count = 0
        for ($y = 0; $y -lt $img.Height; $y += 13) {
            for ($x = 0; $x -lt $img.Width; $x += 13) {
                $a = [int]$img.GetPixel($x, $y).A
                $amin = [Math]::Min($amin, $a); $amax = [Math]::Max($amax, $a)
                if ($a -eq 0) { $clear++ }; if ($a -eq 255) { $opaque++ }; if ($a -gt 0 -and $a -lt 255) { $partial++ }; $count++
            }
        }
        if ($job.transparent -and ($clear -eq 0 -or $amax -eq 0)) { throw "Invalid transparency: $path" }
        $result += [pscustomobject]@{
            file = $job.id + '.png'; name = $job.name; workbookIds = $job.rows
            width = $img.Width; height = $img.Height; bytes = (Get-Item -LiteralPath $path).Length
            sha256 = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLower()
            alphaMinSampled = $amin; alphaMaxSampled = $amax
            transparentSampleRatio = [Math]::Round($clear / $count, 4)
            partialAlphaSampleRatio = [Math]::Round($partial / $count, 4)
            opaqueAlphaSampleRatio = [Math]::Round($opaque / $count, 4)
            status = 'drawn; pending 3D scene matching and runtime validation'
        }
    } finally { $img.Dispose() }
}
$result | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $PSScriptRoot 'images.json') -Encoding UTF8
$result | Select-Object file,width,height,alphaMinSampled,transparentSampleRatio | Format-Table -AutoSize
