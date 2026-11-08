#requires -Version 5.1
<#
.SYNOPSIS
    Builds WordPadNeo as an MSIX package and/or a portable ZIP.
    Unpackaged artifacts are copied from the Visual Studio-compatible Build output.
.EXAMPLE
    .\scripts\Publish-WordPadNeo.ps1 -Mode Both -Architecture x64
.EXAMPLE
    .\scripts\Publish-WordPadNeo.ps1 -Mode Unpackaged -Architecture x64,ARM64
.NOTES
    Requires Windows, .NET 10 SDK and Visual Studio MSBuild/MSIX tools for MSIX.
    MSIX generation is unsigned by default, preserving the project's signing setting.
#>
[CmdletBinding()]
param(
    [ValidateSet('Packaged', 'Unpackaged', 'Both')]
    [string]$Mode = 'Both',

    [ValidateSet('x86', 'x64', 'ARM64')]
    [string[]]$Architecture = @('x64'),

    [string]$ProjectPath,

    [string]$OutputDirectory,

    [switch]$SkipZip
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Invoke-ExternalTool {
    param(
        [Parameter(Mandatory)][string]$Executable,
        [Parameter(Mandatory)][string[]]$Arguments
    )

    Write-Host ('> ' + $Executable + ' ' + ($Arguments -join ' ')) -ForegroundColor DarkGray
    & $Executable @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed (exit code $LASTEXITCODE): $Executable"
    }
}

function Get-MSBuildPath {
    $programFilesX86 = [Environment]::GetFolderPath('ProgramFilesX86')
    if (-not [string]::IsNullOrWhiteSpace($programFilesX86)) {
        $vswhere = Join-Path $programFilesX86 'Microsoft Visual Studio\Installer\vswhere.exe'
        if (Test-Path -LiteralPath $vswhere) {
            $installation = & $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -property installationPath
            if ($LASTEXITCODE -eq 0 -and $installation) {
                $candidate = Join-Path ($installation | Select-Object -First 1) 'MSBuild\Current\Bin\MSBuild.exe'
                if (Test-Path -LiteralPath $candidate) {
                    return $candidate
                }
            }
        }
    }
    $command = Get-Command 'MSBuild.exe' -ErrorAction SilentlyContinue
    if ($null -ne $command) {
        return $command.Source
    }
    throw 'Visual Studio MSBuild.exe not found. Install the Visual Studio MSBuild and Windows packaging tools.'
}

function Get-AppVersion {
    param([Parameter(Mandatory)][xml]$ProjectXml)

    $versionNode = $ProjectXml.SelectSingleNode('/Project/PropertyGroup/Version')
    if ($null -ne $versionNode) {
        $version = $versionNode.InnerText
        if (-not [string]::IsNullOrWhiteSpace($version)) {
            return $version.Trim()
        }
    }
    Write-Warning 'Version is not defined in the project file; using 1.0.0.'
    return '1.0.0'
}

function Get-MsixVersion {
    param([string]$ProjectFolder, [string]$FallbackVersion)

    $manifest = Join-Path $ProjectFolder 'Package.appxmanifest'
    if (Test-Path -LiteralPath $manifest) {
        [xml]$manifestXml = Get-Content -LiteralPath $manifest -Raw
        $identity = $manifestXml.SelectSingleNode('/*[local-name()="Package"]/*[local-name()="Identity"]')
        if ($null -ne $identity -and $null -ne $identity.Attributes['Version']) {
            $manifestVersion = $identity.Attributes['Version'].Value
            if (-not [string]::IsNullOrWhiteSpace($manifestVersion)) {
                return $manifestVersion
            }
        }
    }
    # A valid package version has four numeric components.
    try {
        return ([version]$FallbackVersion).ToString(4)
    }
    catch {
        throw "Invalid MSIX fallback version '$FallbackVersion'. Set a four-part version in Package.appxmanifest."
    }
}

function Get-PackageFiles {
    param([string]$Root)
    return @(Get-ChildItem -LiteralPath $Root -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Extension -in @('.msix', '.msixbundle', '.appx', '.appxbundle', '.msixupload', '.appxupload') })
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Join-Path $repoRoot 'wordpadneo.csproj'
}
$ProjectPath = (Resolve-Path -LiteralPath $ProjectPath -ErrorAction Stop).Path
$projectFolder = Split-Path -Parent $ProjectPath
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot 'artifacts'
}
$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

[xml]$projectXml = Get-Content -LiteralPath $ProjectPath -Raw
$appVersion = Get-AppVersion -ProjectXml $projectXml
$msixVersion = Get-MsixVersion -ProjectFolder $projectFolder -FallbackVersion $appVersion

$msbuildExe = Get-MSBuildPath

