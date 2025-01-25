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
        }

        void Update()
        {
            lightSprite.transform.rotation = Camera.main.transform.rotation;
        }

        void OnDestroy()
        {
            currentInstances--;
        }
    }
}
