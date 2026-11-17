using FS_LevelEditor.Editor;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

namespace FS_LevelEditor
{
    public class LE_Power_Beam : LE_Object
    {
        public const float DefaultBeamThickness = 0.2f;

        LightLaserController script;
        LineRenderer line;
        float baseWidth = DefaultBeamThickness;

        public override string[] EventsIDs =>
        [
            "onTurnOn",
            "onTurnOff",
        ];
        public static Dictionary<string, object> GetDefaultProperties()
        {
            return new Dictionary<string, object>()
            {
                { "ActivateOnStart", false },
                { "BeamThickness", DefaultBeamThickness },

                { "onTurnOn", new List<LE_Event>() },
                { "onTurnOff", new List<LE_Event>() }
            };
        }

        public override void ObjectStart(LEScene scene)
        {
            base.ObjectStart(scene);
        }

        public override void InitComponent()
        {
            LightLaserController template = t_beam;

            GameObject content = gameObject.GetChild("Content");
            content.SetActive(false);
            content.tag = "Laser";
            content.transform.localScale = template.transform.localScale;

            CopyAudioSource(template.GetComponent<AudioSource>(), content.GetComponent<AudioSource>());
            Transform templateAudio2 = template.transform.Find("Audio2");
            GameObject audio2 = content.GetChild("Audio2");
            if (templateAudio2 && audio2)
            {
                audio2.transform.localPosition = templateAudio2.localPosition;
                audio2.transform.localRotation = templateAudio2.localRotation;
                CopyAudioSource(templateAudio2.GetComponent<AudioSource>(), audio2.GetComponent<AudioSource>());
            }

            line = content.GetComponent<LineRenderer>();
            CopyLineRenderer(template.GetComponent<LineRenderer>(), line);
            baseWidth = line.widthCurve.Evaluate(0f);
            if (baseWidth <= 0f) baseWidth = DefaultBeamThickness;
            ApplyBeamThickness();
            
            Transform templateCollision = template.transform.Find("Collision");
            GameObject collision = content.GetChild("Collision");
            if (templateCollision && collision)
            {
                collision.layer = templateCollision.gameObject.layer;
                BoxCollider src = templateCollision.GetComponent<BoxCollider>();
                BoxCollider dst = collision.GetComponent<BoxCollider>();
                dst.center = src.center;
                dst.size = src.size;
            }
            
            GameObject lightObj = content.GetChild("Light");
            Transform templateLight = template.m_light.transform;
            lightObj.transform.localPosition = templateLight.localPosition;
            lightObj.transform.localRotation = templateLight.localRotation;
            lightObj.transform.localScale = templateLight.localScale;
            LensFlare templateFlare = template.m_light.GetComponent<LensFlare>();
            LensFlare flare = lightObj.GetComponent<LensFlare>();
            if (templateFlare && flare)
            {
                flare.flare = templateFlare.flare;
                flare.color = templateFlare.color;
                flare.brightness = templateFlare.brightness;
                flare.fadeSpeed = templateFlare.fadeSpeed;
            }
            else if (flare) DestroyImmediate(flare);
            CopyLight(template.m_light, lightObj.GetComponent<Light>());

            script = content.AddComponent<LightLaserController>();
            script.isImportant = template.isImportant;
            script.isPowerBeamCH4 = false;
            script.laserSound = template.laserSound;
            script.lightLaserMaterial = template.lightLaserMaterial;
            script.stateAtStart = (bool)GetProperty("ActivateOnStart");
            script.breakableObjectsLevel = template.breakableObjectsLevel;
            script.reflectionPosOffset = template.reflectionPosOffset;
            script.unusableFor = template.unusableFor;
            script.unusableForSpeedrunMultiplier = template.unusableForSpeedrunMultiplier;
            script.touchSwitchDelay = template.touchSwitchDelay;
            script.m_layer = template.m_layer;
            script.realisticReflection = template.realisticReflection;
            script.m_light = lightObj.GetComponent<Light>();
            script.particlesObject = template.particlesObject;

            ConfigureEvents(script);

            content.SetActive(true);

            base.InitComponent();
        }

        internal static void CopyAudioSource(AudioSource src, AudioSource dst)
        {
            dst.outputAudioMixerGroup = src.outputAudioMixerGroup;
            dst.volume = src.volume;
            dst.pitch = src.pitch;
            dst.priority = src.priority;
            dst.loop = src.loop;
            dst.playOnAwake = src.playOnAwake;
            dst.spatialBlend = src.spatialBlend;
            dst.spread = src.spread;
            dst.panStereo = src.panStereo;
            dst.dopplerLevel = src.dopplerLevel;
            dst.reverbZoneMix = src.reverbZoneMix;
            dst.minDistance = src.minDistance;
            dst.maxDistance = src.maxDistance;
            dst.rolloffMode = src.rolloffMode;
            if (src.rolloffMode == AudioRolloffMode.Custom)
                dst.SetCustomCurve(AudioSourceCurveType.CustomRolloff, src.GetCustomCurve(AudioSourceCurveType.CustomRolloff));
            dst.bypassEffects = src.bypassEffects;
            dst.bypassListenerEffects = src.bypassListenerEffects;
            dst.bypassReverbZones = src.bypassReverbZones;
        }

