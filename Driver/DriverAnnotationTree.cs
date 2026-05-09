using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Linq;

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

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PathOp
{
    Slash,
    Dot,
    Colon
}

[JsonConverter(typeof(TypePathSegmentConverter))]
public record TypePathSegment(PathOp Op, string Ident);

public class TypePathSegmentConverter : JsonConverter<TypePathSegment>
{
    public override TypePathSegment Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected StartArray for TypePathSegment");
        reader.Read();
        var op = JsonSerializer.Deserialize<PathOp>(ref reader, options);
        reader.Read();
        var ident = reader.GetString()!;
        reader.Read();
        return new TypePathSegment(op, ident);
    }

    public override void Write(Utf8JsonWriter writer, TypePathSegment value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        JsonSerializer.Serialize(writer, value.Op, options);
        writer.WriteStringValue(value.Ident);
        writer.WriteEndArray();
    }
}

public record VarType(
    [property: JsonPropertyName("flags")] string Flags,
    [property: JsonPropertyName("type_path")] string[] TypePath,
    [property: JsonPropertyName("input_type")] string InputType
);

[JsonConverter(typeof(AnnotationConverter))]
public abstract record Annotation
{
    // --- Unit variants (serialized as bare strings) ---
    public record ParentCall() : Annotation;
    public record ReturnVal() : Annotation;

    // --- Newtype variants (single unnamed field → value directly) ---
    public record TreeBlock(string[] Idents) : Annotation;
    public record Variable(string[] Idents) : Annotation;
    public record ScopedMissingIdent(string[] Idents) : Annotation;
    public record UnscopedCall(string Ident) : Annotation;
    public record UnscopedVar(string Ident) : Annotation;
    public record MacroDefinition(string Ident) : Annotation;
    public record InSequence(int Index) : Annotation;
    public record ProcArgument(int Index) : Annotation;
    public record Include(string Path) : Annotation;
    public record Resource(string Path) : Annotation;
    public record TypePath(TypePathSegment[] Segments) : Annotation;

    // --- Tuple variants (multiple unnamed fields → array) ---
    public record TreePath(bool IsAbsolute, string[] Idents) : Annotation;
    public record ProcHeader(string[] Idents, int Arity) : Annotation;
    public record ProcBody(string[] Idents, int Arity) : Annotation;
    public record LocalVarScope(VarType Var, string Ident) : Annotation;
    public record ScopedCall(string[] Scope, string Ident) : Annotation;
    public record ScopedVar(string[] Scope, string Ident) : Annotation;
    public record IncompleteTreePath(bool IsAbsolute, string[] Idents) : Annotation;
    public record IncompleteTypePath(TypePathSegment[] Segments, PathOp Op) : Annotation;
    public record ProcArguments(string[] Scope, string Ident, int Arity) : Annotation;

    // --- Struct variant (named fields → object) ---
    public record MacroUse(string Name, DmLocation DefinitionLocation) : Annotation;
}

public record AnnotationTuple(
    [property: JsonPropertyName("range")] RangeInclusiveDef<AnnotationLocation> Range,
    [property: JsonPropertyName("annotation")] Annotation Annotation
);

