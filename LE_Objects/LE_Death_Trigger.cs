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
    public class LE_Death_Trigger : LE_Object
    {
        public enum TriggerType { Relocation, Imminent }

        void Awake()
        {
            properties = new Dictionary<string, object>()
            {
                { "Type", TriggerType.Relocation },
                { "Delay", 0f }
            };
        }

        public override void OnInstantiated(LEScene scene)
        {
            if (scene == LEScene.Playmode)
            {
                gameObject.GetChildAt("Content/Mesh").SetActive(false);
            }

            base.OnInstantiated(scene);
        }

        public override void InitComponent()
        {
            GameObject content = gameObject.GetChild("Content");

            content.SetActive(false);

            ContainmentBox script = content.GetChild("Trigger").AddComponent<ContainmentBox>();
            script.delay = GetProperty<float>("Delay");
            script.useSeparateDelays = false;
            script.warnDistance = 9;
            script.currentRespawnIndex = 0;
            script.m_resetTransform = content.GetChild("Spawn").transform;
            script.playDialogs = false;
            script.selectivePlayDialogs = false;
            script.dialogsUpperLimit = false;
            script.killPlayer = GetProperty<TriggerType>("Type") == TriggerType.Imminent;
            script.useSeparateKillPlayer = false;
            script.isAreaDenial = false;
            script.considerPlayer = true;
            script.m_collider = script.GetComponent<BoxCollider>();

            content.SetActive(true);

            initialized = true;
        }

        public override bool SetProperty(string name, object value)
        {
            if (name == "Type")
            {
                if (value is int)
                {
                    properties["Type"] = (TriggerType)value;
                    return true;
                }
                else if (value is TriggerType)
                {
                    properties["Type"] = value;
                    return true;
                }
            }
            else if (name == "Delay")
            {
                if (value is string)
                {
                    if (Utils.TryParseFloat((string)value, out float result))
                    {
                        properties["Delay"] = result;
                        return true;
                    }
                }
                else if (value is float)
                {
                    properties["Delay"] = (float)value;
                    return true;
                }
            }

            return base.SetProperty(name, value);
        }

        public static new Color GetDefaultObjectColor(LEObjectContext context)
        {
            return new Color(1f, 0f, 0f, 0.05f);
        }
    }
}
