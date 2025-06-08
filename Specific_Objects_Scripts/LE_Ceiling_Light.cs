using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Il2CppSystem.Linq.Expressions.Interpreter.CastInstruction.CastInstructionNoT;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Ceiling_Light : LE_Object
    {
        GameObject lightObj;
        GameObject neonOff, neonOn;
        Light light;

        RealtimeCeilingLight lightComp;

        void Awake()
        {
            properties = new Dictionary<string, object>
            {
                { "ActivateOnStart", true },
                { "Color", Color.white }
            };

            lightObj = gameObject.GetChildAt("Content/Light");
            neonOff = gameObject.GetChildAt("Content/Mesh/NeonOff");
            neonOn = gameObject.GetChildAt("Content/Mesh/NeonOn");
            light = lightObj.GetComponent<Light>();
        }

        void Start()
        {
            if (EditorController.Instance)
            {
                foreach (var collider in gameObject.TryGetComponents<Collider>())
                {
                    if (collider.gameObject.name == "EditorCollider") continue;

                    collider.enabled = false;
                }

                SetMeshOnEditor();
            }
            else if (PlayModeController.Instance)
            {
                gameObject.GetChildWithName("EditorCollider").SetActive(false);
                gameObject.GetChildAt("Content/ActivateTrigger").SetActive(false);
                InitComponent();

                light.color = (Color)GetProperty("Color");
            }
        }

        void InitComponent()
        {
            RealtimeCeilingLight template = t_ceilingLight;

            gameObject.SetActive(false);

            lightComp = gameObject.GetChildWithName("Content").AddComponent<RealtimeCeilingLight>();
            lightComp.m_light = gameObject.GetChildAt("Content/Light").GetComponent<Light>();
            lightComp.active = false;
            lightComp.activeEditorState = false;
            lightComp.allLightConePlanesRenderers = new Il2CppSystem.Collections.Generic.List<MeshRenderer>();
            lightComp.allLightConePlanesRenderers.Add(gameObject.GetChildAt("Content/LightConePlanes/LightConePlane").GetComponent<MeshRenderer>());
            lightComp.allLightConePlanesRenderers.Add(gameObject.GetChildAt("Content/LightConePlanes/LightConePlane (1)").GetComponent<MeshRenderer>());
            lightComp.animStateBeforeShot = true;
            lightComp.audioSource = gameObject.GetChildWithName("Content").GetComponent<AudioSource>();
            lightComp.canBeDestroyedByHS = true;
            lightComp.currentColor = RealtimeCeilingLight.LightColor.DEFAULT;
            lightComp.editorIntensity = 2;
            lightComp.frameCount = 2;
            lightComp.idleAnim = "CeilingLight_Blink_MediumIntensity";
            lightComp.idleOnIntensity = -1;
            lightComp.intensityEditorValue = 2;
            lightComp.isBakedOnly = false;
            lightComp.isDestroyed = false;
            lightComp.keepProbeEnabled = true;
            lightComp.lightConePlane_default = template.lightConePlane_default;
            lightComp.lightConePlane_greenColor = template.lightConePlane_greenColor;
            lightComp.lightConePlane_redColor = template.lightConePlane_redColor;
            lightComp.lightConePlanes = gameObject.GetChildAt("Content/LightConePlanes");
            lightComp.m_animationComp = gameObject.GetChildWithName("Content").GetComponent<Animation>();
            lightComp.m_defaultColor = (Color)GetProperty("Color");
            lightComp.m_defaultColorNeonMesh = template.m_defaultColorNeonMesh;
            lightComp.m_flareMultiplier = 7;
            lightComp.m_greenColor = new Color(0.3309f, 1f, 0.4186f, 1f);
            lightComp.m_greenColorNeonMesh = template.m_greenColorNeonMesh;
            lightComp.m_lensFlare = gameObject.GetChildAt("Content/Flare").GetComponent<LensFlare>();
            lightComp.m_light = gameObject.GetChildAt("Content/Light").GetComponent<Light>();
            lightComp.m_maxFlair = 1.5f;
            lightComp.m_redColor = new Color(1f, 0.3162f, 0.3162f, 1f);
            lightComp.m_redColorNeonMesh = template.m_redColorNeonMesh;
            lightComp.neonOnMeshFilter = gameObject.GetChildAt("Content/Mesh/NeonOn").GetComponent<MeshFilter>();
            lightComp.offProbeIntensity = 0.4f;
            lightComp.offProbeIntensity_shot = 0.2f;
            lightComp.onProbeIntensity = 0.7f;
            lightComp.rangeEditorValue = 15;
            lightComp.reactToTaserShot = true;
            lightComp.rendererNeonOff = gameObject.GetChildAt("Content/Mesh/NeonOff").GetComponent<MeshRenderer>();
            lightComp.rendererNeonOn = gameObject.GetChildAt("Content/Mesh/NeonOn").GetComponent<MeshRenderer>();
            lightComp.saveColor = true;
            lightComp.soundOff = template.soundOff;
            lightComp.soundOn = template.soundOn;
            lightComp.useLightConePlanes = true;
            lightComp.useTurnOn = true;
            // LOVE YOU CHARLES FOR GIVING ME THIS VARIABLE!!!
            lightComp.stateAtStart = (bool)GetProperty("ActivateOnStart");

            gameObject.GetChildAt("Content/ActivateTrigger").tag = "ActivateTrigger";
            gameObject.GetChildAt("Content/ActivateTrigger").layer = LayerMask.NameToLayer("Ignore Raycast");
            gameObject.GetChildAt("Content/Mesh/Body/LightBase").tag = "RealtimeLight";
            gameObject.GetChildAt("Content/Mesh/Body/LightBase").layer = LayerMask.NameToLayer("IgnorePlayerCollision");

            foreach (var flareCollider in gameObject.GetChildAt("Content/Mesh/Body/LightBase").GetChilds()) flareCollider.layer = LayerMask.NameToLayer("AllExceptPlayer");

            gameObject.GetChildAt("Content/Mesh/NeonOff").layer = LayerMask.NameToLayer("IgnoreLighting");
            gameObject.GetChildAt("Content/Mesh/NeonOn").layer = LayerMask.NameToLayer("IgnoreLighting");
            gameObject.GetChildAt("Content/LightConePlanes/LightConePlane").layer = LayerMask.NameToLayer("TransparentFX");
            gameObject.GetChildAt("Content/LightConePlanes/LightConePlane (1)").layer = LayerMask.NameToLayer("TransparentFX");

            gameObject.SetActive(true);

        }

        public override bool SetProperty(string name, object value)
        {
            if (name == "ActivateOnStart")
            {
                if (value is bool)
                {
                    properties["ActivateOnStart"] = (bool)value;
                    if (EditorController.Instance != null) SetMeshOnEditor();
                    return true;
                }
                else
                {
                    Logger.Error($"Tried to set \"ActivateOnStart\" property with value of type \"{value.GetType().Name}\".");
                    return false;
                }
            }
            else if (name == "Color")
            {
                if (value is Color)
                {
                    properties["Color"] = (Color)value;
                    light.color = (Color)value;
                    if (EditorController.Instance) SetMeshOnEditor();
                    return true;
                }
                else if (value is string)
                {
                    Color? color = Utilities.HexToColor((string)value, false, null);
                    if (color != null)
                    {
                        properties["Color"] = color;
                        light.color = (Color)color;
                        if (EditorController.Instance) SetMeshOnEditor();
                        return true;
                    }
                }
                else
                {
                    Logger.Error($"Tried to set \"Color\" property with value of type \"{value.GetType().Name}\".");
                    return false;
                }
            }

            return false;
        }

        public override bool TriggerAction(string actionName)
        {
            if (actionName == "Activate")
            {
                lightComp.SwitchOn();
                return true;
            }
            else if (actionName == "Deactivate")
            {
                lightComp.SwitchOff();
                return true;
            }
            else if (actionName == "ToggleActivated")
            {
                if (lightComp.active)
                {
                    lightComp.SwitchOff();
                }
                else
                {
                    lightComp.SwitchOn();
                }
                return true;
            }

            return base.TriggerAction(actionName);
        }

        void SetMeshOnEditor()
        {
            bool lightEnabled = (bool)GetProperty("ActivateOnStart");
            Color lightColor = (Color)GetProperty("Color");

            lightObj.SetActive(lightEnabled);
            neonOn.SetActive(lightEnabled);
            neonOff.SetActive(!lightEnabled);

            Material neonOnMat = neonOn.GetComponent<MeshRenderer>().material;
            neonOnMat.color = lightColor;
            neonOn.GetComponent<MeshRenderer>().SetMaterial(neonOnMat);
        }
    }
}
