using FS_LevelEditor.Editor;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;

namespace FS_LevelEditor
{
    public class LE_Power_Beam : LE_Object
    {
        LightLaserController script;

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

            content.GetComponentInChildren<AudioSource>().outputAudioMixerGroup = template.GetComponentInChildren<AudioSource>().outputAudioMixerGroup;
            content.GetComponent<AudioSource>().outputAudioMixerGroup = template.GetComponent<AudioSource>().outputAudioMixerGroup;
            content.GetComponentInChildren<AudioSource>().volume = template.GetComponentInChildren<AudioSource>().volume;
            content.GetComponent<AudioSource>().volume = template.GetComponent<AudioSource>().volume;
            
            LineRenderer templateLine = template.GetComponent<LineRenderer>();
            LineRenderer contentLine = content.GetComponent<LineRenderer>();
            contentLine.material = templateLine.material;
            contentLine.widthCurve = templateLine.widthCurve;
            contentLine.textureMode = templateLine.textureMode;
            contentLine.numCapVertices = templateLine.numCapVertices;
            contentLine.numCornerVertices = templateLine.numCornerVertices;
            contentLine.alignment = templateLine.alignment;
            contentLine.useWorldSpace = templateLine.useWorldSpace;
            contentLine.startColor = templateLine.startColor;
            contentLine.endColor = templateLine.endColor;

            script = content.AddComponent<LightLaserController>();
            script.isImportant = true;
            script.isPowerBeamCH4 = false;
            script.laserSound = template.laserSound;
            script.lightLaserMaterial = template.lightLaserMaterial;
            script.stateAtStart = (bool)GetProperty("ActivateOnStart");

            AccessTools.Field(typeof(LightLaserController), "Line").SetValue(script, AccessTools.Field(typeof(LightLaserController), "Line").GetValue(template));

            script.m_layer = template.m_layer;
            script.realisticReflection = true;
            script.m_light = content.GetChild("Light").GetComponent<Light>();
            script.particlesObject = template.particlesObject;

            ConfigureEvents(script);

            content.SetActive(true);

            base.InitComponent();
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
            return base.SetProperty(name, value);
        }
    }
}