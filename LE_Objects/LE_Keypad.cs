using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Services.Analytics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using FS_LevelEditor.Editor.UI;
using FS_LevelEditor.Editor;
using Il2CppTMPro;
using Il2CppSystem.Runtime.CompilerServices;

namespace FS_LevelEditor
{
	[MelonLoader.RegisterTypeInIl2Cpp]
	public class LE_Keypad : LE_Object
	{
		private int keycodeValue = 0;
		public void Awake()
		{
			properties = new Dictionary<string, object>
			{
				{ "Keycode", 1234 },
				{ "onWinEvents", new List<LE_Event>() },
				{ "onFailEvents", new List<LE_Event>() },
				{ "leaveOnIncorrect", false }
			};
		}

		public override void InitComponent()
		{
			GameObject button = gameObject.GetChild("Content");

			button.tag = "Keypad";
			button.GetChild("Mesh").tag = "Interrupteur";
			button.SetActive(false);

			button.GetChildAt("TMP_Display/KeypadTitle_TMP").GetComponent<TMP_Text>().m_fontAsset = t_keycode.GetComponentsInChildren<TextMeshPro>()[0].m_fontAsset;
			button.GetChildAt("TMP_Display/KeypadInputInGame_TMP").GetComponent<TMP_Text>().m_fontAsset = t_keycode.GetComponentsInChildren<TextMeshPro>()[0].m_fontAsset;
			button.GetChildAt("TMP_Display/KeypadReset_TMP").GetComponent<TMP_Text>().m_fontAsset = t_keycode.GetComponentsInChildren<TextMeshPro>()[0].m_fontAsset;
			button.GetChildAt("TMP_Display/KeypadTitle_TMP").GetComponent<TMP_Text>().fontMaterial = t_keycode.GetComponentsInChildren<TextMeshPro>()[0].fontMaterial;
			button.GetChildAt("TMP_Display/KeypadInputInGame_TMP").GetComponent<TMP_Text>().fontMaterial = t_keycode.GetComponentsInChildren<TextMeshPro>()[0].fontMaterial;
			button.GetChildAt("TMP_Display/KeypadReset_TMP").GetComponent<TMP_Text>().fontMaterial = t_keycode.GetComponentsInChildren<TextMeshPro>()[0].fontMaterial;

			InterrupteurController controller = button.AddComponent<InterrupteurController>();

			controller.ActivateButtonSound = t_keycode.ActivateButtonSound;
			controller.allowWhenSwitchingUIContext = true;
			controller.canBeUsed = true;
			controller.controlScript = Controls.Instance;
			controller.iconActivationSound = t_keycode.iconActivationSound;
			controller.iconDeactivationSound = t_keycode.iconDeactivationSound;
			controller.IGCType = Controls.InGamePlayerKineType.NONE;
			controller.manualInteractionTransitionSpeed = 1;

			controller.interactableWhileDodge = true;
			controller.localizedInteractionString = t_keycode.localizedInteractionString;
			controller.m_audioSource = button.GetComponent<AudioSource>();
			controller.m_meshRenderer = button.GetChild("Mesh").GetComponent<MeshRenderer>();
			controller.m_meshTransform = button.GetChild("Mesh").transform;
			controller.offColor = InterrupteurController.ColorType.RED;
			controller.offMaterials = t_keycode.offMaterials;
			controller.onColor = InterrupteurController.ColorType.GREEN;
			controller.onMaterials = t_keycode.onMaterials;
			controller.unusableColor = InterrupteurController.ColorType.BLACK;
			controller.unusableMaterials = t_keycode.unusableMaterials;
			controller.objectsToDestroy = new GameObject[0];
			controller.objectsToEnableOnly = new GameObject[0];
			controller.objectToActivate = new GameObject();
			controller.messagesOnActivate = new Messenger[0];
			controller.dialogToActivate = new string[0];
			controller.currentInGameInputTMPLabel = button.GetChildAt("TMP_Display/KeypadInputInGame_TMP").GetComponent<TextMeshPro>();
			controller.titleInGameInputTMPLabel = button.GetChildAt("TMP_Display/KeypadTitle_TMP").GetComponent<TextMeshPro>();
			controller.resetInGameInputTMPLabel = button.GetChildAt("TMP_Display/KeypadReset_TMP").GetComponent<TextMeshPro>();
			controller.useManualInteractionSystem = false;

			controller.usableOnce = false;
			controller.ignoreLaser = true;
			controller.canBeUsed = true;
			controller.interactionDistanceMultiplier = .8f;
			controller.isKeypad = true;
			controller.successfulKeypadColor = t_keycode.successfulKeypadColor;
			controller.defaultKeypadColor = t_keycode.defaultKeypadColor;

			GameObject parent = new GameObject("LE_KeypadOffset");
			parent.transform.SetParent(GameObject.Find("2DGUI/Camera/MiniGames").transform);
			parent.transform.localPosition = new Vector3(0, 760, 0);
			parent.transform.localScale = Vector3.one;
			parent.layer = LayerMask.NameToLayer("MiniGames");

			KeycodeController keycode = Instantiate(t_keycodeM, t_keycodeM.transform.position, t_keycodeM.transform.rotation, parent.transform);
			keycode.name = "LE_Keycode";
			keycode.onlyOnce = true;
			keycode.m_messagesOnWin = new Il2CppSystem.Collections.Generic.List<Messenger>();
			keycode.switchVisualState = true;
			keycode.attachedSwitch = controller.gameObject;
			keycode.destroyOnWin = true;
			keycode.onWinEvents = new UnityEvent();
			keycode.onFailEvents = new UnityEvent();
			keycode.gameObject.SetActive(false);
			keycode.sourceToPlayOn = Controls.Instance.m_audioSource;

			keycodeValue = (int)GetProperty<int>("Keycode");

			// Ensure it's always 4 digits (pad with zeros if needed)
			var digits = keycodeValue.ToString("D4").Select(c => int.Parse(c.ToString())).ToList();

			var il2cppDigits = new Il2CppSystem.Collections.Generic.List<int>();
			foreach (var d in digits)
				il2cppDigits.Add(d);

			keycode.keycode.combination = il2cppDigits;
			keycode.keycode.label = keycode.gameObject.GetChildAt("Screen/Label/Label.Label").GetComponent<UILabel>();
			keycode.keycode.keycodeController = keycode;

			controller.objectsToActivate = new GameObject[] { keycode.gameObject };

			button.name = "LE_Keypad";

			button.SetActive(true);

			button.GetChild("AdditionalInteractionCollider").layer = LayerMask.NameToLayer("ActivableCheck");
			button.GetChild("AdditionalInteractionCollider_Radial").layer = LayerMask.NameToLayer("ActivableCheck");
			button.GetChild("AdditionalInteractionCollider").tag = "InteractionCollider";
			button.GetChild("AdditionalInteractionCollider_Radial").tag = "InteractionCollider";

			ConfigureEvents(keycode);

			if(GetProperty<bool>("leaveOnIncorrect"))
			{
				keycode.onFailEvents.AddListener((UnityEngine.Events.UnityAction)delegate { keycode.OnLeaveButton(); });
			}

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
			else if (name == "Keycode")
			{
				if (value is int)
				{
					properties["Keycode"] = (int)value;
					return true;
				}
				else if (value is string)
				{
					if (int.TryParse((string)value, out int result))
					{
						properties["Keycode"] = result;
						return true;
					}
				}
			}
			else if (name == "leaveOnIncorrect")
			{
				if (value is bool)
				{
					properties["leaveOnIncorrect"] = (bool)value;
					return true;
				}
			}

			return base.SetProperty(name, value);
		}
		void ConfigureEvents(KeycodeController script)
		{
			script.onWinEvents = new UnityEngine.Events.UnityEvent();
			script.onWinEvents.AddListener((UnityAction)ExecuteOnDeployEvents);

			script.onFailEvents = new UnityEngine.Events.UnityEvent();
			script.onFailEvents.AddListener((UnityAction)ExecuteOnRetractEvents);
		}
		void ExecuteOnDeployEvents()
		{
			eventExecuter.ExecuteEvents((List<LE_Event>)properties["onWinEvents"]);
		}
		void ExecuteOnRetractEvents()
		{
			eventExecuter.ExecuteEvents((List<LE_Event>)properties["onFailEvents"]);
		}
		public override List<string> GetAvailableEventsIDs()
		{
			return new List<string>()
			{
				"onWinEvents",
				"onFailEvents"
			};
		}
	}
}