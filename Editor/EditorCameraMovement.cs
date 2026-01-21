using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Il2Cpp;
using Il2CppInControl;
using MelonLoader;
using UnityEngine;
using FS_LevelEditor.Editor.UI;

namespace FS_LevelEditor.Editor
{
    [RegisterTypeInIl2Cpp]
    public class EditorCameraMovement : MonoBehaviour
    {
        enum CameraMove { NONE, NORMAL, MOUSE_DRAG }
        CameraMove currentCameraMove;
        private float moveSpeedWhenShiftPressed = 10f;
        public float moveSpeed = 10f;
        public float mouseSensivility = 10f;
        public float downAndUpSpeed = 10f;

        public float xRotation = 0f;
        public float yRotation = 0f;

		//To jaiv - this one is to make sure no "sudden rotation" happens.
        // Alright, but at least make the code somehow readable lmao. - Jav.
		bool rotationInitialized;

		private const float MIN_MOVE_SPEED = 5f;
        private const float MAX_MOVE_SPEED = 60f;
        private const float SPEED_CHANGE_RATE = 5f;

        Vector3 dragOrigin;
        float dragSpeed = 0.1f;

        public static bool isRotatingCamera
        {
            get
            {
                return Input.GetMouseButton(1);
            }
        }

        public static EditorCameraMovement Instance { get; private set; }

        void Awake()
        {
            Instance = this;

            InitializeRotationFromTransform();
		}
		void OnEnable()
		{
			// In case object is disabled/enabled from editor, ensure rotation stays in sync
			InitializeRotationFromTransform();
		}

		void InitializeRotationFromTransform()
		{
			if (rotationInitialized) return;

			// Use current transform local euler so first mouse movement does not snap to (0,0,0)
			Vector3 e = transform.localEulerAngles;

			// Convert Unity's 0..360 representation to -180..180 for the X axis so clamp works correctly
			float ex = e.x;
			if (ex > 180f) ex -= 360f;

			xRotation = Mathf.Clamp(ex, -90f, 90f); // respect existing clamp
			yRotation = e.y; // yaw can wrap freely

			rotationInitialized = true;
		}

		void Update()
        {
            if (!EditorController.IsCurrentState(EditorState.NORMAL) && !EditorController.IsCurrentState(EditorState.SELECTING_TARGET_OBJ)) return;

            // Don't allow camera movement when save popup is active
            if (SaveMetadataPopup.IsPopupActive()) return;

            #region Define Camera Mode To Use
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
 #endregion

            // Camera speed can be changed at any time
            ManageMoveSpeed();

            if (currentCameraMove == CameraMove.NORMAL)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                RotateCamera();
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
        void MoveCamera()
        {
			float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? moveSpeed * 2f : moveSpeed;
			float inputX = InControlSingleton.Instance.playerActions.Move.X;
            float inputZ = InControlSingleton.Instance.playerActions.Move.Y;
			Vector3 toMove = transform.right * inputX * currentSpeed * Time.deltaTime +
				 transform.forward * inputZ * currentSpeed * Time.deltaTime;

			transform.position += toMove;
        }
        void ManageMoveSpeed()
        {
            // Camera speed: Alt + Mouse Wheel only
            float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
            if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Mathf.Abs(scrollDelta) > 0.0001f)
            {
                if (scrollDelta > 0)
                {
                    moveSpeed += SPEED_CHANGE_RATE;
                    moveSpeed = Mathf.Clamp(moveSpeed, MIN_MOVE_SPEED, MAX_MOVE_SPEED);
                    Logger.DebugLog("New move speed: " + moveSpeed);

                    // Show notification
                    if (NotificationSystem.Instance != null)
                    {
                        NotificationSystem.Instance.ShowNotification($"Camera speed: {moveSpeed:0.###}", "WhiteSquare");
                    }
                }
                else if (scrollDelta < 0)
                {
                    moveSpeed -= SPEED_CHANGE_RATE;
                    moveSpeed = Mathf.Clamp(moveSpeed, MIN_MOVE_SPEED, MAX_MOVE_SPEED);
                    Logger.DebugLog("New move speed: " + moveSpeed);

                    // Show notification
                    if (NotificationSystem.Instance != null)
                    {
                        NotificationSystem.Instance.ShowNotification($"Camera speed: {moveSpeed:0.###}", "WhiteSquare");
                    }
                }

                EditorUIManager.Instance.UpdateStatsLabel();
            }
            // Restore Shift speed doubling logic
            if (Input.GetKey(KeyCode.LeftShift))
            {
                downAndUpSpeed = moveSpeed * 2f;
            }
            else
            {
                downAndUpSpeed = moveSpeed;
            }
        }

        void MoveCameraWithMouseDrag()
        {
            if (Input.GetMouseButtonDown(2))
            {
                dragOrigin = Input.mousePosition;
                return;
            }

            float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
            if (scrollDelta > 0)
            {
                dragSpeed += 0.05f;
                Logger.DebugLog("New drag speed: " + dragSpeed);
            }
            else if (scrollDelta < 0)
            {
                dragSpeed -= 0.05f;
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

            xRotation -= mouseY * mouseSensivility;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            yRotation += mouseX * mouseSensivility;

            Vector3 toRotate = new Vector3(xRotation, yRotation, 0f);
            transform.localRotation = Quaternion.Euler(toRotate);
        }

        public void SetRotation(Vector3 eulerAngles)
        {
            xRotation = eulerAngles.x;
            yRotation = eulerAngles.y;
			rotationInitialized = true;
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
