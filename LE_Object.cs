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
            CYAN_GROUND,
            RED_GROUND,
            ORANGE_GROUND,
            LARGE_GROUND,
            WALL,
            WINDOW
        }

        public ObjectType objectType;
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
#if DEBUG
                Melon<Core>.Logger.Warning($"Can't find class of name \"{className}\" for object: \"{originalObjName}\", using default LE_Object class.");
#endif
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
        }

        void SetNameAndType(string originalObjName)
        {
            objectType = (ObjectType)Enum.Parse(typeof(ObjectType), originalObjName.ToUpper().Replace(' ', '_'));
            objectOriginalName = originalObjName;

            int id = 0;
            LE_Object[] objects = EditorController.Instance.levelObjectsParent.GetComponentsInChildren<LE_Object>();
            while (objects.Any(x => x.objectID == id && x.objectOriginalName == objectOriginalName))
            {
                id++;
            }
            objectID = id;

            gameObject.name = objectFullNameWithID;
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
