using FS_LevelEditor.Editor;
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
        [JsonConverter(typeof(LEPropertiesConverterNew))]
        public Dictionary<string, object> properties { get; set; } = new Dictionary<string, object>();
    }

    [MelonLoader.RegisterTypeInIl2Cpp]
    public class WaypointSupport : MonoBehaviour
    {
        public LE_Object targetObject;

        public Transform waypointsParent;
        public List<LE_Waypoint> spawnedWaypoints = new List<LE_Waypoint>();
        public LE_Waypoint firstWaypoint;
        public LineRenderer editorLine;

        int currentWaypointID;
        LE_Waypoint currentWaypoint;

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
            waypointsParent.localScale = Vector3.one;
            waypointsParent.gameObject.SetActive(false); // Disabled by default, until the user selects it.
        }
        void CreateEditorLine()
        {
            if (!editorLine)
            {
                editorLine = Instantiate(Core.LoadOtherObjectInBundle("EditorLine"), transform).GetComponent<LineRenderer>();
                editorLine.transform.localPosition = Vector3.zero;
                editorLine.transform.localScale = Vector3.one;
                editorLine.gameObject.SetActive(false);
            }
        }

        public void OnInstantiated(LEScene scene)
        {
            if (targetObject.waypoints.Count > 0) LoadWaypointsFromSave();
        }
        public void LoadWaypointsFromSave()
        {
            List<WaypointData> waypoints = targetObject.waypoints;

            for (int i = 0; i < waypoints.Count; i++)
            {
                var waypointData = waypoints[i];
                LE_Waypoint createdWaypoint = AddWaypoint(true);

                createdWaypoint.transform.localPosition = waypointData.position;
                foreach (var property in waypointData.properties)
                {
                    createdWaypoint.SetProperty(property.Key, property.Value);
                }
            }
        }

        public void ObjectStart(LEScene scene)
        {
            if (scene == LEScene.Playmode && spawnedWaypoints != null && spawnedWaypoints.Count > 0)
            {
                MelonCoroutines.Start(MoveObject());
            }
        }
        IEnumerator MoveObject()
        {
            Vector3[] cachedWaypointPositions = spawnedWaypoints.Select(x => x.transform.position).ToArray();

            yield return new WaitForSeconds(2f);

            for (int i = 0; i < spawnedWaypoints.Count; i++)
            {
                currentWaypointID = i;
                currentWaypoint = spawnedWaypoints[i];

                Vector3 distance = cachedWaypointPositions[i] - transform.position;
                float duration = distance.magnitude / targetObject.movingSpeed;

                TweenPosition.Begin(gameObject, duration, cachedWaypointPositions[i]);
                yield return new WaitForSeconds(duration);

                yield return new WaitForSeconds(currentWaypoint.GetProperty<float>("WaitTime"));
            }
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
            GameObject waypoint = Instantiate(EditorController.Instance.allCategoriesObjects[targetObject.objectType.Value], waypointsParent);
            waypoint.transform.localPosition = Vector3.zero;
            waypoint.transform.localEulerAngles = Vector3.zero;
            waypoint.transform.localScale = Vector3.one;
            if (EditorController.Instance) waypoint.SetTransparentMaterials();

            LE_Waypoint waypointComp = (LE_Waypoint)LE_Object.AddComponentToObject(waypoint, LE_Object.ObjectType.WAYPOINT);

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
                targetObject.waypoints.Add(data);

                if (EditorController.Instance)
                {
                    EditorController.Instance.SetSelectedObj(waypoint);
                }
            }
            else // Just link the ALREADY EXISTING data to the created waypoint.
            {
                waypointComp.attachedData = targetObject.waypoints[spawnedWaypoints.Count - 1];

                // Force the Awake() call when loading from save since it won't be called until the user selects the main object and the waypoints are enabled for the first time.
                waypointComp.CallMethod("Awake");
            }

            Logger.DebugLog($"Created waypoint! ID: {waypointComp.objectID}.");

            return waypointComp;
        }

        public void RecalculateWaypoints()
        {
            for (int i = 0; i < targetObject.waypoints.Count; i++)
            {
                var waypointData = targetObject.waypoints[i];
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
    }
}
