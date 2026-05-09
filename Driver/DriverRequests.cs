/*
 * Copyright (c) 2026, Joshua 'Joan Metek' Kidder
 *
 * This file is part of metek-lsp-cli,
 *
 * metek-lsp-cli is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 *
 * metek-lsp-cli is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License along with metek-lsp-cli. If not, see <https://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Client;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;

namespace Metek.LspCli;

public static class AST
{
    public const string Method = "textDocument/ast";
}

public record ASTParams
{
    public TextDocumentIdentifier textDocument;
    public OmniSharp.Extensions.LanguageServer.Protocol.Models.Range range;

    public ASTParams(
        string path,
        int startLine,
        int startCharacter,
        int endLine,
        int endCharacter
    )
    {
        textDocument = DocumentUri.FromFileSystemPath(Path.GetFullPath(path));
        range = new(startLine, startCharacter, endLine, endCharacter);
    }
}

public partial class Driver
{
    public TDXTable TDX { get; set; } = null;
    public DOCTable DOC { get; set; } = null;
    public WSPTable WSP { get; set; } = null;

    public DriverResults Results = new();

    public class TokenDecoder(Driver _driver)
    {
        public SemanticTokensLegend _legend =>
            _driver.ClientInterface.ServerSettings.Capabilities.SemanticTokensProvider.Legend;

        public List<SemanticTokenItem> DecodeTokens(SemanticTokens tokens)
        {
            return DecodeTokens(tokens, this._legend);
        }

        public List<SemanticTokenItem> DecodeTokens(SemanticTokens tokens, SemanticTokensLegend legend)
        {
            var result = new List<SemanticTokenItem>();
            var data = tokens.Data;

            int currentLine = 0;
            int currentCharacter = 0;

            for (int i = 0; i < data.Length; i += 5)
            {
                int deltaLine = data[i];
                int deltaStart = data[i + 1];
                int length = data[i + 2];
                int typeIndex = data[i + 3];
                int modifierMask = data[i + 4];

                // Update absolute positions
                currentLine += deltaLine;
                if (deltaLine > 0)
                {
                    currentCharacter = deltaStart;
                }
                else
                {
                    currentCharacter += deltaStart;
                }

                result.Add(new SemanticTokenItem
                {
                    Line = currentLine,
                    Character = currentCharacter,
                    Length = length,
                    Type = legend.TokenTypes.ElementAtOrDefault(typeIndex),
                    Modifiers = DecodeModifiers(modifierMask, legend.TokenModifiers)
                });
            }

            return result;
        }

        private List<string> DecodeModifiers(int mask, Container<SemanticTokenModifier> legendModifiers)
        {
            var modifiers = new List<string>();
            for (int i = 0; i < legendModifiers.Count(); i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    modifiers.Add(legendModifiers.ElementAt(i));
                }
            }

            return modifiers;
        }

        public class SemanticTokenItem
        {
            public int Line { get; set; }
            public int Character { get; set; }
            public int Length { get; set; }
            public string Type { get; set; }
            public List<string> Modifiers { get; set; }
        }
    }

    public TokenDecoder Decoder = null;

    public void SetupRequests()
    {
        System.Console.WriteLine("Configuring Requests.");
        TDX = new TDXTable(this);
        DOC = new DOCTable(this);
        WSP = new WSPTable(this);
        Decoder = new TokenDecoder(this);
    }

    public readonly struct DriverResults()
    {
        public readonly SortedDictionary<string, WorkspaceSymbol[]> WorkspaceSymbols = [];
        public readonly SortedDictionary<string, ObjectTreeType> ObjectTrees = [];
        public readonly SortedDictionary<string, SymbolInformationOrDocumentSymbol[]> DocumentSymbols = [];
        public readonly SortedDictionary<string, SemanticTokens> SemanticTokens = [];
        public readonly SortedDictionary<string, FoldingRange[]> FoldingRanges = [];
        public readonly SortedDictionary<string, CallHierarchyItem[]> CallHierarchyItems = [];
        public readonly SortedDictionary<string, CompletionItem[]> Completions = [];
        public readonly SortedDictionary<string, LocationOrLocationLink[]> Declarations = [];
        public readonly SortedDictionary<string, LocationOrLocationLink[]> Definitions = [];
        public readonly SortedDictionary<string, DocumentHighlight[]> DocumentHighlights = [];
        public readonly SortedDictionary<string, LocationOrLocationLink[]> Implementations = [];
        public readonly SortedDictionary<string, Moniker[]> Monikers = [];
        public readonly SortedDictionary<string, Location[]> References = [];
        public readonly SortedDictionary<string, LocationOrLocationLink[]> TypeDefinitions = [];
        public readonly SortedDictionary<string, TypeHierarchyItem[]> TypeHierarchyItems = [];
        public readonly SortedDictionary<string, Hover> Hover = [];
        public readonly SortedDictionary<string, QueryAnnotationTreeResult> AnnotationTrees = [];
        internal readonly WorkspaceSymbol[] NoneWorkspaceSymbols = [];
        internal readonly SymbolInformationOrDocumentSymbol[] NoneDocumentSymbols = [];
        internal readonly FoldingRange[] NoneFoldingRanges = [];
        internal readonly CallHierarchyItem[] NoneCallHierarchyItems = [];
        internal readonly CompletionItem[] NoneCompletions = [];
        internal readonly LocationOrLocationLink[] NoneDeclarations = [];
        internal readonly LocationOrLocationLink[] NoneDefinitions = [];
        internal readonly DocumentHighlight[] NoneDocumentHighlights = [];
        internal readonly LocationOrLocationLink[] NoneImplementations = [];
        internal readonly Moniker[] NoneMonikers = [];
        internal readonly Location[] NoneReferences = [];
        internal readonly LocationOrLocationLink[] NoneTypeDefinitions = [];
        internal readonly TypeHierarchyItem[] NoneTypeHierarchyItems = [];
    }
}

public abstract class RequestTable(Driver _driver)
{
    protected LanguageClient client => driver.ClientInterface;
    protected Driver driver => _driver;
}

public class DOCTable(Driver _driver) : RequestTable(_driver)
{
    public async Task<SymbolInformationOrDocumentSymbol[]> DocumentSymbol(string fileName)
    {
        var req = await client.RequestDocumentSymbol(new DocumentSymbolParams
        {
            TextDocument = DocumentUri.FromFileSystemPath(Path.GetFullPath(fileName, driver.RootPath))
        });
        if (req is null) return driver.Results.NoneDocumentSymbols;
        driver.Results.DocumentSymbols[fileName] = req.ToArray();
        return driver.Results.DocumentSymbols[fileName];
    }

    public async Task<SemanticTokens?> SemanticTokensRange(string fileName, int startLine, int startCharacter,
        int endLine, int endCharacter)
    {
        var req = await client.RequestSemanticTokensRange(new SemanticTokensRangeParams
        {
            TextDocument = DocumentUri.FromFileSystemPath(Path.GetFullPath(fileName, driver.RootPath)),
            Range = new(startLine, startCharacter, endLine, endCharacter)
        });
        if (req is null) return null;
        driver.Results.SemanticTokens[$"{fileName}:{startLine}:{startCharacter}:{endLine}:{endCharacter}"] = req;
        return req;
    }

    public async Task<SemanticTokens?> SemanticTokensFull(string fileName)
    {
        var req = await client.RequestSemanticTokensFull(new SemanticTokensParams
        {
            TextDocument = DocumentUri.FromFileSystemPath(Path.GetFullPath(fileName, driver.RootPath))
        });
        if (req is null) return null;
        driver.Results.SemanticTokens[fileName] = req;
        return req;
    }

    public async Task<FoldingRange[]> FoldingRange(string fileName)
    {
        var req = await client.RequestFoldingRange(new FoldingRangeRequestParam
        {
            TextDocument = DocumentUri.FromFileSystemPath(Path.GetFullPath(fileName, driver.RootPath))
        });
        if (req is null) return driver.Results.NoneFoldingRanges;
        driver.Results.FoldingRanges[fileName] = req.ToArray();
        return driver.Results.FoldingRanges[fileName];
    }
}

public class TDXTable(Driver _driver) : RequestTable(_driver)
{
    public async Task<CallHierarchyItem[]> CallHierarchyPrepare(string fileName, int line, int character)
    {
        var req = await client.RequestCallHierarchyPrepare(new CallHierarchyPrepareParams
        {
            TextDocument = DocumentUri.FromFileSystemPath(Path.GetFullPath(fileName, driver.RootPath)),
            Position = (line, character)
        });
        if (req is null) return driver.Results.NoneCallHierarchyItems;
        driver.Results.CallHierarchyItems[$"{fileName}:{line}:{character}"] = req.ToArray();
        return driver.Results.CallHierarchyItems[$"{fileName}:{line}:{character}"];
    }

    public async Task<CompletionItem[]> Completion(string fileName, int line, int character)
    {
        var req = await client.RequestCompletion(new CompletionParams
        {
            TextDocument = DocumentUri.FromFileSystemPath(Path.GetFullPath(fileName, driver.RootPath)),
            Position = (line, character),
        });
        if (req is null) return driver.Results.NoneCompletions;
        driver.Results.Completions[$"{fileName}:{line}:{character}"] = req.ToArray();
        return driver.Results.Completions[$"{fileName}:{line}:{character}"];
    }

    public async Task<LocationOrLocationLink[]> Declaration(string fileName, int line, int character)
    {
        var req = await client.RequestDeclaration(new DeclarationParams
        {
            TextDocument = DocumentUri.FromFileSystemPath(Path.GetFullPath(fileName, driver.RootPath)),
            Position = (line, character)
        });
        if (req is null) return driver.Results.NoneDeclarations;
        driver.Results.Declarations[$"{fileName}:{line}:{character}"] = req.ToArray();
        return driver.Results.Declarations[$"{fileName}:{line}:{character}"];
    }

    public async Task<LocationOrLocationLink[]> Definition(string fileName, int line, int character)
    {
        var req = await client.RequestDefinition(new DefinitionParams
        {
            TextDocument = DocumentUri.FromFileSystemPath(Path.GetFullPath(fileName, driver.RootPath)),
            Position = (line, character)
        });
        if (req is null) return driver.Results.NoneDefinitions;
        driver.Results.Definitions[$"{fileName}:{line}:{character}"] = req.ToArray();
        return driver.Results.Definitions[$"{fileName}:{line}:{character}"];
    }

    public async Task<DocumentHighlight[]> DocumentHighlight(string fileName, int line, int character)
    {
        var req = await client.RequestDocumentHighlight(new DocumentHighlightParams
        {
            TextDocument = DocumentUri.FromFileSystemPath(Path.GetFullPath(fileName, driver.RootPath)),
            Position = (line, character)
        });
        if (req is null) return driver.Results.NoneDocumentHighlights;
        driver.Results.DocumentHighlights[$"{fileName}:{line}:{character}"] = req.ToArray();
        return driver.Results.DocumentHighlights[$"{fileName}:{line}:{character}"];
    }

    public async Task<Hover?> Hover(string fileName, int line, int character)
    {
        var req = await client.RequestHover(new HoverParams
        {
            TextDocument = DocumentUri.FromFileSystemPath(Path.GetFullPath(fileName, driver.RootPath)),
            Position = (line, character)
        });
        if (req is null) return null;
        driver.Results.Hover[$"{fileName}:{line}:{character}"] = req;
        return req;
    }

    public async Task<LocationOrLocationLink[]> Implementation(string fileName, int line, int character)
    {
        var req = await client.RequestImplementation(new ImplementationParams
        {
            TextDocument = DocumentUri.FromFileSystemPath(Path.GetFullPath(fileName, driver.RootPath)),
            Position = (line, character)
        });
        if (req is null) return driver.Results.NoneImplementations;
        driver.Results.Implementations[$"{fileName}:{line}:{character}"] = req.ToArray();
        return driver.Results.Implementations[$"{fileName}:{line}:{character}"];
    }

    public Task<LinkedEditingRanges> LinkedEditingRange(string fileName, int line, int character)
        => client.RequestLinkedEditingRange(new LinkedEditingRangeParams
        {
            TextDocument = DocumentUri.FromFileSystemPath(Path.GetFullPath(fileName, driver.RootPath)),
            Position = (line, character)
        });

    public async Task<Moniker[]> Monikers(string fileName, int line, int character)
    {
        var req = await client.RequestMonikers(new MonikerParams
        {
            TextDocument = DocumentUri.FromFileSystemPath(Path.GetFullPath(fileName, driver.RootPath)),
            Position = (line, character)
        });
        if (req is null) return driver.Results.NoneMonikers;
        driver.Results.Monikers[$"{fileName}:{line}:{character}"] = req.ToArray();
        return driver.Results.Monikers[$"{fileName}:{line}:{character}"];
    }

    public async Task<Location[]> References(string fileName, int line, int character)
    {
        var req = await client.RequestReferences(new ReferenceParams
        {
            TextDocument = DocumentUri.FromFileSystemPath(Path.GetFullPath(fileName, driver.RootPath)),
            Position = (line, character),
            Context = new ReferenceContext { IncludeDeclaration = false }
        });
        if (req is null) return driver.Results.NoneReferences;
        driver.Results.References[$"{fileName}:{line}:{character}"] = req.ToArray();
        return driver.Results.References[$"{fileName}:{line}:{character}"];
    }

    public Task<SignatureHelp?> SignatureHelp(string fileName, int line, int character)
        => client.RequestSignatureHelp(new SignatureHelpParams
        {
            TextDocument = DocumentUri.FromFileSystemPath(Path.GetFullPath(fileName, driver.RootPath)),
            Position = (line, character),
        });

    public async Task<LocationOrLocationLink[]> TypeDefinition(string fileName, int line, int character)
    {
        var req = await client.RequestTypeDefinition(new TypeDefinitionParams
        {
            TextDocument = DocumentUri.FromFileSystemPath(Path.GetFullPath(fileName, driver.RootPath)),
            Position = (line, character)
        });
        if (req is null) return driver.Results.NoneTypeDefinitions;
        driver.Results.TypeDefinitions[$"{fileName}:{line}:{character}"] = req.ToArray();
        return driver.Results.TypeDefinitions[$"{fileName}:{line}:{character}"];
    }

    public async Task<TypeHierarchyItem[]> TypeHierarchyPrepare(string fileName, int line, int character)
    {
        var req = await client.RequestTypeHierarchyPrepare(new TypeHierarchyPrepareParams
        {
            TextDocument = DocumentUri.FromFileSystemPath(Path.GetFullPath(fileName, driver.RootPath)),
            Position = (line, character)
        });
        if (req is null) return driver.Results.NoneTypeHierarchyItems;
        driver.Results.TypeHierarchyItems[$"{fileName}:{line}:{character}"] = req.ToArray();
        return driver.Results.TypeHierarchyItems[$"{fileName}:{line}:{character}"];
    }

    public async Task<QueryAnnotationTreeResult?> AnnotationQuery(string fileName, int line, int character)
    {
        var req = await client.RequestQueryAnnotationTree(new QueryAnnotationTreeParams
        {
            TextDocument = DocumentUri.FromFileSystemPath(Path.GetFullPath(fileName, driver.RootPath)),
            Position = (line, character)
        });
        if (req is null)
        {
            return null;
        }

        driver.Results.AnnotationTrees[$"{fileName}:{line}:{character}"] = req;
        return req;
    }
}

public class WSPTable(Driver _driver) : RequestTable(_driver)
{
    public async Task<WorkspaceSymbol[]> WorkspaceSymbols(string query)
    {
        var req = await client.RequestWorkspaceSymbols(new WorkspaceSymbolParams
        {
            Query = query
        });
        if (req is null)
        {
            return driver.Results.NoneWorkspaceSymbols;
        }

        driver.Results.WorkspaceSymbols[query] = req.ToArray();
        return driver.Results.WorkspaceSymbols[query];
    }

    public async Task<ObjectTreeType> QueryObjectTree(string _path)
    {
        var req = await client.RequestQueryObjectTree(new QueryObjectTreeParams { path = _path },
            CancellationToken.None);
        driver.Results.ObjectTrees[_path] = req;
        return req;
    }

    public async Task<ObjectTreeType> QueryObjectTree(string _path, bool _recursive)
    {
        var req = await client.RequestQueryObjectTree(
            new QueryObjectTreeParams { path = _path, recursive = _recursive },
            CancellationToken.None);
        driver.Results.ObjectTrees[_path] = req;
        return req;
    }
}
