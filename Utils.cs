using FS_LevelEditor.SaveSystem;
using Il2Cpp;
using Il2CppI2.Loc;
using Il2CppInControl.NativeDeviceProfiles;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FS_LevelEditor
{
    public static class Utils
    {
        static Coroutine customNotificationCoroutine;

        static Dictionary<string, Coroutine> invokeCoroutines = new Dictionary<string, Coroutine>();

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

        public static bool IsUnity6
        {
            get
            {
                return Application.unityVersion.StartsWith("6000");
            }
        }

        public static GameObject[] GetChilds(this GameObject obj, bool includeInactive = true)
        {
            if (!obj) return Array.Empty<GameObject>();

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

        public static Transform GetChild(this Transform tr, string name)
        {
            foreach (GameObject child in GetChilds(tr.gameObject))
            {
                if (child.name == name) return child.transform;
            }

            return null;
        }
        public static GameObject GetChild(this GameObject obj, string name)
        {
            foreach (GameObject child in GetChilds(obj))
            {
                if (child.name == name) return child;
            }

            return null;
        }
        public static bool ExistsChild(this GameObject obj, string name)
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
                    currentChild = GetChild(currentChild, name);
                }
            }

            return currentChild;
        }

        public static void DeleteAllChildren(this GameObject obj, bool immediate = false)
        {
            foreach (GameObject child in GetChilds(obj))
            {
                if (immediate) GameObject.DestroyImmediate(child);
                else GameObject.Destroy(child);
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

        public static T[] TryGetComponents<T>(this GameObject obj, bool includeInactive = false) where T : Component
        {
            List<T> components = new List<T>();

            if (obj.TryGetComponent<T>(out T component))
            {
                components.AddRange(obj.GetComponents<T>());
            }
            components.AddRange(obj.GetComponentsInChildren<T>(includeInactive));

            return components.ToArray();
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
        public static void PlayIgnoringTimeScale(this TweenPosition tween, bool reversed)
        {
            tween.ignoreTimeScale = true;
            if (reversed) tween.PlayReverse(); else tween.PlayForward();
        }

        public static void SetDirection(this UITweener tween, Il2CppAnimationOrTween.Direction direction)
        {
            if (direction == Il2CppAnimationOrTween.Direction.Forward)
            {
                tween.mAmountPerDelta = Mathf.Abs(tween.amountPerDelta);
            }
            else if (direction == Il2CppAnimationOrTween.Direction.Reverse)
            {
                tween.mAmountPerDelta = -Mathf.Abs(tween.amountPerDelta);
            }
            if (direction == Il2CppAnimationOrTween.Direction.Toggle)
            {
                tween.mAmountPerDelta = -tween.amountPerDelta;
            }
        }
        public static void SetSample(this UITweener tween, float factor, bool isFinished)
        {
            tween.Sample(factor, isFinished);
            tween.tweenFactor = factor;
        }

        public static void ChangeChildIndex(this GameObject child, int newIndex)
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
                GameObject notificationPanel = InGameUIManager.Instance.notificationPanel.gameObject;
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
                var notificationLabel = notificationPanel.GetChildAt("Holder/Label").GetComponent<UILabel>();
                notificationLabel.text = "";
                notificationLabel.text = msg;
                notificationLabel.GetComponent<TypewriterEffect>().ResetToBeginning();

                // Wait the delay and then fade out the panel again.
                yield return new WaitForSecondsRealtime(delay);
                TweenAlpha.Begin(notificationPanel, 0.2f, 0f);

                // After the fade out is done, disable the object again.
                yield return new WaitForSecondsRealtime(0.2f);
                notificationPanel.SetActive(false);
            }
        }

        public static bool ListHasMultipleObjectsWithSameID(List<LE_Object> levelObjects, bool printError = true)
        {
            HashSet<string> seenIds = new HashSet<string>();

            foreach (var obj in levelObjects)
            {
                if (LE_Object.IsWaypoint(obj.objectType.Value)) continue;

                if (!seenIds.Add(obj.objectFullNameWithID))
                {
                    if (printError)
                    {
                        Logger.Error($"There's already an object of type \"{obj.objectType}\" with ID: {obj.objectID}.");
                    }
                    return true;
                }
            }

            return false;
        }
        public static bool ListHasMultipleObjectsWithSameID(List<LE_ObjectData> levelObjects, bool printError = true)
        {
            HashSet<string> seenIds = new HashSet<string>();

            foreach (var obj in levelObjects)
            {
                if (LE_Object.IsWaypoint(obj.objectType.Value)) continue;

                string toAdd = obj.objectType + " " + obj.objectID;
                if (!seenIds.Add(toAdd))
                {
                    if (printError)
                    {
                        Logger.Error($"There's already an object of name \"{obj.objectType}\" with ID: {obj.objectID}.");
                    }
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

        public enum FS_UISound
        {
            POPUP_UI_SHOW,
            POPUP_UI_HIDE,
            INTERACTION_AVAILABLE,
            INTERACTION_UNAVAILABLE,
            SHOW_NEW_PAGE_SOUND
        }
        public static void PlayFSUISound(FS_UISound sound)
        {
            if (sound == FS_UISound.POPUP_UI_SHOW || sound == FS_UISound.POPUP_UI_HIDE)
            {
                PopupController popup = MenuController.GetInstance().m_popupController;
                AudioClip toPlay = sound == FS_UISound.POPUP_UI_SHOW ? popup.showPopupSound : popup.hidePopupSound;
                popup.audioSourceToUse.PlayOneShot(toPlay);
            }
            else if (sound == FS_UISound.INTERACTION_AVAILABLE || sound == FS_UISound.INTERACTION_UNAVAILABLE)
            {
                AudioClip toPlay = sound == FS_UISound.INTERACTION_AVAILABLE ? InGameUIManager.Instance.interactionAvailableSound :
                    InGameUIManager.Instance.interactionNoLongerAvailableSound;
                MenuController.GetInstance().m_uiAudioSource.PlayOneShot(toPlay);
            }
            else if (sound == FS_UISound.SHOW_NEW_PAGE_SOUND)
            {
                MenuController.GetInstance().m_uiAudioSource.PlayOneShot(MenuController.GetInstance().showNewPageSound);
            }
        }

        public static void SetXRotation(this Transform transform, float newValue)
        {
            transform.localEulerAngles = new Vector3(newValue, transform.localEulerAngles.y, transform.localEulerAngles.z);
        }
        public static void SetYRotation(this Transform transform, float newValue)
        {
            transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, newValue, transform.localEulerAngles.z);
        }
        public static void SetZRotation(this Transform transform, float newValue)
        {
            transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y, newValue);
        }

        public static void SetXScale(this Transform transform, float newValue)
        {
            transform.localScale = new Vector3(newValue, transform.localScale.y, transform.localScale.z);
        }
        public static void SetYScale(this Transform transform, float newValue)
        {
            transform.localScale = new Vector3(transform.localScale.x, newValue, transform.localScale.z);
        }
        public static void SetZScale(this Transform transform, float newValue)
        {
            transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, newValue);
        }

        public static bool TryParseFloat(string text, out float result)
        {
            if (float.TryParse(text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float value))
            {
                result = value;
                return true;
            }
            else
            {
                result = 0f;
                return false;
            }
        }

        public static float ParseFloat(string text, bool throwErrorIfCantParse = false)
        {
            if (float.TryParse(text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float value))
            {
                return value;
            }
            else
            {
                if (throwErrorIfCantParse) Logger.Error($"Couldn't parse \"{text}\" to float!");
                return value;
            }
        }

        public static string FloatToString(float value)
        {
            return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        public static bool CallStaticMethodIfExists(Type type, string methodName, out object result)
        {
            if (type == null)
            {
                result = null;
                return false;
            }

            var flags = BindingFlags.Static
                | BindingFlags.Public
                | BindingFlags.NonPublic;

            MethodInfo method = type.GetMethod(methodName, flags);
            if (method != null)
            {
                result = method.Invoke(null, null);
                return true;
            }

            result = null;
            return false;
        }
        public static bool IsOverridingMethod(Type type, string methodName)
        {
            var flags = BindingFlags.Instance
                  | BindingFlags.Public
                  | BindingFlags.NonPublic
                  | BindingFlags.DeclaredOnly;

            return type.GetMethod(methodName, flags) != null;
        }
        public static void CallMethodIfOverrided(Type baseType, object instance, string methodName, params object[] parms)
        {
            var flags = BindingFlags.Instance
                  | BindingFlags.Public
                  | BindingFlags.NonPublic;

            MethodInfo method = instance.GetType().GetMethod(methodName, flags);
            if (method.DeclaringType != baseType)
            {
                method.Invoke(instance, parms);
            }
        }
        public static void CallMethod(this object instance, string methodName, params object[] parms)
        {
            var flags = BindingFlags.Instance
                  | BindingFlags.Public
                  | BindingFlags.NonPublic;

            MethodInfo method = instance.GetType().GetMethod(methodName, flags);
            if (method != null) method.Invoke(instance, parms);
        }

        public static float HighestValueOfVector(Vector3 vector)
        {
            return Mathf.Max(vector.x, Mathf.Max(vector.y, vector.z));
        }

        public static object CreateCopyOf(object value)
        {
            switch (value)
            {
                case int i:
                    return i;
                case float f:
                    return f;
                case string s:
                    return s;
                case bool b:
                    return b;

                case IList list:
                    var newList = (IList)Activator.CreateInstance(list.GetType());
                    foreach (var item in list)
                    {
                        newList.Add(CreateCopyOf(item));
                    }
                    return newList;

                case LE_SawWaypointSerializable waypoint:
                    return new LE_SawWaypointSerializable(waypoint);

                case LE_Event @event:
                    return new LE_Event(@event);

                case WaypointData waypoint:
                    return new WaypointData(waypoint);

                case UpgradeSaveData upgrade:
                    return new UpgradeSaveData(upgrade);
            }

            if (value.GetType().IsValueType)
            {
                Logger.Warning($"Couldn't copy object of type \"{value.GetType().Name}\", but it's an struct so who cares, " +
                    $"don't worry user, everything's fine :)");
            }
            else
            {
                Logger.Error($"Couldn't copy object of type \"{value.GetType().Name}\", returning the reference, but could case some trouble.");
            }
            return value;
        }

        public static T FindObjectOfType<T>(Func<T, bool> predicate = null) where T : Component
        {
            Object[] array = GameObject.FindObjectsOfTypeAll(Il2CppType.From(typeof(T)));
            if (predicate == null)
            {
                return array[0].Cast<T>();
            }
            else
            {
                foreach (var obj in array)
                {
                    T casted = obj.Cast<T>();
                    if (predicate.Invoke(casted))
                    {
                        return casted;
                    }
                }
            }

            return null;
        }
        public static T[] FindObjectsOfTypeIncludingDisabled<T>() where T : Component
        {
            Object[] array = GameObject.FindObjectsOfTypeAll(Il2CppType.From(typeof(T)));

            return array.Select(obj => obj.Cast<T>()).ToArray();
        }

        public static string ObjectTypeToFormatedName(LE_Object.ObjectType objectType)
        {
            string withSpaces = objectType.ToString().Replace("_", " ");

            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(withSpaces.ToLowerInvariant());
        }
        public static (string type, int id) SplitTypeAndId(string input)
        {
            input = input.Trim();

            int lastSpace = input.LastIndexOf(' ');
            if (lastSpace != -1 && lastSpace < input.Length - 1)
            {
                string idPart = input.Substring(lastSpace + 1);
                if (int.TryParse(idPart, out int id))
                {
                    string typePart = input.Substring(0, lastSpace);
                    return (typePart, id);
                }
            }

            return (input, 0);
        }

        public static string SanitizeFileName(string fileName, string replacement = "_", bool collapse = true)
        {
            if (string.IsNullOrEmpty(fileName)) return string.Empty;

            char[] invalidChars = Path.GetInvalidFileNameChars();
            string invalidPattern = "[" + Regex.Escape(new string(invalidChars)) + "]";

            string cleaned = Regex.Replace(fileName, invalidPattern, replacement);

            if (collapse && !string.IsNullOrEmpty(replacement))
            {
                string repEscaped = Regex.Escape(replacement);
                cleaned = Regex.Replace(cleaned, repEscaped + "+", replacement);
            }

            // Remove spaces and replacements at the start and end of the string.
            return cleaned.Trim().Trim(replacement.ToCharArray()).Trim();
        }

        public static void SetLocKey(this UILocalize localize, string key)
        {
            localize.key = key;
            localize.OnLocalize();
        }
        public static void SetLocKey(this UILabel label, string key)
        {
            if (label.TryGetComponent<UILocalize>(out var localize))
            {
                localize.key = key;
                localize.OnLocalize();
            }
        }

		public static void SetChildCollidersState(this GameObject obj, bool state, bool includeInactive = true, params string[] except)
		{
			foreach (var collider in obj.TryGetComponents<Collider>(includeInactive))
			{
				if (except != null && except.Contains(collider.gameObject.name)) continue;
				collider.enabled = state;
			}
		}

		/// <summary>
		/// Gets the hierarchical path of a GameObject from the root to the object.
		/// </summary>
		/// <param name="obj">The GameObject to get the path for.</param>
		/// <param name="separator">The separator to use between path segments. Default is "/".</param>
		/// <param name="includeScene">Whether to include the scene name at the beginning of the path.</param>
		/// <returns>The hierarchical path as a string.</returns>
		public static string GetGameObjectPath(this GameObject obj, string separator = "/", bool includeScene = false)
		{
			if (obj == null) return string.Empty;

			List<string> pathParts = new List<string>();
			Transform current = obj.transform;

			// Traverse up the hierarchy
			while (current != null)
			{
				pathParts.Add(current.name);
				current = current.parent;
			}

			// Reverse to get root-to-object order
			pathParts.Reverse();

			// Optionally include scene name
			if (includeScene && obj.scene.isLoaded)
			{
				pathParts.Insert(0, obj.scene.name);
			}

			return string.Join(separator, pathParts);
		}

        public static void Invoke(Action action, float delay, string id = "")
        {
            Coroutine coroutine = (Coroutine)MelonCoroutines.Start(InvokeCoroutine(action, delay, id));
            if (coroutine == null)
            {
                Logger.Error($"An error occured while trying to start the invoke coroutine. (Delay: {delay}, ID: \"{id}\").");
                return;
            }
            if (!string.IsNullOrEmpty(id))
            {
                invokeCoroutines.Add(id, coroutine);
            }
        }
        public static void CancelInvoke(string id)
        {
            if (invokeCoroutines.ContainsKey(id))
            {
                MelonCoroutines.Stop(invokeCoroutines[id]);
                invokeCoroutines.Remove(id);
            }
            else
            {
                Logger.Warning($"Couldn't find any invoking coroutine with id \"{id}\".");
            }
        }
        static IEnumerator InvokeCoroutine(Action action, float delay, string id)
        {
            yield return new WaitForSeconds(delay);
            action.Invoke();

            if (!string.IsNullOrEmpty(id) && invokeCoroutines.ContainsKey(id))
            {
                invokeCoroutines.Remove(id);
            }
        }

        public static void InvokeAfterOneFrame(Action action)
        {
            MelonCoroutines.Start(InvokeAfterOneFrameCoroutine(action));
        }
        static IEnumerator InvokeAfterOneFrameCoroutine(Action action)
        {
            yield return null;

            action.Invoke();
        }
	}
}
