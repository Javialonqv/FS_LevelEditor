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
    public class LE_Ceiling_Light : LE_Object
    {
        GameObject lightObj;
        GameObject neonOff, neonOn;

        RealtimeCeilingLight light;

        void Awake()
        {
            properties = new Dictionary<string, object>
            {
                { "ActivateOnStart", true }
            };

            lightObj = gameObject.GetChildWithName("Light");
            neonOff = gameObject.GetChildAt("Mesh/NeonOff");
            neonOn = gameObject.GetChildAt("Mesh/NeonOn");
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

                SetMeshOnEditor((bool)GetProperty("ActivateOnStart"));
            }
            else if (PlayModeController.Instance)
            {
                gameObject.GetChildWithName("EditorCollider").SetActive(false);
                gameObject.GetChildWithName("ActivateTrigger").SetActive(false);
                InitComponent();
            }
        }

        void InitComponent()
        {
            RealtimeCeilingLight template = FindObjectOfType<RealtimeCeilingLight>();

            gameObject.SetActive(false);

            light = gameObject.AddComponent<RealtimeCeilingLight>();
            light.m_light = gameObject.GetChildWithName("Light").GetComponent<Light>();
            light.active = false;
            light.activeEditorState = false;
            light.allLightConePlanesRenderers = new Il2CppSystem.Collections.Generic.List<MeshRenderer>();
            light.allLightConePlanesRenderers.Add(gameObject.GetChildAt("LightConePlanes/LightConePlane").GetComponent<MeshRenderer>());
            light.allLightConePlanesRenderers.Add(gameObject.GetChildAt("LightConePlanes/LightConePlane (1)").GetComponent<MeshRenderer>());
            light.animStateBeforeShot = true;
            light.audioSource = gameObject.GetComponent<AudioSource>();
            light.canBeDestroyedByHS = true;
            light.currentColor = RealtimeCeilingLight.LightColor.DEFAULT;
            light.editorIntensity = 2;
            light.frameCount = 2;
            light.idleAnim = "CeilingLight_Blink_MediumIntensity";
            light.idleOnIntensity = -1;
            light.intensityEditorValue = 2;
            light.isBakedOnly = false;
            light.isDestroyed = false;
            light.keepProbeEnabled = true;
            light.lightConePlane_default = template.lightConePlane_default;
            light.lightConePlane_greenColor = template.lightConePlane_greenColor;
            light.lightConePlane_redColor = template.lightConePlane_redColor;
            light.lightConePlanes = gameObject.GetChildWithName("LightConePlanes");
            light.m_animationComp = gameObject.GetComponent<Animation>();
            light.m_defaultColor = Color.white;
            light.m_defaultColorNeonMesh = template.m_defaultColorNeonMesh;
            light.m_flareMultiplier = 7;
            light.m_greenColor = new Color(0.3309f, 1f, 0.4186f, 1f);
            light.m_greenColorNeonMesh = template.m_greenColorNeonMesh;
            light.m_lensFlare = gameObject.GetChildWithName("Flare").GetComponent<LensFlare>();
            light.m_light = gameObject.GetChildWithName("Light").GetComponent<Light>();
            light.m_maxFlair = 1.5f;
            light.m_redColor = new Color(1f, 0.3162f, 0.3162f, 1f);
            light.m_redColorNeonMesh = template.m_redColorNeonMesh;
            light.neonOnMeshFilter = gameObject.GetChildAt("Mesh/NeonOn").GetComponent<MeshFilter>();
            light.offProbeIntensity = 0.4f;
            light.offProbeIntensity_shot = 0.2f;
            light.onProbeIntensity = 0.7f;
            light.rangeEditorValue = 15;
            light.reactToTaserShot = true;
            light.rendererNeonOff = gameObject.GetChildAt("Mesh/NeonOff").GetComponent<MeshRenderer>();
            light.rendererNeonOn = gameObject.GetChildAt("Mesh/NeonOn").GetComponent<MeshRenderer>();
            light.saveColor = true;
            light.soundOff = template.soundOff;
            light.soundOn = template.soundOn;
            light.useLightConePlanes = true;
            light.useTurnOn = true;

            gameObject.GetChildAt("ActivateTrigger").tag = "ActivateTrigger";
            gameObject.GetChildAt("ActivateTrigger").layer = LayerMask.NameToLayer("Ignore Raycast");
            gameObject.GetChildAt("Mesh/Body/LightBase").tag = "RealtimeLight";
            gameObject.GetChildAt("Mesh/Body/LightBase").layer = LayerMask.NameToLayer("IgnorePlayerCollision");

            foreach (var flareCollider in gameObject.GetChildAt("Mesh/Body/LightBase").GetChilds()) flareCollider.layer = LayerMask.NameToLayer("AllExceptPlayer");

            gameObject.GetChildAt("Mesh/NeonOff").layer = LayerMask.NameToLayer("IgnoreLighting");
            gameObject.GetChildAt("Mesh/NeonOn").layer = LayerMask.NameToLayer("IgnoreLighting");
            gameObject.GetChildAt("LightConePlanes/LightConePlane").layer = LayerMask.NameToLayer("TransparentFX");
            gameObject.GetChildAt("LightConePlanes/LightConePlane (1)").layer = LayerMask.NameToLayer("TransparentFX");

            gameObject.SetActive(true);

            if ((bool)GetProperty("ActivateOnStart"))
            {
                Invoke("EnableLightDelayed", 0.2f);
            }
        }

        void EnableLightDelayed()
        {
            light.Activate();
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

        void SetMeshOnEditor(bool lightEnabled)
        {
            lightObj.SetActive(lightEnabled);
            neonOn.SetActive(lightEnabled);
            neonOff.SetActive(!lightEnabled);
        }
    }
}