public class AnnotationConverter : JsonConverter<Annotation>
{
    public override Annotation? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Unit variants — bare string
        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString() switch
            {
                "ParentCall" => new Annotation.ParentCall(),
                "ReturnVal" => new Annotation.ReturnVal(),
                var name => throw new JsonException($"Unknown unit variant: {name}")
            };
        }

        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected StartObject or String for Annotation");

        reader.Read();
        if (reader.TokenType != JsonTokenType.PropertyName)
            throw new JsonException("Expected PropertyName");

        var variant = reader.GetString();
        reader.Read();

        switch (variant)
        {
            // --- Newtype variants ---
            case "TreeBlock":
            {
                var v = new Annotation.TreeBlock(JsonSerializer.Deserialize<string[]>(ref reader, options)!);
                reader.Read();
                return v;
            }
            case "Variable":
            {
                var v = new Annotation.Variable(JsonSerializer.Deserialize<string[]>(ref reader, options)!);
                reader.Read();
                return v;
            }
            case "ScopedMissingIdent":
            {
                var v = new Annotation.ScopedMissingIdent(JsonSerializer.Deserialize<string[]>(ref reader, options)!);
                reader.Read();
                return v;
            }
            case "UnscopedCall":
            {
                var v = new Annotation.UnscopedCall(JsonSerializer.Deserialize<string>(ref reader, options)!);
                reader.Read();
                return v;
            }
            case "UnscopedVar":
            {
                var v = new Annotation.UnscopedVar(JsonSerializer.Deserialize<string>(ref reader, options)!);
                reader.Read();
                return v;
            }
            case "MacroDefinition":
            {
                var v = new Annotation.MacroDefinition(JsonSerializer.Deserialize<string>(ref reader, options)!);
                reader.Read();
                return v;
            }
            case "Include":
            {
                var v = new Annotation.Include(JsonSerializer.Deserialize<string>(ref reader, options)!);
                reader.Read();
                return v;
            }
            case "Resource":
            {
                var v = new Annotation.Resource(JsonSerializer.Deserialize<string>(ref reader, options)!);
                reader.Read();
                return v;
            }
            case "InSequence":
            {
                var v = new Annotation.InSequence(JsonSerializer.Deserialize<int>(ref reader, options));
                reader.Read();
                return v;
            }
            case "ProcArgument":
            {
                var v = new Annotation.ProcArgument(JsonSerializer.Deserialize<int>(ref reader, options));
                reader.Read();
                return v;
            }
            case "TypePath":
            {
                var v = new Annotation.TypePath(JsonSerializer.Deserialize<TypePathSegment[]>(ref reader, options)!);
                reader.Read();
                return v;
            }

            // --- Tuple variants ---
            case "TreePath":
            {
                var v = ReadTreePath(ref reader, options);
                reader.Read();
                return v;
            }
            case "IncompleteTreePath":
            {
                var v = ReadIncompleteTreePath(ref reader, options);
                reader.Read();
                return v;
            }
            case "ProcHeader":
            {
                var v = ReadProcHeader(ref reader, options);
                reader.Read();
                return v;
            }
            case "ProcBody":
            {
                var v = ReadProcBody(ref reader, options);
                reader.Read();
                return v;
            }
            case "ScopedCall":
            {
                var v = ReadScopedCall(ref reader, options);
                reader.Read();
                return v;
            }
            case "ScopedVar":
            {
                var v = ReadScopedVar(ref reader, options);
                reader.Read();
                return v;
            }
            case "LocalVarScope":
            {
                var v = ReadLocalVarScope(ref reader, options);
                reader.Read();
                return v;
            }
            case "IncompleteTypePath":
            {
                var v = ReadIncompleteTypePath(ref reader, options);
                reader.Read();
                return v;
            }
            case "ProcArguments":
            {
                var v = ReadProcArguments(ref reader, options);
                reader.Read();
                return v;
            }

            // --- Struct variant ---
            case "MacroUse":
            {
                var v = ReadMacroUse(ref reader, options);
                reader.Read();
                return v;
            }

            default:
                throw new JsonException($"Unknown data variant: {variant}");
        }
    }

    private Annotation.TreePath ReadTreePath(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var elems = JsonSerializer.Deserialize<JsonElement[]>(ref reader, options)!;
        return new Annotation.TreePath(elems[0].GetBoolean(), elems[1].Deserialize<string[]>(options)!);
    }

    private Annotation.IncompleteTreePath ReadIncompleteTreePath(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var elems = JsonSerializer.Deserialize<JsonElement[]>(ref reader, options)!;
        return new Annotation.IncompleteTreePath(elems[0].GetBoolean(), elems[1].Deserialize<string[]>(options)!);
    }

    private Annotation.ProcHeader ReadProcHeader(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var elems = JsonSerializer.Deserialize<JsonElement[]>(ref reader, options)!;
        return new Annotation.ProcHeader(elems[0].Deserialize<string[]>(options)!, elems[1].GetInt32());
    }

    private Annotation.ProcBody ReadProcBody(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var elems = JsonSerializer.Deserialize<JsonElement[]>(ref reader, options)!;
        return new Annotation.ProcBody(elems[0].Deserialize<string[]>(options)!, elems[1].GetInt32());
    }

    private Annotation.ScopedCall ReadScopedCall(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var elems = JsonSerializer.Deserialize<JsonElement[]>(ref reader, options)!;
        return new Annotation.ScopedCall(elems[0].Deserialize<string[]>(options)!, elems[1].GetString()!);
    }

    private Annotation.ScopedVar ReadScopedVar(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var elems = JsonSerializer.Deserialize<JsonElement[]>(ref reader, options)!;
        return new Annotation.ScopedVar(elems[0].Deserialize<string[]>(options)!, elems[1].GetString()!);
    }

    private Annotation.LocalVarScope ReadLocalVarScope(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var elems = JsonSerializer.Deserialize<JsonElement[]>(ref reader, options)!;
        return new Annotation.LocalVarScope(elems[0].Deserialize<VarType>(options)!, elems[1].GetString()!);
    }

    private Annotation.IncompleteTypePath ReadIncompleteTypePath(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var elems = JsonSerializer.Deserialize<JsonElement[]>(ref reader, options)!;
        return new Annotation.IncompleteTypePath(
            elems[0].Deserialize<TypePathSegment[]>(options)!,
            elems[1].Deserialize<PathOp>(options)
        );
    }

    private Annotation.ProcArguments ReadProcArguments(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var elems = JsonSerializer.Deserialize<JsonElement[]>(ref reader, options)!;
        return new Annotation.ProcArguments(
            elems[0].Deserialize<string[]>(options)!,
            elems[1].GetString()!,
            elems[2].GetInt32()
        );
    }

    private Annotation.MacroUse ReadMacroUse(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var dto = JsonSerializer.Deserialize<MacroUseDto>(ref reader, options)!;
        return new Annotation.MacroUse(dto.Name, dto.DefinitionLocation);
    }

    private record MacroUseDto(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("definition_location")] DmLocation DefinitionLocation
    );

    public override void Write(Utf8JsonWriter writer, Annotation value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case Annotation.ParentCall:
                writer.WriteStringValue("ParentCall");
                break;
            case Annotation.ReturnVal:
                writer.WriteStringValue("ReturnVal");
                break;
            case Annotation.TreeBlock v:
                WriteNewtype(writer, "TreeBlock", v.Idents, options);
                break;
            case Annotation.Variable v:
                WriteNewtype(writer, "Variable", v.Idents, options);
                break;
            case Annotation.ScopedMissingIdent v:
                WriteNewtype(writer, "ScopedMissingIdent", v.Idents, options);
                break;
            case Annotation.UnscopedCall v:
                WriteNewtype(writer, "UnscopedCall", v.Ident, options);
                break;
            case Annotation.UnscopedVar v:
                WriteNewtype(writer, "UnscopedVar", v.Ident, options);
                break;
            case Annotation.MacroDefinition v:
                WriteNewtype(writer, "MacroDefinition", v.Ident, options);
                break;
            case Annotation.Include v:
                WriteNewtype(writer, "Include", v.Path, options);
                break;
            case Annotation.Resource v:
                WriteNewtype(writer, "Resource", v.Path, options);
                break;
            case Annotation.InSequence v:
                WriteNewtype(writer, "InSequence", v.Index, options);
                break;
            case Annotation.ProcArgument v:
                WriteNewtype(writer, "ProcArgument", v.Index, options);
                break;
            case Annotation.TypePath v:
                WriteNewtype(writer, "TypePath", v.Segments, options);
                break;
            case Annotation.TreePath v:
                WriteTuple(writer, "TreePath", options, v.IsAbsolute, v.Idents);
                break;
            case Annotation.IncompleteTreePath v:
                WriteTuple(writer, "IncompleteTreePath", options, v.IsAbsolute, v.Idents);
                break;
            case Annotation.ProcHeader v:
                WriteTuple(writer, "ProcHeader", options, v.Idents, v.Arity);
                break;
            case Annotation.ProcBody v:
                WriteTuple(writer, "ProcBody", options, v.Idents, v.Arity);
                break;
            case Annotation.ScopedCall v:
                WriteTuple(writer, "ScopedCall", options, v.Scope, v.Ident);
                break;
            case Annotation.ScopedVar v:
                WriteTuple(writer, "ScopedVar", options, v.Scope, v.Ident);
                break;
            case Annotation.LocalVarScope v:
                WriteTupleObject(writer, "LocalVarScope", options, v.Var, v.Ident);
                break;
            case Annotation.IncompleteTypePath v:
                WriteTuple(writer, "IncompleteTypePath", options, v.Segments, v.Op);
                break;
            case Annotation.ProcArguments v:
                WriteTuple(writer, "ProcArguments", options, v.Scope, v.Ident, v.Arity);
                break;
            case Annotation.MacroUse v:
                writer.WriteStartObject();
                writer.WritePropertyName("MacroUse");
                writer.WriteStartObject();
                writer.WriteString("name", v.Name);
                writer.WritePropertyName("definition_location");
                JsonSerializer.Serialize(writer, v.DefinitionLocation, options);
                writer.WriteEndObject();
                writer.WriteEndObject();
                break;
            default:
                throw new JsonException($"Unknown Annotation variant: {value.GetType()}");
        }
    }

    private static void WriteNewtype<T>(Utf8JsonWriter writer, string variant, T value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName(variant);
        JsonSerializer.Serialize(writer, value, options);
        writer.WriteEndObject();
    }

    private static void WriteTuple(Utf8JsonWriter writer, string variant, JsonSerializerOptions options, params object[] values)
    {
        writer.WriteStartObject();
        writer.WritePropertyName(variant);
        writer.WriteStartArray();
        foreach (var val in values)
        {
            JsonSerializer.Serialize(writer, val, val?.GetType() ?? typeof(object), options);
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteTupleObject(Utf8JsonWriter writer, string variant, JsonSerializerOptions options, object v1, object v2)
    {
        writer.WriteStartObject();
        writer.WritePropertyName(variant);
        writer.WriteStartArray();
        JsonSerializer.Serialize(writer, v1, v1.GetType(), options);
        JsonSerializer.Serialize(writer, v2, v2.GetType(), options);
        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}

public static class AnnotationExtensions
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static AnnotationTuple[]? ToAnnotationTuples(this JToken? token)
    {
        if (token == null || token.Type == JTokenType.Null)
            return null;

        var json = token.ToString(Newtonsoft.Json.Formatting.None);
        return JsonSerializer.Deserialize<AnnotationTuple[]>(json, SerializerOptions);
    }

    public static string ToJsonString(this Annotation annotation)
    {
        return JsonSerializer.Serialize(annotation, SerializerOptions);
    }

    public static string ToJsonString(this AnnotationTuple tuple)
    {
        return JsonSerializer.Serialize(tuple, SerializerOptions);
    }
}
