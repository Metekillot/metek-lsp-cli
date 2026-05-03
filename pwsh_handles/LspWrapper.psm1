<#
# Copyright (c) 2026, Joshua 'Joan Metek' Kidder
#
# This file is part of metek-lsp-cli,
#
# metek-lsp-cli is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License
# as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
#
# metek-lsp-cli is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.
# 
# You should have received a copy of the GNU Lesser General Public License along with metek-lsp-cli. If not, see <https://www.gnu.org/licenses/>. 
#>

# Assembly loaded via LspWrapper.psd1 RequiredAssemblies
#
# Wrapper for Driver, which is itself a more ergonomic handle on the omnisharp LSP functions
Add-Type -Path (Join-Path $PSScriptRoot 'lib/metek-lsp-cli.dll')
class LspWrapper {
    [Driver]$Driver

    LspWrapper([string]$ServerBinary) {
        $this.Init($ServerBinary, (Get-Location).Path, $null)
    }

    LspWrapper([string]$ServerBinary, [string]$ProjectRoot) {
        $this.Init($ServerBinary, $ProjectRoot, $null)
    }

    LspWrapper([string]$ServerBinary, [string]$ProjectRoot, [string[]]$ServerArgs) {
        $this.Init($ServerBinary, $ProjectRoot, $ServerArgs)
    }

    [void] Init([string]$ServerBinary, [string]$ProjectRoot, [string[]]$ServerArgs) {
        $this.Driver = [Driver]::new($ProjectRoot, $ServerBinary, $ServerArgs)
        $this.Driver.Initialize().GetAwaiter().GetResult()

    }

    [void] Open([string]$Path) {
        $this.Driver.OpenDocument($Path)
    }

    [void] Stop() {
        try {
            if (-not $this.Driver.ServerProcess.HasExited) {
                $this.Driver.ServerProcess.Kill()
            }
        }
        catch {}
    }
}

$TypeAccelerators = [psobject].Assembly.GetType(
    'System.Management.Automation.TypeAccelerators'
)
$TypeAccelerators::Add([LspWrapper].FullName, [LspWrapper])
