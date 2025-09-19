using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

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
                { "Delay", 0f },
				{ "CustomCoordinates", false },
				{ "TeleportCoordinates", new Vector3(0,0,0)},
				{ "OnTeleport", new List<LE_Event>() }
            };
        }

        public override void OnInstantiated(LEScene scene)
        {
            if (scene == LEScene.Playmode)
            {
                gameObject.GetChild("Content/Mesh").SetActive(false);
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
            content.GetChild("Spawn").transform.position = content.transform.position;
            if(GetProperty<bool>("CustomCoordinates"))
            {
				GameObject target = new GameObject();
				target.transform.position = GetProperty<Vector3>("TeleportCoordinates");
				target.transform.eulerAngles = GetProperty<Vector3>("TeleportRotation");
				target.transform.localScale = Vector3.one;
				script.m_resetTransform = target.transform;
			}
            else
            {
				script.m_resetTransform = content.GetChild("Spawn").transform;
			}
            script.playDialogs = false;
            script.selectivePlayDialogs = false;
            script.dialogsUpperLimit = false;
            script.killPlayer = GetProperty<TriggerType>("Type") == TriggerType.Imminent;
            script.useSeparateKillPlayer = false;
            script.isAreaDenial = false;
            script.considerPlayer = true;
            script.m_collider = script.GetComponent<BoxCollider>();

            script.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

			ConfigureEvents(script);

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
			else if (name == "CustomCoordinates")
			{
				if (value is bool)
				{
					properties["CustomCoordinates"] = (bool)value;
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
			else if (name == "TeleportCoordinates")
			{
				if (value is Vector3)
				{
					properties["TeleportCoordinates"] = (Vector3)value;
					return true;
				}
			}
			else if (name == "TeleportRotation")
			{
				if (value is Vector3)
				{
					properties["TeleportRotation"] = (Vector3)value;
					return true;
				}
			}
			else if (GetAvailableEventsIDs().Contains(name))
			{
				if (value is List<LE_Event>)
				{
					properties[name] = (List<LE_Event>)value;
				}
			}
			return base.SetProperty(name, value);
        }
		public override List<string> GetAvailableEventsIDs()
		{
			return new List<string>
			{
				"OnTeleport",
			};
		}
		void ConfigureEvents(ContainmentBox script)
		{
			script.onTeleport = new UnityEngine.Events.UnityEvent();
			script.onTeleport.AddListener((UnityAction)ExecuteOnTeleportEvents);
		}
		void ExecuteOnTeleportEvents()
		{
			eventExecuter.ExecuteEvents((List<LE_Event>)properties["OnTeleport"]);
		}
		public static new Color GetDefaultObjectColor(LEObjectContext context)
        {
            return new Color(1f, 0f, 0f, 0.05f);
        }
    }
}
