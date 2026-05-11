using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Bridge converter to allow Newtonsoft.Json (used by OmniSharp) to delegate to System.Text.Json
public class StjBridgeConverter<T> : Newtonsoft.Json.JsonConverter
{
    public override bool CanConvert(Type objectType) => typeof(T).IsAssignableFrom(objectType);

    private JToken ReorderDiscriminator(JToken token)
    {
        if (token is JObject obj)
        {
            var newObj = new JObject();
            if (obj.ContainsKey("type"))
            {
                newObj.Add("type", obj["type"]);
            }
            foreach (var prop in obj.Properties())
            {
                if (prop.Name != "type")
                {
                    newObj.Add(prop.Name, ReorderDiscriminator(prop.Value));
                }
            }
            return newObj;
        }
        else if (token is JArray arr)
        {
            var newArr = new JArray();
            foreach (var item in arr)
            {
                newArr.Add(ReorderDiscriminator(item));
            }
            return newArr;
        }
        return token;
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, Newtonsoft.Json.JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null) return null;
        var token = JToken.Load(reader);
        token = ReorderDiscriminator(token);

        using var ms = new MemoryStream();
        using var sw = new StreamWriter(ms, new UTF8Encoding(false));
        using var jw = new JsonTextWriter(sw);
        token.WriteTo(jw);
        jw.Flush();
        sw.Flush();
        var json = ms.ToArray();
        return System.Text.Json.JsonSerializer.Deserialize<T>(json, AnnotationExtensions.SerializerOptions);
    }

    public override void WriteJson(JsonWriter writer, object? value, Newtonsoft.Json.JsonSerializer serializer)
    {
        if (value == null) { writer.WriteNull(); return; }
        writer.WriteRawValue(System.Text.Json.JsonSerializer.Serialize(value, AnnotationExtensions.SerializerOptions));
    }
}

// Location DTOs matching Serde serialization of lsp_types::Location
public record AnnotationPosition(
    [property: JsonPropertyName("line")] int Line,
    [property: JsonPropertyName("character")] int Character
);

public record AnnotationRange(
    [property: JsonPropertyName("start")] AnnotationPosition Start,
    [property: JsonPropertyName("end")] AnnotationPosition End
);

public record AnnotationLocation(
    [property: JsonPropertyName("uri")] string Uri,
    [property: JsonPropertyName("range")] AnnotationRange Range
);

// Location DTO matching dreammaker::Location (used internally by some Annotation variants)
public record DmLocation(
    [property: JsonPropertyName("file")] int File,
    [property: JsonPropertyName("line")] int Line,
    [property: JsonPropertyName("column")] int Column
);

// RangeInclusiveDef — mirrors the Serde remote representation of RangeInclusive<T>
public record RangeInclusiveDef<T>(
    [property: JsonPropertyName("start")] T Start,
    [property: JsonPropertyName("end")] T End
);

