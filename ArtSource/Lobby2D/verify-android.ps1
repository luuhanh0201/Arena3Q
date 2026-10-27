$ErrorActionPreference = 'Stop'
$androidSdkPath = 'C:/Program Files/Unity/Hub/Editor/6000.6.6f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK'
$apkPath = 'Builds/Android/Arena3Q-Lobby2D-20261027.apk'
$previousApkPath = 'Builds/Android/Arena3Q-Latest-20261025.apk'
$evidencePath = 'NghiemThu/UI/Lobby2D_Android_20261027'
$previousJavaHome = $env:JAVA_HOME
try {
    $env:JAVA_HOME = 'C:/Program Files/Unity/Hub/Editor/6000.6.6f1/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK'
    $signature = & "$androidSdkPath/build-tools/36.0.0/apksigner.bat" verify --verbose --print-certs $apkPath 2>&1
    if ($LASTEXITCODE -ne 0) { throw 'New APK signature verification failed' }
    $previousSignature = & "$androidSdkPath/build-tools/36.0.0/apksigner.bat" verify --print-certs $previousApkPath 2>&1
    if ($LASTEXITCODE -ne 0) { throw 'Previous APK signature verification failed' }
    $certificate = ($signature | Select-String 'certificate SHA-256 digest:').ToString().Trim()
    $previousCertificate = ($previousSignature | Select-String 'certificate SHA-256 digest:').ToString().Trim()
    if ($certificate -ne $previousCertificate) { throw 'APK signer changed; do not install over existing game' }
    $badging = & "$androidSdkPath/build-tools/36.0.0/aapt.exe" dump badging $apkPath 2>&1
    if ($LASTEXITCODE -ne 0) { throw 'APK manifest could not be read' }
    $package = ($badging | Select-String '^package:').ToString()
    if ($package -notmatch "name='com.Arena3Q.Arena3Q'") { throw 'Unexpected Android package identifier' }
    $signature | Set-Content -LiteralPath "$evidencePath/signature.txt" -Encoding UTF8
    $badging | Set-Content -LiteralPath "$evidencePath/manifest.txt" -Encoding UTF8
    $file = Get-Item -LiteralPath $apkPath
    $proof = [ordered]@{ file=$apkPath; bytes=$file.Length; sha256=(Get-FileHash -Algorithm SHA256 -LiteralPath $apkPath).Hash.ToLower(); signatureVerified=$true; sameSignerAsPrevious=$true; package=$package; deviceMeasured=$false }
    $proof | ConvertTo-Json | Set-Content -LiteralPath "$evidencePath/apk-verification.json" -Encoding UTF8
    $proof | ConvertTo-Json
    $badging | Select-String '^sdkVersion:|^targetSdkVersion:|^native-code:|^launchable-activity:'
} finally { $env:JAVA_HOME = $previousJavaHome }
