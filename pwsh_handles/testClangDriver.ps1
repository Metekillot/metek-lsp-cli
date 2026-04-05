using module '/home/metek/sandbox/wrapperFill/LspWrapper.psd1'
function Get-LspWrapper {
    param(
[string]$repoRoot,
[string]$serverPath,
[string[]]$serverArgs)
$newWrapper = [LspWrapper]::new($serverPath, $repoRoot, $serverArgs)
return $newWrapper
}
[System.IO.Directory]::SetCurrentDirectory('/home/metek/code/git/CataclysmTLG')
Set-Location '/home/metek/code/git/CataclysmTLG'
$clangDriver = Get-LspWrapper '/home/metek/code/git/CataclysmTLG' (which clangd) @("--compile-commands-dir=/home/metek/code/git/CataclysmTLG/clang_build","--background-index","--limit-references=0","--limit-results=0","--sync")
$clangDriver.Open('src/do_turn.cpp')

Write-Host "`n--- Definition (do_turn.cpp:108:8) ---"
$clangDriver.TXP.Definition('src/do_turn.cpp', 108, 8) | Format-List

Write-Host "`n--- DocumentSymbol (do_turn.cpp) ---"
$clangDriver.Document.DocumentSymbol('src/do_turn.cpp') | ForEach-Object { $_.DocumentSymbol } | Select-Object Name, Kind

Write-Host "`n--- Notification Keys ---"
$clangDriver.Driver.Notifications.Keys | ForEach-Object { Write-Host "  $_" }

Write-Host "`n--- Diagnostics Count ---"
$diagKey = 'textDocument/publishDiagnostics'
$diag = $clangDriver.Driver.Notifications[$diagKey]
if ($diag) { Write-Host "  $($diag.Count) diagnostic(s)" } else { Write-Host "  No $diagKey received" }
