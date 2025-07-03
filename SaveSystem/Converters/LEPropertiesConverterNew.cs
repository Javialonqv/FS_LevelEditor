using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FS_LevelEditor.SaveSystem.Converters
{
    public class LEPropertiesConverterNew : JsonConverter<Dictionary<string, object>>
    {
        public override void Write(Utf8JsonWriter writer, Dictionary<string, object> value, JsonSerializerOptions options)
        {
            Logger.Error("[SAVE FILE] LEPRopertiesConverterNew converter is for read only.");
            throw new NotSupportedException("[SAVE FILE] LEPRopertiesConverterNew converter is for read only.");
        }

        public override Dictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                Logger.Error("[SAVE FILE] JSON object was expected.");
                throw new JsonException("JSON object was expected.");
            }

            var deserialized = new Dictionary<string, object>();

            var doc = JsonDocument.ParseValue(ref reader);
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                JsonElement rawValue = prop.Value;
                object value = null;

                if (rawValue.ValueKind == JsonValueKind.Object && rawValue.TryGetProperty("Type", out var rawType) && rawValue.TryGetProperty("Value", out var realRawValue))
                {
                    value = LegacyDeserealize(rawType, realRawValue);
                }
                else
                {
                    // It the json value isn't a primitive type (int, float, string, etc.) this will result in a JsonElement, but this is parsed later with SetProperty()
                    // in LE_Object.
                    value = JsonSerializer.Deserialize<object>(rawValue.GetRawText(), options);
                }

                deserialized.Add(prop.Name, value);
            }

            return deserialized;
        }

        object LegacyDeserealize(JsonElement rawType, JsonElement rawValue)
        {
            string realTypeName = rawType.GetString();
            if (realTypeName == null)
            {
                Logger.Error("[SAVE FILE] [LEGACY] Couldn't get value type, value type was a null string.");
                throw new JsonException("[SAVE FILE] [LEGACY] Couldn't get value type, value type was a null string.");
            }
            Type realType = Type.GetType(SavePatches.GetCorrectTypeNameForLegacySystem(realTypeName));
            if (realType == null)
            {
                Logger.Error($"[SAVE FILE] [LEGACY] Couldn't find type of name \"{realTypeName}\".");
                throw new JsonException($"[SAVE FILE] [LEGACY] Couldn't find type of name \"{realTypeName}\".");
            }

            return JsonSerializer.Deserialize(rawValue.GetRawText(), realType);
        }
        public static object NewDeserealize(Type type, JsonElement rawValue)
        {
            try
            {
                // The properties only contain the ORIGINAL type, but what if the save data contains info about an object with a custom serialization type?
                Type typeToDeserealize = SavePatches.ConvertTypeToSerializedObjectType(type);
                return JsonSerializer.Deserialize(rawValue.GetRawText(), typeToDeserealize);
            }
            catch
            {
                return null;
            }
        }
    }
}
