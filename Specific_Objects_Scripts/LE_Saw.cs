using Il2Cpp;
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
        void Awake()
        {
            properties = new Dictionary<string, object>()
            {
                { "ActivateOnStart", true }
            };
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
            }
        }

        void InitComponent()
        {
            gameObject.SetActive(false);
            gameObject.tag = "Scie";

            gameObject.GetComponent<AudioSource>().outputAudioMixerGroup = FindObjectOfType<ScieScript>().GetComponent<AudioSource>().outputAudioMixerGroup;

            RotationScie rotationScie = gameObject.GetChildWithName("Scie_OFF").AddComponent<RotationScie>();
            rotationScie.vitesseRotation = 500;

            ScieScript script = gameObject.AddComponent<ScieScript>();
            script.doesDamage = true;
            script.rotationScript = rotationScie;
            script.m_damageCollider = GetComponent<BoxCollider>();
            script.m_audioSource = GetComponent<AudioSource>();
            script.movingSaw = false;
            script.movingSpeed = 10;
            script.scieSound = FindObjectOfType<ScieScript>().scieSound;
            script.offMesh = gameObject.GetChildWithName("Scie_OFF").GetComponent<MeshRenderer>();
            script.onMesh = gameObject.GetChildAt("Scie_OFF/Scie_ON").GetComponent<MeshRenderer>();
            script.m_collision = gameObject.GetChildWithName("Collision").GetComponent<BoxCollider>();
            script.physicsCollider = gameObject.GetChildWithName("Saw_PhysicsCollider").GetComponent<MeshCollider>();


            // There's a good reasong for this, I swear, the Activate and Deactivate functions are just inverting the enabled bool in the saw LOL, the both functions
            // do the same thing, so first enable it, and then disable if needed, cause if we don't do anything, there's a bug with the saw animation.
            script.Activate();
            bool activateOnStart = (bool)GetProperty("ActivateOnStart");
            if (!activateOnStart)
            {
                script.Deactivate();
            }

            gameObject.SetActive(true);
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

            return false;
        }

        public override bool TriggerAction(string actionName)
        {
            if (actionName == "AddWaypoint")
            {
                Logger.DebugLog("triggered!");
                return true;
            }

            return false;
        }

        void SetMeshOnEditor(bool isSawOn)
        {
            gameObject.GetChildAt("Scie_OFF").GetComponent<MeshRenderer>().enabled = !isSawOn;
            gameObject.GetChildAt("Scie_OFF/Scie_ON").GetComponent<MeshRenderer>().enabled = isSawOn;
        }
    }
}
