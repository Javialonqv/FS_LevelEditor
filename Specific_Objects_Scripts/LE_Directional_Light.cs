using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Directional_Light : LE_Object
    {
        // Since there's only ONE directional light per level, dont' even put the id on it.
        public override string objectFullNameWithID
        {
            get { return objectOriginalName; }
        }

        GameObject lightSprite;

        static int currentInstances = 0;
        const int maxInstances = 1;

        void Awake()
        {
            currentInstances++;

            lightSprite = gameObject.GetChildWithName("Sprite");

            if (PlayModeController.Instance != null)
            {
                Destroy(gameObject.GetChildWithName("Collider"));
                Destroy(lightSprite);
                Destroy(gameObject.GetChildWithName("Arrow"));
            }
        }

        void Update()
        {
            // If the light sprite is null is probaly because we're already in playmode and the light sprite was destroyed.
            if (lightSprite != null)
            {
                lightSprite.transform.rotation = Camera.main.transform.rotation;
            }
        }

        void OnDestroy()
        {
            currentInstances--;
        }
    }
}
