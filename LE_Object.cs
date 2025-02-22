using Il2Cpp;
using Il2CppInterop.Runtime;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Object : MonoBehaviour
    {
        public enum ObjectType
        {
            GROUND,
            WALL,
            LIGHT,
            VENT_WITH_SMOKE
        }

        public static readonly Dictionary<string, ObjectType> objectVariants = new Dictionary<string, ObjectType>()
        {
            { "CYAN_GROUND", ObjectType.GROUND },
            { "RED_GROUND", ObjectType.GROUND },
            { "ORANGE_GROUND", ObjectType.GROUND },
            { "LARGE_GROUND", ObjectType.GROUND },
            { "GROUND_2", ObjectType.GROUND },

            { "X_WALL", ObjectType.WALL },
            { "WINDOW", ObjectType.WALL },

            { "DIRECTIONAL_LIGHT", ObjectType.LIGHT },
            { "POINT_LIGHT", ObjectType.LIGHT },

            { "VENT_WITH_SMOKE_GREEN", ObjectType.VENT_WITH_SMOKE },
            { "VENT_WITH_SMOKE_CYAN", ObjectType.VENT_WITH_SMOKE },
        };

        public ObjectType? objectType;
        public int objectID;
        public string objectOriginalName;
        public virtual string objectFullNameWithID
        {
            get { return objectOriginalName + " " + objectID; }
        }

        /// <summary>
        /// The correct way to add a LE_Object component to a GameObject.
        /// </summary>
        /// <param name="targetObj">The GameObject ot attach this component to.</param>
        /// <param name="originalObjName">THe "original" name of the desired object.</param>
        /// <returns>An instance of the created LE_Object component class.</returns>
        public static LE_Object AddComponentToObject(GameObject targetObj, string originalObjName)
        {
            string className = "LE_" + originalObjName.Replace(' ', '_');
            Type classType = Type.GetType("FS_LevelEditor." + className);

            if (classType != null)
            {
                if (HasReachedObjectLimit(classType))
                {
                    Utilities.ShowCustomNotificationRed("Object limit reached for this object.", 2f);
                    return null;
                }
                LE_Object instancedComponent = (LE_Object)targetObj.AddComponent(Il2CppType.From(classType));
                instancedComponent.Init(originalObjName);
                return instancedComponent;
            }
            else
            {
                Logger.DebugWarning($"Can't find class of name \"{className}\" for object: \"{originalObjName}\", using default LE_Object class.");

                LE_Object instancedComponent = targetObj.AddComponent<LE_Object>();
                instancedComponent.Init(originalObjName);
                return instancedComponent;
            }
        }

        public virtual void Init()
        {
            // This method is meant to be overrided by classes that inherit from this one.
        }

        void Init(string originalObjName)
        {
            if (EditorController.Instance != null && PlayModeController.Instance == null)
            {
                EditorController.Instance.currentInstantiatedObjects.Add(this);
            }
            else if (EditorController.Instance == null && PlayModeController.Instance != null)
            {
                PlayModeController.Instance.currentInstantiatedObjects.Add(this);
            }

            SetNameAndType(originalObjName);

            // If it's on playmode.
            if (PlayModeController.Instance != null)
            {
                // Destroy the snap triggers of this object.
                Destroy(gameObject.GetChildWithName("SnapTriggers"));
            }
        }

        void SetNameAndType(string originalObjName)
        {
            objectType = ConvertNameToObjectType(originalObjName);
            objectOriginalName = originalObjName;

            int id = 0;
            LE_Object[] objects = null;

            if (EditorController.Instance != null && PlayModeController.Instance == null)
            {
                objects = EditorController.Instance.currentInstantiatedObjects.ToArray();
            }
            else if (EditorController.Instance == null && PlayModeController.Instance != null)
            {
                objects = PlayModeController.Instance.currentInstantiatedObjects.ToArray();
            }

            while (objects.Any(x => x.objectID == id && x.objectOriginalName == objectOriginalName))
            {
                id++;
            }
            objectID = id;

            gameObject.name = objectFullNameWithID;

            // If the objects list has more than 1 object of the same type AND with the same ID, well, that's not allowed, show an error popup.
            if (Utilities.ListHasMultipleObjectsWithSameID(objects.ToList()))
            {
                string title = "LEVEL EDITOR ERROR";
                string description = "This error was generated by the Level Editor mod, DON'T REPORT IT to Haze Games!";
                string errorString = "Two or more objects have the same ID, please report to Javialon_qv in FS Discord server.";
                string stackTrace = "Please attach with your report the Latest.log file in the MelonLoader folder and your level file.";

                MenuController.GetInstance().ShowCodeErrorPopup(title, description, errorString, stackTrace);
            }
        }
        public static ObjectType? ConvertNameToObjectType(string objName)
        {
            try
            {
                string objTypeName = objName.ToUpper().Replace(' ', '_');
                if (objectVariants.ContainsKey(objTypeName))
                {
                    return objectVariants[objTypeName];
                }
                else
                {
                    return (ObjectType)Enum.Parse(typeof(ObjectType), objTypeName);
                }
            }
            catch
            {
                return null;
            }
        }
        static bool HasReachedObjectLimit(Type objectCompType)
        {
            FieldInfo currentInstancesField = objectCompType.GetField("currentInstances", BindingFlags.NonPublic | BindingFlags.Static);
            FieldInfo maxInstancesField = objectCompType.GetField("maxInstances", BindingFlags.NonPublic | BindingFlags.Static);

            int currentInstances = currentInstancesField != null ? (int)currentInstancesField.GetValue(null) : 0;
            int maxInstances = maxInstancesField != null ? (int)maxInstancesField.GetValue(null) : 99999;

            return currentInstances >= maxInstances;
        }

        public virtual void SetProperty(string name, object value)
        {
            
        }

        public virtual object GetProperty(string name)
        {
            return null;
        }
        public virtual T GetProperty<T>(string name)
        {
            return default(T);
        }
    }
}
