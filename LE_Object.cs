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
            WALL
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
        };

        public ObjectType? objectType;
        public int objectID;
        public string objectOriginalName;
        public string objectFullNameWithID
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
            Il2CppSystem.Type classType = Il2CppSystem.Type.GetType(className);

            if (classType != null)
            {
                LE_Object instancedComponent = (LE_Object)targetObj.AddComponent(classType);
                instancedComponent.Init();
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
            SetNameAndType(originalObjName);

            // If it's on playmode.
            if (PlayModeController.Instance != null)
            {
                // Destroy all of the triggers, since they are only needed when in LE.
                foreach (var child in gameObject.GetChilds())
                {
                    // If the child's name contains a "|" or it's "Global" it's a trigger parent for sure.
                    if (child.name.Contains("|") || child.name == "Global")
                    {
                        Destroy(child);
                    }
                    else
                    {
                        List<LE_Object.ObjectType?> availableObjectsForTrigger = child.name.Split('|').Select(x => LE_Object.ConvertNameToObjectType(x.Trim())).ToList();

                        foreach (var availableObject in availableObjectsForTrigger)
                        {
                            // If one of the extracted object types from the child's name isn't null, also it's a parent, destroy it.
                            if (availableObject != null) Destroy(child);
                        }
                    }
                }
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
                objects = EditorController.Instance.levelObjectsParent.GetComponentsInChildren<LE_Object>();
            }
            else if (EditorController.Instance == null && PlayModeController.Instance != null)
            {
                objects = PlayModeController.Instance.levelObjectsParent.GetComponentsInChildren<LE_Object>();
            }

            while (objects.Any(x => x.objectID == id && x.objectOriginalName == objectOriginalName))
            {
                id++;
            }
            objectID = id;

            gameObject.name = objectFullNameWithID;
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
