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

using System.Reflection;
using MediatR;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.JsonRpc.Server;
using OmniSharp.Extensions.JsonRpc.Serialization;
using Newtonsoft.Json;
using OmniSharp.Extensions.LanguageServer.Protocol.Serialization;
using static OmniSharp.Extensions.LanguageServer.Protocol.AbstractHandlers;

public partial class Driver
{

    public static JsonSerializer _notifSerializer = JsonSerializer.Create();
    public JsonSerializer NotifSerializer => Driver._notifSerializer;
    public static readonly Container<SymbolKind> AllSymbolKinds = new(
            SymbolKind.File, SymbolKind.Module, SymbolKind.Namespace, SymbolKind.Package,
            SymbolKind.Class, SymbolKind.Method, SymbolKind.Property, SymbolKind.Field,
            SymbolKind.Constructor, SymbolKind.Enum, SymbolKind.Interface, SymbolKind.Function,
            SymbolKind.Variable, SymbolKind.Constant, SymbolKind.String, SymbolKind.Number,
            SymbolKind.Boolean, SymbolKind.Array, SymbolKind.Object, SymbolKind.Key,
            SymbolKind.Null, SymbolKind.EnumMember, SymbolKind.Struct, SymbolKind.Event,
            SymbolKind.Operator, SymbolKind.TypeParameter
        );

    public static readonly Container<CompletionItemKind> AllCompletionItemKinds = new(
        CompletionItemKind.Text, CompletionItemKind.Method, CompletionItemKind.Function,
        CompletionItemKind.Constructor, CompletionItemKind.Field, CompletionItemKind.Variable,
        CompletionItemKind.Class, CompletionItemKind.Interface, CompletionItemKind.Module,
        CompletionItemKind.Property, CompletionItemKind.Unit, CompletionItemKind.Value,
        CompletionItemKind.Enum, CompletionItemKind.Keyword, CompletionItemKind.Snippet,
        CompletionItemKind.Color, CompletionItemKind.File, CompletionItemKind.Reference,
        CompletionItemKind.Folder, CompletionItemKind.EnumMember, CompletionItemKind.Constant,
        CompletionItemKind.Struct, CompletionItemKind.Event, CompletionItemKind.Operator,
        CompletionItemKind.TypeParameter
    );

