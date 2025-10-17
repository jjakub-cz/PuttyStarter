<# 
  Build & package (self-contained, single-file), versioned filenames, SHA256 hashes.
#>

# strict & nice errors
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Write-Host "=== Build script ==="

# Resolve important paths (relative to this script)
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$appDir     = Resolve-Path (Join-Path $scriptRoot '..\app')
# ensure ../publish exists (relative to this script)
$publishRoot = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '..\publish'
if (-not (Test-Path $publishRoot)) {
    New-Item -ItemType Directory -Path $publishRoot | Out-Null
}
$pubDir = Resolve-Path $publishRoot

if (-not $pubDir) {
    New-Item -ItemType Directory -Path (Join-Path $scriptRoot '..\publish') | Out-Null
    $pubDir = Resolve-Path (Join-Path $scriptRoot '..\publish')
}

Write-Host "Script root : $scriptRoot"
Write-Host "App dir     : $appDir"
Write-Host "Publish dir : $pubDir"

# Ensure dotnet is available
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet SDK not found in PATH."
}

# Enter app directory
Set-Location $appDir

# Locate the main .csproj (prefer .csproj; otherwise first .csproj)
$csproj = Get-ChildItem -Recurse -Filter 'PuttyStarter.csproj' -File -ErrorAction SilentlyContinue |
          Select-Object -First 1

if (-not $csproj) {
    $csproj = Get-ChildItem -Recurse -Filter '*.csproj' -File | Select-Object -First 1
}
if (-not $csproj) {
    throw "No .csproj found under $appDir"
}

Write-Host "Using project: $($csproj.FullName)"

# Read metadata from csproj (Version, TFM, AssemblyName)
[xml]$projXml = Get-Content $csproj.FullName
# Some projects have multiple PropertyGroup entries; take first non-empty
$pgs = @($projXml.Project.PropertyGroup)

function First-NonEmpty([string[]]$vals, [string]$fallback = '') {
    foreach ($v in $vals) { if ($v -and $v.Trim().Length -gt 0) { return $v.Trim() } }
    return $fallback
}

$Version       = First-NonEmpty ($pgs.Version)
$TargetFx      = First-NonEmpty ($pgs.TargetFramework, 'net8.0-windows')
$AssemblyName  = First-NonEmpty ($pgs.AssemblyName, [IO.Path]::GetFileNameWithoutExtension($csproj.Name))

if (-not $Version) { 
    Write-Warning "No <Version> in csproj; defaulting to 1.0.0"
    $Version = '1.0.0'
}

Write-Host "Version:       $Version"
Write-Host "TargetFramework: $TargetFx"
Write-Host "AssemblyName:  $AssemblyName"

# Restore & Publish (self-contained, single-file, win-x64)
Write-Host "`n== dotnet restore =="
dotnet restore "$($csproj.FullName)"

Write-Host "`n== dotnet publish (Release, self-contained, single-file, win-x64) =="
$rid = 'win-x64'
$pubArgs = @(
  'publish', "`"$($csproj.FullName)`"",
  '-c','Release',
  '-r', $rid,
  '--self-contained','true',
  '-p:PublishSingleFile=true',
  '-p:PublishTrimmed=false'
)
dotnet @pubArgs

# Compute publish folder path
$projDir = Split-Path -Parent $csproj.FullName
$dotnetPublishDir = Join-Path $projDir "bin\Release\$TargetFx\$rid\publish"

if (-not (Test-Path $dotnetPublishDir)) {
    throw "Publish output not found at $dotnetPublishDir"
}

# Locate produced EXE (single-file -> AssemblyName.exe)
$builtExe = Join-Path $dotnetPublishDir "$AssemblyName.exe"
if (-not (Test-Path $builtExe)) {
    # fallback: pick the newest exe in publish folder
    $builtExe = Get-ChildItem $dotnetPublishDir -Filter '*.exe' -File | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $builtExe) { throw "No .exe found in $dotnetPublishDir" }
    $builtExe = $builtExe.FullName
}

Write-Host "Built EXE: $builtExe"

# Compose final filenames (in ../publish relative to script)
$versionTag = $Version
$baseName   = "$AssemblyName-$versionTag-win-x64"
$targetExe  = Join-Path $pubDir "$baseName.exe"
$targetZip  = Join-Path $pubDir "$baseName.zip"
$exeShaFile = "$targetExe.sha256"
$zipShaFile = "$targetZip.sha256"

# Copy/rename EXE
Copy-Item -LiteralPath $builtExe -Destination $targetExe -Force
Write-Host "Copied to: $targetExe"

# SHA256 for EXE
$exeHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $targetExe).Hash
"$exeHash  $(Split-Path $targetExe -Leaf)" | Out-File -FilePath $exeShaFile -Encoding ASCII -Force
Write-Host "EXE SHA256: $exeHash"
Write-Host "Saved: $exeShaFile"

# ZIP (only the EXE)
if (Test-Path $targetZip) { Remove-Item $targetZip -Force }
Compress-Archive -LiteralPath $targetExe -DestinationPath $targetZip
Write-Host "ZIP created: $targetZip"

# SHA256 for ZIP
$zipHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $targetZip).Hash
"$zipHash  $(Split-Path $targetZip -Leaf)" | Out-File -FilePath $zipShaFile -Encoding ASCII -Force
Write-Host "ZIP SHA256: $zipHash"
Write-Host "Saved: $zipShaFile"

Write-Host "`n=== Done ==="
Write-Host "Artifacts:"
Write-Host "  $targetExe"
Write-Host "  $exeShaFile"
Write-Host "  $targetZip"
Write-Host "  $zipShaFile"

Write-Host "`nChecksum checks:"
# === Verify checksums at the end ===
function Verify-Checksum {
  param([string]$file, [string]$shaFile)

  if (!(Test-Path $file))   { throw "Missing file: $file" }
  if (!(Test-Path $shaFile)) { throw "Missing checksum file: $shaFile" }

  $raw = Get-Content -Raw -ErrorAction Stop $shaFile
  # Supports "HASH  filename" or just "HASH"
  $expected = ($raw -split '\s+')[0].Trim()
  if ($expected -notmatch '^[0-9A-Fa-f]{64}$') { throw "Invalid SHA256 in $shaFile" }

  $actual = (Get-FileHash -Algorithm SHA256 -LiteralPath $file).Hash
  if ($actual.ToLower() -ne $expected.ToLower()) {
    Write-Error "  Checksum mismatch for $(Split-Path $file -Leaf). Expected $expected, got $actual"
    return $false
  }

  Write-Host "  Checksum OK for $(Split-Path $file -Leaf): $actual"
  return $true
}

$okExe = Verify-Checksum -file $targetExe -shaFile $exeShaFile
$okZip = Verify-Checksum -file $targetZip -shaFile $zipShaFile
if (-not ($okExe -and $okZip)) { throw "  Checksum verification failed." }