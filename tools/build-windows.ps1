[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$Root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$Project = Join-Path $Root "godot"
$OutputDir = Join-Path $Root "build\windows"
$Output = Join-Path $OutputDir "ThreeKingdomsSimulator.exe"

function Resolve-GodotBinary {
    if ($env:GODOT_BIN) {
        if (-not (Test-Path $env:GODOT_BIN -PathType Leaf)) {
            throw "GODOT_BIN 指向的文件不存在：$env:GODOT_BIN"
        }
        return (Resolve-Path $env:GODOT_BIN).Path
    }

    foreach ($CommandName in @("godot-mono", "godot4-mono", "godot")) {
        $Command = Get-Command $CommandName -ErrorAction SilentlyContinue
        if ($Command) {
            return $Command.Source
        }
    }

    throw "未找到 Godot 4.7 .NET。请安装后设置 GODOT_BIN。"
}

$Godot = Resolve-GodotBinary
$Version = (& $Godot --version).Trim()
if ($LASTEXITCODE -ne 0) {
    throw "无法读取 Godot 版本：$Godot"
}
if ($Version -notmatch '^4\.7\..*\.mono\.') {
    throw "需要 Godot 4.7 .NET，当前版本：$Version"
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

& $Godot --headless --path $Project --build-solutions --quit-after 3
if ($LASTEXITCODE -ne 0) {
    throw "C# 编译失败。"
}

& $Godot --headless --path $Project --export-release "Windows Desktop" $Output
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $Output -PathType Leaf)) {
    throw "Windows 导出失败。请确认已为 Godot 4.7 安装 Windows x86_64 export template 和 ICU Data。"
}

Write-Host "Windows 构建完成：$OutputDir"
