using FS_LevelEditor.Editor;
using FS_LevelEditor.Editor.UI;
using FS_LevelEditor.Playmode;
using FS_LevelEditor.SaveSystem;
using FS_LevelEditor.SaveSystem.Converters;
using FS_LevelEditor.UI_Related;
using FS_LevelEditor.WaypointSupports;
using Il2Cpp;
using Il2CppAmazingAssets.TerrainToMesh;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        public enum ObjectType // NEVER MODIFY THE ORDER OF ANY OF THE ELEMENTS HERE.
        {
            #region GROUNDS
            GROUND,
            CYAN_GROUND,
            RED_GROUND,
            ORANGE_GROUND,
            LARGE_GROUND,
            GROUND_2,
            #endregion

            #region WALLS
            WALL,
            WALL_NO_COLOR,
            X_WALL,
            WINDOW, // Yeah, windows are just a structure, so, I'll mark them as a wall.
            #endregion

            #region LIGHTS
            DIRECTIONAL_LIGHT,
            POINT_LIGHT,
            CEILING_LIGHT,
            #endregion

            VENT_WITH_SMOKE_GREEN,
            VENT_WITH_SMOKE_CYAN,
            HEALTH_PACK,
            AMMO_PACK,
            SAW,
            SAW_WAYPOINT,
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
            TRIGGER,
            DOOR,
            LASER_FIELD,
            DOOR_V2,
            DEATH_TRIGGER,
            WAYPOINT
        }

        public static Dictionary<string, List<ObjectType>> classifiedObjectTypes = new Dictionary<string, List<ObjectType>>()
        {
            { "GROUND", new List<ObjectType>(){
                ObjectType.GROUND,
                ObjectType.CYAN_GROUND,
                ObjectType.RED_GROUND,
                ObjectType.ORANGE_GROUND,
                ObjectType.LARGE_GROUND,
                ObjectType.GROUND_2
                } },
            { "WALL", new List<ObjectType>(){
                ObjectType.WALL,
                ObjectType.WALL_NO_COLOR,
                ObjectType.X_WALL,
                ObjectType.WINDOW,
                ObjectType.BREAKABLE_WINDOW
                } },
            { "LIGHT", new List<ObjectType>(){
                ObjectType.DIRECTIONAL_LIGHT,
                ObjectType.POINT_LIGHT,
                ObjectType.CEILING_LIGHT
                } },
            { "VENT_WITH_SMOKE", new List<ObjectType>(){
                ObjectType.VENT_WITH_SMOKE_GREEN,
                ObjectType.VENT_WITH_SMOKE_CYAN
                } },
            { "PACK", new List<ObjectType>(){
                ObjectType.HEALTH_PACK,
                ObjectType.AMMO_PACK
                } }
        };

        public readonly static Dictionary<ObjectType, Type> customWaypointSupports = new Dictionary<ObjectType, Type>()
        {
            { ObjectType.SAW, typeof(SawWaypointSupport) }
        };
        public readonly static Dictionary<ObjectType?, Vector3> defaultScalesForObjects = new Dictionary<ObjectType?, Vector3>()
        {
            { ObjectType.TRIGGER, new Vector3(3.8f, 3.8f, 0.01f) },
            { ObjectType.DOOR, new Vector3(1f, 1.05f, 1f) }
        };

        public static Dictionary<ObjectType, int> alreadyUsedObjectIDs = new Dictionary<ObjectType, int>();

        public ObjectType? objectType;
        public int objectID;
        public string objectLocalizatedName
        {
            get
            {
                return Loc.Get("object." + objectType.ToString());
            }
        }
        public virtual string objectFullNameWithID
        {
            get
            {
                if (GetMaxInstances(GetType()) == 1)
                {
                    // Since there can only be 1 instance of this object, we don't need to add the ID to the name.
                    return objectLocalizatedName;
                }
                else
                {
                    return objectLocalizatedName + " " + objectID;
                }
            }
        }

        public bool setActiveAtStart = true;
        public bool collision = true;
        public bool startMovingAtStart = true;
        public float movingSpeed = 5f;
        public float startDelay = 0f;
        public WaypointMode waypointMode;

        public Dictionary<string, object> properties = new Dictionary<string, object>();
        public List<WaypointData> waypoints = new List<WaypointData>();
        
        public EventExecuter eventExecuter;
        public WaypointSupport waypointSupport;
        public WaypointSupport customWaypointSupport;
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
        public bool canHaveWaypoints { get; protected set; } = true;

        public bool initialized = false;
        bool hasItsOwnClass = false;
        bool onInstantiatedCalled = false;
        public bool isDeleted = false;

        public bool currentCollisionState = true;

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
        public static PorteScript t_door;
        public static PorteScript t_doorV2;

        public static void GetTemplatesReferences()
        {
            t_ammoPack = FindObjectOfType<Ammo>();
            t_healthPack = FindObjectOfType<Health>();
            t_saw = FindObjectOfType<ScieScript>();
            t_switch = FindObjectOfType<InterrupteurController>();
            t_cube = Utils.FindObjectOfType<BlocScript>(x => x.IsCube());
            t_laser = FindObjectOfType<Laser_H_Controller>();
            t_ceilingLight = FindObjectOfType<RealtimeCeilingLight>();
            t_flameTrap = FindObjectOfType<FlameTrapController>();
            t_pressurePlate = Utils.FindObjectOfType<BlocSwitchScript>(x => x.m_associatedSequencer == null);
            t_screen = FindObjectOfType<ScreenController>();
            t_window = Utils.FindObjectOfType<BreakableWindowController>(x => x.name.Contains("BreakableWindow"));
            t_door = Utils.FindObjectOfType<PorteScript>(x => !x.isSkinV2);
            t_doorV2 = Utils.FindObjectOfType<PorteScript>(x => x.isSkinV2);
        }
        #endregion

        public virtual void Start()
        {
            if (EditorController.Instance && !onInstantiatedCalled) OnInstantiated(LEScene.Editor);
            else if (PlayModeController.Instance && !onInstantiatedCalled) OnInstantiated(LEScene.Playmode);

            if (hasItsOwnClass)
            {
                if (Utils.IsOverridingMethod(this.GetType(), "Start"))
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
            else
            {
                // ObjectStart is only called when the object is ACTUALLY being spawned, since Start() is also called when loading the
                // level in playmode to init the component.
                if (gameObject.activeSelf || EditorController.Instance)
                {
                    if (EditorController.Instance) ObjectStart(LEScene.Editor);
                    else if (PlayModeController.Instance) ObjectStart(LEScene.Playmode);
                }
            }
        }
        void Init(ObjectType objectType, bool skipIDInitialization = false)
        {
            if (EditorController.Instance != null && PlayModeController.Instance == null)
            {
                EditorController.Instance.currentInstantiatedObjects.Add(this);
            }
            else if (EditorController.Instance == null && PlayModeController.Instance != null)
            {
                PlayModeController.Instance.currentInstantiatedObjects.Add(this);
            }

            SetNameAndType(objectType, skipIDInitialization);

            if (PlayModeController.Instance != null)
            {
                // Destroy the snap triggers of this object.
                Destroy(gameObject.GetChild("SnapTriggers"));
            }

            // If greater than 0 that means this object DOES support events.
            if (GetAvailableEventsIDs().Count > 0)
            {
                eventExecuter = gameObject.AddComponent<EventExecuter>();
            }

            if (canHaveWaypoints)
            {
                waypointSupport = gameObject.AddComponent<WaypointSupport>();
                if (customWaypointSupports.ContainsKey(objectType))
                {
                    customWaypointSupport = (WaypointSupport)gameObject.AddComponent(Il2CppType.From(customWaypointSupports[objectType]));
                }
            }
        }

        /// <summary>
        /// The correct way to add a LE_Object component to a GameObject.
        /// </summary>
        /// <param name="targetObj">The GameObject ot attach this component to.</param>
        /// <param name="originalObjName">THe "original" name of the desired object.</param>
        /// <returns>An instance of the created LE_Object component class.</returns>
        public static LE_Object AddComponentToObject(GameObject targetObj, ObjectType objectType, bool skipIDInitialization = false)
        {
            string className = "LE_" + Utils.ObjectTypeToFormatedName(objectType).Replace(' ', '_');
            Type classType = Type.GetType("FS_LevelEditor." + className);

            if (classType != null)
            {
                if (HasReachedObjectLimit(classType))
                {
                    Utils.ShowCustomNotificationRed("Object limit reached for this object.", 2f);
                    return null;
                }
                LE_Object instancedComponent = (LE_Object)targetObj.AddComponent(Il2CppType.From(classType));
                instancedComponent.Init(objectType, skipIDInitialization);
                instancedComponent.hasItsOwnClass = true;
                return instancedComponent;
            }
            else
            {
                if (LevelData.currentLevelObjsCount <= 100)
                {
                    Logger.DebugWarning($"Can't find class of name \"{className}\" for object: \"{objectType}\", using default LE_Object class.");
                }

                LE_Object instancedComponent = targetObj.AddComponent<LE_Object>();
                instancedComponent.Init(objectType, skipIDInitialization);
                return instancedComponent;
            }
        }

        void SetNameAndType(ObjectType objectTypeToSet, bool skipIDInitialization = false)
        {
            objectType = objectTypeToSet;

            if (!skipIDInitialization)
            {
                int id = 0;
                LE_Object[] objects = GetReferenceObjectsToGetObjID();

                while (objects.Any(x => x.objectID == id && x.objectType == objectType))
                {
                    id++;
                }
                objectID = id;

                gameObject.name = objectFullNameWithID;

                // If the objects list has more than 1 object of the same type AND with the same ID, well, that's not allowed, show an error popup.
                if (!Utils.IsOverridingMethod(this.GetType(), nameof(GetReferenceObjectsToGetObjID)) &&  Utils.ListHasMultipleObjectsWithSameID(objects.ToList()))
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

            while (objects.Any(x => x.objectID == id && x.objectType == objectType))
            {
                id++;
            }
            objectID = id;

            gameObject.name = objectFullNameWithID;

            // If the objects list has more than 1 object of the same type AND with the same ID, well, that's not allowed, show an error popup.
            if (!Utils.IsOverridingMethod(this.GetType(), nameof(GetReferenceObjectsToGetObjID)) && Utils.ListHasMultipleObjectsWithSameID(objects.ToList()))
            {
                LE_CustomErrorPopups.MultipleObjectsWithSameID();
            }
        }
        public static ObjectType? ConvertNameToObjectType(string objName)
        {
            string objTypeName = objName.ToUpper().Replace(' ', '_');
            if (Enum.TryParse<ObjectType>(objTypeName, true, out ObjectType result))
            {
                return result;
            }
            else
            {
                Logger.Error($"Couldn't convert object name \"{objName}\" to a valid ObjectType, returning null.");
                return null;
            }
        }
        public static List<ObjectType?> GetObjectTypesForSnapToGrid(string targetObjType)
        {
            if (classifiedObjectTypes.ContainsKey(targetObjType))
            {
                return classifiedObjectTypes[targetObjType].Cast<ObjectType?>().ToList();
            }

            if (Enum.TryParse<ObjectType>(targetObjType, true, out ObjectType result))
            {
                return new List<ObjectType?>() { result };
            }

            return new List<ObjectType?>();
        }
        static bool HasReachedObjectLimit(Type objectCompType)
        {
            FieldInfo currentInstancesField = objectCompType.GetField("currentInstances", BindingFlags.NonPublic | BindingFlags.Static);
            FieldInfo maxInstancesField = objectCompType.GetField("maxInstances", BindingFlags.NonPublic | BindingFlags.Static);

            int currentInstances = currentInstancesField != null ? (int)currentInstancesField.GetValue(null) : 0;
            int maxInstances = maxInstancesField != null ? (int)maxInstancesField.GetValue(null) : 99999;

            return currentInstances >= maxInstances;
        }
        static int GetMaxInstances(Type objectCompType)
        {
            FieldInfo maxInstancesField = objectCompType.GetField("maxInstances", BindingFlags.NonPublic | BindingFlags.Static);
            int maxInstances = maxInstancesField != null ? (int)maxInstancesField.GetValue(null) : 99999;

            return maxInstances;
        }

        #region Virtual Methods
        /// <summary>
        /// Called at the start of the level, even if the object is disabled. Properties are already loaded when called. DON'T USE AS THE Awake() METHOD.
        /// </summary>
        /// <param name="scene">The scene type is being loaded.</param>
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
            if (waypointSupport) waypointSupport.OnInstantiated(scene);
            if (customWaypointSupport) customWaypointSupport.OnInstantiated(scene);

            onInstantiatedCalled = true;
        }
        /// <summary>
        /// Use this to initialize the components/data of the object.
        /// </summary>
        public virtual void InitComponent()
        {
            initialized = true;
        }
        /// <summary>
        /// Called at the start of the level if the level is enabled at start, if disabled, called until the object is enabled for the first time. USE THIS AS THE Start() METHOD.
        /// </summary>
        /// <param name="scene">The scene type is being loaded.</param>
        public virtual void ObjectStart(LEScene scene)
        {
            if (waypointSupport) waypointSupport.ObjectStart(scene);
            if (customWaypointSupport) customWaypointSupport.ObjectStart(scene);
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
                    Utils.CallMethodIfOverrided(typeof(LE_Object), this, nameof(SetProperty), name, SavePatches.ConvertFromSerializableValue(converted));
                }
            }

            if (name == "StartMovingAtStart")
            {
                startMovingAtStart = (bool)value;
                return true;
            }
            else if (name == "MovingSpeed")
            {
                movingSpeed = Utils.ParseFloat((string)value);
                return true;
            }
            else if (name == "StartDelay")
            {
                startDelay = Utils.ParseFloat((string)value);
                return true;
            }
            else if (name == "WaypointMode")
            {
                waypointMode = (WaypointMode)value;
                return true;
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
            if (actionName == "SetColliderState_True")
            {
                SetCollidersState(true);
            }
            else if (actionName == "SetColliderState_False")
            {
                SetCollidersState(false);
            }
            else if (actionName == "ManageEvents")
            {
                EventsUIPageManager.Instance.ShowEventsPage(this);
                return true;
            }
            else if (actionName == "OnEventsTabClose" || actionName == "OnSelectTargetObjWithClickBtnClick")
            {
                eventExecuter.CreateInEditorLinksToTargetObjects();
                // Since we're on SELECTING_TARGET_OBJ state, editor links positions won't be updated automatically, updaate it ONE TIME ONLY, since you can't move objects in this state.
                if (actionName == "OnSelectTargetObjWithClickBtnClick")
                {
                    eventExecuter.UpdateEditorLinksPositions();
                }
                return true;
            }

            return false;
        }

        public virtual void OnSelect()
        {
            if (canBeDisabledAtStart) gameObject.SetOpaqueMaterials();

            if (eventExecuter) eventExecuter.OnSelect();
            if (waypointSupport) waypointSupport.OnSelect();
            if (customWaypointSupport) customWaypointSupport.OnSelect();
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
            if (waypointSupport) waypointSupport.OnDeselect();
            if (customWaypointSupport) customWaypointSupport.OnDeselect();
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
        public virtual void BeforeSave()
        {
            if (waypointSupport) waypointSupport.BeforeSave();
            if (customWaypointSupport) customWaypointSupport.BeforeSave();
        }

        public virtual List<string> GetAvailableEventsIDs()
        {
            return new List<string>();
        }

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
        #endregion

        public enum LEObjectContext { PREVIEW, SELECT, NORMAL }
        public static Color GetDefaultObjectColor(LEObjectContext context)
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
        public static Color GetObjectColorForObject(ObjectType objectType, LEObjectContext context)
        {
            string className = "LE_" + Utils.ObjectTypeToFormatedName(objectType).Replace(' ', '_');
            Type classType = Type.GetType("FS_LevelEditor." + className);

            if (classType != null)
            {
                var flags = BindingFlags.Static
                    | BindingFlags.Public
                    | BindingFlags.NonPublic
                    | BindingFlags.DeclaredOnly;

                MethodInfo method = classType.GetMethod(nameof(GetDefaultObjectColor), flags);
                if (method != null)
                {
                    return (Color)method.Invoke(null, new object[] { context });
                }
                else // If it's null is prolly 'cause the class doesn't have the method declared, so, just use the default implementation.
                {
                    return GetDefaultObjectColor(context);
                }
            }
            else
            {
                return GetDefaultObjectColor(context);
            }
        }
        public virtual void SetObjectColor(LEObjectContext context)
        {
            foreach (var renderer in gameObject.TryGetComponents<MeshRenderer>())
            {
                foreach (var material in renderer.materials)
                {
                    if (!material.HasProperty("_Color")) continue;

                    Color toSet = LE_Object.GetObjectColorForObject(objectType.Value, context);
                    toSet.a = material.color.a;
                    material.color = toSet;
                }
            }
        }
        public static void SetObjectColor(GameObject obj, ObjectType objectType, LEObjectContext context)
        {
            foreach (var renderer in obj.TryGetComponents<MeshRenderer>())
            {
                foreach (var material in renderer.materials)
                {
                    if (!material.HasProperty("_Color")) continue;

                    Color toSet = LE_Object.GetObjectColorForObject(objectType, context);
                    toSet.a = material.color.a;
                    material.color = toSet;
                }
            }
        }

        public void SetCollidersState(bool newEnabledState)
        {
            if (!gameObject.ExistsChild("Content"))
            {
                Logger.Error($"\"{objectType}\" object doesn't contain a Content object for some reason???");
                return;
            }

            foreach (var collider in gameObject.GetChild("Content").TryGetComponents<Collider>(true))
            {
                collider.enabled = newEnabledState;
            }
            currentCollisionState = newEnabledState;
        }
        public void SetEditorCollider(bool newEnabledState)
        {
            if (gameObject.ExistsChild("EditorCollider"))
            {
                gameObject.GetChild("EditorCollider").SetActive(newEnabledState);
            }
            else
            {
                Logger.Error($"\"{objectType}\" object doesn't contain an EditorCollider.");
            }
        }

        public static void ResetStaticVariablesInObjects()
        {
            LE_Breakable_Window.staticVariablesInitialized = false;
        }
    }
}