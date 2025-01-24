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
        GameObject lightSprite;

        void Awake()
        {
            lightSprite = gameObject.GetChildWithName("Sprite");
        }

        void Update()
        {
            lightSprite.transform.rotation = Camera.main.transform.rotation;
        }
    }
}
