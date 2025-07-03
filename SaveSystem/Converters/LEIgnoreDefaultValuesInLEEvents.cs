using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FS_LevelEditor.SaveSystem.Converters
{
    public class LEIgnoreDefaultValuesInLEEvents : JsonConverter<LE_Event>
    {
        public override LE_Event Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Logger.Error("[SAVE FILE] LEIgnoreDefaultValuesInLEEvents converter is for write only.");
            throw new NotSupportedException("[SAVE FILE] LEIgnoreDefaultValuesInLEEvents converter is for write only.");
        }

        public override void Write(Utf8JsonWriter writer, LE_Event value, JsonSerializerOptions options)
        {
            var defaultInstance = new LE_Event();
            writer.WriteStartObject();

            foreach (var property in typeof(LE_Event).GetProperties())
            {
                if (!property.CanRead || !property.CanWrite) continue;
                object defaultValue = property.GetValue(defaultInstance);
                object currentValue = property.GetValue(value);

                if (!Equals(defaultValue, currentValue))
                {
                    writer.WritePropertyName(property.Name);
                    JsonSerializer.Serialize(writer, currentValue, property.PropertyType, options);
                }
            }

            writer.WriteEndObject();
        }
    }
}
