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

        public OldPropertiesRename(Dictionary<string, string> renames)
        {
            this.renames = renames;
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            var newProperties = new Dictionary<string, JsonElement>();

            foreach (var prop in root.EnumerateObject())
            {
                string nameToAdd = prop.Name;
                if (renames.ContainsKey(prop.Name))
                {
                    nameToAdd = renames[prop.Name];
                }
                newProperties[nameToAdd] = prop.Value;
            }

            using var modifiedStream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(modifiedStream))
            {
                writer.WriteStartObject();
                foreach (var property in newProperties)
                {
                    writer.WritePropertyName(property.Key);
                    property.Value.WriteTo(writer);
                }
                writer.WriteEndObject();
            }

            modifiedStream.Position = 0;
            return JsonSerializer.Deserialize<T>(modifiedStream)!;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            Logger.Error("[SAVE FILE] OldPropertiesRename converter is for read only.");
            throw new NotSupportedException("[SAVE FILE] OldPropertiesRename converter is for read only.");
        }
    }
}
