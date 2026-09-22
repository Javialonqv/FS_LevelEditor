using FS_LevelEditor.Editor;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace FS_LevelEditor
{
    public class LE_Laser_Field : LE_Object
    {
        GameObject edgesParent;
        GameObject holder;
        Light laserLight;

        void Awake()
        {
            edgesParent = gameObject.GetChildAt("Content/Edges");
            holder = gameObject.GetChildAt("Content/Holder");
            laserLight = gameObject.GetChildAt("Content/Holder/Light").GetComponent<Light>();
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

            if (scene == LEScene.Editor)
            {
                SetLightState(GetProperty<bool>("Light"));
            }

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
                if (value is bool boolValue)
                {
                    properties["InvisibleEdges"] = boolValue;
                    if (EditorController.Instance != null) EnableEdges(!boolValue);
                    return true;
                }
            }
            else if (name == "Light")
            {
                if (value is bool boolValue)
                {
                    properties["Light"] = boolValue;
                    if (EditorController.Instance) SetLightState(boolValue);

                    // If the component is already initialized, update the script's intensity target as well
                    if (contentObject)
                    {
                        KillPlaneController script = contentObject.GetComponent<KillPlaneController>();
                        if (script) script.onLightIntensity = boolValue ? -1 : 0;
                    }

                    return true;
                }
            }
            else if (name == "DestroyCubes")
            {
                if (value is bool boolValue)
                {
                    properties["DestroyCubes"] = boolValue;
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
            if (edgesParent) edgesParent.SetActive(enable);
        }

        void SetLightState(bool enabled)
        {
            if (laserLight) laserLight.enabled = enabled;
        }
    }
}