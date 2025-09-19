using FS_LevelEditor.Editor;
using FS_LevelEditor.Playmode;
using Il2Cpp;
using Il2CppDiscord;
using UnityEngine;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Jetpack : LE_Object
    {
        JetPack jetpack;

        public override void InitComponent()
        {
            GameObject content = gameObject.GetChild("Content");

            content.SetActive(false);

            content.tag = "JetPack";
            content.layer = LayerMask.NameToLayer("PlayerCollisionOnly");

            jetpack = content.AddComponent<JetPack>();
            jetpack.useSave = false;
            jetpack.jetpackMaterial = content.GetChild("Mesh/JetPack").GetComponent<Renderer>().material;
            jetpack.jetpackLight = content.GetChild("Mesh/JetPack/JetpackPickupLight").GetComponent<Light>();
            jetpack.jetpackFlare = new GameObject().AddComponent<LensFlare>();

            // --------- SETUP TAGS & LAYERS ---------

            content.GetChild("Mesh/JetPack").layer = LayerMask.NameToLayer("IgnorePlayerCollision");
            content.GetChild("Mesh/JetPack/JetpackPickupLight").layer = LayerMask.NameToLayer("IgnorePlayerCollision");

            content.SetActive(true);
            initialized = true;
        }
    }
}
