using FS_LevelEditor;
using FS_LevelEditor.Editor;
using FS_LevelEditor.Playmode;
using Il2Cpp;
using Il2CppDiscord;
using UnityEngine;
using UnityEngine.Events;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Gun : LE_Object
    {
        Gun gun;
        public static bool infTaser;
        public static int ammo;
        void Awake()
        {
            properties = new Dictionary<string, object>
            {
                { "OnPickup", new List<LE_Event>() },
                { "InfiniteTaser", false },
                { "Ammo", 1 }
            };
        }
        public override void OnInstantiated(LEScene scene)
        {
            base.OnInstantiated(scene);
        }
        public override void InitComponent()
        {
            GameObject content = gameObject.GetChild("Content");

            content.SetActive(false);
            content.tag = "Gun";
            gun = content.AddComponent<Gun>();
            gun.aimStabilizerModule = new GameObject();
            gun.powerRail1Module = new GameObject();
            gun.powerRail2Module = new GameObject();
            gun.scopeModule = new GameObject();
            gun.hoverModule = new GameObject();
            gun.battery1 = content.GetChildAt("Taser_PC/Battery/Battery1");
            gun.battery2 = new GameObject();
            gun.battery3 = new GameObject();
            ConfigureEvents(gun);
            infTaser = (bool)properties["InfiniteTaser"];
            ammo = (int)properties["Ammo"];

            //setuo layers
            content.GetChildAt("Taser_PC/PhysicsCollider").layer = LayerMask.NameToLayer("IgnorePlayerCollision");
            content.GetChildAt("Taser_PC/PhysicsCollider/PhysicsCollider_Box").layer = LayerMask.NameToLayer("IgnorePlayerCollision");

            content.SetActive(true);
            initialized = true;
        }
        public override bool SetProperty(string name, object value)
        {
            if (name == "InfiniteTaser")
            {
                if (value is bool)
                {
                    properties["InfiniteTaser"] = (bool)value;
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
            else if (name == "Ammo")
            {
                if (value is int)
                {
                    properties["Ammo"] = (int)value;
                    return true;
                }
                else if (value is string)
                {
                    if (int.TryParse((string)value, out int result))
                    {
                        properties["Ammo"] = result;
                        return true;
                    }
                }
            }

            return base.SetProperty(name, value);
        }

        void ConfigureEvents(Gun script)
        {
            script.onPickup = new UnityEngine.Events.UnityEvent();
            script.onPickup.AddListener((UnityAction)ExecuteOnPickUpEvents);
        }

        void ExecuteOnPickUpEvents()
        {
            eventExecuter.ExecuteEvents((List<LE_Event>)properties["OnPickup"]);
        }

        public override List<string> GetAvailableEventsIDs()
        {
            return new List<string>()
            {
                "OnPickup"
            };
        }
    }
}
[HarmonyLib.HarmonyPatch(typeof(GunController), nameof(GunController.SetTutorialMode))]
public static class TazerTutModeFix
{
    static bool result = true;
    public static bool Prefix(GunController __instance, bool state)
    {
        if (PlayModeController.Instance)
        {
            Controls.m_currentJetpackUpgradeLevel = 1;
            Controls.m_currentHealthUpgradeLevel = 1;
            Controls.m_currentTaserCapacityUpgradeLevel = 1;
            Controls.m_currentHealthBackpackLevel = 0;
            Controls.m_currentTaserBackpackLevel = 0;
            Controls.m_currentTaserPowerUpgradeLevel = 0;
            Controls.m_currentStealthUpgradeLevel = 0;
            Controls.m_currentHoverUpgradeLevel = 0;
            Controls.m_currentScopeLevel = 0;
            Controls.m_currentSafeLandingLevel = 0;
            Controls.m_currentUVFlashlightLevel = 0;
            Controls.m_currentScannerLevel = 0;
            if (LE_Gun.infTaser)
            {
                result = true;
            } else
            {
                __instance.currentAmmo = LE_Gun.ammo;
                __instance.SetAmmos(LE_Gun.ammo);
                result = false;
            }

        }
        return result;
        
    }
}