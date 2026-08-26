using FS_LevelEditor.Editor;
using FS_LevelEditor.Playmode;
using Il2Cpp;
using Il2CppDiscord;
using UnityEngine;
using UnityEngine.Events;
using static MelonLoader.MelonLogger;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Jetpack : LE_Object
    {
        JetPack jetpack;

        public static Dictionary<string, object> GetDefaultProperties()
        {
            return new Dictionary<string, object>
            {
                { "Rotate", true }
            };
        }

        public override void InitComponent()
        {
            GameObject content = gameObject.GetChild("Content");

            content.SetActive(false);

            content.tag = "JetPack";
            content.layer = LayerMask.NameToLayer("PlayerCollisionOnly");

            jetpack = content.AddComponent<JetPack>();
            jetpack.useSave = false;
            jetpack.jetpackMaterial = content.GetChildAt("Mesh/JetPack").GetComponent<Renderer>().material;
            jetpack.jetpackLight = content.GetChildAt("Mesh/JetPack/JetpackPickupLight").GetComponent<Light>();
            jetpack.jetpackFlare = new GameObject("ShouldBeSaved").AddComponent<LensFlare>();
            ConfigureEvents(jetpack);

            // --------- SETUP TAGS & LAYERS ---------

            content.GetChildAt("Mesh/JetPack").layer = LayerMask.NameToLayer("IgnorePlayerCollision");
            content.GetChildAt("Mesh/JetPack/JetpackPickupLight").layer = LayerMask.NameToLayer("IgnorePlayerCollision");

            content.SetActive(true);
            initialized = true;
        }

        public override bool SetProperty(string name, object value)
        {
            if (name == "Rotate")
            {
                if (value is bool)
                {
                    properties["Rotate"] = (bool)value;
                    return true;
                }
            }

            return base.SetProperty(name, value);
        }

        void ConfigureEvents(JetPack script)
        {
            script.onEveryPickup = new UnityEngine.Events.UnityEvent();
            script.onEveryPickup.AddListener((UnityAction)ExecuteOnPickUpEvents);
        }
        void ExecuteOnPickUpEvents()
        {
            LE_Dummy_Checkpoint.UpdateStaticValues();
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(JetPack), nameof(JetPack.Update))]
    public static class JetpackRotationPatch
    {
        public static bool Prefix(JetPack __instance)
        {
            if (PlayModeController.Instance && __instance.transform.parent && __instance.transform.parent.TryGetComponent<LE_Jetpack>(out var jetpack))
            {
                if (!jetpack.GetProperty<bool>("Rotate"))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            return true;
        }
    }
}
