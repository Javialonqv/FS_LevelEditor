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
        }

        void Update()
        {
            lightBulbSprite.transform.rotation = Camera.main.transform.rotation;
        }
    }
}
