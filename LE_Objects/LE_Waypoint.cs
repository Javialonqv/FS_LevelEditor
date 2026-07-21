using FS_LevelEditor.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Waypoint : LE_Object
    {
        public WaypointSupport mainSupport;
        public MonoBehaviour previousWaypoint;
        public LE_Waypoint nextWaypoint;
        public WaypointData attachedData;
        public LineRenderer editorLine;
        public ObjectType? mainObjectType => mainSupport.targetObject.objectType;

        public override Transform objectParent => mainSupport.waypointsParent;

        public int waypointIndex;
        public bool isFirstWaypoint => waypointIndex == 0;
        public bool isLastWaypoint => waypointIndex == mainSupport.targetWaypointsData.Count - 1;

        public override string[] EventsIDs =>
        [
            "WhenReached"
        ];

        public LE_Waypoint()
        {
            canBeUsedInEventsTab = false;
            canBeDisabledAtStart = false;
            canUndoDeletion = false;
            canHaveWaypoints = false;
        }

        bool alreadyCalledAwake = false;
        internal virtual void Awake()
        {
            if (alreadyCalledAwake) return;

            mainSupport = GetMainSupport();

            if (EditorController.Instance) CreateEditorLine();

            alreadyCalledAwake = true;
        }

        public static Dictionary<string, object> GetDefaultProperties()
        {
            return new Dictionary<string, object>()
            {
                { "WaitTime", 0f },
                { "MoveSpeed", 5f },
                { "StopHere", false },
                { "WhenReached", new List<LE_Event>() }
            };
        }

        void CreateEditorLine()
        {
            if (!editorLine)
            {
                editorLine = Instantiate(Core.LoadOtherObjectInBundle("EditorLine"), transform).GetComponent<LineRenderer>();
                editorLine.transform.localPosition = Vector3.zero;
                editorLine.transform.localScale = Vector3.one;
                editorLine.startColor = mainSupport.editorLineColor;
                editorLine.endColor = mainSupport.editorLineColor;
                editorLine.gameObject.SetActive(false);
            }
        }

        void Update()
        {
            if (editorLine)
            {
                if (nextWaypoint)
                {
                    editorLine.gameObject.SetActive(true);
                    editorLine.SetPosition(0, transform.position);
                    editorLine.SetPosition(1, nextWaypoint.transform.position);
                }
                else
                {
                    editorLine.gameObject.SetActive(false);
                }
            }
        }

        public override void OnSelect()
        {
            mainSupport.ShowWaypoints(true);
        }
        public override void OnDeselect(GameObject nextSelectedObj)
        {
            mainSupport.ShowWaypoints(false);
        }
        public override void OnDelete()
        {
            base.OnDelete();
            mainSupport.targetWaypointsData.Remove(attachedData);
            mainSupport.spawnedWaypoints.Remove(this);
            mainSupport.RecalculateWaypoints();
        }
        public override void BeforeSave()
        {
            // Refresh the WaypointData... data...

            attachedData.position = transform.localPosition;
            attachedData.rotation = transform.localEulerAngles;
            attachedData.scale = transform.localScale;
            attachedData.properties = properties;
        }

        public override bool SetProperty(string name, object value)
        {
            if (name == "WaitTime")
            {
                if (value is string)
                {
                    if (Utils.TryParseFloat((string)value, out float result))
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
            else if (name == "MoveSpeed")
            {
                if (value is string)
                {
                    if (Utils.TryParseFloat((string)value, out float result))
                    {
                        properties["MoveSpeed"] = result;
                        return true;
                    }
                }
                else if (value is float)
                {
                    properties["MoveSpeed"] = (float)value;
                    return true;
                }
            }
            else if (name == "StopHere")
            {
                if (value is bool)
                {
                    properties["StopHere"] = (bool)value;
                    return true;
                }
            }
            else if (GetAvailableEventsIDs().Contains(name))
            {
                if (value is List<LE_Event>)
                {
                    properties[name] = (List<LE_Event>)value;
                    return true;
                }
            }

            return base.SetProperty(name, value);
        }
        public override bool TriggerAction(string actionName)
        {
            if (actionName == "AddWaypoint")
            {
                AddWaypoint(true);
                return true;
            }

            return base.TriggerAction(actionName);
        }

        // I'll be honest with you, I just added this in a separate method so I could have a reference to the created waypoint. - Jav.
        public LE_Waypoint AddWaypoint(bool setAsSelected)
        {
            LE_Waypoint spawnedWaypoint = mainSupport.AddWaypoint(false, setAsSelected);
            spawnedWaypoint.transform.localPosition = transform.localPosition;
            spawnedWaypoint.transform.localRotation = transform.localRotation;

            return spawnedWaypoint;
        }

        public virtual WaypointSupport GetMainSupport()
        {
            return transform.parent.parent.GetComponent<WaypointSupport>();
        }

        public void ExecuteWhenReachedEvents()
        {
            eventExecuter.ExecuteEventsWithAndLogic((List<LE_Event>)properties["WhenReached"], "WhenReached", true);
        }
    }
}
