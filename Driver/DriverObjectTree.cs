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

using System.Text.Json.Serialization;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using MediatR;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.JsonRpc.Generation;
using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using Newtonsoft.Json.Linq;
using JsonSerializer = System.Text.Json.JsonSerializer;
using OmniSharp.Extensions.LanguageServer.Protocol;


namespace Metek.LspCli;

[Parallel]
[Method("experimental/dreammaker/objectTree2", Direction.ClientToServer)]
[
    GenerateHandler(Name = "QueryObjectTree", AllowDerivedRequests = true),
    GenerateRequestMethods(typeof(ITextDocumentLanguageClient), typeof(ILanguageClient))
]
public class QueryObjectTreeParams : IRequest<ObjectTreeType>
{
    public string path;
    public bool? recursive;
}

[JsonSourceGenerationOptions(WriteIndented = true, IndentSize = 1)]
[JsonSerializable(typeof(ObjectTreeType))]
[JsonSerializable(typeof(ObjectTreeVar))]
[JsonSerializable(typeof(ObjectTreeProc))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}

public record ObjectTreeType(
    string name,
    SymbolKind kind,
    Location? location,
    ObjectTreeVar[] vars,
    ObjectTreeProc[] procs,
    ObjectTreeType[] children,
    long n_vars,
    long n_procs,
    long n_children
)
{
    public IEnumerable<ObjectTreeType> Decompose(ObjectTreeType node)
    {
        return node.children.SelectMany(n => n.children);
    }
    
    public override string ToString()
    {
        return JsonSerializer.Serialize(this, SourceGenerationContext.Default.ObjectTreeType);
    }
    }
public record ObjectTreeVar(
    string name,
    SymbolKind kind,
    Location? location,
    bool is_declaration)
{
    public override string ToString()
    {
        return JsonSerializer.Serialize(this, SourceGenerationContext.Default.ObjectTreeVar);
    }
}

public record ObjectTreeProc(
    string name,
    SymbolKind kind,
    Location? location,
    bool? is_verb
)
{
    public override string ToString()
    {
        return JsonSerializer.Serialize(this, SourceGenerationContext.Default.ObjectTreeProc);
    }
}
[Parallel]
[Method("experimental/dreammaker/queryAnnotation", Direction.ClientToServer)]
[
    GenerateHandler(Name = "QueryAnnotationTree", AllowDerivedRequests = true),
    GenerateRequestMethods(typeof(ITextDocumentLanguageClient), typeof(ILanguageClient))
]
public record QueryAnnotationTreeParams : TextDocumentPositionParams, IRequest<QueryAnnotationTreeResult?>
{
}

public record QueryAnnotationTreeResult
(
    AnnotationTuple[]? outputAnnotations
);

[Parallel]
[Method("experimental/dreammaker/queryAnnotationRange", Direction.ClientToServer)]
[
    GenerateHandler(Name = "QueryAnnotationRange", AllowDerivedRequests = true),
    GenerateRequestMethods(typeof(ITextDocumentLanguageClient), typeof(ILanguageClient))
]
public record QueryAnnotationRangeParams : IRequest<QueryAnnotationTreeResult?>
{
    [JsonPropertyName("text_document")]
    public TextDocumentIdentifier text_document;
    public OmniSharp.Extensions.LanguageServer.Protocol.Models.Range range;
}

public partial class TDXTable
{
    public async Task<QueryAnnotationTreeResult?> AnnotationQueryRange(
        string fileName, OmniSharp.Extensions.LanguageServer.Protocol.Models.Range range)
    {
        return await AnnotationQueryRange(
            fileName, range.Start.Line, range.Start.Character, range.End.Line, range.End.Character);
    }

    public async Task<QueryAnnotationTreeResult?> AnnotationQueryRange(
        string fileName, int startLine, int startCharacter, int endLine, int endCharacter)
    {
        var req = await client.RequestQueryAnnotationRange(new QueryAnnotationRangeParams
        {
            text_document = DocumentUri.FromFileSystemPath(Path.GetFullPath(fileName, driver.RootPath)),
            range = new(startLine, startCharacter, endLine, endCharacter)
        });
        if (req is null) return null;
        driver.Results.AnnotationTrees[$"{fileName}:{startLine}:{startCharacter}:{endLine}:{endCharacter}"] = req;
        return req;
    }

    public async Task<QueryAnnotationTreeResult?> AnnotationQueryRange(
        string fileName, (int line, int character) start, (int line, int character) end)
    {
        return await AnnotationQueryRange(fileName, start.line, start.character, end.line, end.character);
    }

    public async Task<QueryAnnotationTreeResult?> AnnotationQueryRange(
        string fileName, int startLine, int endLine)
    {
        return await AnnotationQueryRange(fileName, startLine, 0, endLine, 0);
    }

    public async Task<QueryAnnotationTreeResult?> AnnotationQueryRange(
        string fileName)
    {
        var fullPath = Path.GetFullPath(fileName, driver.RootPath);
        var lines = await File.ReadAllLinesAsync(fullPath);
        int endLine = lines.Length > 0 ? lines.Length - 1 : 0;
        int endCharacter = lines.Length > 0 ? lines[^1].Length : 0;
        return await AnnotationQueryRange(fileName, 0, 0, endLine, endCharacter);
    }
}