using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Metek.LspCli;

var options = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    Converters = { new JsonStringEnumConverter() },
    WriteIndented = true
};

// Global options for bridge testing
AnnotationExtensions.SetSerializerOptions(options);

int passed = 0, failed = 0;

void Test(string name, string json, Action<object>? validate = null)
{
    try
    {
        var result = System.Text.Json.JsonSerializer.Deserialize<Annotation>(json, options);
        if (result == null) throw new Exception("deserialized to null");
        Console.Write($"  PASS  {name}");
        if (validate != null) validate(result);
        Console.WriteLine();
        passed++;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  FAIL  {name} — {ex.GetType().Name}: {ex.Message}");
        failed++;
    }
}

void TestTuple(string name, string json, Action<AnnotationTuple>? validate = null)
{
    try
    {
        var result = System.Text.Json.JsonSerializer.Deserialize<AnnotationTuple>(json, options);
        if (result == null) throw new Exception("deserialized to null");
        Console.Write($"  PASS  {name}");
        if (validate != null) validate(result);
        Console.WriteLine();
        passed++;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  FAIL  {name} — {ex.GetType().Name}: {ex.Message}");
        failed++;
    }
}

void TestImmediateEnum(string name, string json, Action<QueryAnnotationTreeResult> validate)
{
    try
    {
        var result = System.Text.Json.JsonSerializer.Deserialize<QueryAnnotationTreeResult>(json, options);
        if (result == null) throw new Exception("deserialized to null");
        Console.Write($"  PASS  {name}");
        validate(result);
        Console.WriteLine();
        passed++;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  FAIL  {name} — {ex.GetType().Name}: {ex.Message}");
        failed++;
    }
}

void TestImmediateEnumTree(string name, string json, Action<ObjectTreeType> validate)
{
    try
    {
        var result = System.Text.Json.JsonSerializer.Deserialize<ObjectTreeType>(json, options);
        if (result == null) throw new Exception("deserialized to null");
        Console.Write($"  PASS  {name}");
        validate(result);
        Console.WriteLine();
        passed++;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  FAIL  {name} — {ex.GetType().Name}: {ex.Message}");
        failed++;
    }
}

void TestNewtonsoftBridge<T>(string name, string json, Action<T>? validate = null)
{
    try
    {
        // Simulate OmniSharp client deserializing using Newtonsoft.Json
        var result = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
        if (result == null) throw new Exception("deserialized to null");
        Console.Write($"  PASS  [BRIDGE] {name}");
        if (validate != null) validate(result);
        Console.WriteLine();
        passed++;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  FAIL  [BRIDGE] {name} — {ex.GetType().Name}: {ex.Message}");
        failed++;
    }
}

Console.WriteLine("=== Unit Variants ===");
Test("ParentCall", "\"ParentCall\"", v => Debug.Assert(v is Annotation.ParentCall));
Test("ReturnVal", "\"ReturnVal\"", v => Debug.Assert(v is Annotation.ReturnVal));

Console.WriteLine("\n=== Newtype Variants (single-field) ===");
Test("TreeBlock", "{\"TreeBlock\":[\"usr\",\"src\",\"main\"]}", v =>
{
    var tb = v as Annotation.TreeBlock;
    Debug.Assert(tb!.Idents is ["usr", "src", "main"]);
});
Test("Variable", "{\"Variable\":[\"x\"]}");
Test("ScopedMissingIdent", "{\"ScopedMissingIdent\":[\"a\",\"b\"]}");
Test("UnscopedCall", "{\"UnscopedCall\":\"foo\"}", v =>
{
    var u = v as Annotation.UnscopedCall;
    Debug.Assert(u!.Ident == "foo");
});
Test("UnscopedVar", "{\"UnscopedVar\":\"bar\"}");
Test("MacroDefinition", "{\"MacroDefinition\":\"MY_MACRO\"}");
Test("Include", "{\"Include\":\"/path/to/file.dm\"}");
Test("Resource", "{\"Resource\":\"icons/thing.dmi\"}");
Test("InSequence", "{\"InSequence\":3}", v =>
{
    var s = v as Annotation.InSequence;
    Debug.Assert(s!.Index == 3);
});
Test("ProcArgument", "{\"ProcArgument\":0}");

Console.WriteLine("\n=== TypePath (newtype with complex value) ===");
Test("TypePath", "{\"TypePath\":[[\"Slash\",\"mob\"],[\"Slash\",\"living\"],[\"Dot\",\"src\"]]}",
    v => Debug.Assert(v is Annotation.TypePath));

