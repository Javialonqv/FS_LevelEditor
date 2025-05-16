using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor.UI_Related
{
    public static class NGUI_Utils
    {
        public static GameObject CreateInputField(Transform parent, Vector3 position, Vector3Int size, int fontSize)
        {
            GameObject inputField = new GameObject("InputField");
            inputField.transform.parent = parent;
            inputField.transform.localPosition = position;
            inputField.transform.localScale = Vector3.one;
            
            UISprite bgSprite = inputField.AddComponent<UISprite>();
            bgSprite.atlas = GameObject.Find("MainMenu/Camera/Holder/Main/LargeButtons/1_Resume").GetComponent<UISprite>().atlas;
            bgSprite.spriteName = "Square";
            bgSprite.color = new Color(0.0588f, 0.3176f, 0.3215f, 0.9412f);
            bgSprite.width = size.x;
            bgSprite.height = size.y;

            UILabel label = inputField.AddComponent<UILabel>();
            label.font = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label").GetComponent<UILabel>().font;
            label.fontSize = fontSize;
            label.width = size.x;
            label.height = size.y;
            label.depth = 1;

            BoxCollider collider = inputField.AddComponent<BoxCollider>();
            collider.center = Vector3.zero;
            collider.size = size;

            UIInput input = inputField.AddComponent<UIInput>();
            input.label = label;

            return inputField;
        }

        public static GameObject CreateToggle(Transform parent, Vector3 position, Vector3Int size, string text = "")
        {
            GameObject toggleTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles");

            GameObject toggle = GameObject.Instantiate(toggleTemplate, parent);
            toggle.name = "Toggle";
            toggle.transform.localPosition = position;
            toggle.transform.localScale = Vector3.one;

            UIToggle toggleScript = toggle.GetComponent<UIToggle>();
            toggleScript.onChange.Clear();

            UISprite toggleBg = toggle.GetChildWithName("Background").GetComponent<UISprite>();
            toggleBg.width = size.x;
            toggleBg.height = size.y;

            if (string.IsNullOrEmpty(text))
            {
                GameObject.Destroy(toggle.GetChildWithName("Label"));
                toggleBg.transform.localPosition = Vector3.zero;
                toggle.GetComponent<BoxCollider>().center = Vector3.zero;
                toggle.GetComponent<BoxCollider>().size = size;
            }
            else
            {
                GameObject.Destroy(toggle.GetChildWithName("Label").GetComponent<UILocalize>());
                toggle.GetChildWithName("Label").GetComponent<UILabel>().text = text;
            }

            return toggle;
        }

        public static GameObject CreateButton(Transform parent, Vector3 position, Vector3Int size, string text = "")
        {
            GameObject buttonTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Controls_Options/Buttons/RemapControls");

            GameObject button = GameObject.Instantiate(buttonTemplate, parent);
            button.transform.localPosition = position;
            button.transform.localScale = Vector3.one;

            button.GetComponent<UISprite>().width = size.x;
            button.GetComponent<UISprite>().height = size.y;
            button.GetComponent<BoxCollider>().size = size;
            GameObject.Destroy(button.GetComponent<ButtonController>());

            GameObject.Destroy(button.GetChildAt("Background/Label").GetComponent<UILocalize>());
            button.GetChildAt("Background/Label").GetComponent<UILabel>().text = text;

            return button;
        }

        public static EventDelegate.Parameter CreateEventDelegateParamter(UnityEngine.Object target, string parameterName, Il2CppSystem.Object value)
        {
            return new EventDelegate.Parameter
            {
                field = parameterName,
                value = value,
                obj = target
            };
        }

        public static EventDelegate CreateEvenDelegate(MonoBehaviour target, string methodName, params EventDelegate.Parameter[] parameters)
        {
            EventDelegate eventDelegate = new EventDelegate(target, methodName);
            eventDelegate.mParameters = parameters;

            return eventDelegate;
        }

        public static char ValidateNonNegativeFloat(string text, int charIndex, char addedChar)
        {
            if (char.IsDigit(addedChar) || addedChar == '.')
            {
                if (addedChar == '.' && text.Contains('.'))
                {
                    return '\0';
                }
                return addedChar;
            }

            return '\0';
        }
        public static char ValidateNonNegativeInt(string text, int charIndex, char addedChar)
        {
            if (char.IsDigit(addedChar))
            {
                return addedChar;
            }

            return '\0';
        }
    }
}
