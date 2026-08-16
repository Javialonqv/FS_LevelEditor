using FS_LevelEditor.SaveSystem.SerializableTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections;
using System.Text.Json;
using FS_LevelEditor.SaveSystem.Converters;

namespace FS_LevelEditor.SaveSystem
{
    public static class SavePatchesLegacy
    {
        public static readonly JsonSerializerOptions OnWriteSaveFileOptions = new JsonSerializerOptions
        {
#if DEBUG
            WriteIndented = true,
#endif
            Converters = { new LEIgnoreDefaultValuesInLEEvents() }
        };
        public static readonly JsonSerializerOptions OnReadSaveFileOptions = new JsonSerializerOptions
        {
            Converters =
            {
                new LEPropertiesConverterNew()
            }
        };

        // For the old properties save system, this is to adjust the type name to the NEW type name (since the namespaces changed.)
        public static string GetCorrectTypeNameForLegacySystem(string typeNameInTheJSONFile)
        {
            switch (typeNameInTheJSONFile)
            {
                // Old assembly qualified name.
                case "FS_LevelEditor.ColorSerializable, FS_LevelEditor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null":
                case "FS_LevelEditor.ColorSerializable":
                    return "FS_LevelEditor.SaveSystem.SerializableTypes.ColorSerializable"; // Using the type full name is enough.

                case "FS_LevelEditor.Vector3Serializable, FS_LevelEditor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null":
                case "FS_LevelEditor.Vector3Serializable":
                    return "FS_LevelEditor.SaveSystem.SerializableTypes.Vector3Serializable";
            }

            return typeNameInTheJSONFile;
        }

        // To convert types from its serialized type in the save file to the REAL type used by LE in runtime.
        public static object ConvertFromSerializableValue(object value)
        {
            if (value is Vector3Serializable)
            {
                return (Vector3)(Vector3Serializable)value;
            }
            else if (value is ColorSerializable)
            {
                return (Color)(ColorSerializable)value;
            }

            return value;
        }
        // Used by the new system to convert a normal type to SERIALIZED type while deserealizing an object property.
        public static Type ConvertTypeToSerializedObjectType(Type originalType)
        {
            if (originalType == typeof(Vector3))
            {
                return typeof(Vector3Serializable);
            }
            else if (originalType == typeof(Color))
            {
                return typeof(ColorSerializable);
            }

            return originalType;
        }

        // Convert types to its serialized type, skips empty lists, etc.
        public static void AddPropertiesToObjectToSave(LE_ObjectData objSerializable, LE_Object originalObj)
        {
            foreach (var property in originalObj.properties)
            {
                if (property.Value is Vector3)
                {
                    objSerializable.properties.Add(property.Key, new Vector3Serializable((Vector3)property.Value));
                }
                else if (property.Value is Color)
                {
                    objSerializable.properties.Add(property.Key, new ColorSerializable((Color)property.Value));
                }
                else if (property.Value is ICollection collection) // Skip empty lists
                {
                    if (collection.Count == 0) continue;

                    objSerializable.properties.Add(property.Key, property.Value);
                }
                else
                {
                    objSerializable.properties.Add(property.Key, property.Value);
                }
            }
        }
    }
}
