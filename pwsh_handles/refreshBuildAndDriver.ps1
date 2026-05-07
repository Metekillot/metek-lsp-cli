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

class ConfigInformation {
    [string]   $_ProjectRoot
    [string]   $_LanguageServerCommand
    [string[]] $_AdditionalArguments

    ConfigInformation([string]$root, [string]$command, [string[]]$additionalArgs) {
        $this._ProjectRoot           = $root
        $this._LanguageServerCommand = $command
        $this._AdditionalArguments   = $additionalArgs
    }
}
if ((-not $ProjectRoot -or -not $LanguageServerCommand)) {
    $Missing = @()
    if (-not $ProjectRoot)           { $Missing = $Missing + '$ProjectRoot' }
    if (-not $LanguageServerCommand) { $Missing = $Missing + '$LanguageServerCommand' }
    throw "Missing required parameters: $($Missing -join ',') || Did you mean to use -UsePreviousConfiguration?"
}

rm -rf "$PSScriptRoot/lib/*"
if ($DebugConfig) {
    dotnet build $slnx -c Debug --ucr
}
else {
    dotnet build $slnx -c Release --ucr
}
Add-Type -Path ./lib/metek-lsp-cli.dll
$Global:driver = [driver]::new($Config._ProjectRoot, $Config._LanguageServerCommand, $Config._AdditionalArguments)