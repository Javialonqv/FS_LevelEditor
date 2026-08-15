using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace FS_LevelEditor.SaveSystem
{
    public static class SaveMigrator
    {
        public const int CURRENT_SCHEMA_VERSION = 1;

        public static LevelData DeserializeLevelData(string json, string fileName)
        {
            JsonObject root = JsonNode.Parse(json)?.AsObject();

            UpgradeToCurrent(root, fileName);

            return JsonSerializer.Deserialize<LevelData>(
                root.ToJsonString(),
                SavePatchesLegacy.OnReadSaveFileOptions);
        }

        static void UpgradeToCurrent(JsonObject root, string fileName)
        {
            int schemaVersion = ReadSchemaVersion(root);

            if (schemaVersion > CURRENT_SCHEMA_VERSION)
            {
                Logger.Error($"The level '{fileName}' has schema version {schemaVersion}, but this mod version only supports up to {CURRENT_SCHEMA_VERSION}." +
                             $"Are you trying to open it in an older version of the Level Editor mod?");
                return;
            }

            if (schemaVersion != CURRENT_SCHEMA_VERSION)
                Logger.Log("[SAVE MIGRATOR] Detected and older level file! Schema Version: " + schemaVersion);
            else
                Logger.Log("[SAVE MIGRATOR] The level file is currently in the latest version! Schema Version: " + schemaVersion);

            while (schemaVersion < CURRENT_SCHEMA_VERSION)
            {
                switch (schemaVersion)
                {
                    case 0:
                        MigrateV0ToV1(root);
                        break;
                }

                schemaVersion++;
                root["schemaVersion"] = schemaVersion;
            }
        }
        static int ReadSchemaVersion(JsonObject root)
        {
            if (!root.TryGetPropertyValue("schemaVersion", out JsonNode node) || node == null)
                return 0;

            return node.GetValue<int>();
        }

        static void MigrateV0ToV1(JsonObject root)
        {
            Logger.Log("[SAVE MIGRATOR] Migrating save file from V0 to V1...");

            // LEGACY "OldPropertiesRename" FUNCTIONALITY HERE!!
            foreach (var obj in SaveMigratorHelpers.EnumerateAllJsonObjects(root))
            {
                // Yes, to identify old events, we just do this, not the best thing in the world, but it works... I guess...
                bool isOldEvent = obj.ContainsKey("setActive") || obj.ContainsKey("moveObject");
                if (isOldEvent)
                {
                    SaveMigratorHelpers.RenameProperty(obj.AsObject(), "setActive", "spawn");
                    SaveMigratorHelpers.RenameProperty(obj.AsObject(), "moveObject", "moveState");

                    if (obj.TryGetPropertyValue("moveState", out var moveState))
                    {
                        var valueKind = moveState.GetValueKind();
                        if (valueKind == JsonValueKind.True || valueKind == JsonValueKind.False)
                        {
                            var enumValue = moveState.GetValue<bool>()
                                ? LE_Event.MoveState.Start_Moving
                                : LE_Event.MoveState.Do_Nothing;

                            obj["moveState"] = (int)enumValue;
                        }
                    }
                }

                bool isPlayerEvent = obj.ContainsKey("isForPlayer") && obj["isForPlayer"].AsValue().TryGetValue<bool>(out bool isForPlayer) && isForPlayer;

                // This is to fix a bug where "upgrades" used to be null by default, which caused some issues in playmode. Changing the default value in LE_Event fixes it from now on.
                // But we need to use this code to intercept any null value from old levels and force it to be a correct list.
                if (isPlayerEvent && obj.TryGetPropertyValue("upgrades", out var upgrades))
                {
                    if (upgrades.GetValueKind() == JsonValueKind.Null)
                    {
                        obj["upgrades"] = new JsonArray();
                    }
                }
            }

            // LEGACY "LevelObjectDataConverter" FUNCTIONALITY HERE!!
            foreach (var objNode in SaveMigratorHelpers.EnumerateAllLevelObjects(root))
            {
                var obj = objNode.AsObject();

                if (obj.TryGetPropertyValue("objectOriginalName", out var objNameNode))
                {
                    string objName = objNameNode.ToString();
                    var convertedType = LE_Object.ConvertNameToObjectType(objName);

                    if (convertedType != null)
                    {
                        obj.Remove("objectOriginalName");
                        obj["objectType"] = (int)convertedType;
                    }
                    else
                    {
                        Logger.Error($"Failed to convert \"{objName}\" to an object type! This is probably a bug, report if you didn't modify the save file.");
                    }
                }
            }
        }
    }
}
