using Il2Cpp;
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
        public static bool ExistsChildWithName(this GameObject obj, string name)
        {
            foreach (GameObject child in GetChilds(obj))
            {
                if (child.name == name) return true;
            }

            return false;
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

        public static bool ItsTheOnlyHittedObjectByRaycast(Ray ray, float rayDistance, GameObject desiredObj)
        {
            RaycastHit[] hits = Physics.RaycastAll(ray, rayDistance);
            bool objFound = false;

            foreach (var hit in hits)
            {
                if (hit.collider != null)
                {
                    if (hit.collider.gameObject == desiredObj)
                    {
                        objFound = true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return objFound;
        }

        /// <summary>
        /// Removes a component from an object.
        /// </summary>
        /// <typeparam name="T">The component type to remove.</typeparam>
        /// <returns>If a component was found and could be deleted.</returns>
        public static bool RemoveComponent<T>(this GameObject obj) where T : Component
        {
            if (obj.TryGetComponent<T>(out T component))
            {
                GameObject.Destroy(component);
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool IsMouseOverUIElement()
        {
            if (UICamera.hoveredObject != null)
            {
                return UICamera.hoveredObject.name != "MainMenu";
            }

            return false;
        }
    }
}
