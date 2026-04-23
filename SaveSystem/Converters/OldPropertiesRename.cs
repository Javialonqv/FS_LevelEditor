using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FS_LevelEditor.SaveSystem.Converters
{
    public class OldPropertiesRename<T> : JsonConverter<T>
    {
        private readonly Dictionary<string, string> renames;
        private readonly Dictionary<string, Func<JsonElement, object>> valueConverters;

        public OldPropertiesRename(Dictionary<string, string> renames, Dictionary<string, Func<JsonElement, object>> valueConverters = null)
        {
            this.renames = renames;
            this.valueConverters = valueConverters ?? new Dictionary<string, Func<JsonElement, object>>();
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            using var modifiedStream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(modifiedStream))
            {
                writer.WriteStartObject();

                foreach (var prop in root.EnumerateObject())
                {
                    // Check for the final name (rename if it exists on the "renames" dict).
                    string targetName = renames.ContainsKey(prop.Name) ? renames[prop.Name] : prop.Name;
                    writer.WritePropertyName(targetName);

                    // Check if this name has a value conversion func.
                    if (valueConverters.TryGetValue(targetName, out var converter))
                    {
                        var convertedValue = converter(prop.Value);
                        // Serialize the converted value.
                        JsonSerializer.Serialize(writer, convertedValue, options);
                    }
                    else
                    {
                        // Serialie the original value, no modifications at all.
                        prop.Value.WriteTo(writer);
                    }
                }

                writer.WriteEndObject();
            }

            modifiedStream.Position = 0;

            // Use the same options, BUT removing this converter from there to avoid infinite loops.
            var fallbackOptions = new JsonSerializerOptions(options);
            var existing = fallbackOptions.Converters.FirstOrDefault(c => c is OldPropertiesRename<T>);
            if (existing != null) fallbackOptions.Converters.Remove(existing);

            return JsonSerializer.Deserialize<T>(modifiedStream, fallbackOptions)!;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            Logger.Error("[SAVE FILE] OldPropertiesRename converter is for read only.");
            throw new NotSupportedException("[SAVE FILE] OldPropertiesRename converter is for read only.");
        }
    }
}
