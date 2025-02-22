using Il2Cpp;
using Il2CppInControl.NativeDeviceProfiles;
using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Il2CppSystem.Linq.Expressions.Interpreter.CastInstruction.CastInstructionNoT;

namespace FS_LevelEditor
{
    public static class Utilities
    {
        static Coroutine customNotificationCoroutine;

        public static bool theresAnInputFieldSelected
        {
            get
            {
                if (UICamera.selectedObject != null)
                {
                    return UICamera.selectedObject.TryGetComponent<UIInput>(out var input);
                }

                return false;
            }
        }

        public static GameObject[] GetChilds(this GameObject obj, bool includeInactive = true)
        {
            List<GameObject> children = new List<GameObject>();

            for (int i = 0; i < obj.transform.childCount; i++)
            {
                GameObject child = obj.transform.GetChild(i).gameObject;
                if (child.activeSelf || includeInactive)
                {
                    children.Add(child);
                }
            }

            return children.ToArray();
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
        public static void DisableAllChildren(this GameObject obj)
        {
            foreach (GameObject child in GetChilds(obj))
            {
                child.SetActive(false);
            }
        }
        public static void EnableAllChildren(this GameObject obj)
        {
            foreach (GameObject child in GetChilds(obj))
            {
                child.SetActive(true);
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
            if (theresAnInputFieldSelected)
            {
                return true;
            }

            if (UICamera.hoveredObject != null)
            {
                return UICamera.hoveredObject.name != "MainMenu";
            }

            return false;
        }

        public static void PlayIgnoringTimeScale(this TweenAlpha tween, bool reversed)
        {
            tween.ignoreTimeScale = true;
            if (reversed) tween.PlayReverse(); else tween.PlayForward();
        }
        public static void PlayIgnoringTimeScale(this TweenScale tween, bool reversed)
        {
            tween.ignoreTimeScale = true;
            if (reversed) tween.PlayReverse(); else tween.PlayForward();
        }

        public static void CangeChildIndex(this GameObject child, int newIndex)
        {
            if (child.transform.parent == null)
            {
                Debug.LogError("The GameObject has no parent!");
                return;
            }

            Transform parent = child.transform.parent;
            int childCount = parent.childCount;

            // Make sure te new index is inside of the child count of the parent.
            newIndex = Mathf.Clamp(newIndex, 0, childCount - 1);

            // Change the child index.
            child.transform.SetSiblingIndex(newIndex);
        }
        public static void ChangeChildIndexToLastOne(this GameObject child)
        {
            if (child.transform.parent == null)
            {
                Debug.LogError("The GameObject has no parent!");
                return;
            }

            Transform parent = child.transform.parent;
            int lastIndex = parent.childCount - 1;

            // Move the child to the last index.
            child.transform.SetSiblingIndex(lastIndex);
        }

        public static void ShowCustomNotificationRed(string msg, float delay)
        {
            if (customNotificationCoroutine != null)
            {
                MelonCoroutines.Stop(customNotificationCoroutine);
            }

            customNotificationCoroutine = (UnityEngine.Coroutine)MelonCoroutines.Start(Coroutine());
            IEnumerator Coroutine()
            {
                // Get the variable.
                GameObject notificationPanel = GameObject.Find("(singleton) InGameUIManager/Camera/Panel/Notifications");
                // For some reason once going back to menu after playing a normal chapter, notificatons panel is disabled, we need to enable it manually again.
                notificationPanel.GetComponent<UIPanel>().enabled = true;

                // Set the red color in the sprites.
                notificationPanel.GetChildAt("Holder/Background").GetComponent<UISprite>().color = new Color32(255, 120, 120, 160);
                notificationPanel.GetChildAt("Holder/BorderLines").GetComponent<UISprite>().color = new Color32(255, 120, 120, 255);

                // Play the notification sound.
                InGameUIManager.Instance.m_uiAudioSource.PlayOneShot(InGameUIManager.Instance.m_notificationSound_bad);

                // Enable the panel and start the fade in.
                notificationPanel.SetActive(true);
                TweenAlpha.Begin(notificationPanel, 0.2f, 1f);
                // Set the text and start the typing effect while the fade is occurring.
                notificationPanel.GetChildAt("Holder/Label").GetComponent<UILabel>().text = "";
                notificationPanel.GetChildAt("Holder/Label").GetComponent<UILabel>().text = msg;
                notificationPanel.GetChildAt("Holder/Label").GetComponent<TypewriterEffect>().ResetToBeginning();

                // Wait the delay and then fade out the panel again.
                yield return new WaitForSecondsRealtime(delay);
                TweenAlpha.Begin(notificationPanel, 0.2f, 0f);

                // After the fade out is done, disable the object again.
                yield return new WaitForSecondsRealtime(0.2f);
                notificationPanel.SetActive(false);
            }
        }

        public static bool ListHasMultipleObjectsWithSameID(List<LE_Object> levelObjects)
        {
            HashSet<string> seenIds = new HashSet<string>();

            foreach (var obj in levelObjects)
            {
                if (!seenIds.Add(obj.objectFullNameWithID))
                {
                    Logger.Error($"There's already an object of name \"{obj.objectOriginalName}\" with ID: {obj.objectID}.");
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Converts a hex string value into a Unity Color.
        /// </summary>
        /// <param name="hexValue">The hex value WITHOUT the '#' sufix.</param>
        /// <returns>The converted hex value into Color.</returns>
        public static Color? HexToColor(string hexValue, bool throwExceptionIfInvalid = true, Color? defaultValue = null)
        {
            if (ColorUtility.TryParseHtmlString("#" + hexValue, out Color color))
            {
                return color;
            }
            else
            {
                if (throwExceptionIfInvalid)
                {
                    Logger.Error($"Couldn't convert the hex value \"{hexValue}\" to Color. Returning white.");
                }
                return defaultValue;
            }
        }
        public static string ColorToHex(Color color)
        {
            int r = Mathf.RoundToInt(color.r * 255);
            int g = Mathf.RoundToInt(color.g * 255);
            int b = Mathf.RoundToInt(color.b * 255);

            return $"{r:X2}{g:X2}{b:X2}";
        }

        public static object ConvertFromSerializableValue(object value)
        {
            if (value is Vector3Serializable)
            {
                return (Vector3)(Vector3Serializable)value;
            }
            else if (value is ColorSerializable)
            {
                return (Color)(ColorSerializable)value;
            }

            return value;
        }
    }
}
