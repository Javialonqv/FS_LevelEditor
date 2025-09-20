using FS_LevelEditor.Editor;
using FS_LevelEditor.Misc;
using FS_LevelEditor.Playmode;
using FS_LevelEditor.SaveSystem.Converters;
using FS_LevelEditor.SaveSystem.SerializableTypes;
using Il2Cpp;
using MelonLoader;
using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor
{
    [Serializable]
    public class WaypointData
    {
        public Vector3Serializable position { get; set; }
        public Vector3Serializable rotation { get; set; }
        public Vector3Serializable scale { get; set; } = Vector3.one;

        [JsonConverter(typeof(LEPropertiesConverterNew))]
        public Dictionary<string, object> properties { get; set; } = new Dictionary<string, object>();

        public WaypointData() { }
        public WaypointData(WaypointData original)
        {
            position = original.position;
            rotation = original.rotation;
            scale = original.scale;
            properties = new Dictionary<string, object>(properties);
        }
    }
    public enum WaypointMode
    {
        NONE,
        TRAVEL_BACK,
        LOOP
    }

    [MelonLoader.RegisterTypeInIl2Cpp]
    public class WaypointSupport : MonoBehaviour
    {
        public LE_Object targetObject;

        public Transform waypointsParent;
        public List<LE_Waypoint> spawnedWaypoints = new List<LE_Waypoint>();
        public LE_Waypoint firstWaypoint;
        public LineRenderer editorLine;

        Coroutine moveObjectCoroutine;
        int currentWaypointID;
        LE_Waypoint currentWaypoint;

        public virtual List<WaypointData> targetWaypointsData => targetObject.waypoints;
        public virtual LE_Object.ObjectType waypointTypeToUse => LE_Object.ObjectType.WAYPOINT;
        public virtual bool needsEmptyWaypointAtStart => false;
        public virtual bool usesCustomMoveSystem => false;
        public virtual Color editorLineColor => Color.white;
        public virtual GameObject waypointTemplate => null; // If null (by default), it'll create a copy of the main object.

        void Awake()
        {
            targetObject = GetComponent<LE_Object>();
            CreateWaypointsParent();
            if (EditorController.Instance) CreateEditorLine();
        }
        void CreateWaypointsParent()
        {
            waypointsParent = new GameObject("Waypoints").transform;
            waypointsParent.parent = targetObject.transform;
            waypointsParent.localPosition = Vector3.zero;
            waypointsParent.localEulerAngles = Vector3.zero;
            waypointsParent.localScale = Vector3.one;
            GlobalScaleChanger.AddTo(waypointsParent.gameObject, waypointsParent.parent, Vector3.one, true); // Avoid inheriting scale from parent.
            if (EditorController.Instance)
            {
                waypointsParent.gameObject.SetActive(false); // Disabled by default, until the user selects it.
            }
            else if (PlayModeController.Instance)
            {
                // They're empty in playmode, no problem with that.
                waypointsParent.gameObject.SetActive(true);
            }
        }
        void CreateEditorLine()
        {
            if (!editorLine)
            {
                editorLine = Instantiate(Core.LoadOtherObjectInBundle("EditorLine"), transform).GetComponent<LineRenderer>();
                editorLine.transform.localPosition = Vector3.zero;
                editorLine.transform.localScale = Vector3.one;
                editorLine.startColor = editorLineColor;
                editorLine.endColor = editorLineColor;
                editorLine.gameObject.SetActive(false);
            }
        }

        public void OnInstantiated(LEScene scene)
        {
            if (targetWaypointsData.Count > 0) LoadWaypointsFromSave();
        }
        public void LoadWaypointsFromSave()
        {
            List<WaypointData> waypoints = targetWaypointsData;

            if (PlayModeController.Instance)
            {
                if (needsEmptyWaypointAtStart) CreateFirstWaypointEver(waypoints);

                switch (GetWaypointMode())
                {
                    case WaypointMode.LOOP: CreateLoopWaypoint(waypoints); break;
                    case WaypointMode.TRAVEL_BACK: CreateTravelBackWaypoints(waypoints); break;
                }
            }

            for (int i = 0; i < waypoints.Count; i++)
            {
                var waypointData = waypoints[i];
                LE_Waypoint createdWaypoint = AddWaypoint(true);

                createdWaypoint.transform.localPosition = waypointData.position;
                createdWaypoint.transform.localEulerAngles = waypointData.rotation;
                createdWaypoint.transform.localScale = waypointData.scale;
                foreach (var property in waypointData.properties)
                {
                    createdWaypoint.SetProperty(property.Key, property.Value);
                }
            }
            // Init the components NOW before SetupForCustomSystem() is called.
            if (PlayModeController.Instance) spawnedWaypoints.ForEach(x => x.InitComponent());
        }
        // --------------------------------------------------
        void CreateFirstWaypointEver(List<WaypointData> originalList)
        {
            WaypointData firstWaypoint = new WaypointData();
            // Waypoints positions are relative to the main object position, Vector3.zero means the waypoint will be in the same positions as the main object.
            firstWaypoint.position = Vector3.zero;
            firstWaypoint.rotation = Vector3.zero;
            firstWaypoint.scale = transform.localScale; // Same as the object at start.
            if (targetObject.waypointMode == WaypointMode.TRAVEL_BACK)
            {
                firstWaypoint.properties["WaitTime"] = targetObject.waitTime;
            }
            else
            {
                firstWaypoint.properties["WaitTime"] = 0f;
            }

            originalList.Insert(0, firstWaypoint);
        }
        void CreateLoopWaypoint(List<WaypointData> originalList)
        {
            WaypointData finalWaypoint = new WaypointData();
            // Waypoints positions are relative to the main object position, Vector3.zero means the waypoint will be in the same positions as the main object.
            finalWaypoint.position = Vector3.zero;
            finalWaypoint.rotation = Vector3.zero;
            finalWaypoint.scale = transform.localScale; // Same as the object at start.
            // Use the original object's "Wait Time" attribute since the user won't be able to select/change this final waypoint.
            finalWaypoint.properties["WaitTime"] = targetObject.waitTime;

            originalList.Add(finalWaypoint);
        }
        void CreateTravelBackWaypoints(List<WaypointData> originalList)
        {
            for (int i = originalList.Count - 2; i >= 0; i--)
            {
                WaypointData data = new WaypointData();
                data.position = originalList[i].position;
                data.rotation = originalList[i].rotation;
                data.scale = originalList[i].scale;
                foreach (var property in originalList[i].properties) data.properties[property.Key] = property.Value;

                originalList.Add(data);
            }

            if (!needsEmptyWaypointAtStart)
            {
                // Create the last waypoint so the object goes to its original position.
                WaypointData lastWaypoint = new WaypointData();
                lastWaypoint.position = Vector3.zero;
                lastWaypoint.rotation = Vector3.zero;
                lastWaypoint.scale = transform.localScale; // Same as the object at start.
                // Use the original object's "Wait Time" attribute since the user won't be able to select/change this last waypoint.
                lastWaypoint.properties["WaitTime"] = targetObject.waitTime;
                originalList.Add(lastWaypoint);
            }
        }

        public void ObjectStart(LEScene scene)
        {
            if (scene == LEScene.Playmode && spawnedWaypoints != null && spawnedWaypoints.Count > 0)
            {
                if (usesCustomMoveSystem)
                {
                    SetupForCustomSystem();
                }
                else if (targetObject.startMovingAtStart) // Default system for global waypoints.
                {
                    StartObjectMovement();
                }
            }
        }
        public void StartObjectMovement()
        {
            if (usesCustomMoveSystem) return;
            if (moveObjectCoroutine != null) return; // There's already a coroutine running, don't do shit.

            moveObjectCoroutine = (Coroutine)MelonCoroutines.Start(MoveObject());
            Logger.Log("Started waypoint movement for object object: " + gameObject.name);
        }
        IEnumerator MoveObject()
        {
            Vector3[] cachedWaypointPositions = spawnedWaypoints.Select(x => x.transform.position).ToArray();
            Vector3[] cachedWaypointRotations = spawnedWaypoints.Select(x => x.transform.eulerAngles).ToArray();
            Vector3[] cachedWaypointScales = spawnedWaypoints.Select(x => x.transform.localScale).ToArray();

            yield return new WaitForSeconds(targetObject.startDelay);

            for (int i = 0; i < spawnedWaypoints.Count; i++)
            {
                currentWaypointID = i;
                currentWaypoint = spawnedWaypoints[i];

                Vector3 distance = cachedWaypointPositions[i] - transform.position;
                float duration = distance.magnitude / targetObject.movingSpeed;

                TweenPosition tween = TweenPosition.Begin(gameObject, duration, cachedWaypointPositions[i]);
                tween.ignoreTimeScale = false; // Avoid object moving while the game's paused.

                RotationTweener tweenRotation = RotationTweener.RotateTo(gameObject, cachedWaypointRotations[i], duration, RotationPath.Shortest);

                TweenScale tweenScale = TweenScale.Begin(gameObject, duration, cachedWaypointScales[i]);
                tweenScale.ignoreTimeScale = false; // Avoid object scaling while the game's paused.

                yield return new WaitForSeconds(duration);
                yield return new WaitForSeconds(currentWaypoint.GetProperty<float>("WaitTime"));

                if (i == spawnedWaypoints.Count - 1 && (targetObject.waypointMode == WaypointMode.LOOP || targetObject.waypointMode == WaypointMode.TRAVEL_BACK))
                {
                    i = -1; // the 'for' loop will automatically add 1 in the next iteration, converting 'i' to 0.
                }
            }
        }
        public void StopObjectMovement()
        {
            if (moveObjectCoroutine == null) return; // Just in case trying to stop a null coroutine throws an error.

            MelonCoroutines.Stop(moveObjectCoroutine);
            Logger.Log("Waypoint movement stopped for object: " + gameObject.name);
        }
        // --------------------------------------------------
        public virtual void SetupForCustomSystem()
        {

        }
        public virtual WaypointMode GetWaypointMode()
        {
            return targetObject.waypointMode;
        }

        void Update()
        {
            // Update the editor link every frame while it's active.
            if (firstWaypoint && editorLine && editorLine.gameObject.active)
            {
                if (!editorLine.enabled) editorLine.enabled = true;
                editorLine.SetPosition(0, transform.position);
                editorLine.SetPosition(1, firstWaypoint.transform.position);
            }
            if (!firstWaypoint && editorLine) editorLine.enabled = false;
        }

        public void OnSelect()
        {
            ShowWaypoints(true);
        }
        public void OnDeselect()
        {
            ShowWaypoints(false);
        }
        public void BeforeSave()
        {
            // Since the waypoints aren't saved automatically, call the method manually in them.
            spawnedWaypoints.ForEach(x => x.BeforeSave());
        }

        public void ShowWaypoints(bool show)
        {
            if (show)
            {
                waypointsParent.gameObject.SetActive(true);
                if (editorLine) editorLine.gameObject.SetActive(true); // Technically this can only be called when we're on the editor, but just in case.
            }
            else if (!EditorController.Instance.showAllWaypoints)
            {
                waypointsParent.gameObject.SetActive(false);
                if (editorLine) editorLine.gameObject.SetActive(false); // Technically this can only be called when we're on the editor, but just in case.
            }

            // Set the transparent materials in the waypoints just in case.
            foreach (var waypoint in spawnedWaypoints)
            {
                waypoint.gameObject.SetTransparentMaterials();
            }
        }
        public LE_Waypoint AddWaypoint(bool fromSave = false, bool selectIfNotFromSave = true)
        {
            GameObject waypoint = null;
            if (EditorController.Instance)
            {
                GameObject template = waypointTemplate ? waypointTemplate : EditorController.Instance.allCategoriesObjects[targetObject.objectType.Value];

                waypoint = Instantiate(template, waypointsParent);
                waypoint.SetTransparentMaterials();
                if(targetObject.objectType == LE_Object.ObjectType.CEILING_LIGHT || targetObject.objectType == LE_Object.ObjectType.POINT_LIGHT 
                    || targetObject.objectType == LE_Object.ObjectType.DIRECTIONAL_LIGHT)
                {
                    waypoint.GetComponentInChildren<Light>().range = targetObject.GetProperty<float>("Range");
                    targetObject.TryGetProperty("Intensity", out object intensity);
                    if(intensity != null)
                    {
						waypoint.GetComponentInChildren<Light>().intensity = (float)intensity;
					}
				}
                // DESTROY EVERY FUCKING RIGIDBODY WE FIND.
                foreach (var rigidBody in waypoint.TryGetComponents<Rigidbody>(true))
                {
                    Destroy(rigidBody);
                }
            }
            else // We don't need any meshes or shit in playmode, just create an empty object.
            {
                waypoint = new GameObject("Waypoint"); // AddComponentToObject will overwrite the name, so fuck it.
                waypoint.transform.parent = waypointsParent;
            }

            waypoint.transform.localPosition = Vector3.zero;
            waypoint.transform.localEulerAngles = Vector3.zero;
            waypoint.transform.localScale = Vector3.one;

            LE_Waypoint waypointComp = (LE_Waypoint)LE_Object.AddComponentToObject(waypoint, waypointTypeToUse);
            waypointComp.waypointIndex = spawnedWaypoints.Count;

            if (!firstWaypoint)
            {
                firstWaypoint = waypointComp;
                waypointComp.previousWaypoint = this;
            }
            else
            {
                waypointComp.previousWaypoint = spawnedWaypoints.Last();
                spawnedWaypoints.Last().nextWaypoint = waypointComp;
            }

            spawnedWaypoints.Add(waypointComp);

            if (!fromSave) // Create a new WaypointData, link it and everything.
            {
                WaypointData data = new WaypointData();
                waypointComp.attachedData = data;
                targetWaypointsData.Add(data);

                if (EditorController.Instance && selectIfNotFromSave)
                {
                    EditorController.Instance.SetSelectedObj(waypoint);
                }

                // Force the Awake() call when loading from save since it won't be called until the user selects the main object and the waypoints are enabled for the first time.
                waypointComp.CallMethod("Awake");
            }
            else // Just link the ALREADY EXISTING data to the created waypoint.
            {
                waypointComp.attachedData = targetWaypointsData[spawnedWaypoints.Count - 1];

                // Force the Awake() call when loading from save since it won't be called until the user selects the main object and the waypoints are enabled for the first time.
                waypointComp.CallMethod("Awake");
            }

            if (EditorController.Instance)
            {
                // In the editor, the waypoints have a mesh, disable the colliders on the content object, so only EditorCollider works.
                waypointComp.SetCollidersState(false);
            }

            return waypointComp;
        }

        public void RecalculateWaypoints()
        {
            for (int i = 0; i < targetWaypointsData.Count; i++)
            {
                var waypointData = targetWaypointsData[i];
                var waypoint = spawnedWaypoints[i];

                if (i == 0)
                {
                    firstWaypoint = waypoint;
                    waypoint.previousWaypoint = this;
                }
                else
                {
                    spawnedWaypoints[i - 1].nextWaypoint = waypoint;
                    waypoint.previousWaypoint = spawnedWaypoints[i - 1];
                }
            }
        }

        void OnDestroy()
        {
            if (moveObjectCoroutine != null) StopObjectMovement();
        }
    }
}