[System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
public enum PathOp
{
    Slash,
    Dot,
    Colon
}

public record TypePathSegment(
    [property: JsonPropertyName("op")] PathOp Op,
    [property: JsonPropertyName("ident")] string Ident
);

public record VarType(
    [property: JsonPropertyName("flags")] string Flags,
    [property: JsonPropertyName("type_path")] string[] TypePath,
    [property: JsonPropertyName("input_type")] string InputType
);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Annotation.TreeBlock), "TreeBlock")]
[JsonDerivedType(typeof(Annotation.TreePath), "TreePath")]
[JsonDerivedType(typeof(Annotation.TypePath), "TypePath")]
[JsonDerivedType(typeof(Annotation.Variable), "Variable")]
[JsonDerivedType(typeof(Annotation.ProcHeader), "ProcHeader")]
[JsonDerivedType(typeof(Annotation.ProcBody), "ProcBody")]
[JsonDerivedType(typeof(Annotation.LocalVarScope), "LocalVarScope")]
[JsonDerivedType(typeof(Annotation.UnscopedCall), "UnscopedCall")]
[JsonDerivedType(typeof(Annotation.UnscopedVar), "UnscopedVar")]
[JsonDerivedType(typeof(Annotation.ScopedCall), "ScopedCall")]
[JsonDerivedType(typeof(Annotation.ScopedVar), "ScopedVar")]
[JsonDerivedType(typeof(Annotation.ParentCall), "ParentCall")]
[JsonDerivedType(typeof(Annotation.ReturnVal), "ReturnVal")]
[JsonDerivedType(typeof(Annotation.InSequence), "InSequence")]
[JsonDerivedType(typeof(Annotation.MacroDefinition), "MacroDefinition")]
[JsonDerivedType(typeof(Annotation.MacroUse), "MacroUse")]
[JsonDerivedType(typeof(Annotation.Include), "Include")]
[JsonDerivedType(typeof(Annotation.Resource), "Resource")]
[JsonDerivedType(typeof(Annotation.ScopedMissingIdent), "ScopedMissingIdent")]
[JsonDerivedType(typeof(Annotation.IncompleteTypePath), "IncompleteTypePath")]
[JsonDerivedType(typeof(Annotation.IncompleteTreePath), "IncompleteTreePath")]
[JsonDerivedType(typeof(Annotation.ProcArguments), "ProcArguments")]
[JsonDerivedType(typeof(Annotation.ProcArgument), "ProcArgument")]
[Newtonsoft.Json.JsonConverter(typeof(StjBridgeConverter<Annotation>))]
public abstract record Annotation
{
    public record TreeBlock([property: JsonPropertyName("idents")] string[] Idents) : Annotation;
    public record TreePath([property: JsonPropertyName("is_absolute")] bool IsAbsolute, [property: JsonPropertyName("idents")] string[] Idents) : Annotation;
    public record TypePath([property: JsonPropertyName("path")] TypePathSegment[] Path) : Annotation;
    public record Variable([property: JsonPropertyName("idents")] string[] Idents) : Annotation;
    public record ProcHeader([property: JsonPropertyName("idents")] string[] Idents, [property: JsonPropertyName("arity")] int Arity) : Annotation;
    public record ProcBody([property: JsonPropertyName("idents")] string[] Idents, [property: JsonPropertyName("arity")] int Arity) : Annotation;
    public record LocalVarScope([property: JsonPropertyName("var")] VarType Var, [property: JsonPropertyName("ident")] string Ident) : Annotation;
    public record UnscopedCall([property: JsonPropertyName("ident")] string Ident) : Annotation;
    public record UnscopedVar([property: JsonPropertyName("ident")] string Ident) : Annotation;
    public record ScopedCall([property: JsonPropertyName("scope")] string[] Scope, [property: JsonPropertyName("ident")] string Ident) : Annotation;
    public record ScopedVar([property: JsonPropertyName("scope")] string[] Scope, [property: JsonPropertyName("ident")] string Ident) : Annotation;
    public record ParentCall() : Annotation;
    public record ReturnVal() : Annotation;
    public record InSequence([property: JsonPropertyName("index")] int Index) : Annotation;
    public record MacroDefinition([property: JsonPropertyName("ident")] string Ident) : Annotation;
    public record MacroUse(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("definition_location")] DmLocation DefinitionLocation
    ) : Annotation;
    public record Include([property: JsonPropertyName("path")] string Path) : Annotation;
    public record Resource([property: JsonPropertyName("path")] string Path) : Annotation;
    public record ScopedMissingIdent([property: JsonPropertyName("idents")] string[] Idents) : Annotation;
    public record IncompleteTypePath([property: JsonPropertyName("path")] TypePathSegment[] Path, [property: JsonPropertyName("op")] PathOp Op) : Annotation;
    public record IncompleteTreePath([property: JsonPropertyName("is_absolute")] bool IsAbsolute, [property: JsonPropertyName("idents")] string[] Idents) : Annotation;
    public record ProcArguments([property: JsonPropertyName("scope")] string[] Scope, [property: JsonPropertyName("ident")] string Ident, [property: JsonPropertyName("arity")] int Arity) : Annotation;
    public record ProcArgument([property: JsonPropertyName("index")] int Index) : Annotation;
}

[Newtonsoft.Json.JsonConverter(typeof(StjBridgeConverter<AnnotationTuple>))]
public record AnnotationTuple(
    [property: JsonPropertyName("range")] RangeInclusiveDef<AnnotationPosition> Range,
    [property: JsonPropertyName("annotation")] Annotation Annotation
);

public static class AnnotationExtensions
{
    internal static JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
        TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
    };

    public static void SetSerializerOptions(JsonSerializerOptions options)
    {
        SerializerOptions = options;
    }

    public static AnnotationTuple[]? ToAnnotationTuples(this JToken? token)
    {
        if (token == null || token.Type == JTokenType.Null)
            return null;

        var json = token.ToString(Newtonsoft.Json.Formatting.None);
        return System.Text.Json.JsonSerializer.Deserialize<AnnotationTuple[]>(json, SerializerOptions);
    }

    public static string ToJsonString(this Annotation annotation)
    {
        return System.Text.Json.JsonSerializer.Serialize(annotation, SerializerOptions);
    }

    public static string ToJsonString(this AnnotationTuple tuple)
    {
        return System.Text.Json.JsonSerializer.Serialize(tuple, SerializerOptions);
    }
}
