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
if ((-not $ProjectRoot -or -not $LanguageServerCommand) -and -not $UsePreviousConfiguration) {
    $Missing = @()
    if (-not $ProjectRoot)           { $Missing = $Missing + '$ProjectRoot' }
    if (-not $LanguageServerCommand) { $Missing = $Missing + '$LanguageServerCommand' }
    throw "Missing required parameters: $($Missing -join ',') || Did you mean to use -UsePreviousConfiguration?"
}

$PreviousConfigFile = "$PSScriptRoot/PreviousDriverRefreshConfig.cli_xml"

[ConfigInformation]$Config = $null
if ($UsePreviousConfiguration) {
    if (![System.IO.Path]::Exists($PreviousConfigFile)) {
        throw ('$UsePreviousConfiguration was enabled, but there was no file present at ' + $PreviousConfigFile)
    }
    # I am overriding your ErrorPreference, pray I do not override further
    try { $Config = Import-Clixml -Path $PreviousConfigFile }
    catch { throw $_ }
} else {
    $Config = [ConfigInformation]::new($ProjectRoot, $LanguageServerCommand, $AdditionalArguments)
    $Config | Export-Clixml -Path $PreviousConfigFile
}

dotnet clean $slnx
if ($DebugConfig) {
    dotnet build $slnx -c Debug --ucr
}
else {
    dotnet build $slnx -c Release --ucr
}
Add-Type -Path ./lib/metek-lsp-cli.dll
$driver = [driver]::new($Config._ProjectRoot, $Config._LanguageServerCommand, $Config._AdditionalArguments)