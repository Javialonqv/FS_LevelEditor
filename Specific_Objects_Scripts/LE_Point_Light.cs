using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Point_Light : LE_Object
    {
        GameObject lightBulbSprite;

        void Awake()
        {
            lightBulbSprite = gameObject.GetChildWithName("Sprite");

            if (PlayModeController.Instance != null)
            {
                Destroy(gameObject.GetChildWithName("Collider"));
                Destroy(lightBulbSprite);
                Destroy(gameObject.GetChildWithName("Arrow"));
            }
        }

        void Update()
        {
            // If the light sprite is null is probaly because we're already in playmode and the light sprite was destroyed.
            if (lightBulbSprite != null)
            {
                lightBulbSprite.transform.rotation = Camera.main.transform.rotation;
            }
        }
    }
}
