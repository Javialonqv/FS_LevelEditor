using FS_LevelEditor;
using Il2Cpp;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Saw_Waypoint : LE_Object
    {
        public bool isTheFirstWaypoint = false;

        public LE_Object previousWaypoint;
        public LE_Saw_Waypoint nextWaypoint;
        public LE_Saw mainSaw;
        public LineRenderer editorWaypointLine;

        public bool wasDeleted = false;

        void Awake()
        {
            properties = new Dictionary<string, object>()
            {
                { "WaitTime", 0.3f }
            };

            canUndoDeletion = false;

            if (PlayModeController.Instance == null)
            {
                CreateWaypointEditorLine();
            }
        }

        void Start()
        {
            if (PlayModeController.Instance != null)
            {
                InitComponent();

                // Disable the transparent saw mesh ingame.
                gameObject.GetChildWithName("Mesh").SetActive(false);
            }
            else if (!isTheFirstWaypoint) // If it's not in playmode, just create a collider so the user can click the object in LE.
            {
                GameObject collider = new GameObject("Collider");
                collider.transform.parent = transform;
                collider.transform.localScale = Vector3.one;
                collider.transform.localPosition = Vector3.zero;
                collider.AddComponent<BoxCollider>().size = new Vector3(0.1f, 1.3f, 1.3f);
            }

            if (isTheFirstWaypoint)
            {
                gameObject.GetChildWithName("Mesh").SetActive(false);
            }
        }

        void Update()
        {
            // Update the waypoint link every frame.
            if (editorWaypointLine && nextWaypoint && EditorController.Instance != null)
            {
                editorWaypointLine.SetPosition(0, transform.position);
                editorWaypointLine.SetPosition(1, nextWaypoint.transform.position);
            }

            // Update the position and rotation every frame :)
            if (EditorController.Instance != null)
            {
                LE_SawWaypointSerializable toModify = ((List<LE_SawWaypointSerializable>)mainSaw.properties["waypoints"]).Find(x => x.objectID == objectID);
                int index = ((List<LE_SawWaypointSerializable>)mainSaw.properties["waypoints"]).IndexOf(toModify);
                toModify.waitTime = (float)GetProperty("WaitTime");
                toModify.waypointPosition = transform.localPosition;
                toModify.waypointRotation = transform.localEulerAngles;
                ((List<LE_SawWaypointSerializable>)mainSaw.properties["waypoints"])[index] = toModify;
            }
        }

        void InitComponent()
        {
            Waypoint waypoint = gameObject.AddComponent<Waypoint>();

            waypoint.speedMultiplier = 1f;
            waypoint.waitHere = (float)GetProperty("WaitTime");
            if (nextWaypoint)
            {
                waypoint.nextWaypoint = nextWaypoint.gameObject;
            }
            else
            {
                // If this waypoint is the last one, then set the next waypoint as the first waypoint that is right after the saw itself (the invisible one).
                if (mainSaw.waypointsGOs.Last() == gameObject && nextWaypoint == null)
                {
                    waypoint.nextWaypoint = mainSaw.nextWaypoint.gameObject;
                }
            }
            waypoint.checkpoints = mainSaw.waypointsGOs.ToArray();
        }

        void CreateWaypointEditorLine()
        {
            if (editorWaypointLine != null)
            {
                Destroy(editorWaypointLine.gameObject);
            }

            editorWaypointLine = new GameObject("EditorWaypointLine").AddComponent<LineRenderer>();
            editorWaypointLine.transform.parent = transform;
            editorWaypointLine.transform.localPosition = Vector3.zero;

            editorWaypointLine.startWidth = 0.1f;
            editorWaypointLine.endWidth = 0.1f;
            editorWaypointLine.positionCount = 2;

            editorWaypointLine.material = new Material(Shader.Find("Sprites/Default"));
            editorWaypointLine.startColor = Color.white;
            editorWaypointLine.endColor = Color.white;
        }

        public override void OnSelect()
        {
            mainSaw.HideOrShowAllWaypointsInEditor(true);
        }

        public override void OnDeselect(GameObject nextSelectedObj)
        {
            // Make sure the parent is the main saw, just in case the user tried moving multiple objects at once.
            transform.parent = mainSaw.waypointsParent.transform;

            mainSaw.HideOrShowAllWaypointsInEditor(false);
        }

        public override void OnDelete()
        {
            wasDeleted = true;

            mainSaw.waypointsGOs.Remove(gameObject);

            mainSaw.RecalculateWaypoints();
        }

        public override bool SetProperty(string name, object value)
        {
            if (name == "WaitTime")
            {
                if (value is string)
                {
                    if (float.TryParse((string)value, out float result))
                    {
                        properties["WaitTime"] = result;
                        return true;
                    }
                }
                else if (value is float)
                {
                    properties["WaitTime"] = (float)value;
                    return true;
                }
            }

            return false;
        }

        public override bool TriggerAction(string actionName)
        {
            if (actionName == "AddWaypoint")
            {
                mainSaw.AddWaypoint();
                return true;
            }

            return false;
        }

        public LE_Saw_Waypoint GetLastWaypoint()
        {
            if (nextWaypoint != null)
            {
                return nextWaypoint.GetLastWaypoint();
            }
            else
            {
                return this;
            }
        }

        public void HideOrShowSawInEditor(bool show)
        {
            // This method only works if the object ISN'T deleted, otherwise, the deleted object can be enabled, and we don't want that..
            if (!wasDeleted)
            {
                gameObject.SetActive(show);
                
                if (nextWaypoint != null)
                {
                    editorWaypointLine.gameObject.SetActive(show);
                }
            }

            // Override the green color with the transparent white color for the waypoints.
            if (show)
            {
                gameObject.GetChildWithName("Mesh").GetComponent<MeshRenderer>().material.color = new Color(1f, 1f, 1f, 0.3921f);
            }

            // Follow the loop.
            if (nextWaypoint != null)
            {
                nextWaypoint.HideOrShowSawInEditor(show);
            }
        }
    }
}

[Serializable]
public class LE_SawWaypointSerializable
{
    public float waitTime { get; set; }

    public int objectID { get; set; }
    public Vector3Serializable waypointPosition { get; set; }
    public Vector3Serializable waypointRotation { get; set; }

    public LE_SawWaypointSerializable() { }
    public LE_SawWaypointSerializable(LE_Saw_Waypoint waypoint)
    {
        waitTime = (float)waypoint.GetProperty("WaitTime");

        objectID = waypoint.objectID;
        waypointPosition = waypoint.transform.localPosition;
        waypointRotation = waypoint.transform.localEulerAngles;
    }
}
