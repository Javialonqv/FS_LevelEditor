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
		public enum InitialState { RETRACTED, DEPLOYED };
		void Awake()
		{
			properties = new Dictionary<string, object>
			{
				{ "InitialState", InitialState.RETRACTED },
				{ "OnDeploy", new List<LE_Event>() },
				{ "OnRetract", new List<LE_Event>() },
			};
		}
		public override void OnInstantiated(LEScene scene)
		{
			if (scene == LEScene.Editor)
			{
				UpdateBridgeStateInEditor();
			}
			base.OnInstantiated(scene);
		}
		public override void ObjectStart(LEScene scene)
		{
			if (scene == LEScene.Playmode)
			{
				// Set initial state when starting in playmode
				if (GetProperty<InitialState>("InitialState") == InitialState.DEPLOYED)
				{
					bridgeController.deployed = false; // Ensure state is correct before deploying
					bridgeController.Deploy();
				}
				else
				{
					bridgeController.deployed = true; // Ensure state is correct before retracting
					bridgeController.Retract();
				}
			}
			base.ObjectStart(scene);
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
			if (name == "InitialState")
			{
				if (value is int)
				{
					properties["InitialState"] = (InitialState)value;
					UpdateBridgeStateInEditor();
					return true;
				}
				else if (value is InitialState)
				{
					properties["InitialState"] = value;
					UpdateBridgeStateInEditor();
					return true;
				}
			}

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
		void UpdateBridgeStateInEditor()
		{
			// Only update visuals in editor mode
			if (!EditorController.Instance) return;

			if (bridgeController != null)
			{
				InitialState state = GetProperty<InitialState>("InitialState");
				bridgeController.deployed = state == InitialState.DEPLOYED;

				// Update animation state to match
				if (bridgeController.m_animationComp != null)
				{
					bridgeController.m_animationComp.Play(
						state == InitialState.DEPLOYED ? "Deploy" : "Retract"
					);
				}
			}
		}
	}
}
