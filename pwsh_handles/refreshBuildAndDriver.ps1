using namespace System.Collections.Generic
[CmdletBinding(PositionalBinding=$false)]
param(
    [switch]$UsePreviousConfiguration,
    [switch]$DebugConfig,
    [string]$ProjectRoot,
    [string]$LanguageServerCommand,
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$AdditionalArguments
)
$Script:slnx = "$PSScriptRoot/../metek-lsp-cli.slnx"

if ((-not $ProjectRoot -or -not $LanguageServerCommand)) {
    $Missing = @()
    if (-not $ProjectRoot)           { $Missing = $Missing + '$ProjectRoot' }
    if (-not $LanguageServerCommand) { $Missing = $Missing + '$LanguageServerCommand' }
    throw "Missing required parameters: $($Missing -join ',')"
}

rm -rf "$PSScriptRoot/lib/*"
if ($DebugConfig) {
    dotnet build $slnx -c Debug --ucr
}
else {
    dotnet build $slnx -c Release --ucr
}
Add-Type -Path ./lib/metek-lsp-cli.dll
$Global:driver = [driver]::new($ProjectRoot, $LanguageServerCommand, $AdditionalArguments)