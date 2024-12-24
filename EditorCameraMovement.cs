using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using UnityEngine;

namespace FS_LevelEditor
{
    [RegisterTypeInIl2Cpp]
    public class EditorCameraMovement : MonoBehaviour
    {
        public float moveSpeed = 10f;

        void Start()
        {

        }

        void Update()
        {
            float inputX = Input.GetAxis("Horizontal");
            float inputZ = Input.GetAxis("Vertical");
            Vector3 toMove = transform.right * inputX * moveSpeed * Time.deltaTime +
                transform.forward * inputZ * moveSpeed * Time.deltaTime;

            transform.position += toMove;
        }
    }
}
