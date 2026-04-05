#!/usr/bin/env pwsh
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
param (
    [Parameter(Mandatory=$true, Position=0, HelpMessage="Path to the C# file containing the method signatures")]
    [string]$FilePath
)

if (-not (Test-Path $FilePath)) {
    Write-Error "File not found: $FilePath"
    exit 1
}

$Lines = Get-Content $FilePath

foreach ($line in $Lines) {
    if ([string]::IsNullOrWhiteSpace($line)) { continue }

    # Match the signature pattern
    if ($line -match 'public static (.*?) (Request[A-Za-z0-9_]+)\(this [A-Za-z0-9_]+ [A-Za-z0-9_]+,\s*([A-Za-z0-9_]+)\s+request') {
        $rawReturnType = $matches[1].Trim()
        $targetMethod = $matches[2]
        $paramsType = $matches[3]

        # 1. Parse Return Type
        $returnType = $rawReturnType
        if ($rawReturnType -match '^IRequestProgressObservable<.+,\s*(.+?)>$') {
            $returnType = $matches[1]
        } elseif ($rawReturnType -match '^Task<(.+?)>$') {
            $returnType = $matches[1]
        }

        # 2. Derive Wrapper Name
        $wrapperMethod = $targetMethod -replace '^Request', ''

        # 3. Determine if `.AsTask()` is needed
        $asTaskSuffix = ""
        if ($rawReturnType -match '^IRequestProgressObservable') {
            $asTaskSuffix = ".AsTask()"
        }

        # 4. Heuristically determine parameter requirements
        $needsPosition = $paramsType -match "(Hover|Completion|SignatureHelp|Declaration|Definition|TypeDefinition|Implementation|Reference|DocumentHighlight|Rename|CallHierarchyPrepare|LinkedEditingRange|Moniker|DocumentOnTypeFormatting|TypeHierarchyPrepare)Params"
        $needsRange = $paramsType -match "(DocumentRangeFormatting|SemanticTokensRange|CodeAction|InlayHint|InlineValue|ColorPresentation)Params"
        $isWorkspaceOrConfig = $paramsType -match "(Workspace|Configuration|Initialize|Will(Create|Delete|Rename)Files|CallHierarchy(Incoming|Outgoing)|TypeHierarchy(Supertypes|Subtypes))Params"

        $methodArgs = "string fileName"
        $objectInit = "`n            TextDocument = DocumentUri.FromFileSystemPath(Path.GetFullPath(fileName, Driver.RootPath))"

        if ($needsPosition) {
            $methodArgs += ", int line, int character"
            $objectInit += ",`n            Position = (line, character)"
        }

        if ($needsRange) {
            $methodArgs += ", int startLine, int startCharacter, int endLine, int endCharacter"
            # Utilizes the implicit conversion operator: public static implicit operator Range((Position start, Position end) value)
            $objectInit += ",`n            Range = ((startLine, startCharacter), (endLine, endCharacter))"
        }

        if ($isWorkspaceOrConfig) {
            $methodArgs = ""
            $objectInit = ""
        }

        # 5. Build and output the C# method string
        if ($objectInit -eq "") {
            $generatedCode = @"
public Task<$returnType> ${wrapperMethod}($methodArgs)
    => client.${targetMethod}(new ${paramsType}())${asTaskSuffix};
"@
        } else {
            $generatedCode = @"
public Task<$returnType> ${wrapperMethod}($methodArgs)
    => client.${targetMethod}(new ${paramsType}
    {$objectInit
    })${asTaskSuffix};
"@
        }

        Write-Output $generatedCode
        Write-Output ""
    }
}
