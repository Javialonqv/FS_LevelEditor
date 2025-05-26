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
        // Returns "Fractal_Space" atlas.
        public static UIAtlas fractalSpaceAtlas
        {
            get
            {
                // It's one of the objects I found it uses the Fractal_Space atlas...
                return GameObject.Find("MainMenu/Camera/Holder/Main/LargeButtons/1_Resume").GetComponent<UISprite>().atlas;
            }
        }

        public static GameObject CreateInputField(Transform parent, Vector3 position, Vector3Int size, int fontSize = 27, string defaultText = "",
            bool hasOutline = false, NGUIText.Alignment alignment = NGUIText.Alignment.Left)
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
            bgSprite.depth = 1;

            // Create the outline AFTER the main sprite, so the main sprite is the default result when using GetComponent.
            if (hasOutline)
            {
                UISprite outlineSprite = inputField.AddComponent<UISprite>();
                outlineSprite.atlas = fractalSpaceAtlas;
                outlineSprite.spriteName = "Square";
                outlineSprite.color = Color.black;
                outlineSprite.width = size.x + 10;
                outlineSprite.height = size.y + 10;
                outlineSprite.depth = 0;
            }

            UILabel label = inputField.AddComponent<UILabel>();
            label.font = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label").GetComponent<UILabel>().font;
            label.fontSize = fontSize;
            label.width = size.x - 5;
            label.height = size.y;
            label.depth = 2;
            label.alignment = alignment;
            label.color = Color.gray;

            BoxCollider collider = inputField.AddComponent<BoxCollider>();
            collider.center = Vector3.zero;
            collider.size = size;

            UIInput input = inputField.AddComponent<UIInput>();
            input.label = label;
            input.defaultText = defaultText;
            input.activeTextColor = Color.white;
            input.onChange.Clear();

            return inputField;
        }

        // Never ever ever dare to change ANYTHING inside of this method, it's literally the worst code in the whole mod.
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
            toggleBg.width = string.IsNullOrEmpty(text) ? size.x : size.y;
            toggleBg.height = size.y;

            GameObject.Destroy(toggle.GetComponent<UIWidget>());

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
                toggle.GetChildWithName("Label").GetComponent<UILabel>().width = size.x;
                Vector3 colliderCenter = toggle.GetChildWithName("Label").transform.localPosition;
                colliderCenter.x += toggle.GetChildWithName("Label").GetComponent<UILabel>().width / 2 - (size.y / 2) - 6;
                toggle.GetComponent<BoxCollider>().center = colliderCenter;
                Vector2 colliderSize = new Vector2(size.x + 56, size.y);
                toggle.GetComponent<BoxCollider>().size = colliderSize;

#if SHOW_TOGGLE_COLLISION_BONDS
                GameObject square = new GameObject("Square");
                square.transform.parent = toggle.transform;
                square.transform.localScale = Vector3.one;
                square.transform.localPosition = colliderCenter;
                square.AddComponent<UISprite>().atlas = GameObject.Find("MainMenu/Camera/Holder/Main/LargeButtons/1_Resume").GetComponent<UISprite>().atlas;
                square.GetComponent<UISprite>().spriteName = "Square";
                square.GetComponent<UISprite>().width = (int)colliderSize.x;
                square.GetComponent<UISprite>().height = (int)colliderSize.y;
#endif
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

            // For some reason the buttons have two labels? One is disabled (Button/Label) and the other one is the one being used (Button/Background/Label).
            // UPDATE: We'll still be using that one, for SOME FUCKING REASON if you change the label the button colors start to behave weird... idk...
            GameObject.Destroy(button.GetChildAt("Background/Label").GetComponent<UILocalize>());
            button.GetChildAt("Background/Label").GetComponent<UILabel>().text = text;
            button.GetChildAt("Background/Label").GetComponent<UILabel>().SetAnchor(button, 0, 0, 0, 0);
            // Just change the label anchor so its size is the same as the button size.

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
