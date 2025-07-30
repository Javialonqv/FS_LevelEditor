using Il2CppTMPro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor.Misc
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class ScaleScreenText : MonoBehaviour
    {
        public Vector3 globalScale = Vector3.one;
        public Transform relativeTo;
        public bool resetLocalScaleToOneWhenDisabled = true;

        TextMeshPro text;
        float originalWidth, originalHeigth;

        void Awake()
        {
            text = GetComponent<TextMeshPro>();
            originalWidth = text.rectTransform.sizeDelta.x;
            originalHeigth = text.rectTransform.sizeDelta.y;
        }

        void LateUpdate()
        {
            Vector3 parentScale = relativeTo.localScale;
            transform.localScale = new Vector3(1f/parentScale.x, 1f/parentScale.z, 1f/ parentScale.y);

            text.rectTransform.sizeDelta = new Vector2(originalWidth * parentScale.x, originalHeigth * parentScale.y);
        }

        void OnDisable()
        {
            if (resetLocalScaleToOneWhenDisabled)
            {
                transform.localScale = Vector3.one;
                text.rectTransform.sizeDelta = new Vector2(originalWidth, originalHeigth);
            }
        }
    }
}
