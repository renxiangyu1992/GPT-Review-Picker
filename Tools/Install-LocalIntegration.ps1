[CmdletBinding()]
param(
    [string]$SourceExe,
    [switch]$SetUserEnvironmentVariable
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($SourceExe)) {
    $repositoryRoot = Split-Path -Parent $PSScriptRoot
    $SourceExe = Join-Path $repositoryRoot "bin\Release\net8.0-windows\win-x64\publish\GPTReviewPicker.exe"
}

$source = (Resolve-Path -LiteralPath $SourceExe).Path
if ([IO.Path]::GetExtension($source) -ine ".exe") {
    throw "Source must be a GPTReviewPicker.exe file: $source"
}

$targetDirectory = Join-Path $env:LOCALAPPDATA "GPTReviewPicker"
$target = Join-Path $targetDirectory "GPTReviewPicker.exe"
New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null

if ($source -ine $target) {
    $temporary = "$target.tmp.$([guid]::NewGuid().ToString('N'))"
    try {
        Copy-Item -LiteralPath $source -Destination $temporary
        Move-Item -LiteralPath $temporary -Destination $target -Force
    }
    finally {
        if (Test-Path -LiteralPath $temporary) {
            Remove-Item -LiteralPath $temporary -Force
        }
    }
}

if ($SetUserEnvironmentVariable) {
    [Environment]::SetEnvironmentVariable("GPT_REVIEW_PICKER_EXE", $target, "User")
    $env:GPT_REVIEW_PICKER_EXE = $target
}

$sourceHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
$targetHash = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
if ($sourceHash -ne $targetHash) {
    throw "Installed executable hash does not match the source executable."
}

[PSCustomObject]@{
    Source = $source
    Installed = $target
    SHA256 = $targetHash
    UserEnvironmentVariableSet = [bool]$SetUserEnvironmentVariable
}
