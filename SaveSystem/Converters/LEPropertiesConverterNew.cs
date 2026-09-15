using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FS_LevelEditor.SaveSystem.Converters
{
    public class LEPropertiesConverterNew : JsonConverter<Dictionary<string, object>>
    {
        public override void WriteJson(JsonWriter writer, Dictionary<string, object> existingValue, JsonSerializer serializer)
        {
            // Fuck it, I need to do this because I needed to use a fucking attribute for the properties in WaypointData, and now I NEED to implement Write().
            serializer.Serialize(writer, existingValue);
            return;

            Logger.Error("[SAVE FILE] LEPRopertiesConverterNew converter is for read only.");
            throw new NotSupportedException("[SAVE FILE] LEPRopertiesConverterNew converter is for read only.");
        }

        public override Dictionary<string, object> ReadJson(JsonReader reader, Type objectType, Dictionary<string, object> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartObject)
            {
                Logger.Error("[SAVE FILE] JSON object was expected.");
                throw new JsonException("JSON object was expected.");
            }

            var deserialized = new Dictionary<string, object>();

            JObject obj = JObject.Load(reader);
            foreach (var prop in obj.Properties())
            {
                JToken rawValue = prop.Value;
                object value = null;

                // If this is the Global Properties dictionary.
                if (LevelData.GetDefaultGlobalProperties().ContainsKey(prop.Name))
                {
                    var valueType = LevelData.GetDefaultGlobalProperties()[prop.Name].GetType();
                    value = rawValue.ToObject(valueType, JsonSerializer.CreateDefault());
                }
                else // Default deserialization, take it as if it were a normal object properties entry.
                {
                    // Keep the raw JToken value, it will get resolved later in LE_Object.SetProperty, which will then handle the conversion correctly based on the property type.
                    value = rawValue;
                }

                deserialized.Add(prop.Name, value);
            }

            return deserialized;
        }

        public static object NewDeserealize(Type type, JToken rawValue)
        {
            try
            {
                // The properties only contain the ORIGINAL type, but what if the save data contains info about an object with a CUSTOM serialization type?
                // Example: property value type is Vector3, but the saved type is actually Vector3Serializable.
                Type typeToDeserealize = SavePatchesLegacy.ConvertTypeToSerializedObjectType(type);
                return rawValue.ToObject(typeToDeserealize, JsonSerializer.Create(SavePatchesLegacy.OnReadSaveFileOptions));
            }
            catch
            {
                return null;
            }
        }
    }
}
