[CmdletBinding()]
param(
    [string]$DesktopPath = [Environment]::GetFolderPath("Desktop")
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$polarRepoRoot = Join-Path (Split-Path -Parent $repoRoot) "PolarH10"

$targets = @(
    [pscustomobject]@{
        ShortcutName = "SyntheticBio App.lnk"
        TargetPath = Join-Path $repoRoot "artifacts\\publish\\SyntheticBio.App-win-x64\\SyntheticBio.App.exe"
        Description = "Launch the canonical SyntheticBio published app"
    },
    [pscustomobject]@{
        ShortcutName = "Polar H10 App.lnk"
        TargetPath = Join-Path $polarRepoRoot "artifacts\\publish\\PolarH10.App-win-x64\\PolarH10.App.exe"
        Description = "Launch the canonical Polar H10 published app"
    }
)

$legacyDesktopCopies = @(
    "SyntheticBio App.exe",
    "Polar H10 App.exe",
    "Synthetic Polar Session.lnk"
)

foreach ($legacyName in $legacyDesktopCopies)
{
    $legacyPath = Join-Path $DesktopPath $legacyName
    if (Test-Path $legacyPath)
    {
        Remove-Item $legacyPath -Force
    }
}

$shell = New-Object -ComObject WScript.Shell

foreach ($target in $targets)
{
    if (-not (Test-Path $target.TargetPath))
    {
        throw "Launcher target not found: $($target.TargetPath)"
    }

    $shortcutPath = Join-Path $DesktopPath $target.ShortcutName
    $shortcut = $shell.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = $target.TargetPath
    $shortcut.WorkingDirectory = Split-Path $target.TargetPath -Parent
    $shortcut.IconLocation = $target.TargetPath
    $shortcut.Description = $target.Description
    $shortcut.Save()
}

Get-ChildItem $DesktopPath |
    Where-Object { $_.Name -like "Polar*" -or $_.Name -like "Synthetic*" } |
    Sort-Object Name |
    Select-Object Name, FullName, Extension
