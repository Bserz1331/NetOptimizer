param(
    [string]$Version = "3.0.11",
    [string]$OutputDirectory = "",
    [string]$SigningCertificateThumbprint = "",
    [string]$TimestampUrl = ""
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$buildDirectory = Join-Path (Split-Path -Parent $root) "build"
New-Item -ItemType Directory -Path $buildDirectory -Force | Out-Null
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $root "build"
}
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

$compiler = Join-Path $env:windir "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path -LiteralPath $compiler)) {
    throw "x64 C# compiler not found: $compiler"
}

$rcCandidates = @(
    "C:\Program Files (x86)\Windows Kits\10\bin\10.0.19041.0\x64\rc.exe",
    "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\rc.exe"
)
$rc = $rcCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $rc) {
    $rc = Get-ChildItem -Path "C:\Program Files (x86)\Windows Kits\10\bin" -Filter rc.exe -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.DirectoryName -match "\\x64$" } |
        Sort-Object FullName -Descending |
        Select-Object -First 1 -ExpandProperty FullName
}
if (-not $rc) {
    throw "Windows SDK resource compiler rc.exe not found."
}

$resourcePath = Join-Path $buildDirectory "NetOptimizer.res"
Push-Location $root
try {
    & $rc /nologo /fo $resourcePath "NetOptimizer.rc"
}
finally {
    Pop-Location
}
if ($LASTEXITCODE -ne 0) { throw "rc.exe failed with exit code $LASTEXITCODE" }

$exePath = Join-Path $OutputDirectory ("NetOptimizer-v{0}.exe" -f $Version)
$cscArgs = @(
    "/nologo",
    "/target:winexe",
    "/platform:x64",
    "/optimize+",
    "/debug:pdbonly",
    ("/out:" + $exePath),
    ("/win32res:" + $resourcePath)
)
foreach ($reference in @("System.dll", "System.Core.dll", "System.Drawing.dll", "System.Windows.Forms.dll", "System.Xml.dll")) {
    $cscArgs += "/reference:$([IO.Path]::Combine((Split-Path $compiler), $reference))"
}
$cscArgs += Get-ChildItem -LiteralPath $root -Filter "*.cs" | Sort-Object Name | ForEach-Object { $_.FullName }
& $compiler @cscArgs
if ($LASTEXITCODE -ne 0) { throw "csc.exe failed with exit code $LASTEXITCODE" }

if (-not [string]::IsNullOrWhiteSpace($SigningCertificateThumbprint)) {
    $signTool = Get-ChildItem -Path "C:\Program Files (x86)\Windows Kits\10\bin" -Filter signtool.exe -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match "\\x64\\signtool\.exe$" } |
        Sort-Object FullName -Descending |
        Select-Object -First 1 -ExpandProperty FullName
    if (-not $signTool) {
        throw "Signing requested but Windows SDK signtool.exe was not found."
    }

    $signArgs = @(
        "sign",
        "/sha1", ($SigningCertificateThumbprint -replace "\s", ""),
        "/fd", "SHA256"
    )
    if (-not [string]::IsNullOrWhiteSpace($TimestampUrl)) {
        $signArgs += @("/tr", $TimestampUrl, "/td", "SHA256")
    }
    $signArgs += $exePath
    & $signTool @signArgs
    if ($LASTEXITCODE -ne 0) { throw "signtool.exe failed with exit code $LASTEXITCODE" }

    $signature = Get-AuthenticodeSignature -LiteralPath $exePath
    if ($signature.Status -ne "Valid") {
        throw "Authenticode signature verification failed: $($signature.Status)"
    }
    Write-Output ("Signing: valid ($($signature.SignerCertificate.Subject))")
}
else {
    Write-Output "Signing: skipped (no certificate thumbprint supplied)"
}

$hash = Get-FileHash -LiteralPath $exePath -Algorithm SHA256
[pscustomobject]@{
    Output = $exePath
    Size = (Get-Item -LiteralPath $exePath).Length
    SHA256 = $hash.Hash
    Version = (Get-Item -LiteralPath $exePath).VersionInfo.FileVersion
} | Format-List
