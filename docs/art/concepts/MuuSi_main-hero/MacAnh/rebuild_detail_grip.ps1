$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path "$PSScriptRoot/../../../../..").Path
Set-Location $projectRoot
$blenderPath = Join-Path $projectRoot 'Library/LuBuTools/blender-4.5.3-windows-x64/blender.exe'
function Invoke-Step([string]$source, [string]$script, [string[]]$parameters) {
    & $blenderPath -b $source -t 4 --python-exit-code 1 --python "$PSScriptRoot/$script" -- @parameters
    if ($LASTEXITCODE -ne 0) { throw "Failed: $script" }
}
# Candidate build only. Export/import and visual acceptance are separate steps.
Invoke-Step "$PSScriptRoot/Production/LOD/MacAnh_Battle_TailoredSleeves.blend" 'restore_macanh_hand_detail.py' @('--output','Library/MacAnh_HandDetail.blend')
Invoke-Step 'Library/MacAnh_HandDetail.blend' 'restore_macanh_finger_weights.py' @('--output','Library/MacAnh_HandDetailWeighted.blend')
Invoke-Step 'Library/MacAnh_HandDetailWeighted.blend' 'relax_macanh_hand_edges.py' @()
Invoke-Step 'Library/MacAnh_HandRelaxed.blend' 'fit_macanh_hand_to_grip.py' @('--sculpt-hand')
Invoke-Step 'Library/MacAnh_HandFit.blend' 'relax_macanh_hand_edges.py' @()
Invoke-Step 'Library/MacAnh_HandRelaxed.blend' 'close_macanh_grip_gaps.py' @()
Invoke-Step 'Library/MacAnh_GripClosed.blend' 'bind_macanh_closed_grip.py' @()
Invoke-Step 'Library/MacAnh_GripSurface.blend' 'articulate_macanh_shoulders.py' @('--output','Library/MacAnh_DetailShoulder.blend')
Invoke-Step 'Library/MacAnh_DetailShoulder.blend' 'shape_macanh_sleeve_folds.py' @('--output','Library/MacAnh_DetailShoulderCloth.blend')
