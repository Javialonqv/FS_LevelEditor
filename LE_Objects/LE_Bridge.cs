using FS_LevelEditor.Editor;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;

namespace FS_LevelEditor
{
	[MelonLoader.RegisterTypeInIl2Cpp]
	public class LE_Bridge : LE_Object
	{
		private BridgeController bridgeController;

		void Awake()
		{
			properties = new Dictionary<string, object>
			{
				{ "OnDeploy", new List<LE_Event>() },
				{ "OnRetract", new List<LE_Event>() },
			};
		}

		public override void InitComponent()
		{
			GameObject content = gameObject.GetChild("Content");
			content.tag = "Bridge";
			content.SetActive(false);

			bridgeController = content.AddComponent<BridgeController>();
			bridgeController.isLightBridge = false;
			bridgeController.movePlayerComp = null;
			bridgeController.deployed = false;
			bridgeController.playNecessaryAtStart = true;
			bridgeController.instantAtStart = true;
			bridgeController.m_animationComp = content.GetComponent<Animation>();
			ConfigureEvents(bridgeController);

			content.SetActive(true);
			initialized = true;
		}
		public override bool SetProperty(string name, object value)
		{
			if (GetAvailableEventsIDs().Contains(name))
			{
				if (value is List<LE_Event>)
				{
					properties[name] = (List<LE_Event>)value;
				}
			}
			return base.SetProperty(name, value);
		}
		void ConfigureEvents(BridgeController script)
		{
			script.onDeploy = new UnityEngine.Events.UnityEvent();
			script.onDeploy.AddListener((UnityAction)ExecuteOnDeployEvents);

			script.onRetract = new UnityEngine.Events.UnityEvent();
			script.onRetract.AddListener((UnityAction)ExecuteOnRetractEvents);
		}
		void ExecuteOnDeployEvents()
		{
			eventExecuter.ExecuteEvents((List<LE_Event>)properties["OnDeploy"]);
		}
		void ExecuteOnRetractEvents()
		{
			eventExecuter.ExecuteEvents((List<LE_Event>)properties["OnRetract"]);
		}
		public override List<string> GetAvailableEventsIDs()
		{
			return new List<string>()
			{
				"OnDeploy",
				"OnRetract"
			};
		}
		public override bool TriggerAction(string actionName)
		{
			switch (actionName)
			{
				case "Deploy":
					if (!bridgeController.deployed)
					{
						bridgeController.Deploy();
					}
					return true;

				case "Retract":
					if (bridgeController.deployed)
					{
						bridgeController.Retract();
					}
					return true;

				case "Toggle":
					if (bridgeController.deployed)
					{
						bridgeController.Retract();
					}
					else
					{
						bridgeController.Deploy();
					}
					return true;
			}

			return base.TriggerAction(actionName);
		}
	}
}
