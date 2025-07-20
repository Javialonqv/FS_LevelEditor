using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Il2Cpp;
using Il2CppInControl;
using MelonLoader;
using UnityEngine;

namespace FS_LevelEditor.Editor
{
    [RegisterTypeInIl2Cpp]
    public class EditorCameraMovement : MonoBehaviour
    {
        enum CameraMove { NONE, NORMAL, MOUSE_DRAG }
        CameraMove currentCameraMove;
        public float moveSpeed = 10f;
        public float mouseSensivility = 10f;
        public float downAndUpSpeed = 10f;

        public float xRotation = 0f;
        public float yRotation = 0f;

        Vector3 dragOrigin;
        float dragSpeed = 0.1f;

        public static bool isRotatingCamera
        {
            get
            {
                return Input.GetMouseButton(1);
            }
        }

        void Update()
        {
            if (!EditorController.IsCurrentState(EditorState.NORMAL) && !EditorController.IsCurrentState(EditorState.SELECTING_TARGET_OBJ)) return;

            if (!Input.GetMouseButton(0) && Input.GetMouseButton(1) && currentCameraMove == CameraMove.NONE)
            {
                currentCameraMove = CameraMove.NORMAL;
            }
            else if (!Input.GetMouseButton(0) && Input.GetMouseButton(2) && currentCameraMove == CameraMove.NONE)
            {
                currentCameraMove = CameraMove.MOUSE_DRAG;
            }

            if (Input.GetMouseButtonUp(1) && currentCameraMove == CameraMove.NORMAL) currentCameraMove = CameraMove.NONE;
            if (Input.GetMouseButtonUp(2) && currentCameraMove == CameraMove.MOUSE_DRAG) currentCameraMove = CameraMove.NONE;

            if (currentCameraMove == CameraMove.NORMAL)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                RotateCamera();
                ManageMoveSpeed();
                MoveCamera();
            }
            else if (currentCameraMove == CameraMove.MOUSE_DRAG)
            {
                MoveCameraWithMouseDrag();
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            
            if (currentCameraMove != CameraMove.MOUSE_DRAG)
            {
                ManageDownAndUp();
            }
        }

        void ManageMoveSpeed()
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                moveSpeed = 20f;
            }
            else
            {
                moveSpeed = 10f;
            }
        }

        void MoveCamera()
        {
            float inputX = InControlSingleton.Instance.playerActions.Move.X;
            float inputZ = InControlSingleton.Instance.playerActions.Move.Y;
            Vector3 toMove = transform.right * inputX * moveSpeed * Time.deltaTime +
                transform.forward * inputZ * moveSpeed * Time.deltaTime;

            transform.position += toMove;
        }

        void MoveCameraWithMouseDrag()
        {
            if (Input.GetMouseButtonDown(2))
            {
                dragOrigin = Input.mousePosition;
                return;
            }

            if (Input.GetAxis("Mouse ScrollWhell") > 0)
            {
                dragSpeed++;
                Logger.DebugLog("New drag speed: " + dragSpeed);
            }
            else if (Input.GetAxis("Mouse ScrollWhell") < 0)
            {
                dragSpeed--;
                Logger.DebugLog("New drag speed: " + dragSpeed);
            }

            Vector3 delta = Input.mousePosition - dragOrigin;
            dragOrigin = Input.mousePosition;

            Vector3 right = transform.right;
            Vector3 forward = Vector3.ProjectOnPlane(transform.up, transform.forward).normalized;

            Vector3 movement = (-right * delta.x + -forward * delta.y) * dragSpeed;
            transform.position += movement;
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

        public void SetRotation(Vector3 eulerAngles)
        {
            xRotation = eulerAngles.x;
            yRotation = eulerAngles.y;
            RotateCamera();
        }

        void ManageDownAndUp()
        {
            if (!Input.GetMouseButton(1)) return;

            if (Input.GetKey(KeyCode.Q))
            {
                Vector3 toMove = transform.up * -1f * downAndUpSpeed * Time.deltaTime;

                transform.position += toMove;
            }
            else if (Input.GetKey(KeyCode.E))
            {
                Vector3 toMove = transform.up * 1f * downAndUpSpeed * Time.deltaTime;

                transform.position += toMove;
            }
        }
    }
}
