using System.Text;
using Newtonsoft.Json.Linq;

namespace Metek.LspCli;

[RegisterNotification("window/showMessage")]
[RegisterNotification("window/logMessage")]
[RegisterNotification("window/showMessageRequest")]
[RegisterNotification("window/showDocument")]
[RegisterNotification("telemetry/event")]
[RegisterNotification("window/workDoneProgress/create")]
[RegisterNotification("textDocument/publishDiagnostics")]
[RegisterNotification("workspace/semanticTokens/refresh")]
[RegisterNotification("workspace/codeLens/refresh")]
[RegisterNotification("workspace/inlayHint/refresh")]
[RegisterNotification("workspace/inlineValue/refresh")]
[RegisterNotification("workspace/diagnostic/refresh")]
[RegisterNotification("workspace/applyEdit")]
[RegisterNotification("workspace/workspaceFolders")]
[RegisterNotification("$/logTrace")]
[RegisterNotification("$/progress")]
[RegisterNotification("$/cancelRequest")]
[RegisterNotification("client/registerCapability")]
[RegisterNotification("client/unregisterCapability")]
public partial class Driver
{
    public sealed record StoredNotification(string Method, byte[] Data);

    public Dictionary<string, List<StoredNotification>> Notifications { get; } = new();

    private static byte[] JTokenToUtf8Bytes(JToken token)
    {
        using var ms = new MemoryStream();
        using var sw = new StreamWriter(ms, new UTF8Encoding(false));
        using var jw = new Newtonsoft.Json.JsonTextWriter(sw);
        token.WriteTo(jw);
        jw.Flush();
        sw.Flush();
        return ms.ToArray();
    }

    private void StoreNotification(string method, JToken token)
    {
        if (!Notifications.TryGetValue(method, out var list))
        {
            list = new List<StoredNotification>();
            Notifications[method] = list;
        }
        list.Add(new StoredNotification(method, JTokenToUtf8Bytes(token)));
    }

    public IEnumerable<T> GetNotifications<T>(string method)
    {
        if (!Notifications.TryGetValue(method, out var list))
            yield break;
        foreach (var stored in list)
        {
            var result = System.Text.Json.JsonSerializer.Deserialize<T>(stored.Data, AnnotationExtensions.SerializerOptions);
            if (result is not null)
                yield return result;
        }
    }

    public int NotificationCount(string method)
    {
        return Notifications.TryGetValue(method, out var list) ? list.Count : 0;
    }
}
