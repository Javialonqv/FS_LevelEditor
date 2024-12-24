using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MelonLoader;

namespace FS_LevelEditor
{
    [RegisterTypeInIl2Cpp]
    public class EditorController : MonoBehaviour
    {
        GameObject currentSelectedObj = null;

        GameObject previewObject = null;

        void Start()
        {
            currentSelectedObj = Melon<Core>.Instance.groundObj;

            previewObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            previewObject.GetComponent<BoxCollider>().enabled = false;
            previewObject.transform.localScale = Vector3.one * 0.5f;
        }

        void Update()
        {
            if (!Input.GetMouseButton(1))
            {
                PreviewObject();
            }
        }

        void PreviewObject()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity))
            {
                Melon<Core>.Logger.Msg("hit");

                previewObject.transform.position = hit.point;
            }
        }
    }
}
