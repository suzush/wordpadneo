#requires -Version 5.1
<#
.SYNOPSIS
    Builds WordPadNeo Setup EXE (Inno Setup) and/or MSI (WiX) from an existing
    Release-Unpackaged publish folder.
.EXAMPLE
    .\scripts\Build-Installers.ps1 -Format Both -Architecture x64
.EXAMPLE
    .\scripts\Build-Installers.ps1 -Format Msi -Architecture ARM64
.NOTES
    First run Publish-WordPadNeo.ps1 -Mode Unpackaged -Architecture x64.
    Requires Inno Setup 6.3+ for EXE, and WiX 5+ CLI for MSI.
#>
[CmdletBinding()]
param(
    [ValidateSet('Exe', 'Msi', 'Both')]
    [string]$Format = 'Both',

    [ValidateSet('x86', 'x64', 'ARM64')]
    [string[]]$Architecture = @('x64'),

    [string]$ProjectPath,
    [string]$ArtifactsDirectory,
    [string]$Version,
    [string]$InnoCompilerPath,
    [string]$IconPath,
    [switch]$ExcludeSymbols
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-Tool {
    param([string]$Executable, [string[]]$Arguments)
    Write-Host ('> ' + $Executable + ' ' + ($Arguments -join ' ')) -ForegroundColor DarkGray
    & $Executable @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed (exit code $LASTEXITCODE): $Executable"
    }
}

function Find-InnoCompiler {
    if (-not [string]::IsNullOrWhiteSpace($InnoCompilerPath)) {
        if (-not (Test-Path -LiteralPath $InnoCompilerPath -PathType Leaf)) {
            throw "Inno Setup compiler not found: $InnoCompilerPath"
        }
        return (Resolve-Path -LiteralPath $InnoCompilerPath).Path
    }
    $command = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        return $command.Source
    }
    $candidates = @()
    if (-not [string]::IsNullOrWhiteSpace(${env:ProgramFiles(x86)})) {
        $candidates += (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe')
        $candidates += (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 7\ISCC.exe')
    }
    if (-not [string]::IsNullOrWhiteSpace($env:ProgramFiles)) {
        $candidates += (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe')
        $candidates += (Join-Path $env:ProgramFiles 'Inno Setup 7\ISCC.exe')
    }
    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            return $candidate
        }
    }
    throw 'ISCC.exe not found. Install Inno Setup 6.3+ or specify -InnoCompilerPath.'
}

function Get-VersionFromProject {
    param([string]$Path)
    [xml]$projectXml = Get-Content -LiteralPath $Path -Raw
    $versionNode = $projectXml.SelectSingleNode('/Project/PropertyGroup/Version')
    if ($null -ne $versionNode -and -not [string]::IsNullOrWhiteSpace($versionNode.InnerText)) {
        return $versionNode.InnerText.Trim()
    }
    $evaluation = & dotnet msbuild $Path '-getProperty:Version' 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw 'Could not read Version from the project. Set -Version explicitly.'
    }
    $result = ($evaluation | Out-String).Trim()
    if ($result -notmatch '^\d+\.\d+\.\d+([-.+][\w.-]+)?$') {
        throw "Invalid Version '$result'. Set -Version explicitly."
    }
    return $result
}

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Join-Path $repositoryRoot 'wordpadneo.csproj'
}
$ProjectPath = (Resolve-Path -LiteralPath $ProjectPath -ErrorAction Stop).Path
if ([string]::IsNullOrWhiteSpace($ArtifactsDirectory)) {
    $ArtifactsDirectory = Join-Path $repositoryRoot 'artifacts'
}
$ArtifactsDirectory = [System.IO.Path]::GetFullPath($ArtifactsDirectory)

