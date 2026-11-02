using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MineSenseSafety.Shared.Infrastructure.Persistence.MongoDb;

/// <summary>
/// Exact JSON snapshot of an aggregate, stored as the <c>Payload</c> of its MongoDB document
/// (the convention of Feedback &amp; Reliability). Domain types (any type in a <c>.Domain</c>
/// namespace) are written field by field, private setters and collections included, and restored
/// without running their constructors, so the domain classes stay free of persistence attributes
/// and rehydration constructors. Times keep their full precision and offset, which the hash chain
/// of the access audit log and the "newer wins" rules rely on.
/// </summary>
public static class AggregateSnapshot
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter(), new DomainStateConverterFactory() }
    };

    public static string Serialize<T>(T value) where T : class => JsonSerializer.Serialize(value, Options);

    public static T Deserialize<T>(string payload) where T : class =>
        JsonSerializer.Deserialize<T>(payload, Options)
        ?? throw new InvalidOperationException($"Invalid stored {typeof(T).Name}.");

    private sealed class DomainStateConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) =>
            !typeToConvert.IsEnum && !typeToConvert.IsPrimitive && !typeToConvert.IsAbstract
            && !typeToConvert.IsInterface && !typeToConvert.IsGenericType
            && typeToConvert.Namespace is { } name
            && (name.Contains(".Domain.", StringComparison.Ordinal) || name.EndsWith(".Domain", StringComparison.Ordinal));

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
            (JsonConverter)Activator.CreateInstance(typeof(DomainStateConverter<>).MakeGenericType(typeToConvert))!;
    }

    private sealed class DomainStateConverter<T> : JsonConverter<T>
    {
        private static readonly IReadOnlyDictionary<string, FieldInfo> Fields = StateFields.Of(typeof(T));

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException($"Expected the stored state of {typeof(T).Name}.");

            var state = RuntimeHelpers.GetUninitializedObject(typeof(T));
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                var name = reader.GetString()!;
                reader.Read();
                if (Fields.TryGetValue(name, out var field))
                    field.SetValue(state, JsonSerializer.Deserialize(ref reader, field.FieldType, options));
                else
                    reader.Skip();
            }
            return (T)state;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            foreach (var (name, field) in Fields)
            {
                writer.WritePropertyName(name);
                JsonSerializer.Serialize(writer, field.GetValue(value), field.FieldType, options);
            }
            writer.WriteEndObject();
        }
    }

    private static class StateFields
    {
        private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, FieldInfo>> Cache = new();

        /// <summary>Instance fields of the type and its bases, named after their property when auto-generated.</summary>
        public static IReadOnlyDictionary<string, FieldInfo> Of(Type type) => Cache.GetOrAdd(type, static target =>
        {
            var fields = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
            for (var current = target; current is not null && current != typeof(object); current = current.BaseType)
            {
                foreach (var field in current.GetFields(BindingFlags.Instance | BindingFlags.Public |
                                                        BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    fields.TryAdd(NameOf(field), field);
            }
            return fields;
        });

        private static string NameOf(FieldInfo field)
        {
            var name = field.Name;
            if (name.StartsWith('<'))
                return name[1..name.IndexOf('>')];
            return name.TrimStart('_');
        }
    }
}
