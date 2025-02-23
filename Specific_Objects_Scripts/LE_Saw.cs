using Il2Cpp;
using Il2CppSimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Saw : LE_Object
    {
        public LE_SawWaypoint nextWaypoint;
        LineRenderer editorWaypointLine;
        public GameObject waypointsParent;

        void Awake()
        {
            properties = new Dictionary<string, object>()
            {
                { "ActivateOnStart", true },
                { "waypoints", new List<LE_SawWaypointSerializable>() }
            };

            waypointsParent = gameObject.GetChildWithName("Waypoints");
        }

        void Start()
        {
            if (PlayModeController.Instance != null)
            {
                InitComponent();
            }
            else // If it's not in playmode, just create a collider so the user can click the object in LE.
            {
                GameObject collider = new GameObject("Collider");
                collider.transform.parent = transform;
                collider.transform.localScale = Vector3.one;
                collider.transform.localPosition = Vector3.zero;
                collider.AddComponent<BoxCollider>().size = new Vector3(0.1f, 1.3f, 1.3f);

                // Also set the saw on or off.
                SetMeshOnEditor((bool)GetProperty("ActivateOnStart"));

                CreateWaypointEditorLine();
            }
        }

        void Update()
        {
            // Update the link with the position of this saw and the next waypoint.
            if (editorWaypointLine && nextWaypoint && EditorController.Instance != null)
            {
                editorWaypointLine.SetPosition(0, transform.position);
                editorWaypointLine.SetPosition(1, nextWaypoint.transform.position);
            }
        }

        void InitComponent()
        {
            GameObject content = gameObject.GetChildWithName("SawContent");

            content.SetActive(false);
            content.tag = "Scie";

            content.GetComponent<AudioSource>().outputAudioMixerGroup = FindObjectOfType<ScieScript>().GetComponent<AudioSource>().outputAudioMixerGroup;

            RotationScie rotationScie = content.GetChildWithName("Scie_OFF").AddComponent<RotationScie>();
            rotationScie.vitesseRotation = 500;

            ScieScript script = content.AddComponent<ScieScript>();
            script.doesDamage = true;
            script.rotationScript = rotationScie;
            script.m_damageCollider = GetComponent<BoxCollider>();
            script.m_audioSource = GetComponent<AudioSource>();
            script.movingSaw = false;
            script.movingSpeed = 10;
            script.scieSound = FindObjectOfType<ScieScript>().scieSound;
            script.offMesh = content.GetChildWithName("Scie_OFF").GetComponent<MeshRenderer>();
            script.onMesh = content.GetChildAt("Scie_OFF/Scie_ON").GetComponent<MeshRenderer>();
            script.m_collision = content.GetChildWithName("Collision").GetComponent<BoxCollider>();
            script.physicsCollider = content.GetChildWithName("Saw_PhysicsCollider").GetComponent<MeshCollider>();


            // There's a good reasong for this, I swear, the Activate and Deactivate functions are just inverting the enabled bool in the saw LOL, the both functions
            // do the same thing, so first enable it, and then disable if needed, cause if we don't do anything, there's a bug with the saw animation.
            script.Activate();
            bool activateOnStart = (bool)GetProperty("ActivateOnStart");
            if (!activateOnStart)
            {
                script.Deactivate();
            }

            content.SetActive(true);
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

            editorWaypointLine.gameObject.SetActive(false);
        }

        public override bool SetProperty(string name, object value)
        {
            if (name == "ActivateOnStart")
            {
                if (value is bool)
                {
                    if (EditorController.Instance != null) SetMeshOnEditor((bool)value);
                    properties["ActivateOnStart"] = (bool)value;
                    return true;
                }
                else
                {
                    Logger.Error($"Tried to set \"ActivateOnStart\" property with value of type \"{value.GetType().Name}\".");
                    return false;
                }
            }
            else if (name == "waypoints") // For now, this is only called when loading the level...
            {
                if (value is List<LE_SawWaypointSerializable>)
                {
                    foreach (var waypoint in (List<LE_SawWaypointSerializable>)value)
                    {
                        LE_SawWaypoint instance = AddWaypoint(false);
                        instance.objectID = waypoint.objectID;
                        instance.transform.localPosition = waypoint.waypointPosition;
                        instance.transform.localEulerAngles = waypoint.waypointRotation;
                    }

                    HideOrShowAllWaypointsInEditor(false);
                }
            }

            return false;
        }

        public override bool TriggerAction(string actionName)
        {
            if (actionName == "AddWaypoint")
            {
                AddWaypoint();
                return true;
            }

            return false;
        }

        public override void OnSelect()
        {
            HideOrShowAllWaypointsInEditor(true);
        }

        public override void OnDeselect(GameObject nextSelectedObj)
        {
            #region Disable Waypoints when selecting another object
            // And also if it's not a waypoint from this saw.
            if (nextSelectedObj != null)
            {
                if (nextSelectedObj.TryGetComponent<LE_SawWaypoint>(out var component))
                {
                    if (component.mainSaw == this)
                    {
                        return;
                    }
                }
            }

            HideOrShowAllWaypointsInEditor(false);
            #endregion
        }

        void SetMeshOnEditor(bool isSawOn)
        {
            gameObject.GetChildAt("SawContent/Scie_OFF").GetComponent<MeshRenderer>().enabled = !isSawOn;
            gameObject.GetChildAt("SawContent/Scie_OFF/Scie_ON").GetComponent<MeshRenderer>().enabled = isSawOn;
        }

        LE_SawWaypoint AddWaypoint(bool selectWhenInstantiated = true)
        {
            GameObject waypoint = Instantiate(EditorController.Instance.LoadOtherObjectInBundle("TransparentSaw"), gameObject.GetChildWithName("Waypoints").transform);

            waypoint.transform.localPosition = Vector3.zero;
            waypoint.transform.localEulerAngles = Vector3.zero;
            waypoint.GetChildWithName("Mesh").transform.localEulerAngles = new Vector3(90f, 90f, 0f);
            waypoint.transform.localScale = Vector3.one;

            LE_Object objComponent = LE_Object.AddComponentToObject(waypoint, "SawWaypoint");

            if (nextWaypoint == null)
            {
                ((LE_SawWaypoint)objComponent).lastWaypoint = this;
                nextWaypoint = (LE_SawWaypoint)objComponent;
                ((LE_SawWaypoint)objComponent).mainSaw = this;
            }
            else
            {
                ((LE_SawWaypoint)objComponent).lastWaypoint = GetLastWaypoint();
                GetLastWaypoint().nextWaypoint = (LE_SawWaypoint)objComponent;
                ((LE_SawWaypoint)objComponent).mainSaw = this;
            }

            ((List<LE_SawWaypointSerializable>)properties["waypoints"]).Add(new LE_SawWaypointSerializable((LE_SawWaypoint)objComponent));

            if (selectWhenInstantiated)
            {
                EditorController.Instance.SetSelectedObj(waypoint);
            }

            return (LE_SawWaypoint)objComponent;
        }

        LE_SawWaypoint GetLastWaypoint()
        {
            if (nextWaypoint != null)
            {
                return nextWaypoint.GetLastWaypoint();
            }
            else
            {
                return null;
            }
        }

        public void HideOrShowAllWaypointsInEditor(bool show)
        {
            foreach (var waypoint in waypointsParent.GetChilds())
            {
                if (waypoint.GetComponent<LE_SawWaypoint>().wasDeleted) continue;

                waypoint.SetActive(show);
                waypoint.GetComponent<LE_SawWaypoint>().editorWaypointLine.gameObject.SetActive(show);
                // Override the green color with the transparent white color for the waypoints.
                waypoint.GetChildWithName("Mesh").GetComponent<MeshRenderer>().material.color = new Color(1f, 1f, 1f, 0.3921f);
            }

            if (editorWaypointLine)
            {
                editorWaypointLine.gameObject.SetActive(show);
            }
        }
    }
}