if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = Get-VersionFromProject -Path $ProjectPath
}
if ($Version -notmatch '^\d+\.\d+\.\d+(\.\d+)?$') {
    throw 'Installer version must be numeric, such as 1.0.0 or 1.0.0.0.'
}
$versionParts = $Version.Split('.')
if ([int]$versionParts[0] -gt 255 -or [int]$versionParts[1] -gt 255 -or [int]$versionParts[2] -gt 65535) {
    throw 'Version is outside supported Windows Installer limits.'
}
if ([string]::IsNullOrWhiteSpace($IconPath)) {
    $IconPath = Join-Path $repositoryRoot 'installer\WordPadNeo.ico'
}
if (-not (Test-Path -LiteralPath $IconPath -PathType Leaf)) {
    throw "Icon file not found: $IconPath. Place the app's .ico file at installer\WordPadNeo.ico or pass -IconPath."
}
$IconPath = (Resolve-Path -LiteralPath $IconPath).Path
if ([System.IO.Path]::GetExtension($IconPath).ToLowerInvariant() -ne '.ico') {
    throw 'The installer icon must be a real .ico file, not a renamed PNG.'
}
$installerOutput = Join-Path $ArtifactsDirectory 'installer' 
New-Item -ItemType Directory -Path $installerOutput -Force | Out-Null

$innoCompiler = $null
if ($Format -in @('Exe', 'Both')) {
    $innoCompiler = Find-InnoCompiler
}
if ($Format -in @('Msi', 'Both')) {
    if ($null -eq (Get-Command wix -ErrorAction SilentlyContinue)) {
        throw 'WiX CLI not found. Install with: dotnet tool install --global wix'
    }
}

foreach ($arch in $Architecture) {
    $architectureLower = $arch.ToLowerInvariant()
    $runtimeIdentifier = 'win-' + $architectureLower
    $publishDirectory = Join-Path $ArtifactsDirectory ('portable\' + $runtimeIdentifier)
    if (-not (Test-Path -LiteralPath $publishDirectory -PathType Container)) {
        throw "Publish folder not found: $publishDirectory. Run Publish-WordPadNeo.ps1 -Mode Unpackaged first."
    }
    $mainExecutable = Join-Path $publishDirectory 'wordpadneo.exe'
    if (-not (Test-Path -LiteralPath $mainExecutable -PathType Leaf)) {
        throw "Main EXE not found: $mainExecutable"
    }
    $publishDirectory = (Resolve-Path -LiteralPath $publishDirectory).Path
    Write-Host "`n== Installers: $runtimeIdentifier (v$Version) ==" -ForegroundColor Cyan

    if ($Format -in @('Exe', 'Both')) {
        $scriptPath = Join-Path $repositoryRoot 'installer\WordPadNeo.iss'
        $innoArguments = @(
            "/DAppVersion=$Version",
            "/DSourceDirectory=$publishDirectory",
            "/DTargetArchitecture=$arch",
            "/DInstallerOutputDirectory=$installerOutput",
            "/DAppIconPath=$IconPath",
            $scriptPath
        )
        Invoke-Tool -Executable $innoCompiler -Arguments $innoArguments
        $exeOutput = Join-Path $installerOutput ("WordPadNeo_{0}_{1}_setup.exe" -f $Version, $runtimeIdentifier)
        if (-not (Test-Path -LiteralPath $exeOutput -PathType Leaf)) {
            throw "EXE installer was not created: $exeOutput"
        }
        Write-Host "Setup EXE: $exeOutput" -ForegroundColor Green
    }

    if ($Format -in @('Msi', 'Both')) {
        $wixScript = Join-Path $repositoryRoot 'installer\WordPadNeo.wxs'
        $msiOutput = Join-Path $installerOutput ("WordPadNeo_{0}_{1}.msi" -f $Version, $runtimeIdentifier)
        $wixArguments = @(
            'build', $wixScript,
            '-arch', $architectureLower,
            '-d', "AppVersion=$Version",
            '-d', "PayloadDir=$publishDirectory",
            "-d", "AppIconPath=$IconPath",
            '-o', $msiOutput
        )
        Invoke-Tool -Executable 'wix' -Arguments $wixArguments
        if (-not (Test-Path -LiteralPath $msiOutput -PathType Leaf)) {
            throw "MSI was not created: $msiOutput"
        }
        Write-Host "MSI: $msiOutput" -ForegroundColor Green
    }
}
Write-Host "`nFinished. Output: $installerOutput" -ForegroundColor Cyan
