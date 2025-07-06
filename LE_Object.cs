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
using UnityEngine.SceneManagement;
using FS_LevelEditor.Editor;
using System.Text.Json;
using FS_LevelEditor.SaveSystem;
using FS_LevelEditor.SaveSystem.Converters;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace FS_LevelEditor
{
    public enum LEScene
    {
        Editor,
        Playmode
    }

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
            CUBE,
            LASER,
            FLAME_TRAP,
            COLLIDER,
            END_TRIGGER,
            PRESSURE_PLATE,
            SCREEN,
            SMALL_SCREEN,
            BREAKABLE_WINDOW,
            TRIGGER
        }

        public static readonly Dictionary<string, ObjectType> objectVariants = new Dictionary<string, ObjectType>()
        {
            { "CYAN_GROUND", ObjectType.GROUND },
            { "RED_GROUND", ObjectType.GROUND },
            { "ORANGE_GROUND", ObjectType.GROUND },
            { "LARGE_GROUND", ObjectType.GROUND },
            { "GROUND_2", ObjectType.GROUND },

            { "WALL_NO_COLOR", ObjectType.WALL },
            { "X_WALL", ObjectType.WALL },
            { "WINDOW", ObjectType.WALL },

            { "DIRECTIONAL_LIGHT", ObjectType.LIGHT },
            { "POINT_LIGHT", ObjectType.LIGHT },
            { "CEILING_LIGHT", ObjectType.LIGHT },

            { "VENT_WITH_SMOKE_GREEN", ObjectType.VENT_WITH_SMOKE },
            { "VENT_WITH_SMOKE_CYAN", ObjectType.VENT_WITH_SMOKE },

            { "HEALTH_PACK", ObjectType.PACK },
            { "AMMO_PACK", ObjectType.PACK },

            { "SAW_WAYPOINT", ObjectType.SAW }
        };

        public static Dictionary<ObjectType, int> alreadyUsedObjectIDs = new Dictionary<ObjectType, int>();

        public ObjectType? objectType;
        public int objectID;
        public string objectOriginalName;
        public virtual string objectFullNameWithID
        {
            get { return objectOriginalName + " " + objectID; }
        }
        public bool setActiveAtStart = true;
        public bool collision = true;
        public Dictionary<string, object> properties = new Dictionary<string, object>();
        public EventExecuter eventExecuter;

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
        public bool canBeDisabledAtStart { get; protected set; } = true;

        public bool initialized = false;
        bool hasItsOwnClass = false;
        public bool isDeleted = false;

        public LE_Object(IntPtr ptr) : base(ptr) { }
        public LE_Object() { }

        #region Object Templates References
        public static Ammo t_ammoPack;
        public static Health t_healthPack;
        public static ScieScript t_saw;
        public static InterrupteurController t_switch;
        public static BlocScript t_cube;
        public static Laser_H_Controller t_laser;
        public static RealtimeCeilingLight t_ceilingLight;
        public static FlameTrapController t_flameTrap;
        public static BlocSwitchScript t_pressurePlate;
        public static ScreenController t_screen;
        public static BreakableWindowController t_window;

        public static void GetTemplatesReferences()
        {
            t_ammoPack = FindObjectOfType<Ammo>();
            t_healthPack = FindObjectOfType<Health>();
            t_saw = FindObjectOfType<ScieScript>();
            t_switch = FindObjectOfType<InterrupteurController>();
            t_cube = Utilities.FindObjectOfType<BlocScript>(x => x.IsCube());
            t_laser = FindObjectOfType<Laser_H_Controller>();
            t_ceilingLight = FindObjectOfType<RealtimeCeilingLight>();
            t_flameTrap = FindObjectOfType<FlameTrapController>();
            t_pressurePlate = Utilities.FindObjectOfType<BlocSwitchScript>(x => x.m_associatedSequencer == null);
            t_screen = FindObjectOfType<ScreenController>();
            t_window = Utilities.FindObjectOfType<BreakableWindowController>(x => x.name.Contains("BreakableWindow"));
        }
        #endregion

        public virtual void Start()
        {
            if (EditorController.Instance) OnInstantiated(LEScene.Editor);
            else if (PlayModeController.Instance) OnInstantiated(LEScene.Playmode);

            if (hasItsOwnClass)
            {
                if (Utilities.IsOverridingMethod(this.GetType(), "Start"))
                {
                    Logger.Error($"\"{GetType().Name}\" is overriding Start() method, this is not allowed, please use ObjectStart() instead.");
                }

                // ObjectStart is only called when the object is ACTUALLY being spawned, since Start() is also called when loading the
                // level in playmode to init the component.
                if (gameObject.activeSelf || EditorController.Instance)
                {
                    if (EditorController.Instance) ObjectStart(LEScene.Editor);
                    else if (PlayModeController.Instance) ObjectStart(LEScene.Playmode);
                }
            }
        }
        void Init(string originalObjName, bool skipIDInitialization = false)
        {
            if (EditorController.Instance != null && PlayModeController.Instance == null)
            {
                EditorController.Instance.currentInstantiatedObjects.Add(this);
            }
            else if (EditorController.Instance == null && PlayModeController.Instance != null)
            {
                PlayModeController.Instance.currentInstantiatedObjects.Add(this);
            }

            SetNameAndType(originalObjName, skipIDInitialization);

            if (PlayModeController.Instance != null)
            {
                // Destroy the snap triggers of this object.
                Destroy(gameObject.GetChildWithName("SnapTriggers"));
            }

            // If greater than 0 that means this object DOES support events.
            if (GetAvailableEventsIDs().Count > 0)
            {
                eventExecuter = gameObject.AddComponent<EventExecuter>();
            }
        }

        /// <summary>
        /// The correct way to add a LE_Object component to a GameObject.
        /// </summary>
        /// <param name="targetObj">The GameObject ot attach this component to.</param>
        /// <param name="originalObjName">THe "original" name of the desired object.</param>
        /// <returns>An instance of the created LE_Object component class.</returns>
        public static LE_Object AddComponentToObject(GameObject targetObj, string originalObjName, bool skipIDInitialization = false)
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
                instancedComponent.Init(originalObjName, skipIDInitialization);
                instancedComponent.hasItsOwnClass = true;
                return instancedComponent;
            }
            else
            {
                if (LevelData.currentLevelObjsCount <= 100)
                {
                    Logger.DebugWarning($"Can't find class of name \"{className}\" for object: \"{originalObjName}\", using default LE_Object class.");
                }

                LE_Object instancedComponent = targetObj.AddComponent<LE_Object>();
                instancedComponent.Init(originalObjName, skipIDInitialization);
                return instancedComponent;
            }
        }

        void SetNameAndType(string originalObjName, bool skipIDInitialization = false)
        {
            objectType = ConvertNameToObjectType(originalObjName);
            objectOriginalName = originalObjName;
            if (objectType == null)
            {
                Logger.Error($"Couldn't find a proper Object Type for object with name: \"{objectOriginalName}\".");
                LE_CustomErrorPopups.ObjectWithoutObjectType();
            }

            if (!skipIDInitialization)
            {
                int id = 0;
                LE_Object[] objects = GetReferenceObjectsToGetObjID();

                while (objects.Any(x => x.objectID == id && x.objectOriginalName == objectOriginalName))
                {
                    id++;
                }
                objectID = id;

                gameObject.name = objectFullNameWithID;

                // If the objects list has more than 1 object of the same type AND with the same ID, well, that's not allowed, show an error popup.
                if (!Utilities.IsOverridingMethod(this.GetType(), nameof(GetReferenceObjectsToGetObjID)) &&  Utilities.ListHasMultipleObjectsWithSameID(objects.ToList()))
                {
                    LE_CustomErrorPopups.MultipleObjectsWithSameID();
                }
            }
        }
        // For now, this method is only used to setup the ID manually for Saw Waypoints, because the main Saw needs to setup the reference to it in the waypoint.
        public void SetupObjectID()
        {
            int id = 0;
            LE_Object[] objects = GetReferenceObjectsToGetObjID();

            while (objects.Any(x => x.objectID == id && x.objectOriginalName == objectOriginalName))
            {
                id++;
            }
            objectID = id;

            gameObject.name = objectFullNameWithID;

            // If the objects list has more than 1 object of the same type AND with the same ID, well, that's not allowed, show an error popup.
            if (!Utilities.IsOverridingMethod(this.GetType(), nameof(GetReferenceObjectsToGetObjID)) && Utilities.ListHasMultipleObjectsWithSameID(objects.ToList()))
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

        #region Virtual Methods
        public virtual void OnInstantiated(LEScene scene)
        {
            if (scene == LEScene.Editor)
            {
                SetCollidersState(false);
                SetEditorCollider(true);
            }
            else if (scene == LEScene.Playmode)
            {
                // Don't set the colliders to true because they can be objects with enabled AND disabled colliders, we don't want to break
                // that.
                SetEditorCollider(false);

                if (!initialized) InitComponent();
            }

            // Colliders are enabled by default.
            if (!collision && scene == LEScene.Playmode)
            {
                SetCollidersState(false);
            }

            if (eventExecuter) eventExecuter.OnInstantiated(scene);
        }
        public virtual void InitComponent()
        {
            initialized = true;
        }
        public virtual void ObjectStart(LEScene scene)
        {

        }

        /// <summary>
        /// Sets a property inside of the object properties list if it exists.
        /// </summary>
        /// <param name="name">The name of the property to set.</param>
        /// <param name="value">The value of the property, it need to be the same as the expected depending of the property name. It also can manage some conversions.</param>
        /// <returns>True ff the property was setted correctly or false if there's some invalid value.</returns>
        public virtual bool SetProperty(string name, object value)
        {
            if (properties.ContainsKey(name) && value is JsonElement)
            {
                Type toConvert = properties[name].GetType();
                object converted = LEPropertiesConverterNew.NewDeserealize(toConvert, (JsonElement)value);
                if (converted != null)
                {
                    // converted should be an original value OR an object with a custom serialization type (ColorSerializable), convert it back to original.
                    Utilities.CallMethodIfOverrided(typeof(LE_Object), this, nameof(SetProperty), name, SavePatches.ConvertFromSerializableValue(converted));
                }
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
                else
                {
                    Logger.Error($"The property of name \"{name}\" couldn't be casted to \"{typeof(T).Name}\" for object with name: \"{objectFullNameWithID}\".");
                    return default(T);
                }
            }
            else
            {
                Logger.Error($"Couldn't find property of name \"{name}\" OF TYPE \"{typeof(T).Name}\" for object with name: \"{objectFullNameWithID}\".");
                return default(T);
            }
        }
        public bool TryGetProperty(string name, out object value)
        {
            if (properties.ContainsKey(name))
            {
                value = GetProperty(name);
                return true;
            }
            else
            {
                value = null;
                return false;
            }
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

        public virtual void OnSelect()
        {
            if (canBeDisabledAtStart) gameObject.SetOpaqueMaterials();

            if (eventExecuter) eventExecuter.OnSelect();
        }
        public virtual void OnDeselect(GameObject nextSelectedObj)
        {
            if (canBeDisabledAtStart)
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

            if (eventExecuter) eventExecuter.OnDeselect();
        }
        public virtual void OnDelete()
        {
            if (canUndoDeletion)
            {
                isDeleted = true;
            }
            else
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
        }

        public virtual List<string> GetAvailableEventsIDs()
        {
            return new List<string>();
        }
        #endregion

        public virtual LE_Object[] GetReferenceObjectsToGetObjID()
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

        public enum LEObjectContext { PREVIEW, SELECT, NORMAL }
        public static Color GetObjectColor(LEObjectContext context)
        {
            switch (context)
            {
                case LEObjectContext.PREVIEW:
                    return new Color(0f, 0.666f, 0.894f, 1f);

                case LEObjectContext.SELECT:
                    return new Color(0f, 1f, 0f);

                case LEObjectContext.NORMAL:
                    return new Color(1f, 1f, 1f);
            }

            return new Color(1f, 1f, 1f);
        }
        public static Color GetObjectColorForObject(string objName, LEObjectContext context)
        {
            string className = "LE_" + objName.Replace(' ', '_');
            Type classType = Type.GetType("FS_LevelEditor." + className);

            if (classType != null)
            {
                var flags = BindingFlags.Static
                    | BindingFlags.Public
                    | BindingFlags.NonPublic
                    | BindingFlags.DeclaredOnly;

                MethodInfo method = classType.GetMethod(nameof(GetObjectColor), flags);
                if (method != null)
                {
                    return (Color)method.Invoke(null, new object[] { context });
                }
                else // If it's null is prolly 'cause the class doesn't have the method declared, so, just use the default implementation.
                {
                    return GetObjectColor(context);
                }
            }
            else
            {
                return GetObjectColor(context);
            }
        }
        public void SetObjectColor(LEObjectContext context)
        {
            foreach (var renderer in gameObject.TryGetComponents<MeshRenderer>())
            {
                foreach (var material in renderer.materials)
                {
                    if (!material.HasProperty("_Color")) continue;

                    Color toSet = LE_Object.GetObjectColorForObject(objectOriginalName, context);
                    toSet.a = material.color.a;
                    material.color = toSet;
                }
            }
        }
        public static void SetObjectColor(GameObject obj, string objInternalName, LEObjectContext context)
        {
            foreach (var renderer in obj.TryGetComponents<MeshRenderer>())
            {
                foreach (var material in renderer.materials)
                {
                    if (!material.HasProperty("_Color")) continue;

                    Color toSet = LE_Object.GetObjectColorForObject(objInternalName, context);
                    toSet.a = material.color.a;
                    material.color = toSet;
                }
            }
        }

        public void SetCollidersState(bool newEnabledState)
        {
            if (!gameObject.ExistsChildWithName("Content"))
            {
                Logger.Error($"\"{objectOriginalName}\" object doesn't contain a Content object for some reason???");
                return;
            }

            foreach (var collider in gameObject.GetChildWithName("Content").TryGetComponents<Collider>())
            {
                collider.enabled = newEnabledState;
            }
        }
        public void SetEditorCollider(bool newEnabledState)
        {
            if (gameObject.ExistsChildWithName("EditorCollider"))
            {
                gameObject.GetChildWithName("EditorCollider").SetActive(newEnabledState);
            }
            else
            {
                Logger.Error($"\"{objectOriginalName}\" object doesn't contain an EditorCollider.");
            }
        }
    }
}