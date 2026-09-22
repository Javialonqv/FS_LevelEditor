using FS_LevelEditor.Editor;
using HarmonyLib;
using UnityEngine;

namespace FS_LevelEditor
{

    public class LE_Laser_Field : LE_Object
    {
        GameObject edgesParent;
        GameObject holder;

        void Awake()
        {
            edgesParent = gameObject.GetChildAt("Content/Edges");
            holder = gameObject.GetChildAt("Content/Holder");
        }

        public static Dictionary<string, object> GetDefaultProperties()
        {
            return new Dictionary<string, object>()
            {
                { "InvisibleEdges", false },
                { "DestroyCubes", true },
                { "Light", true }
            };
        }

        public override void ObjectStart(LEScene scene)
        {
            // Execute on editor and on playmode.
            EnableEdges(!GetProperty<bool>("InvisibleEdges"));

            base.ObjectStart(scene);
        }

        public override void InitComponent()
        {
            GameObject content = gameObject.GetChild("Content");

            content.SetActive(false);

            KillPlaneController script = content.AddComponent<KillPlaneController>();
            script.activationAllowed = true;
            AccessTools.Field(script.GetType(), "currentState").SetValue(script, true);
            script.destroyCubes = GetProperty<bool>("DestroyCubes");
            script.destroyOnlyIfNotInHands = true;
            AccessTools.Field(script.GetType(), "fakeZeroScale").SetValue(script, Vector3.one * 0.0001f);
            script.generalAnimator = content.GetComponent<Animator>();
            AccessTools.Field(script.GetType(), "m_desiredScale").SetValue(script, Vector3.one * 0.4f);
            script.m_light = content.GetChildAt("Holder/Light").GetComponent<Light>();
            script.m_onTurnOff = new UnityEngine.Events.UnityEvent();
            script.m_onTurnOn = new UnityEngine.Events.UnityEvent();
            AccessTools.Field(script.GetType(), "m_scaleSpeed").SetValue(script, 0.25f);
            script.onLightIntensity = -1;
            if (!GetProperty<bool>("Light"))
            {
                script.onLightIntensity = 0;
            }

            // ---------- SETUP TAGS & LAYERS ----------

            content.GetChildAt("Holder/KillPlane_Mesh").layer = LayerMask.NameToLayer("TransparentFX");
            content.GetChildAt("Holder/KillZone").tag = "KillZone";
            content.GetChildAt("Holder/KillZone").layer = LayerMask.NameToLayer("Ignore Raycast");
            content.GetChildAt("Holder/KillZone/InteractionOccluder1").tag = "InteractionOccluder_ALL";
            content.GetChildAt("Holder/KillZone/InteractionOccluder1").layer = LayerMask.NameToLayer("ActivableCheck");

            content.SetActive(true);

            initialized = true;
        }

        public override bool SetProperty(string name, object value)
        {
            if (name == "InvisibleEdges")
            {
                if (value is bool)
                {
                    properties["InvisibleEdges"] = (bool)value;
                    if (EditorController.Instance != null) EnableEdges(!(bool)value);
                    return true;
                }
            }
            else if (name == "Light")
            {
                if (value is bool)
                {
                    properties["Light"] = (bool)value;
                    return true;
                }
            }
            else if (name == "DestroyCubes")
            {
                if (value is bool)
                {
                    properties["DestroyCubes"] = (bool)value;
                    return true;
                }
            }

            return base.SetProperty(name, value);
        }

        public override bool TriggerAction(string actionName)
        {
            if (actionName == "Activate")
            {
                if (!holder.activeSelf)
                    holder.SetActive(true);
                return true;
            }
            else if (actionName == "Deactivate")
            {
                if (holder.activeSelf)
                    holder.SetActive(false);
                return true;
            }
            else if (actionName == "ToggleActivated")
            {
                holder.SetActive(!holder.activeSelf);
                return true;
            }

            return base.TriggerAction(actionName);
        }

        void EnableEdges(bool enable)
        {
            edgesParent.SetActive(enable);
        }
    }
}
