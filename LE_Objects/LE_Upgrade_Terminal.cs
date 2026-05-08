using FS_LevelEditor.Editor.UI;
using FS_LevelEditor.SaveSystem;
using Il2Cpp;
using Il2CppSystem;
using Il2CppSystem.Security.Cryptography;
using Il2CppTMPro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Il2Cpp.UpgradePageController;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Upgrade_Terminal : LE_Object
    {
        public override string contentObjectName => "Computer";

        InterrupteurController interrupteur;
        ScreenController screen;
        ComputerInterfaceController computerInterface;

        public bool isActive = true;

        public bool firstUpgradeAlreadyTaken = false;
        public UpgradePageController.UpgradeType firstTakenUpgrade;
        public int firstTakenUpgradeLevel;

        public bool secondUpgradeAlreadyTaken = false;
        public UpgradePageController.UpgradeType secondTakenUpgrade;
        public int secondTakenUpgradeLevel;

        static List<ComputerInterfaceController> allComputerInterfaces = new();

        public static Dictionary<string, object> GetDefaultProperties()
        {
            return new Dictionary<string, object>()
            {
                { "upgrades", new List<UpgradeSaveData>() }
            };
        }

        public override void InitComponent()
        {
            contentObject.SetActive(false);

            #region Interrupteur
            interrupteur = contentObject.AddComponent<InterrupteurController>();
            interrupteur.ActivateButtonSound = t_upgradeTerminal.ActivateButtonSound;
            interrupteur.activated = false;
            interrupteur.additionalInteractionGO = contentObject.GetChild("AdditionalInteractionCollider");
            interrupteur.allowManualInteractAnim = true;
            interrupteur.allowWhenSwitchingUIContext = true;
            interrupteur.alwaysInstantActivation = true;
            interrupteur.canBeUsed = true;
            interrupteur.cyanTableMesh = t_upgradeTerminal.cyanTableMesh;
            interrupteur.delayBetween = 1;
            interrupteur.delayBetweenSpeedrunMultiplier = 1;
            interrupteur.dialogToActivate = new string[0];
            interrupteur.doorsToClose = new GameObject[0];
            interrupteur.hasUnusableMaterials = true;
            interrupteur.iconActivationSound = t_upgradeTerminal.iconActivationSound;
            interrupteur.iconDeactivationSound = t_upgradeTerminal.iconDeactivationSound;
            interrupteur.ignoreColorPlane = true;
            interrupteur.ignoreLaser = true;
            interrupteur.interactableWhileDodge = true;
            interrupteur.interactionDistanceMultiplier = 0.5f;
            interrupteur.interactionOccluderGO = contentObject.GetChild("InteractionOccluder");
            interrupteur.isComputer = true;
            interrupteur.isImportant = true;
            interrupteur.m_alwaysEnable = true;
            interrupteur.m_audioSource = contentObject.GetComponent<AudioSource>();
            interrupteur.m_meshTransform = contentObject.GetChild("Mesh").transform;
            interrupteur.m_onActivate = new UnityEngine.Events.UnityEvent();
            interrupteur.m_onActivate_HandOnly = new UnityEngine.Events.UnityEvent();
            interrupteur.m_onActivate_TaserOnly = new UnityEngine.Events.UnityEvent();
            interrupteur.m_onDeactivate = new UnityEngine.Events.UnityEvent();
            interrupteur.m_onDeactivate_HandOnly = new UnityEngine.Events.UnityEvent();
            interrupteur.m_onDeactivate_TaserOnly = new UnityEngine.Events.UnityEvent();
            interrupteur.m_onManualIGCStart = new UnityEngine.Events.UnityEvent();
            interrupteur.messagesOnActivate = new Messenger[0];
            interrupteur.messagesOnDeactivate = new Messenger[0];
            interrupteur.objectsToActivate = new GameObject[0];
            interrupteur.objectsToDestroy = new GameObject[0];
            interrupteur.objectsToEnableOnly = new GameObject[1]; // To later add the ComputerInterfaceController.
            interrupteur.offColor = InterrupteurController.ColorType.RED;
            interrupteur.offMaterials = t_upgradeTerminal.offMaterials;
            interrupteur.onColor = InterrupteurController.ColorType.GREEN;
            interrupteur.onMaterials = t_upgradeTerminal.onMaterials;
            interrupteur.proximityCollider = contentObject.GetChild("ScannerProximity_Upgrade").GetComponent<BoxCollider>();
            interrupteur.redTableMesh = t_upgradeTerminal.redTableMesh;
            interrupteur.tableMesh = contentObject.GetChildAt("Mesh/MetalTable/Table/Metal_Table_Mesh").GetComponent<MeshFilter>();
            interrupteur.toggleCanBeUsed = new GameObject[0];
            interrupteur.unusableColor = InterrupteurController.ColorType.BLACK;
            interrupteur.unusableMaterials = t_upgradeTerminal.unusableMaterials;
            interrupteur.upgradeTakenLabel = contentObject.GetChildAt("Mesh/Content/Label/UpgradeTakenLabel_TMP").GetComponent<TextMeshPro>();
            interrupteur.upgradeTakenLabel_EndGame = contentObject.GetChildAt("Mesh/Content/Label/UpgradeTakenLabel_EndGame_TMP").GetComponent<TextMeshPro>();

            interrupteur.m_audioSource.outputAudioMixerGroup = t_upgradeTerminal.m_audioSource.outputAudioMixerGroup;
            #endregion

            #region Screen
            screen = contentObject.GetChild("Mesh").AddComponent<ScreenController>();
            screen.m_content = contentObject.GetChildAt("Mesh/Content").transform;
            screen.m_contentAnim = contentObject.GetChildAt("Mesh/Content").GetComponent<Animation>();
            screen.m_screenRenderer = contentObject.GetChildAt("Mesh/Mesh").GetComponent<MeshRenderer>();
            screen.redColorPlane = contentObject.GetChildAt("Mesh/Mesh/RedPlane");
            screen.greenColorPlane = contentObject.GetChildAt("Mesh/Mesh/GreenPlane");
            screen.m_mainLabelTMP = contentObject.GetChildAt("Mesh/Content/Label/MainPCLabel").GetComponent<TextMeshPro>();
            screen.m_mainLabelRenderer = contentObject.GetChildAt("Mesh/Content/Label/MainPCLabel").GetComponent<MeshRenderer>();
            screen.m_secondaryLabelTMP = contentObject.GetChildAt("Mesh/Content/Label/SecondaryLabel").GetComponent<TextMeshPro>();
            screen.m_secondaryLabelRenderer = contentObject.GetChildAt("Mesh/Content/Label/SecondaryLabel").GetComponent<MeshRenderer>();
            screen.m_lockdownLabelTMP = contentObject.GetChildAt("Mesh/Content/Label/LockdownLabel").GetComponent<TextMeshPro>();
            screen.m_lockdownLabelRenderer = contentObject.GetChildAt("Mesh/Content/Label/LockdownLabel").GetComponent<MeshRenderer>();
            screen.firstEnableEver = true;
            screen.redColor = t_screen.redColor;
            screen.greenColor = t_screen.greenColor;
            screen.cyanColor = t_screen.cyanColor;
            screen.whiteColor = Color.white;
            screen.useColorTint = true;
            screen.currentColor = ScreenController.ColorType.CYAN;

            screen.m_contentAnim.clip = t_screen.m_contentAnim.clip;
            foreach (var clip in t_screen.m_contentAnim)
            {
                AnimationState state = clip.Cast<AnimationState>();
                screen.m_contentAnim.AddClipFixed(state.clip, state.name);
            }

            screen.m_mainLabelRenderer.material = t_screen.m_mainLabelRenderer.material;
            screen.m_mainLabelTMP.m_fontAsset = t_screen.m_mainLabelTMP.m_fontAsset;
            screen.m_mainLabelTMP.m_sharedMaterial = t_screen.m_mainLabelTMP.m_sharedMaterial;

            screen.m_secondaryLabelRenderer.material = t_screen.m_secondaryLabelRenderer.material;
            screen.m_secondaryLabelTMP.m_fontAsset = t_screen.m_secondaryLabelTMP.m_fontAsset;
            screen.m_secondaryLabelTMP.m_sharedMaterial = t_screen.m_secondaryLabelTMP.m_sharedMaterial;

            screen.m_lockdownLabelRenderer.material = t_screen.m_lockdownLabelRenderer.material;
            screen.m_lockdownLabelTMP.m_fontAsset = t_screen.m_lockdownLabelTMP.m_fontAsset;
            screen.m_lockdownLabelTMP.m_sharedMaterial = t_screen.m_lockdownLabelTMP.m_sharedMaterial;

            foreach (var label in contentObject.GetChildAt("Mesh/Content/Label").GetChilds())
            {
                FormatLabel formatter = label.AddComponent<FormatLabel>();
                formatter.appendsAllowed = true;
                formatter.formatValues = new FormatLabel.FormatValues[0];
                formatter.hasTextMesh = true;
                formatter.localizationKeys = new string[0];
                formatter.refreshOnEnable = true;
                formatter.requirementLevels = new Il2CppSystem.Collections.Generic.List<int>();
                formatter.requirements = new Il2CppSystem.Collections.Generic.List<string>();
                formatter.textMesh = label.GetComponent<TextMeshPro>();
                formatter.useAutoSizing = true;
                formatter.values = new string[0];
            }
            screen.m_mainLabelFormatter = screen.m_mainLabelTMP.GetComponent<FormatLabel>();
            screen.m_secondaryLabelFormatter = screen.m_secondaryLabelTMP.GetComponent<FormatLabel>();
            screen.m_lockdownLabelFormatter = screen.m_lockdownLabelTMP.GetComponent<FormatLabel>();

            screen.m_mainLabelFormatter.SetLocalizedKey("Upgrade_Word");
            #endregion

            interrupteur.upgradeTakenFormatter = interrupteur.upgradeTakenLabel.GetComponent<FormatLabel>();
            interrupteur.upgradeTakenEndGameFormatter = interrupteur.upgradeTakenLabel_EndGame.GetComponent<FormatLabel>();

            string[] keyboardAndMousePerfectColliders = new string[]
            {
                "Mesh/Terminal_Physics_Keyboard/Keyboard_Physics_PerfectCollider",
                "Mesh/Terminal_Physics_Keyboard/Keyboard_Physics_PerfectCollider"
            };
            foreach (var path in keyboardAndMousePerfectColliders)
            {
                FollowObject followKeyboard = contentObject.GetChildAt(path).AddComponent<FollowObject>();
                followKeyboard.followPos = true;
                followKeyboard.followRot = true;
                followKeyboard.objectToFollow = followKeyboard.transform.parent.GetChild(0);
                followKeyboard.smoothTime = 0.2f;
                followKeyboard.thisT = followKeyboard.transform;
                ForwardPhysicsEvents forwardKeyboard = contentObject.GetChildAt(path).AddComponent<ForwardPhysicsEvents>();
                forwardKeyboard.forwardTarget = forwardKeyboard.transform.parent.GetChild(0).GetComponent<Rigidbody>();
            }

            ForwardShootEvents forwardShoot = contentObject.GetChildAt("Mesh/ShootDetectionCollider").AddComponent<ForwardShootEvents>();
            forwardShoot.target = contentObject.GetChild("Mesh");

            #region Setup Tags & Layers
            contentObject.tag = "Interrupteur";

            contentObject.GetChildAt("Mesh/Content/Label/MainPCLabel").layer = LayerMask.NameToLayer("IgnoreLighting");
            contentObject.GetChildAt("Mesh/Content/Label/SecondaryLabel").layer = LayerMask.NameToLayer("IgnoreLighting");
            contentObject.GetChildAt("Mesh/Content/Label/LockdownLabel").layer = LayerMask.NameToLayer("IgnoreLighting");
            contentObject.GetChildAt("Mesh/Content/Label/UpgradeTakenLabel_TMP").layer = LayerMask.NameToLayer("IgnoreLighting");
            contentObject.GetChildAt("Mesh/Content/Label/UpgradeTakenLabel_EndGame_TMP").layer = LayerMask.NameToLayer("IgnoreLighting");

            contentObject.GetChildAt("Mesh/TerminalBodyMesh/MainBox").layer = LayerMask.NameToLayer("AllExceptPlayer");
            contentObject.GetChildAt("Mesh/TerminalBodyMesh/PowerBox").layer = LayerMask.NameToLayer("AllExceptPlayer");
            contentObject.GetChildAt("Mesh/TerminalBodyMesh/SimplifiedCollision").layer = LayerMask.NameToLayer("PlayerCollisionOnly");

            contentObject.GetChildAt("Mesh/Terminal_Physics_Keyboard/Keyboard_Mesh").layer = LayerMask.NameToLayer("Ignore Raycast");
            contentObject.GetChildAt("Mesh/Terminal_Physics_Keyboard/Keyboard_Mesh/AdditionalSimpleKeyboardCollider").layer = LayerMask.NameToLayer("Ignore Raycast");
            contentObject.GetChildAt("Mesh/Terminal_Physics_Keyboard/Keyboard_Physics_PerfectCollider").layer = LayerMask.NameToLayer("LaserObstructionOnly");

            contentObject.GetChildAt("Mesh/Physics_Mouse/Mouse_Mesh").layer = LayerMask.NameToLayer("Ignore Raycast");
            contentObject.GetChildAt("Mesh/Physics_Mouse/Mouse_Mesh/Mouse_SimpleCollider").layer = LayerMask.NameToLayer("Ignore Raycast");
            contentObject.GetChildAt("Mesh/Physics_Mouse/Physics_Mouse_PerfectCollider").layer = LayerMask.NameToLayer("LaserObstructionOnly");

            contentObject.GetChildAt("Mesh/MetalTable/Table/Metal_Table_Mesh/Metal_Table_PlayerColliders").layer = LayerMask.NameToLayer("PlayerCollisionOnly");
            contentObject.GetChildAt("Mesh/MetalTable/Table/Metal_Table_Mesh/Metal_Table_PlayerColliders/Main").layer = LayerMask.NameToLayer("Ignore Raycast");

            contentObject.GetChildAt("Mesh/ShootDetectionCollider").tag = "Screen";
            contentObject.GetChildAt("Mesh/ShootDetectionCollider").layer = LayerMask.NameToLayer("LaserObstructionOnly");



            contentObject.GetChild("InteractionOccluder").tag = "InteractionOccluder";
            contentObject.GetChild("InteractionOccluder").layer = LayerMask.NameToLayer("ActivableCheck");

            contentObject.GetChild("AdditionalInteractionCollider").tag = "InteractionCollider";
            contentObject.GetChild("AdditionalInteractionCollider").layer = LayerMask.NameToLayer("ActivableCheck");

            contentObject.GetChild("ScannerProximity_Upgrade").layer = LayerMask.NameToLayer("Ignore Raycast");

            contentObject.GetChild("ApproachComputerTrigger").layer = LayerMask.NameToLayer("PlayerCollisionOnly");
            #endregion

            contentObject.SetActive(true);

            #region Computer Interface (UI)
            ComputerInterfaceController computerTemp = GameObject.Find("2DGUI/Camera/MiniGames/UpgradeComputerInterface5_Bonus2").GetComponent<ComputerInterfaceController>();

            computerInterface = Instantiate(computerTemp.gameObject, computerTemp.transform.parent).GetComponent<ComputerInterfaceController>();
            computerInterface.name = "LE_UpgradeComputerInterface_" + objectID;
            Destroy(computerInterface.GetComponent<UniqueId>());
            Destroy(computerInterface.GetComponent<SavedObject>());
            computerInterface.m_associatedComputer = interrupteur;
            computerInterface.bonusRoomObjective = null;
            computerInterface.bonusRoomPanelScreen = null;
            computerInterface.disableIfAlreadyUsed = null;
            computerInterface.m_onActionDisabled = new UnityEngine.Events.UnityEvent();
            computerInterface.m_onActionEnabled = new UnityEngine.Events.UnityEvent();
            computerInterface.m_onCorrectEncryptionFormatted = new UnityEngine.Events.UnityEvent();
            computerInterface.m_onFail = new UnityEngine.Events.UnityEvent();
            computerInterface.m_onLeave = new UnityEngine.Events.UnityEvent();
            computerInterface.m_onShow = new UnityEngine.Events.UnityEvent();
            computerInterface.m_onShowWithEncKeyInserted = new UnityEngine.Events.UnityEvent();
            computerInterface.m_onWin = new UnityEngine.Events.UnityEvent();
            ComputerInterfaceController.endGameMode = false;

            UpgradePageController upgradePage = computerInterface.m_upgradePage.GetComponent<UpgradePageController>();
            upgradePage.availableUpgrades = new Il2CppSystem.Collections.Generic.List<UpgradePageController.UpgradeType>();
            foreach (var upgrade in GetProperty<List<UpgradeSaveData>>("upgrades"))
            {
                if (upgrade.active)
                    upgradePage.availableUpgrades.Add(UpgradeSaveData.ConvertTypeToFSType(upgrade.type).Value);
            }

            allComputerInterfaces.Add(computerInterface);
            #endregion

            interrupteur.objectsToEnableOnly[0] = computerInterface.gameObject;

            initialized = true;
        }

        public override void ObjectStart(LEScene scene)
        {
            if (scene == LEScene.Playmode)
            {
                // Refresh terminal.
                computerInterface.CheckUpgradeAvailability();
            }

            base.ObjectStart(scene);
        }

        public override bool SetProperty(string name, object value)
        {
            if (name == "upgrades")
            {
                if (value is List<UpgradeSaveData>)
                {
                    properties["upgrades"] = value;
                }
            }

            return base.SetProperty(name, value);
        }

        public override bool TriggerAction(string actionName)
        {
            if (actionName == "ManageUpgrades")
            {
                UpgradesPanel.Instance.ShowUpgradesPanel(GetProperty<List<UpgradeSaveData>>("upgrades"), objectFullNameWithID, this, 4);
            }
            else if (actionName == "ActiveState_True")
            {
                isActive = true;
                computerInterface.CheckUpgradeAvailability();
            }
            else if (actionName == "ActiveState_False")
            {
                isActive = false;
                computerInterface.CheckUpgradeAvailability();
            }
            else if (actionName == "ActiveState_Toggle")
            {
                isActive = !isActive;
                computerInterface.CheckUpgradeAvailability();
            }

            return base.TriggerAction(actionName);
        }

        public static void RefreshUsableStateInAllTerminals()
        {
            foreach (var computer in allComputerInterfaces)
            {
                computer.CheckUpgradeAvailability();
            }
        }
    }

    // These patches are for forcing the terminal to show the right upgrades (the defined ones in the props).
    // This is because even if we set availableUpgrades to ours, Controls.GetMissingUpgradesList overwrites it, prevent that.
    // NOTE: This would've been WAY easier if I could patch UpgradePageController.GetUpgradesFromMissing, but the patch didn't work for some reason.
    [HarmonyLib.HarmonyPatch(typeof(UpgradePageController), nameof(UpgradePageController.CreateUpgradeButtons))]
    public static class GetRightUpgradesPatch1
    {
        public static bool IsCreatingUpgradeButtons = false;
        public static Il2CppSystem.Collections.Generic.List<UpgradePageController.UpgradeType> CurrentlyCreatingUpgrades;

        public static void Prefix(UpgradePageController __instance)
        {
            IsCreatingUpgradeButtons = true;
            CurrentlyCreatingUpgrades = __instance.availableUpgrades;
        }
        public static void Postfix()
        {
            IsCreatingUpgradeButtons = false;
            CurrentlyCreatingUpgrades = null;
        }
    }
    [HarmonyLib.HarmonyPatch(typeof(Controls), nameof(Controls.GetMissingUpgradesList))]
    public static class GetRightUpgradesPatch2
    {
        public static bool Prefix(Controls __instance, out Il2CppSystem.Collections.Generic.List<UpgradePageController.UpgradeType> __result)
        {
            if (GetRightUpgradesPatch1.IsCreatingUpgradeButtons)
            {
                __result = GetRightUpgradesPatch1.CurrentlyCreatingUpgrades;
                return false;
            }

            __result = null;
            return true;
        }
    }

    // Detect when the upgrade is taken and update our variables.
    [HarmonyLib.HarmonyPatch(typeof(ComputerInterfaceController), nameof(ComputerInterfaceController.OnUpgradeTaken))]
    public static class OnUpgradeTakenPatch
    {
        public static void Prefix(ComputerInterfaceController __instance, UpgradePageController.UpgradeType _selectedUpgrade)
        {
            if (__instance.name.Contains("LE"))
            {
                LE_Upgrade_Terminal terminal = __instance.m_associatedComputer.transform.GetComponentInParent<LE_Upgrade_Terminal>(true);
                if (terminal)
                {
                    if (!terminal.firstUpgradeAlreadyTaken)
                    {
                        terminal.firstTakenUpgrade = _selectedUpgrade;
                        terminal.firstTakenUpgradeLevel = Controls.GetCurrentLevelFor(_selectedUpgrade);

                        terminal.firstUpgradeAlreadyTaken = true;

                        terminal.isActive = false;
                    }
                    else
                    {
                        terminal.secondTakenUpgrade = _selectedUpgrade;
                        terminal.secondTakenUpgradeLevel = Controls.GetCurrentLevelFor(_selectedUpgrade);

                        terminal.secondUpgradeAlreadyTaken = true;

                        terminal.isActive = false;
                    }
                }
            }
        }
    }

    // These patches are for when the terminal checks the upgrades state and decides to enable/disable itself, force it to use OUR values.
    // In this case, patching FractalSave.HasKey to return if the terminal has already been used or not depending of our isActive variable.
    // NOTE: It was this ugly patch, or recreating CheckUpgradeAvailability from scratch.
    [HarmonyLib.HarmonyPatch(typeof(ComputerInterfaceController), nameof(ComputerInterfaceController.CheckUpgradeAvailability))]
    public static class SetUpdateConsumedCorrectlyPatch1
    {
        public static bool IsCheckingUpgradeAvailability = false;
        public static ComputerInterfaceController CurrentlyCheckingFor;

        public static void Prefix(ComputerInterfaceController __instance)
        {
            IsCheckingUpgradeAvailability = true;
            CurrentlyCheckingFor = __instance;
        }
        public static void Postfix()
        {
            IsCheckingUpgradeAvailability = false;
            CurrentlyCheckingFor = null;
        }
    }
    [HarmonyLib.HarmonyPatch(typeof(FractalSave), nameof(FractalSave.HasKey))]
    public static class SetUpdateConsumedCorrectlyPatch2
    {
        public static bool Prefix(string _key, ref bool __result)
        {
            // This is the key used for every LE terminal, since the name of the object where the interrupteur script is "Computer", and it's always Ch4 (Level4_PC).
            if ((_key == "UpgradeComputer_Level4_PC_Computer" || _key == "UpgradeComputer_Level4_PC_Computer_EndGame") && SetUpdateConsumedCorrectlyPatch1.IsCheckingUpgradeAvailability)
            {
                LE_Upgrade_Terminal terminal = SetUpdateConsumedCorrectlyPatch1.CurrentlyCheckingFor.m_associatedComputer.GetComponentInParent<LE_Upgrade_Terminal>();
                if (terminal)
                {
                    // If returns true, the terminal will deactivate, and viceversa.
                    __result = !terminal.isActive;
                }

                return false;
            }

            return true;
        }
    }

    // Custom implementation of RefreshTakenUpgradeScreen.
    // EXACTLY the same as the main game, but using our variables instead of FractalSave, which is blocked in playmode to not mess with the main game's save.
    [HarmonyLib.HarmonyPatch(typeof(ComputerInterfaceController), nameof(ComputerInterfaceController.RefreshTakenUpgradeScreen))]
    public static class RefreshTakenUpgradeScreenPatch
    {
        public static bool Prefix(ComputerInterfaceController __instance)
        {
            if (__instance.name.Contains("LE"))
            {
                LE_Upgrade_Terminal terminal = __instance.m_associatedComputer.transform.GetComponentInParent<LE_Upgrade_Terminal>(true);
                if (terminal)
                {
                    string upgradeLocalized = "";

                    if (terminal.firstUpgradeAlreadyTaken)
                    {
                        upgradeLocalized = Localization.Get("Upgrade_" + terminal.firstTakenUpgrade.ToString() + "_Title");
                        if (Controls.GetMaxLevelFor(terminal.firstTakenUpgrade) > 1 && ComputerInterfaceController.IsUpgradableType(terminal.firstTakenUpgrade))
                        {
                            upgradeLocalized = string.Concat(new string[]
                            {
                            upgradeLocalized,
                            " ",
                            Localization.Get("Level_Word"),
                            " ",
                            terminal.firstTakenUpgradeLevel.ToString()
                            });
                        }
                        __instance.m_associatedComputer.SetUpgradeTakenText(upgradeLocalized, false);
                    }
                    else
                    {
                        __instance.m_associatedComputer.SetUpgradeTakenText("", false);
                    }

                    if (terminal.secondUpgradeAlreadyTaken)
                    {
                        upgradeLocalized = Localization.Get("Upgrade_" + terminal.secondTakenUpgrade.ToString() + "_Title");
                        if (Controls.GetMaxLevelFor(terminal.secondTakenUpgrade) > 1 && ComputerInterfaceController.IsUpgradableType(terminal.secondTakenUpgrade))
                        {
                            upgradeLocalized = string.Concat(new string[]
                            {
                                upgradeLocalized,
                                " ",
                                Localization.Get("Level_Word"),
                                " ",
                                terminal.secondTakenUpgradeLevel.ToString()
                            });
                        }
                        __instance.m_associatedComputer.SetUpgradeTakenText(upgradeLocalized, true);
                    }
                    else
                    {
                        __instance.m_associatedComputer.SetUpgradeTakenText("", true);
                    }
                }

                return false;
            }

            return true;
        }
    }
}
