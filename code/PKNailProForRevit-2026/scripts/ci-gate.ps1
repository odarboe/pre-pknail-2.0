#!/usr/bin/env pwsh
#requires -Version 5.1
<#
.SYNOPSIS
    PKNAIL 2.0 full test gate — Revit-free unit tests + PKNailForRevit plugin build +
    in-process integration tests. Used by the self-hosted Revit CI lane
    (.github/workflows/ci-revit-integration.yml) and runnable locally as the required
    pre-commit gate for any change touching PKNailForRevit or the AI-import bridge.

.DESCRIPTION
    Runs three stages and fails fast on the first non-zero exit:
      1. Revit-free unit tests   (PKNail.AiImport.Tests, net8.0)
      2. Plugin build            (PKNailForRevit, net8.0-windows)
      3. Integration tests       (PKNail.AiImport.IntegrationTests, drives the in-process fake)

    Preconditions (see DevDoc/BuildSetup-2.0.md):
      * The vendored Revit 2026 API DLLs are present under RevitAPI/ and PLUSManaged.dll under
        Tools/SoftwareKey/ (git-ignored). On a self-hosted runner, set PKNAIL_VENDOR_LIBS to a
        folder containing them and this script copies them into place.
      * Revit 2026 is CLOSED, so the plugin output DLLs are not locked during the build.

.PARAMETER Configuration
    Build/test configuration. Default: Debug (matches the deployed add-in). CI uses Debug.

.EXAMPLE
    pwsh code/PKNailProForRevit-2026/scripts/ci-gate.ps1
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'

# Solution root = parent of this scripts/ folder (code/PKNailProForRevit-2026).
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    # --- Provision vendored DLLs (self-hosted runner) --------------------------------------
    # A fresh CI checkout has no git-ignored vendor binaries. If PKNAIL_VENDOR_LIBS points to a
    # folder holding them, copy them into the expected locations before building.
    if ($env:PKNAIL_VENDOR_LIBS) {
        Write-Host "Provisioning vendor DLLs from $env:PKNAIL_VENDOR_LIBS" -ForegroundColor Cyan
        New-Item -ItemType Directory -Force -Path 'RevitAPI', 'Tools/SoftwareKey' | Out-Null
        foreach ($dll in 'RevitAPI.dll', 'RevitAPIUI.dll', 'RevitAddInUtility.dll') {
            Copy-Item -Force (Join-Path $env:PKNAIL_VENDOR_LIBS $dll) (Join-Path 'RevitAPI' $dll)
        }
        Copy-Item -Force (Join-Path $env:PKNAIL_VENDOR_LIBS 'PLUSManaged.dll') 'Tools/SoftwareKey/PLUSManaged.dll'
    }

    # --- Pre-flight: the plugin cannot build without these ---------------------------------
    $missing = @()
    foreach ($p in 'RevitAPI/RevitAPI.dll', 'RevitAPI/RevitAPIUI.dll', 'RevitAPI/RevitAddInUtility.dll', 'Tools/SoftwareKey/PLUSManaged.dll') {
        if (-not (Test-Path $p)) { $missing += $p }
    }
    if ($missing.Count -gt 0) {
        throw "Missing vendored build dependencies:`n  $($missing -join "`n  ")`n" +
              "Provision them (see DevDoc/BuildSetup-2.0.md) or set PKNAIL_VENDOR_LIBS."
    }

    function Invoke-Step {
        param([string]$Name, [scriptblock]$Action)
        Write-Host "== $Name ==" -ForegroundColor Cyan
        & $Action
        if ($LASTEXITCODE -ne 0) { throw "$Name FAILED (exit $LASTEXITCODE)" }
    }

    Invoke-Step '1/3 Revit-free unit tests' {
        dotnet test PKNail.AiImport.Tests/PKNail.AiImport.Tests.csproj -c $Configuration --nologo `
            --logger "trx;LogFileName=unit-tests.trx"
    }

    Invoke-Step "2/3 Plugin build ($Configuration)" {
        dotnet build PKNailForRevit/PKNailForRevit.csproj -c $Configuration --nologo
    }

    Invoke-Step '3/3 In-process integration tests' {
        dotnet test PKNail.AiImport.IntegrationTests/PKNail.AiImport.IntegrationTests.csproj -c $Configuration --nologo `
            --logger "trx;LogFileName=integration-tests.trx"
    }

    Write-Host ''
    Write-Host 'ALL GATES PASSED (unit + plugin build + integration)' -ForegroundColor Green
}
catch {
    Write-Host ''
    Write-Host "GATE FAILED: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Common causes: Revit 2026 is still OPEN (it locks the plugin output DLLs during the build);" -ForegroundColor Yellow
    Write-Host "missing vendored DLLs (RevitAPI/ + PLUSManaged.dll); or a genuine test failure above." -ForegroundColor Yellow
    exit 1
}
finally {
    Pop-Location
}
