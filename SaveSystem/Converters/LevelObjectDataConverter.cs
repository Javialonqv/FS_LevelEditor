using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FS_LevelEditor.SaveSystem.Converters
{
    public class LevelObjectDataConverter : JsonConverter<LE_ObjectData>
    {
        public override void Write(Utf8JsonWriter writer, LE_ObjectData value, JsonSerializerOptions options)
        {
            Logger.Error("[SAVE FILE] LevelObjectDataConverter converter is for read only.");
            throw new NotSupportedException("[SAVE FILE] LevelObjectDataConverter converter is for read only.");
        }
        public override LE_ObjectData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;
            var result = JsonSerializer.Deserialize<LE_ObjectData>(root.GetRawText());

            if (root.TryGetProperty("objectOriginalName", out var originalRawName))
            {
                string originalName = originalRawName.GetString();
                var convertedType = LE_Object.ConvertNameToObjectType(originalName);

                if (convertedType != null)
                {
                    result.objectType = convertedType;
                    Logger.Log($"Succesfully converted \"{originalName}\" to {convertedType}!");
                }
                else
                {
                    Logger.Error($"Failed to convert \"{originalName}\" to an object type! This is probably a bug, report if you didn't modify the" +
                        $"save file.");
                }
            }

            return result;
        }
    }
}
