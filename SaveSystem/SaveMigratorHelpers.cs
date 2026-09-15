
using Newtonsoft.Json.Linq;

namespace FS_LevelEditor.SaveSystem
{
    public static class SaveMigratorHelpers
    {
        public static void RenameProperty(JObject objectNode, string oldName, string newName)
        {
            if (!objectNode.TryGetValue(oldName, out JToken oldValue))
                return;

            objectNode.Remove(oldName);
            objectNode.Add(newName, oldValue);
        }

        public static JArray EnumerateAllLevelObjects(JObject root)
        {
            if (!root.TryGetValue("objects", out JToken objects))
                return null;

            return objects as JArray;
        }

        public static IEnumerable<JObject> EnumerateAllJsonObjects(JToken node)
        {
            if (node is JObject jsonObject)
            {
                yield return jsonObject;

                foreach (JToken child in jsonObject.Properties()
                    .Select(pair => pair.Value)
                    .Where(value => value != null)
                    .ToArray())
                {
                    foreach (JObject nestedObject in EnumerateAllJsonObjects(child))
                        yield return nestedObject;
                }

                yield break;
            }

            if (node is JArray jsonArray)
            {
                foreach (JToken child in jsonArray
                    .Where(value => value != null)
                    .ToArray())
                {
                    foreach (JObject nestedObject in EnumerateAllJsonObjects(child))
                        yield return nestedObject;
                }
            }
        }

        public static T GetValueNoException<T>(this JObject jsonObj, string propertyName, T defaultValue)
        {
            JToken token = jsonObj[propertyName];
            if (token == null || token.Type == JTokenType.Null)
                return defaultValue;

            try
            {
                return token.Value<T>();
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}
