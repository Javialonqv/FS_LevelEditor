using FS_LevelEditor;
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
    public class LE_SawWaypoint : LE_Object
    {
        public LE_Object lastWaypoint;
        public LE_SawWaypoint nextWaypoint;
        public LE_Saw mainSaw;
        public LineRenderer editorWaypointLine;

        public bool wasDeleted = false;

        void Awake()
        {
            if (PlayModeController.Instance == null)
            {
                CreateWaypointEditorLine();
            }
        }

        void Start()
        {
            if (PlayModeController.Instance != null)
            {

            }
            else // If it's not in playmode, just create a collider so the user can click the object in LE.
            {
                GameObject collider = new GameObject("Collider");
                collider.transform.parent = transform;
                collider.transform.localScale = Vector3.one;
                collider.transform.localPosition = Vector3.zero;
                collider.AddComponent<BoxCollider>().size = new Vector3(0.1f, 1.3f, 1.3f);
            }
        }

        void Update()
        {
            if (editorWaypointLine && nextWaypoint && EditorController.Instance != null)
            {
                editorWaypointLine.SetPosition(0, transform.position);
                editorWaypointLine.SetPosition(1, nextWaypoint.transform.position);
            }

            if (EditorController.Instance != null)
            {
                LE_SawWaypointSerializable toModify = ((List<LE_SawWaypointSerializable>)mainSaw.properties["waypoints"]).Find(x => x.objectID == objectID);
                int index = ((List<LE_SawWaypointSerializable>)mainSaw.properties["waypoints"]).IndexOf(toModify);
                toModify.waypointPosition = transform.localPosition;
                toModify.waypointRotation = transform.localEulerAngles;
                ((List<LE_SawWaypointSerializable>)mainSaw.properties["waypoints"])[index] = toModify;
            }
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

        public override void OnDeselect(GameObject nextSelectedObj)
        {
            // Make sure the parent is the main saw, just in case the user tried moving multiple objects at once.
            transform.parent = mainSaw.waypointsParent.transform;

            #region Disable Waypoints when selecting another object
            // And also if it's not a waypoint from this saw.
            if (nextSelectedObj != null)
            {
                if (nextSelectedObj.TryGetComponent<LE_SawWaypoint>(out var component))
                {
                    if (component.mainSaw == mainSaw)
                    {
                        return;
                    }
                }
                else if (nextSelectedObj.TryGetComponent<LE_Saw>(out var component2))
                {
                    if (component2 == mainSaw)
                    {
                        return;
                    }
                }
            }

            mainSaw.HideOrShowAllWaypointsInEditor(false);
            #endregion
        }

        public override void OnDelete()
        {
            wasDeleted = true;

            if (lastWaypoint is LE_Saw)
            {
                ((LE_Saw)lastWaypoint).nextWaypoint = nextWaypoint;
            }
            else if (lastWaypoint is LE_SawWaypoint)
            {
                ((LE_SawWaypoint)lastWaypoint).nextWaypoint = nextWaypoint;
            }

            LE_SawWaypointSerializable toRemove = ((List<LE_SawWaypointSerializable>)mainSaw.properties["waypoints"]).Find(x => x.objectID == objectID);
            ((List<LE_SawWaypointSerializable>)mainSaw.properties["waypoints"]).Remove(toRemove);
        }

        public LE_SawWaypoint GetLastWaypoint()
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
    }
}

[Serializable]
public class LE_SawWaypointSerializable
{
    public int objectID { get; set; }
    public Vector3Serializable waypointPosition { get; set; }
    public Vector3Serializable waypointRotation { get; set; }

    public LE_SawWaypointSerializable() { }
    public LE_SawWaypointSerializable(LE_SawWaypoint waypoint)
    {
        objectID = waypoint.objectID;
        waypointPosition = waypoint.transform.localPosition;
        waypointRotation = waypoint.transform.localEulerAngles;
    }
}
