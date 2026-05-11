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
}

public abstract class RequestTable(Driver _driver)
{
    protected LanguageClient client => driver.ClientInterface;
    protected Driver driver => _driver;
}

public partial class DOCTable(Driver _driver) : RequestTable(_driver)
{
}

public partial class TDXTable(Driver _driver) : RequestTable(_driver)
{
}

public partial class WSPTable(Driver _driver) : RequestTable(_driver)
{
}

public readonly partial struct DriverResults()
{
}
