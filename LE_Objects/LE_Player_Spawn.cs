using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Player_Spawn : LE_Object
    {
        // Since there's only ONE player spawn obj per level, don't even put the id on it.
        public override string objectFullNameWithID
        {
            get { return objectOriginalName; }
        }

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
