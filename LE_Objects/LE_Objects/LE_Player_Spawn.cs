using UnityEngine;

namespace FS_LevelEditor
{

    public class LE_Player_Spawn : LE_Object
    {
        GameObject spawnSprite;

        static int currentInstances = 0;
        const int maxInstances = 1;

        void Awake()
        {
            currentInstances++;

            canUndoDeletion = false;
            canBeDisabledAtStart = false;

            spawnSprite = gameObject.GetChildAt("Content/Sprite");
        }

        public override void OnInstantiated(LEScene scene)
        {
            if (scene == LEScene.Playmode)
            {
                Destroy(spawnSprite);
                Destroy(gameObject.GetChildAt("Content/Arrow"));
            }

            base.OnInstantiated(scene);
        }

        void Update()
        {
            // If the spawn sprite is null is probaly because we're already in playmode and the spawn sprite was destroyed.
            if (spawnSprite && Camera.main)
            {
                spawnSprite.transform.rotation = Camera.main.transform.rotation;
            }
        }

        void OnDestroy()
        {
            currentInstances--;
        }
    }
}