    public static ClientCapabilities BuildClientCapabilities() => new()
    {
        Workspace = new WorkspaceClientCapabilities
        {
            ApplyEdit = true,
            WorkspaceEdit = new WorkspaceEditCapability
            {
                DocumentChanges = true,
                ResourceOperations = new Container<ResourceOperationKind>(
                    ResourceOperationKind.Create, ResourceOperationKind.Rename, ResourceOperationKind.Delete),
                FailureHandling = FailureHandlingKind.TextOnlyTransactional,
                NormalizesLineEndings = true,
                ChangeAnnotationSupport = new WorkspaceEditSupportCapabilitiesChangeAnnotationSupport
                {
                    GroupsOnLabel = true
                },
            },
            DidChangeConfiguration = new DidChangeConfigurationCapability { DynamicRegistration = true },
            DidChangeWatchedFiles = new DidChangeWatchedFilesCapability
            {
                DynamicRegistration = true,
                RelativePatternSupport = true,
            },
            Symbol = new WorkspaceSymbolCapability
            {
                DynamicRegistration = true,
                SymbolKind = new SymbolKindCapabilityOptions { ValueSet = AllSymbolKinds },
                TagSupport = new TagSupportCapabilityOptions
                {
                    ValueSet = new Container<SymbolTag>(SymbolTag.Deprecated)
                },
            },
            ExecuteCommand = new ExecuteCommandCapability { DynamicRegistration = true },
            WorkspaceFolders = true,
            Configuration = true,
            SemanticTokens = new SemanticTokensWorkspaceCapability { RefreshSupport = false },
            CodeLens = new CodeLensWorkspaceClientCapabilities { RefreshSupport = true },
            InlineValue = new InlineValueWorkspaceClientCapabilities { RefreshSupport = true },
            InlayHint = new InlayHintWorkspaceClientCapabilities { RefreshSupport = true },
            Diagnostics = new DiagnosticWorkspaceClientCapabilities { RefreshSupport = true },
        },
        TextDocument = new TextDocumentClientCapabilities
        {
            Synchronization = new TextSynchronizationCapability
            {
                DynamicRegistration = true,
                WillSave = true,
                WillSaveWaitUntil = true,
                DidSave = true,
            },
            Completion = new CompletionCapability
            {
                DynamicRegistration = true,
                CompletionItem = new CompletionItemCapabilityOptions
                {
                    SnippetSupport = true,
                    CommitCharactersSupport = true,
                    DocumentationFormat = new Container<MarkupKind>(MarkupKind.Markdown, MarkupKind.PlainText),
                    DeprecatedSupport = true,
                    PreselectSupport = true,
                    TagSupport = new CompletionItemTagSupportCapabilityOptions
                    {
                        ValueSet = new Container<CompletionItemTag>(CompletionItemTag.Deprecated)
                    },
                    InsertReplaceSupport = true,
                    ResolveAdditionalTextEditsSupport = true,
                    LabelDetailsSupport = true,
                },
                CompletionItemKind = new CompletionItemKindCapabilityOptions { ValueSet = AllCompletionItemKinds },
                ContextSupport = true,
            },
            Hover = new HoverCapability
            {
                DynamicRegistration = true,
                ContentFormat = new Container<MarkupKind>(MarkupKind.Markdown, MarkupKind.PlainText),
            },
            SignatureHelp = new SignatureHelpCapability
            {
                DynamicRegistration = true,
                SignatureInformation = new SignatureInformationCapabilityOptions
                {
                    DocumentationFormat = new Container<MarkupKind>(MarkupKind.PlainText),
                    ParameterInformation = new SignatureParameterInformationCapabilityOptions { LabelOffsetSupport = true },
                    ActiveParameterSupport = true,
                },
//                ContextSupport = true,
            },
            References = new ReferenceCapability { DynamicRegistration = true },
            DocumentHighlight = new DocumentHighlightCapability { DynamicRegistration = true },
            DocumentSymbol = new DocumentSymbolCapability
            {
                DynamicRegistration = true,
                SymbolKind = new SymbolKindCapabilityOptions { ValueSet = AllSymbolKinds },
                HierarchicalDocumentSymbolSupport = true,
                TagSupport = new TagSupportCapabilityOptions
                {
                    ValueSet = new Container<SymbolTag>(SymbolTag.Deprecated)
                },
                LabelSupport = true,
            },
            Formatting = new DocumentFormattingCapability { DynamicRegistration = true },
            RangeFormatting = new DocumentRangeFormattingCapability { DynamicRegistration = true },
            OnTypeFormatting = new DocumentOnTypeFormattingCapability { DynamicRegistration = true },
            Definition = new DefinitionCapability { DynamicRegistration = true, LinkSupport = true },
            Declaration = new DeclarationCapability { DynamicRegistration = true, LinkSupport = true },
            TypeDefinition = new TypeDefinitionCapability { DynamicRegistration = true, LinkSupport = true },
            Implementation = new ImplementationCapability { DynamicRegistration = true, LinkSupport = true },
            CodeAction = new CodeActionCapability
            {
                DynamicRegistration = true,
                IsPreferredSupport = true,
                CodeActionLiteralSupport = new CodeActionLiteralSupportOptions
                {
                    CodeActionKind = new CodeActionKindCapabilityOptions
                    {
                        ValueSet = new Container<CodeActionKind>(
                            CodeActionKind.QuickFix, CodeActionKind.Refactor, CodeActionKind.RefactorExtract,
                            CodeActionKind.RefactorInline, CodeActionKind.RefactorRewrite, CodeActionKind.Source,
                            CodeActionKind.SourceOrganizeImports)
                    }
                },
                HonorsChangeAnnotations = true,
                ResolveSupport = new CodeActionCapabilityResolveSupportOptions
                {
                    Properties = new Container<string>("edit")
                },
            },
            CodeLens = new CodeLensCapability { DynamicRegistration = true },
            DocumentLink = new DocumentLinkCapability { DynamicRegistration = true, TooltipSupport = true },
            Rename = new RenameCapability
            {
                DynamicRegistration = true,
                PrepareSupport = true,
                HonorsChangeAnnotations = true,
            },
            ColorProvider = new ColorProviderCapability { DynamicRegistration = true },
            FoldingRange = new FoldingRangeCapability
            {
                DynamicRegistration = true,
                LineFoldingOnly = true,
            },
            SelectionRange = new SelectionRangeCapability { DynamicRegistration = true },
            LinkedEditingRange = new LinkedEditingRangeClientCapabilities { DynamicRegistration = true },
            CallHierarchy = new CallHierarchyCapability { DynamicRegistration = true },
            SemanticTokens = new SemanticTokensCapability
            {
                DynamicRegistration = true,
                Requests = new SemanticTokensCapabilityRequests
                {
                    Full = new SemanticTokensCapabilityRequestFull { Delta = true },
                },
                TokenTypes = new Container<SemanticTokenType>(
                    SemanticTokenType.Namespace, SemanticTokenType.Type, SemanticTokenType.Class,
                    SemanticTokenType.Enum, SemanticTokenType.Interface, SemanticTokenType.Struct,
                    SemanticTokenType.TypeParameter, SemanticTokenType.Parameter, SemanticTokenType.Variable,
                    SemanticTokenType.Property, SemanticTokenType.EnumMember, SemanticTokenType.Event,
                    SemanticTokenType.Function, SemanticTokenType.Method, SemanticTokenType.Macro,
                    SemanticTokenType.Keyword, SemanticTokenType.Modifier, SemanticTokenType.Comment,
                    SemanticTokenType.String, SemanticTokenType.Number, SemanticTokenType.Regexp,
                    SemanticTokenType.Operator, SemanticTokenType.Decorator),
                TokenModifiers = new Container<SemanticTokenModifier>(
                    SemanticTokenModifier.Declaration, SemanticTokenModifier.Definition,
                    SemanticTokenModifier.Readonly, SemanticTokenModifier.Static,
                    SemanticTokenModifier.Deprecated, SemanticTokenModifier.Abstract,
                    SemanticTokenModifier.Async, SemanticTokenModifier.Modification,
                    SemanticTokenModifier.Documentation, SemanticTokenModifier.DefaultLibrary),
                Formats = new Container<SemanticTokenFormat>(SemanticTokenFormat.Relative),
                MultilineTokenSupport = false,
                OverlappingTokenSupport = false,
            },
            TypeHierarchy = new TypeHierarchyCapability { DynamicRegistration = true },
            InlayHint = new InlayHintClientCapabilities { DynamicRegistration = true },
            Diagnostic = new DiagnosticClientCapabilities { DynamicRegistration = true },
            PublishDiagnostics = new PublishDiagnosticsCapability
            {
                RelatedInformation = true,
                VersionSupport = false,
                TagSupport = new PublishDiagnosticsTagSupportCapabilityOptions
                {
                    ValueSet = new Container<DiagnosticTag>(DiagnosticTag.Unnecessary, DiagnosticTag.Deprecated)
                },
                CodeDescriptionSupport = true,
                DataSupport = true,
            },
        },
        Window = new WindowClientCapabilities
        {
            WorkDoneProgress = true,
            ShowMessage = new ShowMessageRequestClientCapabilities
            {
                MessageActionItem = new ShowMessageRequestMessageActionItemClientCapabilities
                {
                    AdditionalPropertiesSupport = true
                }
            },
            ShowDocument = new ShowDocumentClientCapabilities { Support = true },
        },
        General = new GeneralClientCapabilities
        {
            RegularExpressions = new RegularExpressionsClientCapabilities { Engine = "ECMAScript", Version = "ES2020" },
            Markdown = new MarkdownClientCapabilities { Parser = "marked", Version = "1.1.0" },
            StaleRequestSupport = new StaleRequestSupportClientCapabilities
            {
                Cancel = true,
                RetryOnContentModified = new Container<string>(
                    "textDocument/semanticTokens/full",
                    "textDocument/semanticTokens/range",
                    "textDocument/semanticTokens/full/delta")
            },
            PositionEncodings = new Container<PositionEncodingKind>(PositionEncodingKind.UTF16),
        },
        Experimental = new Dictionary<string, JToken>()
        {
            {"dreammaker", JToken.Parse("{\"objectTree\": true}") }
        }
    };

