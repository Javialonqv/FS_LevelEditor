using FS_LevelEditor.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor
{
    public class WaypointData
    {

    }

    [MelonLoader.RegisterTypeInIl2Cpp]
    public class WaypointSupport : MonoBehaviour
    {
        LE_Object targetObject;

        Transform waypointsParent;
        public List<LE_Waypoint> spawnedWaypoints = new List<LE_Waypoint>();
        public LE_Waypoint firstWaypoint;
        public LineRenderer editorLine;

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

        public void LoadWaypointsFromSave()
        {
            List<WaypointData> waypoints = targetObject.waypoints;

            for (int i = 0; i < waypoints.Count; i++)
            {
                var waypoint = waypoints[i];
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

        public void ShowWaypoints(bool show)
        {
            waypointsParent.gameObject.SetActive(show);
            editorLine.gameObject.SetActive(show);

            if (show)
            {
                foreach (var waypoint in spawnedWaypoints)
                {
                    waypoint.gameObject.SetTransparentMaterials();
                }
            }
        }
        public void AddWaypoint()
        {
            GameObject waypoint = Instantiate(EditorController.Instance.allCategoriesObjects[targetObject.objectType.Value], waypointsParent);
            waypoint.transform.localPosition = Vector3.zero;
            waypoint.transform.localEulerAngles = Vector3.zero;
            waypoint.transform.localScale = Vector3.one;
            waypoint.SetTransparentMaterials();

            LE_Waypoint waypointComp = (LE_Waypoint)LE_Object.AddComponentToObject(waypoint, LE_Object.ObjectType.WAYPOINT);

            if (!firstWaypoint)
            {
                firstWaypoint = waypointComp;
                waypointComp.previousWaypoint = this;
            }
            else
            {
                waypointComp.previousWaypoint = spawnedWaypoints.Last();
                spawnedWaypoints.Last().previousWaypoint = waypointComp;
            }

            spawnedWaypoints.Add(waypointComp);

            if (EditorController.Instance)
            {
                EditorController.Instance.SetSelectedObj(waypoint);
            }

            Logger.DebugLog($"Created waypoint! ID: {waypointComp.objectID}.");
        }
    }
}
