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
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;


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

[System.Text.Json.Serialization.JsonSerializable(typeof(ObjectTreeType))]
public record ObjectTreeType
{
    [System.Text.Json.Serialization.JsonRequired]
    public string name;
    [System.Text.Json.Serialization.JsonRequired]
    public SymbolKind kind;
    [System.Text.Json.Serialization.JsonRequired]
    public Location? location;
    [System.Text.Json.Serialization.JsonRequired]
    public ObjectTreeVar[] vars;
    [System.Text.Json.Serialization.JsonRequired]
    public ObjectTreeProc[] procs;
    [System.Text.Json.Serialization.JsonRequired]
    public ObjectTreeType[] children;
    [System.Text.Json.Serialization.JsonRequired]
    public long n_vars;
    [System.Text.Json.Serialization.JsonRequired]
    public long n_procs;
    [System.Text.Json.Serialization.JsonRequired]
    public long n_children;
}

public record ObjectTreeVar
{
    [System.Text.Json.Serialization.JsonRequired]
    public string name;
    [System.Text.Json.Serialization.JsonRequired]
    public SymbolKind kind;
    [System.Text.Json.Serialization.JsonRequired]
    public Location? location;
    [System.Text.Json.Serialization.JsonRequired]
    public bool is_declaration;
}

public record ObjectTreeProc
{
    [System.Text.Json.Serialization.JsonRequired]
    public string name;
    [System.Text.Json.Serialization.JsonRequired]
    public SymbolKind kind;
    [System.Text.Json.Serialization.JsonRequired]
    public Location? location;
    [System.Text.Json.Serialization.JsonRequired]
    public bool? is_verb;
}