foreach ($arch in $Architecture) {
    $runtimeId = 'win-' + $arch.ToLowerInvariant()
    Write-Host "`n== WordPadNeo $arch ==" -ForegroundColor Cyan

    if ($Mode -in @('Packaged', 'Both')) {
        $msixIntermediate = Join-Path $OutputDirectory ("msix-intermediate\" + $arch)
        $msixFinal = Join-Path $OutputDirectory 'msix'
        # Remove prior intermediates to avoid accidentally copying stale packages.
        if (Test-Path -LiteralPath $msixIntermediate) {
            Remove-Item -LiteralPath $msixIntermediate -Recurse -Force
        }
        New-Item -ItemType Directory -Path $msixIntermediate, $msixFinal -Force | Out-Null

        $appxDir = $msixIntermediate.TrimEnd('\', '/') + '\'
        $buildArgs = @(
            $ProjectPath,
            '/restore',
            '/t:Rebuild',
            '/p:Configuration=Release',
            "/p:Platform=$arch",
            "/p:RuntimeIdentifier=$runtimeId",
            '/p:WindowsPackageType=MSIX',
            '/p:GenerateAppxPackageOnBuild=true',
            '/p:AppxBundle=Never',
            '/p:AppxPackageSigningEnabled=false',
            "/p:AppxPackageDir=$appxDir",
            '/p:GenerateTemporaryStoreCertificate=false',
            '/verbosity:minimal',
            '/nologo'
        )
        Invoke-ExternalTool -Executable $msbuildExe -Arguments $buildArgs

        $packages = @(Get-PackageFiles -Root $msixIntermediate)
        if ($packages.Count -eq 0) {
            throw "MSIX was not found in '$msixIntermediate'. Check the package build output and publish profile."
        }
        foreach ($package in $packages) {
            $extension = $package.Extension.ToLowerInvariant()
            $destination = Join-Path $msixFinal ("WordPadNeo_{0}_{1}{2}" -f $msixVersion, $runtimeId, $extension)
            Copy-Item -LiteralPath $package.FullName -Destination $destination -Force
            Write-Host "MSIX output: $destination" -ForegroundColor Green
        }
    }

    if ($Mode -in @('Unpackaged', 'Both')) {
        # IMPORTANT: Use the same Build target and Release-Unpackaged
        # configuration that are known to work when built in Visual Studio.
        # Do not use dotnet publish here: its output was observed to exit
        # immediately on startup for this project.
        $buildArguments = @(
            $ProjectPath,
            '/restore',
            '/t:Build',
            '/p:Configuration=Release-Unpackaged',
            "/p:Platform=$arch",
            "/p:RuntimeIdentifier=$runtimeId",
            '/verbosity:minimal',
            '/nologo'
        )
        Invoke-ExternalTool -Executable $msbuildExe -Arguments $buildArguments

        # Ask the SAME MSBuild executable for the resolved build directory.
        # -getProperty is supported by modern Visual Studio MSBuild.
        $propertyArguments = @(
            $ProjectPath,
            '/p:Configuration=Release-Unpackaged',
            "/p:Platform=$arch",
            "/p:RuntimeIdentifier=$runtimeId",
            '-getProperty:TargetDir',
            '/nologo'
        )
        $buildOutputText = & $msbuildExe @propertyArguments
        if ($LASTEXITCODE -ne 0) {
            throw "Could not resolve TargetDir for $runtimeId."
        }
        $buildOutput = ($buildOutputText | Out-String).Trim()
        if ([string]::IsNullOrWhiteSpace($buildOutput) -or
            -not (Test-Path -LiteralPath $buildOutput -PathType Container)) {
            throw "Build output directory not found: $buildOutput"
        }
        $buildOutput = (Resolve-Path -LiteralPath $buildOutput).Path
        $mainExecutable = Join-Path $buildOutput 'wordpadneo.exe'
        if (-not (Test-Path -LiteralPath $mainExecutable -PathType Leaf)) {
            throw "The expected EXE was not found in the build output: $mainExecutable"
        }

        # Copy the entire build output, preserving its directory structure.
        $publishDir = Join-Path $OutputDirectory ("portable\" + $runtimeId)
        if ($buildOutput.TrimEnd('\') -ieq $publishDir.TrimEnd('\')) {
            throw 'The build output must not be the same directory as the portable output.'
        }
        if (Test-Path -LiteralPath $publishDir) {
            Remove-Item -LiteralPath $publishDir -Recurse -Force
        }
        New-Item -ItemType Directory -Path $publishDir -Force | Out-Null
        Copy-Item -Path (Join-Path $buildOutput '*') -Destination $publishDir -Recurse -Force

        $portableExecutable = Join-Path $publishDir 'wordpadneo.exe'
        if (-not (Test-Path -LiteralPath $portableExecutable -PathType Leaf)) {
            throw "Portable EXE was not copied: $portableExecutable"
        }
        Write-Host "Build output: $buildOutput" -ForegroundColor DarkGray
        Write-Host "Portable folder: $publishDir" -ForegroundColor Green

        if (-not $SkipZip) {
            $zipPath = Join-Path $OutputDirectory ("WordPadNeo_{0}_{1}_portable.zip" -f $appVersion, $runtimeId)
            if (Test-Path -LiteralPath $zipPath) {
                Remove-Item -LiteralPath $zipPath -Force
            }
            Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $zipPath -CompressionLevel Optimal
            Write-Host "Portable ZIP: $zipPath" -ForegroundColor Green
        }
    }
}

Write-Host "`nFinished. Output: $OutputDirectory" -ForegroundColor Cyan