Console.WriteLine("\n=== Tuple Variants (multi-field) ===");
Test("TreePath", "{\"TreePath\":[true,[\"usr\"]]}", v =>
{
    var tp = v as Annotation.TreePath;
    Debug.Assert(tp!.IsAbsolute == true);
    Debug.Assert(tp.Idents is ["usr"]);
});
Test("IncompleteTreePath", "{\"IncompleteTreePath\":[false,[\"a\",\"b\"]]}");
Test("ProcHeader", "{\"ProcHeader\":[[\"do_thing\"],2]}", v =>
{
    var ph = v as Annotation.ProcHeader;
    Debug.Assert(ph!.Arity == 2);
});
Test("ProcBody", "{\"ProcBody\":[[\"do_thing\"],2]}");
Test("ScopedCall", "{\"ScopedCall\":[[\"src\"],\"SomeProc\"]}");
Test("ScopedVar", "{\"ScopedVar\":[[\"src\"],\"some_var\"]}");
Test("ProcArguments", "{\"ProcArguments\":[[\"src\"],\"SomeProc\",1]}", v =>
{
    var pa = v as Annotation.ProcArguments;
    Debug.Assert(pa!.Scope is ["src"]);
    Debug.Assert(pa.Ident == "SomeProc");
    Debug.Assert(pa.Arity == 1);
});

Console.WriteLine("\n=== VarType / LocalVarScope ===");
Test("LocalVarScope",
    "{\"LocalVarScope\":[{\"flags\":\"static/tmp\",\"type_path\":[\"mob\"],\"input_type\":\"mob\"},\"x\"]}",
    v => Debug.Assert(v is Annotation.LocalVarScope));

Console.WriteLine("\n=== IncompleteTypePath ===");
Test("IncompleteTypePath", "{\"IncompleteTypePath\":[[[\"Slash\",\"mob\"]],\"Dot\"]}",
    v => Debug.Assert(v is Annotation.IncompleteTypePath));

Console.WriteLine("\n=== MacroUse (struct variant) ===");
Test("MacroUse",
    "{\"MacroUse\":{\"name\":\"ASSERT\",\"definition_location\":{\"file\":0,\"line\":42,\"column\":10}}}",
    v =>
    {
        var mu = v as Annotation.MacroUse;
        Debug.Assert(mu!.Name == "ASSERT");
        Debug.Assert(mu.DefinitionLocation.File == 0);
        Debug.Assert(mu.DefinitionLocation.Line == 42);
    });

Console.WriteLine("\n=== AnnotationTuple (Position-based Range) ===");
TestTuple("AnnotationTuple with TreePath",
    "{\"range\":{\"start\":{\"line\":10,\"character\":0},\"end\":{\"line\":10,\"character\":5}},\"annotation\":{\"TreePath\":[true,[\"usr\"]]}}",
    v =>
    {
        Debug.Assert(v.Range.Start.Line == 10);
        Debug.Assert(v.Annotation is Annotation.TreePath);
    });

Console.WriteLine("\n=== Newtonsoft Bridge Tests ===");
TestNewtonsoftBridge<Annotation>("Bridge Annotation (ParentCall)", "\"ParentCall\"", v => Debug.Assert(v is Annotation.ParentCall));
TestNewtonsoftBridge<Annotation>("Bridge Annotation (MacroUse)", 
    "{\"MacroUse\":{\"name\":\"MACRO\",\"definition_location\":{\"file\":1,\"line\":2,\"column\":3}}}",
    v => Debug.Assert(v is Annotation.MacroUse mu && mu.Name == "MACRO"));

TestNewtonsoftBridge<AnnotationTuple>("Bridge AnnotationTuple",
    "{\"range\":{\"start\":{\"line\":5,\"character\":1},\"end\":{\"line\":5,\"character\":10}},\"annotation\":{\"UnscopedVar\":\"x\"}}",
    v => {
        Debug.Assert(v.Range.Start.Character == 1);
        Debug.Assert(v.Annotation is Annotation.UnscopedVar uv && uv.Ident == "x");
    });