    public static readonly string[] ServerNotificationMethods = DiscoverServerNotifications();

    public static string[] DiscoverServerNotifications()
    {
        var assembly = typeof(TextDocumentPositionParams).Assembly;
        var positionParamNotifs = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && typeof(IRequest<Unit>).IsAssignableFrom(t))
            .SelectMany(t => t.GetCustomAttributes<MethodAttribute>())
            .Where(a => (a.Direction & Direction.ServerToClient) != 0)
            .Select(a => a.Method)
            .Distinct()
            .ToArray();
        string[] specialNotifs = ["experimental/dreammaker/objectTree"];
        string[] combinedListening = positionParamNotifs.Concat(specialNotifs).ToArray();
        return combinedListening;
    }

    public static void ConfigureOptions(LanguageClientOptions options)
    {
        options
            .WithInput(ServerProcess.StandardOutput.BaseStream)
            .WithOutput(ServerProcess.StandardInput.BaseStream)
            .WithRootUri(ProjectRoot)
            .WithClientInfo(new ClientInfo { Name = "LspCLIWrapper", Version = "0.1" })
            .WithClientCapabilities(BuildClientCapabilities());


        foreach (var method in ServerNotificationMethods)
        {
            var key = method;
            options.OnJsonNotification(key, (JToken token) =>
            {
                if (!Notifications.TryGetValue(key, out var list))
                {
                    list = new List<Notification>();
                    Notifications[key] = list;
                }
                var serialized = new Notification(key, token);
                list.Add(serialized);
            });
        }
    }
}
