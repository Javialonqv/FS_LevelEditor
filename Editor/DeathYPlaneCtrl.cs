using UnityEngine;

namespace FS_LevelEditor.Editor
{

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