TestNewtonsoftBridge<QueryAnnotationTreeResult>("Bridge Full Result",
    "{\"outputAnnotations\": [{\"range\":{\"start\":{\"line\":1,\"character\":1},\"end\":{\"line\":1,\"character\":2}},\"annotation\":\"ReturnVal\"}]}",
    v => {
        Debug.Assert(v.outputAnnotations!.Length == 1);
        Debug.Assert(v.outputAnnotations[0].Annotation is Annotation.ReturnVal);
    });

Console.WriteLine("\n=== Round-trip (STJ) ===");
try
{
    var original = new Annotation.TreePath(true, new[] { "usr", "src" });
    var json = System.Text.Json.JsonSerializer.Serialize<Annotation>(original, options);
    var back = System.Text.Json.JsonSerializer.Deserialize<Annotation>(json, options);
    Debug.Assert(back is Annotation.TreePath tp && tp.IsAbsolute && tp.Idents is ["usr", "src"]);
    Console.WriteLine("  PASS  TreePath round-trip");
    passed++;
}
catch (Exception ex)
{
    Console.WriteLine($"  FAIL  TreePath round-trip — {ex.GetType().Name}: {ex.Message}");
    failed++;
}

Console.WriteLine("\n=== Immediate Enumeration ===");
// Prove annotation collections are concrete arrays, not lazy IEnumerable wrappers,
// so they can be enumerated immediately after LSP response deserialization.

// 1) QueryAnnotationTreeResult.outputAnnotations is AnnotationTuple[] (not IEnumerable deferred)
var resultJson = """{"outputAnnotations":[{"range":{"start":{"line":1,"character":1},"end":{"line":1,"character":2}},"annotation":"ReturnVal"},{"range":{"start":{"line":2,"character":0},"end":{"line":2,"character":3}},"annotation":{"TreePath":[true,["usr","src"]]}}]}""";
TestImmediateEnum("OutputAnnotations type", resultJson, r =>
{
    Debug.Assert(r.outputAnnotations is AnnotationTuple[], "outputAnnotations should be AnnotationTuple[] (concrete array)");
});
TestImmediateEnum("OutputAnnotations.Length", resultJson, r =>
{
    int len = r.outputAnnotations!.Length;
    Debug.Assert(len == 2, $"expected 2, got {len}");
});
TestImmediateEnum("OutputAnnotations indexer", resultJson, r =>
{
    var first = r.outputAnnotations![0];
    Debug.Assert(first.Annotation is Annotation.ReturnVal);
    var second = r.outputAnnotations[1];
    Debug.Assert(second.Annotation is Annotation.TreePath);
});
TestImmediateEnum("OutputAnnotations double iteration", resultJson, r =>
{
    // Prove not one-shot: iterating twice yields same results
    int count1 = 0;
    foreach (var a in r.outputAnnotations!) count1++;
    int count2 = 0;
    foreach (var a in r.outputAnnotations!) count2++;
    Debug.Assert(count1 == 2 && count2 == 2, "double iteration should yield same count");
});

// 2) ObjectTreeType.children is ObjectTreeType[] (concrete array)
var treeJson = """{"name":"root","kind":1,"location":null,"vars":[],"procs":[],"children":[{"name":"child1","kind":2,"location":null,"vars":[],"procs":[],"children":[],"n_vars":0,"n_procs":0,"n_children":0},{"name":"child2","kind":2,"location":null,"vars":[],"procs":[],"children":[],"n_vars":0,"n_procs":0,"n_children":0}],"n_vars":0,"n_procs":0,"n_children":2}""";
TestImmediateEnumTree("ObjectTreeType.children type", treeJson, r =>
{
    Debug.Assert(r.children is ObjectTreeType[], "children should be ObjectTreeType[] (concrete array)");
});
TestImmediateEnumTree("ObjectTreeType.children.Length", treeJson, r =>
{
    Debug.Assert(r.children.Length == 2, "expected 2 children");
});
TestImmediateEnumTree("ObjectTreeType.children indexer", treeJson, r =>
{
    Debug.Assert(r.children[0].name == "child1");
    Debug.Assert(r.children[1].name == "child2");
});
TestImmediateEnumTree("ObjectTreeType.children double iteration", treeJson, r =>
{
    int c1 = 0; foreach (var c in r.children) c1++;
    int c2 = 0; foreach (var c in r.children) c2++;
    Debug.Assert(c1 == 2 && c2 == 2);
});

Console.WriteLine("\n" + new string('=', 50));
Console.WriteLine($"Results: {passed} passed, {failed} failed out of {passed + failed} tests");
return failed > 0 ? 1 : 0;
