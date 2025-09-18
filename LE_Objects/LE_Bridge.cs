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
				// NOTE: Use Instant methods to avoid playing animations/sounds at start.
				if (GetProperty<InitialState>("InitialState") == InitialState.DEPLOYED)
				{
					// InstantDeploy doesn't check if "deployed" is false.
					bridgeController.InstantDeploy();
					bridgeController.deployed = true; // It seems InstantRetract doesn't set this. Do it manually.
				}
				else
				{
					// InstantRetract doesn't check if "deployed" is true.
					bridgeController.InstantRetract();
					bridgeController.deployed = false; // It seems InstantRetract doesn't set this. Do it manually.
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

		// Gray, just in case you wanna revert this and play the whole animation, just change 'instant' to false and fuck it. - Jav
		void UpdateBridgeStateInEditor(bool instant = true)
		{
			// Only update visuals in editor mode
			if (!EditorController.Instance) return;

			InitialState state = GetProperty<InitialState>("InitialState");

			// Update animation state to match
			Animation animation = gameObject.GetChild("Content").GetComponent<Animation>();
			if (animation != null)
			{
				if (instant)
				{
					string animName = state == InitialState.DEPLOYED ? "BridgeDeploy" : "BridgeRetract";
					// Don't ask me why I'm substracting 0.01 here, I'm just copy pasting what I saw inside of Charles' code. - Jav
					animation[animName].time = animation[animName].length - 0.01f;
					animation.Play(animName);
				}
				else
				{
					animation.Play(
						state == InitialState.DEPLOYED ? "BridgeDeploy" : "BridgeRetract"
					);
				}
			}
		}
	}
}
