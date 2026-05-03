using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Orbits.GeneralProject.Core.Infrastructure;

namespace OrbitsProject.API.Infrastructure
{
    public sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var raw = reader.GetString();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return default;
            }

            if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var offsetValue))
            {
                return offsetValue.UtcDateTime;
            }

            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedValue))
            {
                return BusinessDateTime.NormalizeClientDateTimeToUtc(parsedValue);
            }

            throw new JsonException($"Invalid DateTime value '{raw}'.");
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(BusinessDateTime.EnsureUtc(value).ToString("O", CultureInfo.InvariantCulture));
        }
    }

    public sealed class NullableUtcDateTimeJsonConverter : JsonConverter<DateTime?>
    {
        private static readonly UtcDateTimeJsonConverter InnerConverter = new();

        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            return InnerConverter.Read(ref reader, typeof(DateTime), options);
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (!value.HasValue)
            {
                writer.WriteNullValue();
                return;
            }

            InnerConverter.Write(writer, value.Value, options);
        }
    }
}
