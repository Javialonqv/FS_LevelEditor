using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace FS_LevelEditor.SaveSystem
{
    public static class SaveMigratorHelpers
    {
        public static void RenameProperty(JsonObject objectNode, string oldName, string newName)
        {
            if (!objectNode.TryGetPropertyValue(oldName, out JsonNode oldValue))
                return;

            objectNode.Remove(oldName);
            objectNode.Add(newName, oldValue);
        }

        public static JsonArray EnumerateAllLevelObjects(JsonObject root)
        {
            if (!root.TryGetPropertyValue("objects", out JsonNode objects))
                return null;

            return objects.AsArray();
        }

        public static IEnumerable<JsonObject> EnumerateAllJsonObjects(JsonNode node)
        {
            if (node is JsonObject jsonObject)
            {
                yield return jsonObject;

                foreach (JsonNode child in jsonObject
                    .Select(pair => pair.Value)
                    .Where(value => value != null)
                    .ToArray())
                {
                    foreach (JsonObject nestedObject in EnumerateAllJsonObjects(child))
                        yield return nestedObject;
                }

                yield break;
            }

            if (node is JsonArray jsonArray)
            {
                foreach (JsonNode child in jsonArray
                    .Where(value => value != null)
                    .ToArray())
                {
                    foreach (JsonObject nestedObject in EnumerateAllJsonObjects(child))
                        yield return nestedObject;
                }
            }
        }

        // JsonNode.GetValueKind wasn't introduced until .NET 8 or so, here it's .NET 6, do it ourselves.
        public static JsonValueKind GetValueKind(this JsonNode node)
        {
            if (node == null)
                return JsonValueKind.Null;
            if (node is JsonObject)
                return JsonValueKind.Object;
            if (node is JsonArray)
                return JsonValueKind.Array;

            if (node is JsonValue value && value.TryGetValue(out JsonElement element))
                return element.ValueKind;

            return JsonValueKind.Undefined;
        }

        // JsonNode.DeepClone wasn't introduced until .NET 8 or so, here it's .NET 6, do it ourselves.
        public static JsonNode DeepClone(this JsonNode node)
        {
            if (node == null) return null;

            // If an Object { }, iterate throught its properties.
            if (node is JsonObject obj)
            {
                var newObj = new JsonObject();
                foreach (var property in obj)
                {
                    // Add a copy of each property.
                    newObj.Add(property.Key, property.Value?.DeepClone());
                }
                return newObj;
            }

            // If an Array [ ], iterate throught its properties.
            if (node is JsonArray arr)
            {
                var newArr = new JsonArray();
                foreach (var element in arr)
                {
                    // Add a copy of each element.
                    newArr.Add(element?.DeepClone());
                }
                return newArr;
            }

            // If it's a primitive value (string, int, bool, null), clone it directly.
            return JsonValue.Create(node.AsValue().GetValue<object>());
        }

        public static T GetValueNoException<T>(this JsonObject jsonObj, string propertyName, T defaultValue)
        {
            if (jsonObj[propertyName] is not JsonValue propValue)
                return defaultValue;

            if (propValue.TryGetValue<T>(out var result))
                return result;
            else
                return defaultValue;
        }
    }
}
