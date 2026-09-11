param(
    [string]$Version = "3.0.13",
    [string]$OutputDirectory = "",
    [string]$SigningCertificateThumbprint = "",
    [string]$TimestampUrl = ""
)

$ErrorActionPreference = "Stop"
$sourceBuild = Join-Path $PSScriptRoot "src\build.ps1"
if (-not (Test-Path -LiteralPath $sourceBuild)) {
    throw "Source build script not found: $sourceBuild"
}
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $PSScriptRoot "dist"
}

& $sourceBuild `
    -Version $Version `
    -OutputDirectory $OutputDirectory `
    -SigningCertificateThumbprint $SigningCertificateThumbprint `
    -TimestampUrl $TimestampUrl
if ($LASTEXITCODE -ne 0) {
    throw "Source build failed with exit code $LASTEXITCODE"
}
