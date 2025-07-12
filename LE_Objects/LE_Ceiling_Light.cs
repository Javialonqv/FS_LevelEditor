using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using FS_LevelEditor.Editor;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Ceiling_Light : LE_Object
    {
        GameObject lightObj;
        GameObject neonOff, neonOn;
        Light light;
        GameObject rangeSphere;

        RealtimeCeilingLight lightComp;

        void Awake()
        {
            properties = new Dictionary<string, object>
            {
                { "ActivateOnStart", true },
                { "Color", Color.white },
                { "Range", 6f }
            };

            lightObj = gameObject.GetChildAt("Content/Light");
            neonOff = gameObject.GetChildAt("Content/Mesh/NeonOff");
            neonOn = gameObject.GetChildAt("Content/Mesh/NeonOn");
            light = lightObj.GetComponent<Light>();
            rangeSphere = gameObject.GetChildAt("Content/RangeSphere");
        }

        public override void OnInstantiated(LEScene scene)
        {
            if (scene == LEScene.Editor)
            {
                SetEnabledMeshOnEditor();
            }
            else if (scene == LEScene.Playmode)
            {
                gameObject.GetChildAt("Content/ActivateTrigger").SetActive(false);

                light.color = (Color)GetProperty("Color");
            }

            base.OnInstantiated(scene);
        }

        public override void InitComponent()
        {
            RealtimeCeilingLight template = t_ceilingLight;

            gameObject.GetChildWithName("Content").SetActive(false);

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

            // Add ceiling lights animations.
            foreach (var state in template.GetComponent<Animation>())
            {
                var animState = state.Cast<AnimationState>();
                lightComp.GetComponent<Animation>().AddClip(animState.clip, animState.name);
            }

            gameObject.GetChildWithName("Content").SetActive(true);

            initialized = true;
        }

        public override bool SetProperty(string name, object value)
        {
            if (name == "ActivateOnStart")
            {
                if (value is bool)
                {
                    properties["ActivateOnStart"] = (bool)value;
                    if (EditorController.Instance != null) SetEnabledMeshOnEditor();
                    return true;
                }
            }
            else if (name == "Color")
            {
                if (value is Color)
                {
                    properties["Color"] = (Color)value;
                    light.color = (Color)value;
                    SetMeshColor();
                    return true;
                }
                else if (value is string)
                {
                    Color? color = Utilities.HexToColor((string)value, false, null);
                    if (color != null)
                    {
                        properties["Color"] = color;
                        light.color = (Color)color;
                        SetMeshColor();
                        return true;
                    }
                }
            }
            else if (name == "Range")
            {
                if (value is float)
                {
                    light.range = (float)value;
                    SetRangeSphereScale((float)value);
                    properties["Range"] = (float)value;
                    return true;
                }
                else if (value is string)
                {
                    if (Utilities.TryParseFloat((string)value, out float result))
                    {
                        light.range = result;
                        SetRangeSphereScale(result);
                        properties["Range"] = result;
                        return true;
                    }
                }
            }

            return base.SetProperty(name, value);
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

        public override void OnSelect()
        {
            base.OnSelect();

            rangeSphere.SetActive(true);
        }
        public override void OnDeselect(GameObject nextSelectedObj)
        {
            base.OnDeselect(nextSelectedObj);

            rangeSphere.SetActive(false);
        }

        void SetEnabledMeshOnEditor()
        {
            bool lightEnabled = (bool)GetProperty("ActivateOnStart");

            lightObj.SetActive(lightEnabled);
            neonOn.SetActive(lightEnabled);
            neonOff.SetActive(!lightEnabled);
        }
        void SetMeshColor()
        {
            Color lightColor = GetProperty<Color>("Color");

            neonOn.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", lightColor);
        }

        void SetRangeSphereScale(float range)
        {
            Vector3 rangeSpherescale = Vector3.one * light.range * 2;
            rangeSphere.transform.localScale = rangeSpherescale;
        }

        // Basically the same method as in the base class, but skipping the range sphere.
        public override void SetObjectColor(LEObjectContext context)
        {
            foreach (var renderer in gameObject.TryGetComponents<MeshRenderer>())
            {
                if (renderer.name == rangeSphere.name) continue;

                foreach (var material in renderer.materials)
                {
                    if (!material.HasProperty("_Color")) continue;

                    Color toSet = LE_Object.GetObjectColorForObject(objectType.Value, context);
                    toSet.a = material.color.a;
                    material.color = toSet;
                }
            }
        }
    }
}
