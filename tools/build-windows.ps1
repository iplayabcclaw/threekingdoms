[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$Root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$Project = Join-Path $Root "godot"
$OutputDir = Join-Path $Root "build\windows"
$Output = Join-Path $OutputDir "ThreeKingdomsSimulator.exe"
$Archive = Join-Path $Root "build\ThreeKingdomsSimulator-windows.zip"

function Resolve-GodotBinary {
    if ($env:GODOT_BIN) {
        if (-not (Test-Path $env:GODOT_BIN -PathType Leaf)) {
            throw "GODOT_BIN does not exist: $env:GODOT_BIN"
        }
        return (Resolve-Path $env:GODOT_BIN).Path
    }

    $LocalCandidates = @(
        (Join-Path $Root ".tools\godot\Godot_v4.7-stable_mono_win64_console.exe"),
        (Join-Path $Root ".tools\godot\Godot_v4.7-stable_mono_win64.exe")
    )
    foreach ($Candidate in $LocalCandidates) {
        if (Test-Path $Candidate -PathType Leaf) {
            return (Resolve-Path $Candidate).Path
        }
    }

    foreach ($CommandName in @("godot-mono", "godot4-mono", "godot")) {
        $Command = Get-Command $CommandName -ErrorAction SilentlyContinue
        if ($Command) {
            return $Command.Source
        }
    }

    throw "Godot 4.7 .NET was not found in .tools\godot, GODOT_BIN, or PATH."
}

function Resolve-DotnetBinary {
    $Command = Get-Command "dotnet" -ErrorAction SilentlyContinue
    if ($Command) {
        return $Command.Source
    }

    $MachineDotnet = Join-Path $env:ProgramFiles "dotnet\dotnet.exe"
    if (Test-Path $MachineDotnet -PathType Leaf) {
        return $MachineDotnet
    }

    throw ".NET SDK was not found in PATH or Program Files."
}

$Godot = Resolve-GodotBinary
$Dotnet = Resolve-DotnetBinary
$Version = (& $Godot --version).Trim()
if ($LASTEXITCODE -ne 0) {
    throw "Unable to read the Godot version: $Godot"
}
if ($Version -notmatch '^4\.7\..*\.mono\.') {
    throw "Godot 4.7 .NET is required. Current version: $Version"
}

$TemplateDir = Join-Path $env:APPDATA "Godot\export_templates\4.7.stable.mono"
$WindowsTemplate = Join-Path $TemplateDir "windows_release_x86_64.exe"
$IcuData = Join-Path $TemplateDir "icudt_godot.dat"
if (-not (Test-Path $WindowsTemplate -PathType Leaf) -or -not (Test-Path $IcuData -PathType Leaf)) {
    throw "Godot Windows export template or ICU data is missing: $TemplateDir"
}

& $Dotnet build (Join-Path $Project "ThreeKingdomsSimulator.Godot.csproj") --nologo
if ($LASTEXITCODE -ne 0) {
    throw "C# build failed."
}

if (Test-Path $OutputDir) {
    Remove-Item $OutputDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

& $Godot --headless --path $Project --export-release "Windows Desktop" $Output
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $Output -PathType Leaf)) {
    throw "Windows export failed."
}

$Smoke = Start-Process -FilePath $Output -ArgumentList "--headless", "--quit-after", "3" -PassThru -Wait
if ($Smoke.ExitCode -ne 0) {
    throw "Exported game smoke test failed with exit code $($Smoke.ExitCode)."
}

Compress-Archive -Path (Join-Path $OutputDir "*") -DestinationPath $Archive -Force

Write-Host "Windows build complete."
Write-Host "EXE: $Output"
Write-Host "ZIP: $Archive"
