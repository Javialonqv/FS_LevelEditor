using FS_LevelEditor.UI_Related;
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
            VENT_WITH_SMOKE,
            PACK,
            SAW,
            SWITCH,
            PLAYER_SPAWN,
            CUBE
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

            { "HEALTH_PACK", ObjectType.PACK },
            { "AMMO_PACK", ObjectType.PACK },

            { "SAW_WAYPOINT", ObjectType.SAW }
        };

        public ObjectType? objectType;
        public int objectID;
        public string objectOriginalName;
        public virtual string objectFullNameWithID
        {
            get { return objectOriginalName + " " + objectID; }
        }
        public bool setActiveAtStart = true;
        public Dictionary<string, object> properties = new Dictionary<string, object>();

        public virtual Transform objectParent
        {
            get
            {
                if (EditorController.Instance != null) return EditorController.Instance.levelObjectsParent.transform;
                else if (PlayModeController.Instance != null) return PlayModeController.Instance.levelObjectsParent.transform;

                return null;
            }
        }
        public bool canUndoDeletion { get; protected set; }  = true;
        public bool canBeUsedInEventsTab { get; protected set; } = true;

        public LE_Object(IntPtr ptr) : base(ptr) { }
        public LE_Object() { }

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
                if (LevelData.currentLevelObjsCount <= 100)
                {
                    Logger.DebugWarning($"Can't find class of name \"{className}\" for object: \"{originalObjName}\", using default LE_Object class.");
                }

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
            if (objectType == null)
            {
                Logger.Error($"Couldn't find a proper Object Type for object with name: \"{objectOriginalName}\".");
                LE_CustomErrorPopups.ObjectWithoutObjectType();
            }

            int id = 0;
            LE_Object[] objects = GetReferenceObjectsToGetObjID();

            while (objects.Any(x => x.objectID == id && x.objectOriginalName == objectOriginalName))
            {
                id++;
            }
            objectID = id;

            gameObject.name = objectFullNameWithID;

            // If the objects list has more than 1 object of the same type AND with the same ID, well, that's not allowed, show an error popup.
            if (Utilities.ListHasMultipleObjectsWithSameID(objects.ToList()))
            {
                LE_CustomErrorPopups.MultipleObjectsWithSameID();
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

        /// <summary>
        /// Sets a property inside of the object properties list if it exists.
        /// </summary>
        /// <param name="name">The name of the property to set.</param>
        /// <param name="value">The value of the property, it need to be the same as the expected depending of the property name. It also can manage some conversions.</param>
        /// <returns>True ff the property was setted correctly or false if there's some invalid value.</returns>
        public virtual bool SetProperty(string name, object value)
        {
            return false;
        }

        public virtual bool TriggerAction(string actionName)
        {
            if (actionName == "SetActive_True")
            {
                gameObject.SetActive(true);
            }
            else if (actionName == "SetActive_False")
            {
                gameObject.SetActive(false);
            }

            return false;
        }

        /// <summary>
        /// Gets a property from the object properties list.
        /// </summary>
        /// <param name="name">The name of property to get if it exists.</param>
        /// <returns>The value of the property in the list, without any conversions.</returns>
        public virtual object GetProperty(string name)
        {
            if (properties.ContainsKey(name))
            {
                return properties[name];
            }
            else
            {
                Logger.Error($"Couldn't find property of name \"{name}\" for object with name: \"{objectFullNameWithID}\"");
                return null;
            }
        }
        public virtual T GetProperty<T>(string name)
        {
            if (properties.ContainsKey(name))
            {
                if (properties[name] is T)
                {
                    return (T)properties[name];
                }
            }

            Logger.Error($"Couldn't find property of name \"{name}\" OF TYPE \"{typeof(T).Name}\" for object with name: \"{objectFullNameWithID}\"");
            return default(T);
        }

        public virtual void OnSelect()
        {
            gameObject.SetOpaqueMaterials();
        }
        public virtual void OnDeselect(GameObject nextSelectedObj)
        {
            if (!setActiveAtStart)
            {
                gameObject.SetTransparentMaterials();
            }
            else
            {
                gameObject.SetOpaqueMaterials();
            }
        }
        public virtual void OnDelete()
        {
            if (EditorController.Instance != null && PlayModeController.Instance == null)
            {
                EditorController.Instance.currentInstantiatedObjects.Remove(this);
            }
            else if (EditorController.Instance == null && PlayModeController.Instance != null)
            {
                PlayModeController.Instance.currentInstantiatedObjects.Remove(this);
            }
        }

        virtual protected LE_Object[] GetReferenceObjectsToGetObjID()
        {
            if (EditorController.Instance != null && PlayModeController.Instance == null)
            {
                return EditorController.Instance.currentInstantiatedObjects.ToArray();
            }
            else if (EditorController.Instance == null && PlayModeController.Instance != null)
            {
                return PlayModeController.Instance.currentInstantiatedObjects.ToArray();
            }

            return null;
        }
    }
}
