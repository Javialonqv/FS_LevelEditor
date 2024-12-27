using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor
{
    public static class Utilities
    {
        public static GameObject[] GetChilds(this GameObject obj)
        {
            GameObject[] array = new GameObject[obj.transform.childCount];

            for (int i = 0; i < obj.transform.childCount; i++)
            {
                array[i] = obj.transform.GetChild(i).gameObject;
            }

            return array;
        }

        public static Transform GetChildWithName(this Transform tr, string name)
        {
            foreach (GameObject child in GetChilds(tr.gameObject))
            {
                if (child.name == name) return child.transform;
            }

            return null;
        }
        public static GameObject GetChildWithName(this GameObject obj, string name)
        {
            foreach (GameObject child in GetChilds(obj))
            {
                if (child.name == name) return child;
            }

            return null;
        }

        public static GameObject GetChildAt(this GameObject obj, string path)
        {
            string[] childNames = path.Split('/');
            GameObject currentChild = obj;

            foreach (string name in childNames)
            {
                if (name == "..")
                {
                    currentChild = currentChild.transform.parent.gameObject;
                }
                else
                {
                    currentChild = GetChildWithName(currentChild, name);
                }
            }

            return currentChild;
        }

        public static void DeleteAllChildren(this GameObject obj)
        {
            foreach (GameObject child in GetChilds(obj))
            {
                GameObject.Destroy(child);
            }
        }

        public static T[] TryGetComponents<T>(this GameObject obj) where T : Component
        {
            if (obj.TryGetComponent<T>(out T component))
            {
                return obj.GetComponents<T>();
            }
            else
            {
                return obj.GetComponentsInChildren<T>();
            }
        }

        public static Vector3 GetMousePositionInWorld()
        {
            Vector3 mouseScreenPosition = Input.mousePosition;
            mouseScreenPosition.z = Camera.main.nearClipPlane;
            return Camera.main.ScreenToWorldPoint(mouseScreenPosition);
        }
    }
}
