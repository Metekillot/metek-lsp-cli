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

using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using MediatR;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.JsonRpc.Generation;
using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using System.Text.Json;


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
}

public record ObjectTreeType
{
    public string name;
    public SymbolKind kind;
    public Location? location;
    public ObjectTreeVar[] vars;
    public ObjectTreeProc[] procs;
    public ObjectTreeType[] children;
    public long n_vars;
    public long n_procs;
    public long n_children;
    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}

public record ObjectTreeVar
{
    public string name;
    public SymbolKind kind;
    public Location? location;
    public bool is_declaration;
    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}

public record ObjectTreeProc
{
    public string name;
    public SymbolKind kind;
    public Location? location;
    public bool? is_verb;
    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}