using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace FS_LevelEditor.SaveSystem
{
    public static class SaveMigrator
    {
        public const int CURRENT_SCHEMA_VERSION = 2;

        public static LevelData DeserializeLevelData(string json, string fileName)
        {
            JObject root = JObject.Parse(json);

            UpgradeToCurrent(root, fileName);

            return root.ToObject<LevelData>(JsonSerializer.Create(SavePatchesLegacy.OnReadSaveFileOptions));
        }

        static void UpgradeToCurrent(JObject root, string fileName)
        {
            int schemaVersion = ReadSchemaVersion(root);

            if (schemaVersion > CURRENT_SCHEMA_VERSION)
            {
                Logger.Error($"[SAVE SYSTEM] [MIGRATOR] The level '{fileName}' has schema version {schemaVersion}, but this mod version only supports up to {CURRENT_SCHEMA_VERSION}." +
                             $"Are you trying to open it in an older version of the Level Editor mod?");
                return;
            }

            bool needsUpgrade = false;
            if (schemaVersion != CURRENT_SCHEMA_VERSION)
            {
                Logger.Log("[SAVE SYSTEM] [MIGRATOR] Detected and older level file! Schema Version: " + schemaVersion);
                needsUpgrade = true;
            }
            else
            {
                Logger.Log("[SAVE SYSTEM] [MIGRATOR] The level file is currently in the latest version! Schema Version: " + schemaVersion);
            }

            while (schemaVersion < CURRENT_SCHEMA_VERSION)
            {
                Stopwatch watch = Stopwatch.StartNew();
                Logger.Log($"[SAVE SYSTEM] [MIGRATOR] Migrating save file from V{schemaVersion} to V{schemaVersion + 1}...");

                switch (schemaVersion)
                {
                    case 0:
                        MigrateV0ToV1(root);
                        break;

                    case 1:
                        MigrateV1ToV2(root);
                        break;
                }

                watch.Stop();
                Logger.Log($"[SAVE SYSTEM] [MIGRATOR] Finished migrating from V{schemaVersion} to V{schemaVersion + 1}! Took: {watch.Elapsed}");

                schemaVersion++;
                root["schemaVersion"] = schemaVersion;
            }

            if (needsUpgrade)
            {
                Logger.Log("[SAVE SYSTEM] [MIGRATOR] FINISHED MIGRATING LEVEL SAVE FILE");
            }
        }
        static int ReadSchemaVersion(JObject root)
        {
            if (!root.TryGetValue("schemaVersion", out JToken node) || node.Type == JTokenType.Null)
                return 0;

            return node.Value<int>();
        }

        // In Newtonsoft a JSON null is a REAL JValue (Type == Null), not a C# null like in System.Text.Json.
        // Every "== null" check from the old code has to go through here to keep the same behaviour.
        static bool IsNullOrMissing(JToken token)
        {
            return token == null || token.Type == JTokenType.Null;
        }

        static void MigrateV0ToV1(JObject root)
        {
            // LEGACY "OldPropertiesRename" FUNCTIONALITY HERE!!
            // TARGET: LE_Event
            // Rename:
            //  - setActive       ->    spawn
            //  - moveObject      ->    moveState
            // Convert:
            //  - moveState BOOL  ->    moveState ENUM
            // Ensure:
            //  - upgrades is NOT null, and replace it with an empty list if so.
            foreach (var obj in SaveMigratorHelpers.EnumerateAllJsonObjects(root))
            {
                // Yes, to identify old events, we just do this, not the best thing in the world, but it works... I guess...
                bool isOldEvent = obj.ContainsKey("setActive") || obj.ContainsKey("moveObject");
                if (isOldEvent)
                {
                    SaveMigratorHelpers.RenameProperty(obj, "setActive", "spawn");
                    SaveMigratorHelpers.RenameProperty(obj, "moveObject", "moveState");

                    if (obj.TryGetValue("moveState", out var moveState))
                    {
                        if (moveState.Type == JTokenType.Boolean)
                        {
                            var enumValue = moveState.Value<bool>()
                                ? LE_Event.MoveState.Start_Moving
                                : LE_Event.MoveState.Do_Nothing;

                            obj["moveState"] = (int)enumValue;
                        }
                    }
                }

                bool isPlayerEvent = obj.GetValueNoException<bool>("isForPlayer", false);
                if (!isPlayerEvent)
                    isPlayerEvent = obj.GetValueNoException<string>("targetObjName", "") == Loc.Get("Player");

                // This is to fix a bug where "upgrades" used to be null by default, which caused some issues in playmode. Changing the default value in LE_Event fixes it from now on.
                // But we need to use this code to intercept any null value from old levels and force it to be a correct list.
                if (isPlayerEvent && obj.TryGetValue("upgrades", out var upgrades))
                {
                    if (upgrades.Type == JTokenType.Null)
                    {
                        obj["upgrades"] = new JArray();
                    }
                }
            }

            // LEGACY "LevelObjectDataConverter" FUNCTIONALITY HERE!!
            // TARGET: LE_Object
            // Convert:
            //  - objectOriginalName STRING -> objectType ENUM and objectID INT
            foreach (var objNode in SaveMigratorHelpers.EnumerateAllLevelObjects(root))
            {
                if (objNode is not JObject obj)
                    continue;

                if (obj.TryGetValue("objectOriginalName", out var objNameNode))
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
            // TARGET: LE_Event
            // Convert:
            //  - targetObjName STRING
            //      - isForPlayer, isForTaser, isForJetpack, isForObjective BOOLS
            //      OR
            //      - targetObjType ENUM AND targetObjID INT.
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

                bool isValid = obj["isValid"] is JValue validValue
                    && validValue.Type == JTokenType.Boolean
                    && validValue.Value<bool>();

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
                else if (IsNullOrMissing(obj["targetObjType"]) && isValid && !string.IsNullOrEmpty(targetObjName))
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
            // TARGET: LE_Object Properties AND Global Properties.
            // Convert:
            //  - Properties with the structure:
            //  propName:
            //      Type STRING (Type Namespace in C#)
            //      Value OBJECT
            //  TO
            //  propName: Value OBJECT
            foreach (var objNode in SaveMigratorHelpers.EnumerateAllLevelObjects(root))
            {
                if (objNode is not JObject obj)
                    continue;

                if (obj["properties"] is not JObject properties)
                    continue;

                MigrateLegacyTypedProperties(properties);
            }
            if (root["globalProperties"] is JObject globalProperties)
            {
                MigrateLegacyTypedProperties(globalProperties);
            }

            // LEGACY "SavePatchesLegacy.ReevaluateOldProperties" FUNCTIONALITY HERE!!
            // TARGET: LE_Object Properties Names
            // Rename:
            //  - OnActivatedEvents     ->      WhenActivatingEvents
            //  - OnDeactivatedEvents   ->      WhenDeactivatingEvents
            //  - OnChangeEvents        ->      WhenInvertingEvents
            foreach (var objNode in SaveMigratorHelpers.EnumerateAllLevelObjects(root))
            {
                if (objNode is not JObject obj)
                    continue;

                if (obj["properties"] is not JObject properties)
                    continue;

                SaveMigratorHelpers.RenameProperty(properties, "OnActivatedEvents", "WhenActivatingEvents");
                SaveMigratorHelpers.RenameProperty(properties, "OnDeactivatedEvents", "WhenDeactivatingEvents");
                SaveMigratorHelpers.RenameProperty(properties, "OnChangeEvents", "WhenInvertingEvents");
            }

            // LEGACY "SavePatchesLegacy.IsOldSawWaypointsSave" FUNCTIONALITY HERE!!
            // TARGET: LE_Saw Wayputos
            // Rename:
            //  - waypointPosition      ->      position
            //  - waypointRotation      ->      rotation
            foreach (var objNode in SaveMigratorHelpers.EnumerateAllLevelObjects(root))
            {
                if (objNode is not JObject obj)
                    continue;

                // This only works for SAW waypoints.
                if (obj["objectType"] is not JValue objectType || objectType.Type != JTokenType.Integer || objectType.Value<int>() != (int)LE_Object.ObjectType.SAW)
                    continue;

                if (obj["properties"] is not JObject properties             // properties exist.
                    || properties["waypoints"] is not JArray waypoints      // waypoints exist.
                    || waypoints.Count == 0                                 // waypoints aren't empty.
                    || waypoints[0] is not JObject firstWaypoint            // first waypoint is an object.
                    || !firstWaypoint.ContainsKey("waypointPosition"))      // waypoint has the old position prop.
                    continue;

                // In the old system, there was an empty waypoint at the start, in the new one, there isn't, so skip the first one since it's useless.
                waypoints.RemoveAt(0);

                foreach (var waypointNode in waypoints)
                {
                    if (waypointNode is not JObject waypoint)
                        continue;

                    SaveMigratorHelpers.RenameProperty(waypoint, "waypointPosition", "position");
                    SaveMigratorHelpers.RenameProperty(waypoint, "waypointRotation", "rotation");
                }
            }
        }
        static void MigrateLegacyTypedProperties(JObject properties)
        {
            foreach (var property in properties.Properties().ToList())
            {
                if (property.Value is JObject propertyObj
                    && propertyObj.TryGetValue("Type", out var typeNode)
                    && propertyObj.TryGetValue("Value", out var valueNode))
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
                    properties[property.Name] = valueNode.DeepClone();
                }
            }
        }

        static void MigrateV1ToV2(JObject root)
        {
            // For V2, the default spawn state changed from: Toggle -> Do Nothing.
            // Make sure every value in V1 stays as is.
            // Iterate through each event and when we find one that does NOT have spawn specified,
            // then we know that, since it's from V1, it has to be Toggle, specify it.
            foreach (var objNode in SaveMigratorHelpers.EnumerateAllLevelObjects(root))
            {
                if (objNode is not JObject obj)
                    continue;

                if (obj["properties"] is not JObject properties)
                    continue;

                // we don't have a proper way to tell if a property has event lists or not, so...
                foreach (var prop in properties)
                {
                    #region Check The Prop Entry Is A Events Array
                    if (prop.Value is not JArray array) // Events ARE arrays.
                        continue;

                    if (array.Count == 0) // Skip empty arrays.
                        continue;

                    if (array[0] is not JObject firstItem) // The elements in the events' arrays are objects.
                        continue;

                    // MOMENT OF TRUTH: An event needs to have AT LEAST one of these properties, this way we make sure that this array indeed contains events.
                    if (!firstItem.ContainsKey("isForPlayer") && !firstItem.ContainsKey("isForTaser") && !firstItem.ContainsKey("isForJetpack") && !firstItem.ContainsKey("isForObjective") && !firstItem.ContainsKey("targetObjType"))
                        continue;
                    #endregion

                    foreach (var item in array)
                    {
                        if (item is not JObject eventObj)
                            continue;

                        if (eventObj.ContainsKey("spawn")) // Skips ones that ALREADY have spawn specified.
                            continue;

                        eventObj["spawn"] = (int)LE_Event.SpawnState.Toggle;
                    }
                }
            }
        }
    }
}