        static void CopyLineRenderer(LineRenderer src, LineRenderer dst)
        {
            dst.sharedMaterials = src.sharedMaterials;
            dst.shadowCastingMode = src.shadowCastingMode;
            dst.receiveShadows = src.receiveShadows;
            dst.motionVectorGenerationMode = src.motionVectorGenerationMode;
            dst.lightProbeUsage = src.lightProbeUsage;
            dst.reflectionProbeUsage = src.reflectionProbeUsage;
            dst.allowOcclusionWhenDynamic = src.allowOcclusionWhenDynamic;
            dst.renderingLayerMask = src.renderingLayerMask;
            dst.sortingLayerID = src.sortingLayerID;
            dst.sortingOrder = src.sortingOrder;
            dst.rendererPriority = src.rendererPriority;

            dst.useWorldSpace = src.useWorldSpace;
            dst.loop = src.loop;
            dst.alignment = src.alignment;
            dst.textureMode = src.textureMode;
            dst.numCapVertices = src.numCapVertices;
            dst.numCornerVertices = src.numCornerVertices;
            dst.widthCurve = src.widthCurve;
            dst.widthMultiplier = src.widthMultiplier;
            dst.colorGradient = src.colorGradient;
            dst.shadowBias = src.shadowBias;
            dst.generateLightingData = src.generateLightingData;
        }

        static void CopyLight(Light src, Light dst)
        {
            dst.type = src.type;
            dst.color = src.color;
            dst.intensity = src.intensity;
            dst.range = src.range;
            dst.spotAngle = src.spotAngle;
            dst.innerSpotAngle = src.innerSpotAngle;
            dst.shadows = src.shadows;
            dst.shadowStrength = src.shadowStrength;
            dst.shadowBias = src.shadowBias;
            dst.shadowNormalBias = src.shadowNormalBias;
            dst.shadowNearPlane = src.shadowNearPlane;
            dst.cullingMask = src.cullingMask;
            dst.renderMode = src.renderMode;
            dst.bounceIntensity = src.bounceIntensity;
            dst.cookie = src.cookie;
            dst.flare = src.flare;
            dst.useColorTemperature = src.useColorTemperature;
            dst.colorTemperature = src.colorTemperature;
            dst.lightShadowCasterMode = src.lightShadowCasterMode;
            dst.renderingLayerMask = src.renderingLayerMask;
        }

        public override bool TriggerAction(string actionName)
        {
            if (actionName == "Activate")
            {
                if (!script.activated)
                    script.RequestOn();
                return true;
            }
            else if (actionName == "Deactivate")
            {
                if (script.activated)
                    script.RequestOff();
                return true;
            }
            else if (actionName == "ToggleActivated")
            {
                if (script.activated)
                {
                    script.RequestOff();
                }
                else
                {
                    script.RequestOn();
                }
                return true;
            }

            return base.TriggerAction(actionName);
        }

        void ConfigureEvents(LightLaserController script)
        {
            script.onTurnOn = new UnityEngine.Events.UnityEvent();
            script.onTurnOn.AddListener((UnityAction)ExecuteOnTurnOnEvents);

            script.onTurnOff = new UnityEngine.Events.UnityEvent();
            script.onTurnOff.AddListener((UnityAction)ExecuteOnTurnOffEvents);
        }
        void ExecuteOnTurnOnEvents()
        {
            eventExecuter.ExecuteEventsWithAndLogic((List<LE_Event>)properties["onTurnOn"], "onTurnOn", true);
        }
        void ExecuteOnTurnOffEvents()
        {
            // The receiver this beam was activating (if any) is deactivated by the beam only while the beam is on, so handle the turn off here.
            FieldInfo currentSwitchField = AccessTools.Field(typeof(LightLaserController), "m_currentlyActivatedSwitch");
            InterrupteurController currentSwitch = (InterrupteurController)currentSwitchField.GetValue(script);
            if (currentSwitch)
            {
                LE_Power_Reciever receiver = currentSwitch.GetComponentInParent<LE_Power_Reciever>();
                if (receiver)
                {
                    currentSwitchField.SetValue(script, null);
                    receiver.OnBeamTurnedOff();
                }
            }

            eventExecuter.ExecuteEventsWithAndLogic((List<LE_Event>)properties["onTurnOff"], "onTurnOff", false);
        }
        public override bool SetProperty(string name, object value)
        {
            if (name == "ActivateOnStart")
            {
                if (value is bool)
                {
                    properties["ActivateOnStart"] = (bool)value;
                    return true;
                }
            }
            else if (name == "BeamThickness")
            {
                if (TryParseFloatOrString(value, out float result))
                {
                    properties["BeamThickness"] = Mathf.Max(0.001f, result);
                    ApplyBeamThickness();
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

        static bool TryParseFloatOrString(object value, out float result)
        {
            result = 0f;
            if (value is float f)
            {
                result = f;
                return true;
            }
            else if (value is string str && Utils.TryParseFloat(str, out float parsed))
            {
                result = parsed;
                return true;
            }
            return false;
        }
        
        void ApplyBeamThickness()
        {
            if (!line) return;
            line.widthMultiplier = GetProperty<float>("BeamThickness") / baseWidth;
        }
    }
}