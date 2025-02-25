using Il2Cpp;
using Il2CppDiscord;
using Il2CppSimpleJSON;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
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
        public List<GameObject> waypointsGOs = new List<GameObject>();

        ScieScript sawScript;

        void Awake()
        {
            properties = new Dictionary<string, object>()
            {
                { "ActivateOnStart", true },
                { "waypoints", new List<LE_SawWaypointSerializable>() },
                { "Damage", 50 }
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
            script.damage = (int)GetProperty("Damage");
            script.rotationScript = rotationScie;
            script.m_damageCollider = content.GetComponent<BoxCollider>();
            script.m_audioSource = content.GetComponent<AudioSource>();
            script.movingSaw = false;
            script.movingSpeed = 10;
            if (waypointsGOs.Count > 0)
            {
                script.currentWaypoint = waypointsGOs[0];
                script.movingSaw = true;
                script.forcedHeading = true;
                script.allowSideRotation = true;
            }
            script.scieSound = FindObjectOfType<ScieScript>().scieSound;
            script.offMesh = content.GetChildWithName("Scie_OFF").GetComponent<MeshRenderer>();
            script.onMesh = content.GetChildAt("Scie_OFF/Scie_ON").GetComponent<MeshRenderer>();
            script.m_collision = content.GetChildWithName("Collision").GetComponent<BoxCollider>();
            script.physicsCollider = content.GetChildWithName("Saw_PhysicsCollider").GetComponent<MeshCollider>();


            // There's a good reason for this, I swear, the Activate and Deactivate functions are just inverting the enabled bool in the saw LOL, the both functions
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
                    // Foreach value in the saved list, create a new waypoint in the editor.
                    for (int i = 0; i < ((List<LE_SawWaypointSerializable>)value).Count; i++)
                    {
                        var waypoint = ((List<LE_SawWaypointSerializable>)value)[i];

                        // Set the waypoint values, we don't need to worry about the waypoints list since when loading data, the data is not altered.
                        LE_SawWaypoint instance = AddWaypoint(true);
                        instance.objectID = waypoint.objectID;
                        instance.transform.localPosition = waypoint.waypointPosition;
                        instance.transform.localEulerAngles = waypoint.waypointRotation;

                        // Set the waypoint properties:
                        instance.SetProperty("WaitTime", waypoint.waitTime);

                        // Set the previous and next waypoints.
                        if (i >= 1)
                        {
                            waypointsGOs[i - 1].GetComponent<LE_SawWaypoint>().nextWaypoint = instance;
                            instance.previousWaypoint = waypointsGOs[i - 1].GetComponent<LE_SawWaypoint>();
                        }
                        else // If the previous condition is false, that means we're in the FIRST waypoint.
                        {
                            instance.isTheFirstWaypoint = true;
                        }
                    }

                    // If it's in the editor, hide all, the links and the waypoints.
                    if (EditorController.Instance)
                    {
                        HideOrShowAllWaypointsInEditor(false);
                    }

                    // Since the data in the list is not altered when adding new waypoints while loading data, set the list manually rn.
                    properties["waypoints"] = value;
                }
            }
            else if (name == "Damage")
            {
                if (value is string)
                {
                    if (int.TryParse((string)value, out int result))
                    {
                        properties["Damage"] = result;
                        return true;
                    }
                }
                else if (value is int)
                {
                    properties["Damage"] = (int)value;
                    return true;
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
            HideOrShowAllWaypointsInEditor(false);
        }

        void SetMeshOnEditor(bool isSawOn)
        {
            gameObject.GetChildAt("SawContent/Scie_OFF").GetComponent<MeshRenderer>().enabled = !isSawOn;
            gameObject.GetChildAt("SawContent/Scie_OFF/Scie_ON").GetComponent<MeshRenderer>().enabled = isSawOn;
        }

        public LE_SawWaypoint AddWaypoint(bool isFromSavedData = false, bool ignoreIfCurrentNextWaypointIsNull = false)
        {
            // This is for creating an extra waypoint in the same position as the saw, but it'll NOT visible in the LE.
            // Also, this will not be executed when the waypoint is created from save since the save file already includes that extra waypoint, let it be LOL.
            if (nextWaypoint == null && !ignoreIfCurrentNextWaypointIsNull && !isFromSavedData)
            {
                LE_SawWaypoint firstWaypoint = AddWaypoint(isFromSavedData, true);
                firstWaypoint.isTheFirstWaypoint = true;

                nextWaypoint = firstWaypoint;
            }

            GameObject waypoint = Instantiate(Core.LoadOtherObjectInBundle("TransparentSaw"), waypointsParent.transform);

            waypoint.transform.localPosition = Vector3.zero;
            waypoint.transform.localEulerAngles = Vector3.zero;
            waypoint.GetChildWithName("Mesh").transform.localEulerAngles = new Vector3(90f, 90f, 0f);
            waypoint.transform.localScale = Vector3.one;

            LE_SawWaypoint objComponent = (LE_SawWaypoint)LE_Object.AddComponentToObject(waypoint, "SawWaypoint");

            if (nextWaypoint == null)
            {
                nextWaypoint = objComponent;
                objComponent.previousWaypoint = this;
                objComponent.mainSaw = this;
            }
            else
            {
                objComponent.previousWaypoint = GetLastWaypoint();
                GetLastWaypoint().nextWaypoint = objComponent;
                objComponent.mainSaw = this;
            }

            // Register the new waypoint in these lists.
            if (!isFromSavedData)
            {
                AddNewWaypointToList(new LE_SawWaypointSerializable(objComponent));
            }
            waypointsGOs.Add(objComponent.gameObject);

            // Select the object only if the waypoint is NOT loaded from save data ;)
            if (!isFromSavedData && EditorController.Instance)
            {
                EditorController.Instance.SetSelectedObj(waypoint);
            }

            Logger.DebugLog($"Created waypoint! ID: {objComponent.objectID}, isFromSaveData: {isFromSavedData}, ignoreIfCurrentNextWaypointIsNull: {ignoreIfCurrentNextWaypointIsNull}.");
            return objComponent;
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

        public void RecalculateWaypoints()
        {
            HideOrShowAllWaypointsInEditor(false);

            // Clear this waypoints list.
            ((List<LE_SawWaypointSerializable>)properties["waypoints"]).Clear();
            nextWaypoint = null;

            LE_SawWaypoint lastWaypoint = null;

            foreach (var waypoint in waypointsGOs)
            {
                LE_SawWaypoint waypointComponent = waypoint.GetComponent<LE_SawWaypoint>();
                waypointComponent.previousWaypoint = null;
                waypointComponent.nextWaypoint = null;

                if (lastWaypoint != null)
                {
                    lastWaypoint.nextWaypoint = waypointComponent;
                    waypointComponent.previousWaypoint = lastWaypoint;
                }
                else
                {
                    waypointComponent.previousWaypoint = this;
                }

                AddNewWaypointToList(new LE_SawWaypointSerializable(waypointComponent));

                // If the next waypoint from this saw is null, that means this is the first waypoint in this new list, assign it to this saw.
                if (nextWaypoint == null) nextWaypoint = waypointComponent;
            }

            HideOrShowAllWaypointsInEditor(true);
        }

        public void HideOrShowAllWaypointsInEditor(bool show)
        {
            if (nextWaypoint != null)
            {
                // This is a loop, when we call this in the next waypoint, it'll automatically call it in the OTHER waypoints till the end.
                nextWaypoint.HideOrShowSawInEditor(show);
            }

            if (editorWaypointLine)
            {
                editorWaypointLine.gameObject.SetActive(show);
            }
        }

        void AddNewWaypointToList(LE_SawWaypointSerializable newWaypoint)
        {
            ((List<LE_SawWaypointSerializable>)properties["waypoints"]).Add(newWaypoint);
        }
    }
}
