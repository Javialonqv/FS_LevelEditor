using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor.Level_Editor_Itself
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class DeathYPlaneCtrl : MonoBehaviour
    {
        public GameObject editorCamera;

        void Awake()
        {
            editorCamera = GameObject.Find("Main Camera");
        }

        void Update()
        {
            Vector3 newPos = editorCamera.transform.position;
            newPos.y = transform.position.y;
            transform.position = newPos;
        }

        public void SetYPos(float newYValue)
        {
            Vector3 newPos = transform.position;
            newPos.y = newYValue;
            transform.position = newPos;
        }
    }
}
