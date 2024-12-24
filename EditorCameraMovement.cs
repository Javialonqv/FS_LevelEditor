using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Il2Cpp;
using Il2CppInControl;
using MelonLoader;
using UnityEngine;

namespace FS_LevelEditor
{
    [RegisterTypeInIl2Cpp]
    public class EditorCameraMovement : MonoBehaviour
    {
        public float moveSpeed = 10f;
        public float mouseSensivility = 10f;
        public float downAndUpSpeed = 10f;

        float xRotation = 0f;
        float yRotation = 0f;

        void Start()
        {

        }

        void Update()
        {

            MoveCamera();
            if (Input.GetMouseButton(1))
            {
                Cursor.lockState = CursorLockMode.Locked;
                RotateCamera();
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
            }
            ManageDownAndUp();
        }

        void MoveCamera()
        {
            float inputX = InControlSingleton.Instance.playerActions.Move.X;
            float inputZ = InControlSingleton.Instance.playerActions.Move.Y;
            Vector3 toMove = transform.right * inputX * moveSpeed * Time.deltaTime +
                transform.forward * inputZ * moveSpeed * Time.deltaTime;

            transform.position += toMove;
        }

        void RotateCamera()
        {
            float mouseX = InControlSingleton.Instance.playerActions.MouseOnly.X;
            float mouseY = InControlSingleton.Instance.playerActions.MouseOnly.Y;

            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            yRotation += mouseX;

            Vector3 toRotate = new Vector3(xRotation, yRotation, 0f);
            transform.localRotation = Quaternion.Euler(toRotate * mouseSensivility);
        }

        void ManageDownAndUp()
        {
            if (Input.GetKey(KeyCode.LeftControl))
            {
                Vector3 toMove = transform.up * -1f * downAndUpSpeed * Time.deltaTime;

                transform.position += toMove;
            }
            else if (Input.GetKey(KeyCode.Space))
            {
                Vector3 toMove = transform.up * 1f * downAndUpSpeed * Time.deltaTime;

                transform.position += toMove;
            }
        }
    }
}
