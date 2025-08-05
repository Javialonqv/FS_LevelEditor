using FS_LevelEditor.Editor;
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

        [JsonConverter(typeof(LEPropertiesConverterNew))]
        public Dictionary<string, object> properties { get; set; } = new Dictionary<string, object>();
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

            originalList.Insert(0, firstWaypoint);
        }
        void CreateLoopWaypoint(List<WaypointData> originalList)
        {
            WaypointData finalWaypoint = new WaypointData();
            // Waypoints positions are relative to the main object position, Vector3.zero means the waypoint will be in the same positions as the main object.
            finalWaypoint.position = Vector3.zero;
            finalWaypoint.rotation = Vector3.zero;
            finalWaypoint.properties["WaitTime"] = targetObject.startDelay;

            originalList.Add(finalWaypoint);
        }
        void CreateTravelBackWaypoints(List<WaypointData> originalList)
        {
            for (int i = originalList.Count - 1; i >= 0; i--)
            {
                WaypointData data = new WaypointData();
                data.position = originalList[i].position;
                data.rotation = originalList[i].rotation;
                foreach (var property in originalList[i].properties) data.properties[property.Key] = property.Value;

                originalList.Add(data);
            }

            if (!needsEmptyWaypointAtStart)
            {
                // Create the last waypoint so the object goes to its original position.
                WaypointData lastWaypoint = new WaypointData();
                lastWaypoint.position = Vector3.zero;
                lastWaypoint.rotation = Vector3.zero;
                lastWaypoint.properties["WaitTime"] = targetObject.startDelay;
                originalList.Add(lastWaypoint);
            }
        }

        public void ObjectStart(LEScene scene)
        {
            if (targetObject.startMovingAtStart && scene == LEScene.Playmode && spawnedWaypoints != null && spawnedWaypoints.Count > 0)
            {
                if (usesCustomMoveSystem)
                {
                    SetupForCustomSystem();
                }
                else // Default system for global waypoints.
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

            yield return new WaitForSeconds(targetObject.startDelay);

            for (int i = 0; i < spawnedWaypoints.Count; i++)
            {
                currentWaypointID = i;
                currentWaypoint = spawnedWaypoints[i];

                Vector3 distance = cachedWaypointPositions[i] - transform.position;
                float duration = distance.magnitude / targetObject.movingSpeed;

                TweenPosition.Begin(gameObject, duration, cachedWaypointPositions[i]);
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
                editorLine.SetPosition(0, transform.position);
                editorLine.SetPosition(1, firstWaypoint.transform.position);
            }
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
            waypointsParent.gameObject.SetActive(show);
            if (editorLine) editorLine.gameObject.SetActive(show); // Technically this can only be called when we're on the editor, but just in case.

            if (show)
            {
                foreach (var waypoint in spawnedWaypoints)
                {
                    waypoint.gameObject.SetTransparentMaterials();
                }
            }
        }
        public LE_Waypoint AddWaypoint(bool fromSave = false)
        {
            GameObject waypoint = null;
            if (EditorController.Instance)
            {
                GameObject template = waypointTemplate ? waypointTemplate : EditorController.Instance.allCategoriesObjects[targetObject.objectType.Value];

                waypoint = Instantiate(template, waypointsParent);
                waypoint.SetTransparentMaterials();
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

                if (EditorController.Instance)
                {
                    EditorController.Instance.SetSelectedObj(waypoint);
                }

                // Force the Awake() call when loading from save since it won't be called until the user selects the main object and the waypoints are enabled for the first time.
                waypointComp.CallMethod("Awake");
            }
            else // Just link the ALREADY EXISTING data to the created waypoint.
            {
                if (EditorController.Instance)
                {
                    // Only in editor, in playmode it may be using travel back or loop modes which can break this, attachedData is for editor only.
                    
                }
                waypointComp.attachedData = targetWaypointsData[spawnedWaypoints.Count - 1];

                // Force the Awake() call when loading from save since it won't be called until the user selects the main object and the waypoints are enabled for the first time.
                waypointComp.CallMethod("Awake");
            }

            Logger.DebugLog($"Created waypoint! ID: {waypointComp.objectID}.");

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
