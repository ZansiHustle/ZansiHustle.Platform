using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZansiHustle.API.Json
{
    /// <summary>
    /// Serializes every <see cref="DateTime"/> as a UTC ISO-8601 string WITH a
    /// trailing 'Z'. EF Core reads SQL <c>datetime2</c> back as
    /// <see cref="DateTimeKind.Unspecified"/>; without this converter
    /// System.Text.Json omits the 'Z', so mobile clients parse the instant as
    /// LOCAL time — the "times appear 2 hours behind" bug. All our persisted
    /// timestamps are UTC, so an Unspecified kind is treated as UTC; a Local kind
    /// is converted. This is the single, central fix (no scattered +2h hacks).
    /// </summary>
    public sealed class UtcDateTimeConverter : JsonConverter<DateTime>
    {
        private const string Format = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.GetDateTime();

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            var utc = value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            };
            writer.WriteStringValue(utc.ToString(Format, CultureInfo.InvariantCulture));
        }
    }

    /// <summary>Nullable companion to <see cref="UtcDateTimeConverter"/>.</summary>
    public sealed class NullableUtcDateTimeConverter : JsonConverter<DateTime?>
    {
        private const string Format = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";

        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.TokenType == JsonTokenType.Null ? null : reader.GetDateTime();

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNullValue();
                return;
            }
            var v = value.Value;
            var utc = v.Kind switch
            {
                DateTimeKind.Utc => v,
                DateTimeKind.Local => v.ToUniversalTime(),
                _ => DateTime.SpecifyKind(v, DateTimeKind.Utc),
            };
            writer.WriteStringValue(utc.ToString(Format, CultureInfo.InvariantCulture));
        }
    }
}
