param(
    [string]$Version = "1.0.0.0"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path $csc)) {
    throw "未找到 .NET Framework C# 编译器: $csc"
}

$outExe = Join-Path $root "AntigProxy.exe"
$manifest = Join-Path $root "app.manifest"
$iconFile = Join-Path $root "assets\app.ico"
$logoPng = Join-Path $root "assets\app_icon_preview.png"

# Generate temporary AssemblyInfo with version & metadata for Windows File Properties
$asmInfoPath = Join-Path $env:TEMP ("AntigProxy_AssemblyInfo_" + [Guid]::NewGuid().ToString("N") + ".cs")
$asmInfoContent = @"
using System.Reflection;

[assembly: AssemblyTitle("AntigProxy")]
[assembly: AssemblyDescription("Antigravity 通用免 TUN 代理启动器")]
[assembly: AssemblyProduct("AntigProxy")]
[assembly: AssemblyVersion("$Version")]
[assembly: AssemblyFileVersion("$Version")]
"@
[System.IO.File]::WriteAllText($asmInfoPath, $asmInfoContent, (New-Object System.Text.UTF8Encoding($false)))

$sources = @(
    (Join-Path $root "src\Models.cs"),
    (Join-Path $root "src\ProxyService.cs"),
    (Join-Path $root "src\UiControls.cs"),
    (Join-Path $root "src\MainForm.cs"),
    (Join-Path $root "src\Program.cs"),
    $asmInfoPath
)

Write-Host "正在编译 AntigProxy.exe (Version: $Version) ..." -ForegroundColor Cyan

$cscArgs = @(
    "/nologo",
    "/target:winexe",
    "/platform:anycpu",
    "/optimize+",
    "/codepage:65001",
    "/utf8output",
    "/win32manifest:$manifest",
    "/out:$outExe",
    "/reference:System.dll",
    "/reference:System.Core.dll",
    "/reference:System.Drawing.dll",
    "/reference:System.Windows.Forms.dll",
    "/reference:System.Web.Extensions.dll"
)

if (Test-Path $iconFile) {
    $cscArgs += "/win32icon:$iconFile"
}
if (Test-Path $logoPng) {
    $cscArgs += "/resource:$logoPng,AntigProxy.logo.png"
}

$cscArgs += $sources

try {
    & $csc @cscArgs
    if ($LASTEXITCODE -ne 0) {
        throw "编译失败，退出码: $LASTEXITCODE"
    }
} finally {
    if (Test-Path $asmInfoPath) {
        Remove-Item $asmInfoPath -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "编译成功: $outExe" -ForegroundColor Green
