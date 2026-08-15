using FS_LevelEditor.SaveSystem.SerializableTypes;
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

                bool isPlayerEvent = obj["isForPlayer"] is JsonValue playerValue && playerValue.TryGetValue<bool>(out bool isForPlayer) && isForPlayer;
                if (!isPlayerEvent)
                    isPlayerEvent = obj["targetObjName"] is JsonValue playerValue2 && playerValue2.TryGetValue<string>(out string targetObjName) && targetObjName == "Player";

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

            // LEGACY "EventExecuter.UpdateLEEventsToTheNewSystem" FUNCTIONALITY HERE!!
            foreach (var obj in SaveMigratorHelpers.EnumerateAllJsonObjects(root))
            {
                bool isOldEvent = obj.ContainsKey("targetObjName");
                if (!isOldEvent)
                    continue;

                // This method is used to update the LE_Event targetObjType and targetObjID properties in case it comes from a previous version that used targetObjName.
                string targetObjName = obj["targetObjName"].ToString();

                bool isPlayer = string.Equals(targetObjName, Loc.Get("Player"), StringComparison.OrdinalIgnoreCase);
                bool isTaser = string.Equals(targetObjName, Loc.Get("Taser"), StringComparison.OrdinalIgnoreCase);
                bool isJetpack = string.Equals(targetObjName, Loc.Get("Jetpack"), StringComparison.OrdinalIgnoreCase);
                bool isObjective = targetObjName.StartsWith("Obj_", StringComparison.OrdinalIgnoreCase);

                bool isValid = obj["isValid"] is JsonValue validValue && validValue.TryGetValue<bool>(out bool parsedIsValid) && parsedIsValid;

                if (isPlayer)
                {
                    obj["isForPlayer"] = true;
                    obj["isForTaser"] = false;
                    obj["isForJetpack"] = false;
                    obj["isForObjective"] = false;
                    obj["targetObjType"] = null;
                    obj["targetObjID"] = 0;
                    obj.Remove("targetObjName");
                }
                else if (isTaser)
                {
                    obj["isForPlayer"] = false;
                    obj["isForTaser"] = true;
                    obj["isForJetpack"] = false;
                    obj["isForObjective"] = false;
                    obj["targetObjType"] = null;
                    obj["targetObjID"] = 0;
                    obj.Remove("targetObjName");
                }
                else if (isJetpack)
                {
                    obj["isForPlayer"] = false;
                    obj["isForTaser"] = false;
                    obj["isForJetpack"] = true;
                    obj["isForObjective"] = false;
                    obj["targetObjType"] = null;
                    obj["targetObjID"] = 0;
                    obj.Remove("targetObjName");
                }
                else if (isObjective)
                {
                    obj["isForPlayer"] = false;
                    obj["isForTaser"] = false;
                    obj["isForJetpack"] = false;
                    obj["isForObjective"] = true;
                    obj["targetObjType"] = null;
                    obj["targetObjID"] = 0;
                    obj["objectiveName"] = targetObjName.Substring(4);
                    obj.Remove("targetObjName");
                }
                else if (obj["targetObjType"] == null && isValid && !string.IsNullOrEmpty(targetObjName))
                {
                    var objData = Utils.SplitTypeAndId(targetObjName);
                    var objType = LE_Object.ConvertNameToObjectType(objData.type);

                    if (objType != null)
                    {
                        obj["targetObjType"] = (int)objType;
                        obj["targetObjID"] = objData.id;
                        obj.Remove("targetObjName"); // Clear the name, since we are using the type and ID now.
                    }
                }
            }

            // LEGACY "LegacyDeserealize" for { Type, Value } properties FUNCTIONALITY HERE!!
            foreach (var objNode in SaveMigratorHelpers.EnumerateAllLevelObjects(root))
            {
                var obj = objNode.AsObject();

                if (!obj.TryGetPropertyValue("properties", out var properties))
                    continue;

                foreach (var property in properties.AsObject().ToList())
                {
                    if (property.Value is JsonObject propertyObj
                        && propertyObj.TryGetPropertyValue("Type", out var typeNode)
                        && propertyObj.TryGetPropertyValue("Value", out var valueNode))
                    {
                        string realTypeName = typeNode.ToString();
                        if (realTypeName == null)
                        {
                            Logger.Error("[SAVE FILE] [LEGACY] Couldn't get value type, value type was a null string.");
                            continue;
                        }
                        Type realType = Type.GetType(SavePatchesLegacy.GetCorrectTypeNameForLegacySystem(realTypeName));
                        if (realType == null)
                        {
                            Logger.Error($"[SAVE FILE] [LEGACY] Couldn't find type of name \"{realTypeName}\".");
                            continue;
                        }

                        // Create a copy of the node because it already belongs to the property.
                        properties[property.Key] = valueNode.DeepClone();
                    }
                }
            }

            // LEGACY "SavePatchesLegacy.ReevaluateOldProperties" FUNCTIONALITY HERE!!
            foreach (var objNode in SaveMigratorHelpers.EnumerateAllLevelObjects(root))
            {
                var obj = objNode.AsObject();

                if (!obj.TryGetPropertyValue("properties", out var properties))
                    continue;

                SaveMigratorHelpers.RenameProperty(properties.AsObject(), "OnActivatedEvents", "WhenActivatingEvents");
                SaveMigratorHelpers.RenameProperty(properties.AsObject(), "OnDeactivatedEvents", "WhenDeactivatingEvents");
                SaveMigratorHelpers.RenameProperty(properties.AsObject(), "OnChangeEvents", "WhenInvertingEvents");
            }

            // LEGACY "SavePatchesLegacy.IsOldSawWaypointsSave" FUNCTIONALITY HERE!!
            foreach (var objNode in SaveMigratorHelpers.EnumerateAllLevelObjects(root))
            {
                var obj = objNode.AsObject();

                // This only works for SAW waypoints.
                if (obj["objectType"] is not JsonValue objectType || objectType.GetValue<int>() != (int)LE_Object.ObjectType.SAW)
                    continue;

                if (obj["properties"] is not JsonObject properties          // properties exist.
                    || properties["waypoints"] is not JsonArray waypoints   // waypoints exist.
                    || waypoints.Count == 0                                 // waypoints aren't empty.
                    || waypoints[0] is not JsonObject firstWaypoint         // first waypoint is an object.
                    || !firstWaypoint.ContainsKey("waypointPosition"))      // waypoint has the old position prop.
                    continue;

                // In the old system, there was an empty waypoint at the start, in the new one, there isn't, so skip the first one since it's useless.
                waypoints.RemoveAt(0);

                foreach (var waypointNode in waypoints)
                {
                    if (waypointNode is not JsonObject waypoint)
                        continue;

                    SaveMigratorHelpers.RenameProperty(waypointNode, "waypointPosition", "position");
                    SaveMigratorHelpers.RenameProperty(waypointNode, "waypointRotation", "rotation");
                }
            }
        }
    }
}
