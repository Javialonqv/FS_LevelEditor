using FS_LevelEditor;
using Il2Cpp;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using FS_LevelEditor.Editor;
using FS_LevelEditor.SaveSystem.SerializableTypes;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Saw_Waypoint : LE_Object
    {
        public bool isTheFirstWaypoint = false;

        public LE_Object previousWaypoint;
        public LE_Saw_Waypoint nextWaypoint;
        public int waypointID;
        public LE_Saw mainSaw;
        public LineRenderer editorWaypointLine;

        public bool wasDeleted = false;

        // Since the mainSaw can be null at the start just for a frame or smth, use parents as a reference.
        public override Transform objectParent => transform.parent.parent;

        void Awake()
        {
            properties = new Dictionary<string, object>()
            {
                { "WaitTime", 0.3f }
            };

            canUndoDeletion = false;
            canBeUsedInEventsTab = false;
            canBeDisabledAtStart = false;

            editorWaypointLine = gameObject.GetChildWithName("EditorWaypointLine").GetComponent<LineRenderer>();
            if (EditorController.Instance)
            {
                // For some reason the shader is broken when the game's running, assign it manually.
                editorWaypointLine.material.shader = Shader.Find("Sprites/Default");
                editorWaypointLine.gameObject.SetActive(true);
            }
        }

        public override void OnInstantiated(LEScene scene)
        {
            // Don't call the base function since saw waypoints don't have an EditorCollider and such.
        }
        public override void ObjectStart(LEScene scene)
        {
            if (scene == LEScene.Playmode)
            {
                if (!initialized) InitComponent();

                // Disable the transparent saw mesh ingame.
                gameObject.GetChildWithName("Mesh").SetActive(false);

                // Disable the collider in playmode.
                gameObject.GetChildWithName("Collider").SetActive(false);
            }
            // The "first waypoint" is just supposed to be internal, the user can't interact with it and it's in the same pos as the original saw.
            else if (isTheFirstWaypoint)
            {
                gameObject.GetChildWithName("Collider").SetActive(false);
            }

            if (isTheFirstWaypoint)
            {
                gameObject.GetChildWithName("Mesh").SetActive(false);
            }
        }

        void Update()
        {
            // Update the position and rotation every frame :)
            if (EditorController.Instance != null)
            {
                LE_SawWaypointSerializable toModify = ((List<LE_SawWaypointSerializable>)mainSaw.properties["waypoints"])[waypointID];
                toModify.waitTime = GetProperty<float>("WaitTime");
                toModify.waypointPosition = transform.localPosition;
                toModify.waypointRotation = transform.localEulerAngles;
                ((List<LE_SawWaypointSerializable>)mainSaw.properties["waypoints"])[waypointID] = toModify;
            }
        }
        void LateUpdate()
        {
            // Update the waypoint link every frame.
            // In LateUpdate() to avoid THAT ONE frame where the link is bugged, before it's set to the new position, dunno how to explain, good luck future me LOL.
            if (editorWaypointLine && nextWaypoint && EditorController.Instance != null)
            {
                editorWaypointLine.SetPosition(0, transform.position);
                editorWaypointLine.SetPosition(1, nextWaypoint.transform.position);
            }
        }

        public override void InitComponent()
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

            initialized = true;
        }

        public override void OnSelect()
        {
            base.OnSelect();
            mainSaw.HideOrShowAllWaypointsInEditor(true);
        }
        public override void OnDeselect(GameObject nextSelectedObj)
        {
            base.OnDeselect(nextSelectedObj);

            // Make sure the parent is the main saw, just in case the user tried moving multiple objects at once.
            transform.parent = mainSaw.waypointsParent.transform;

            mainSaw.HideOrShowAllWaypointsInEditor(false);
        }
        public override void OnDelete()
        {
            // Base method removes the object from the instantiated objects list.
            base.OnDelete();

            wasDeleted = true;

            mainSaw.waypointsGOs.Remove(gameObject);
            mainSaw.waypointsComps.Remove(this);

            mainSaw.RecalculateWaypoints();
        }

        public override bool SetProperty(string name, object value)
        {
            if (name == "WaitTime")
            {
                if (value is string)
                {
                    if (Utilities.TryParseFloat((string)value, out float result))
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

            return base.SetProperty(name, value);
        }
        public override bool TriggerAction(string actionName)
        {
            if (actionName == "AddWaypoint")
            {
                mainSaw.AddWaypoint();
                return true;
            }

            return base.TriggerAction(actionName);
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

            // Force the damn waypoint to be transparent, for the love of god...
            if (show)
            {
                gameObject.GetChildWithName("Mesh").SetTransparentMaterials();
                gameObject.GetChildWithName("Mesh").GetComponent<MeshRenderer>().material.color = new Color(1f, 1f, 1f, 0.3921f);
            }

            // Follow the loop.
            if (nextWaypoint != null)
            {
                nextWaypoint.HideOrShowSawInEditor(show);
            }
        }
        public override LE_Object[] GetReferenceObjectsToGetObjID()
        {
            return mainSaw.waypointsComps.ToArray();
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
    public LE_SawWaypointSerializable(LE_SawWaypointSerializable toCopy)
    {
        objectID = toCopy.objectID;
        waypointPosition = toCopy.waypointPosition;
        waypointRotation = toCopy.waypointRotation;
    }
    public LE_SawWaypointSerializable(LE_Saw_Waypoint waypoint)
    {
        waitTime = (float)waypoint.GetProperty("WaitTime");

        objectID = waypoint.objectID;
        waypointPosition = waypoint.transform.localPosition;
        waypointRotation = waypoint.transform.localEulerAngles;
    }
}
