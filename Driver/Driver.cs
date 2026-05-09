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

/*
Driver class - intended to serve as an obfuscation handle to make CLI
handling of the LSP tools more ergonomic
*/
using OmniSharp.Extensions.LanguageServer.Client;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using MediatR;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Dynamic;
using System.Reflection;
using System.Text;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.JsonRpc.Server;


public static class FreeAsInFreedom
{
    public static void LicenseNotice()
    {
        const string notice = """
        
        =

        metek-lsp-cli 0.2 Copyright (c) 2026 Joshua 'Joan Metek' Kidder

        metek-lsp-cli is free software: you can redistribute it and/or modify it 
        under the terms of the GNU Lesser General Public License as published by
        the Free Software Foundation, either version 3 of the License, or (at 
        your option) any later version.

        metek-lsp-cli is distributed in the hope that it will be useful, but
        this program comes with ABSOLUTELY NO WARRANTY.

        You should have received a copy of the GNU Lesser General Public License 
        along with metek-lsp-cli. If not, see <https://www.gnu.org/licenses/>. 
        
        =

        """;
        Console.Error.Write(notice);
    }
}
namespace  Metek.LspCli {
public partial class Driver : IDisposable, IAsyncDisposable
{
    public Uri ProjectRoot { get; set; } = null;
    public DirectoryInfo RootDirectory { get; set; } = null;
    public string RootPath { get; set; } = null;
    public string ServerBinary { get; set; } = null;
    public string[]? ServerArgs { get; set; } = null;

    public Process ServerProcess { get; set; } = null!;
    public LanguageClient ClientInterface { get; set; } = null!;
    public LanguageClient cI => ClientInterface;
    public Dictionary<string, List<Notification>> Notifications { get; } = new();

    public void Dispose()
    {
        ClientInterface?.Dispose();
        if (ServerProcess != null)
        {
            if (!ServerProcess.HasExited)
            {
                ServerProcess.Kill();
            }
            ServerProcess.Dispose();
        }
#if DUAL_STREAM
        Streams?.Dispose();
#endif
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
#if DUAL_STREAM
        await Streams.DisposeAsync();
#endif
        Dispose();
    }

    public static async Task<Driver> CreateAsync(string rootPath, string serverBinary, string[]? serverArgs = null)
    {
        var driver = new Driver(rootPath, serverBinary, serverArgs, skipInitialize: true);
        await driver.Initialize();
        return driver;
    }

    public Driver(string rootPath, string serverBinary, string[]? serverArgs = null) 
        : this(rootPath, serverBinary, serverArgs, skipInitialize: false)
    {
    }

    private Driver(string rootPath, string serverBinary, string[]? serverArgs, bool skipInitialize)
    {   
        try
        {
            RootPath = Path.GetFullPath(rootPath);
            RootDirectory = new DirectoryInfo(rootPath);
            ProjectRoot = new Uri(RootPath);
            ServerBinary = serverBinary;
            ServerArgs = serverArgs;

            if (!skipInitialize)
            {
                Initialize().GetAwaiter().GetResult();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Source);
            Console.WriteLine(e.Message);
            Console.WriteLine(e.Data);
            Console.Write(e.StackTrace);
        }
    }

    public async Task Initialize()
    {
        FreeAsInFreedom.LicenseNotice();
        ServerProcess = StartServerProcess();
        ClientInterface = LanguageClient.Create(ConfigureOptions);
#if DUAL_STREAM
        var _ = Task.Run(() => Streams.HandleOutput());
#endif
        await ClientInterface.Initialize(CancellationToken.None);
        SetupRequests();
    }

    public Process StartServerProcess()
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ServerBinary,
                Arguments = ServerArgs is not null ? string.Join(' ', ServerArgs) : "",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = RootPath,
                StandardInputEncoding = new UTF8Encoding(),
                StandardOutputEncoding = new UTF8Encoding()
            }
        };
        process.Start();
        return process;
    }

    public static readonly Dictionary<string, string> ExtensionToLanguageId = new(StringComparer.OrdinalIgnoreCase)
    {
        [".c"] = "c",
        [".h"] = "c",
        [".cpp"] = "cpp",
        [".cxx"] = "cpp",
        [".cc"] = "cpp",
        [".hpp"] = "cpp",
        [".hxx"] = "cpp",
        [".hh"] = "cpp",
        [".cs"] = "csharp",
        [".py"] = "python",
        [".js"] = "javascript",
        [".mjs"] = "javascript",
        [".ts"] = "typescript",
        [".mts"] = "typescript",
        [".jsx"] = "javascriptreact",
        [".tsx"] = "typescriptreact",
        [".rs"] = "rust",
        [".go"] = "go",
        [".java"] = "java",
        [".lua"] = "lua",
        [".rb"] = "ruby",
        [".sh"] = "shellscript",
        [".bash"] = "shellscript",
        [".json"] = "json",
        [".jsonc"] = "jsonc",
        [".xml"] = "xml",
        [".yaml"] = "yaml",
        [".yml"] = "yaml",
        [".toml"] = "toml",
        [".md"] = "markdown",
        [".html"] = "html",
        [".htm"] = "html",
        [".css"] = "css",
    };

    public static string ResolveLanguageId(string path)
    {
        var ext = Path.GetExtension(path);
        return ext is not null && ExtensionToLanguageId.TryGetValue(ext, out var id) ? id : "";
    }

    public void OpenDocument(string path)
    {
        var fullPath = Path.GetFullPath(path, RootPath);

        var uri = DocumentUri.FromFileSystemPath(fullPath);
        var document = new TextDocumentItem
        {
            Uri = uri,
            LanguageId = ResolveLanguageId(fullPath),
            Version = 1,
            Text = File.ReadAllText(fullPath)
        };

        cI.SendNotification(new DidOpenTextDocumentParams
        {
            TextDocument = document
        });
    }


}}