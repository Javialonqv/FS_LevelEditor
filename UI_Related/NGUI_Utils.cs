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
        static UIAtlas _fractalSpaceAtlas;
        public static UIAtlas fractalSpaceAtlas
        {
            get
            {
                if (!_fractalSpaceAtlas)
                {
                    // It's one of the objects I found it uses the Fractal_Space atlas...
                    _fractalSpaceAtlas = GameObject.Find("MainMenu/Camera/Holder/Main/LargeButtons/1_Resume").GetComponent<UISprite>().atlas;
                }
                return _fractalSpaceAtlas;
            }
        }
        static UIAtlas _uiTexturesAtlas;
        public static UIAtlas UITexturesAtlas
        {
            get
            {
                if (!_uiTexturesAtlas)
                {
                    _uiTexturesAtlas = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Background").GetComponent<UISprite>().atlas;
                }
                return _uiTexturesAtlas;
            }
        }

        static UIFont _labelFont;
        public static UIFont labelFont
        {
            get
            {
                if (!_labelFont)
                {
                    _labelFont = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label").GetComponent<UILabel>().font;
                }
                return _labelFont;
            }
        }

        public static Color fsButtonsDefaultColor
        {
            get { return new Color(0.218f, 0.6464f, 0.6509f, 1f); }
        }
        public static Color fsButtonsHoveredColor
        {
            get { return new Color(0f, 0.8314f, 0.8667f, 1f); }
        }
        public static Color fsButtonsPressedColor
        {
            get { return new Color(0.2868f, 0.971f, 1f, 1f); }
        }

        static GameObject _labelTemplate;
        public static GameObject labelTemplate
        {
            get
            {
                if (!_labelTemplate)
                {
                    _labelTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label");
                }
                return _labelTemplate;
            }
        }
        static GameObject _buttonTemplate;
        public static GameObject buttonTemplate
        {
            get
            {
                if (!_buttonTemplate)
                {
                    _buttonTemplate = GameObject.Find("MainMenu/Camera/Holder/Options/Controls_Options/Buttons/RemapControls");
                }
                return _buttonTemplate;
            }
        }
        static GameObject _optionsPanel;
        public static GameObject optionsPanel
        {
            get
            {
                if (!_optionsPanel)
                {
                    _optionsPanel = GameObject.Find("MainMenu/Camera/Holder/Options");
                }
                return _optionsPanel;
            }
        }

        static Camera _mainMenuCamera;
        public static Camera mainMenuCamera
        {
            get
            {
                if (_mainMenuCamera == null) _mainMenuCamera = GameObject.Find("MainMenu/Camera").GetComponent<Camera>();

                return _mainMenuCamera;
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
            bgSprite.atlas = fractalSpaceAtlas;
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

            GameObject labelObj = new GameObject("Text");
            labelObj.transform.parent = inputField.transform;
            labelObj.transform.localPosition = Vector3.zero;
            labelObj.transform.localScale = Vector3.one;
            UILabel label = labelObj.AddComponent<UILabel>();
            label.font = labelFont;
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
        // (Followed by the gizmos arrows, ofc).
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
                UILabel toggleLabel = toggle.GetChildWithName("Label").GetComponent<UILabel>();
                GameObject.Destroy(toggleLabel.GetComponent<UILocalize>());
                toggleLabel.text = text;
                toggleLabel.width = size.x;
                Vector3 colliderCenter = toggleLabel.transform.localPosition;
                colliderCenter.x += toggleLabel.width / 2 - (size.y / 2) - 6;
                toggle.GetComponent<BoxCollider>().center = colliderCenter;
                Vector2 colliderSize = new Vector2(size.x + 56, size.y);
                toggle.GetComponent<BoxCollider>().size = colliderSize;
            }

            toggle.AddComponent<UIToggleCheckedFix>();

            return toggle;
        }

        public static GameObject CreateButton(Transform parent, Vector3 position, Vector3Int size, string text = "")
        {
            GameObject button = GameObject.Instantiate(buttonTemplate, parent);
            button.transform.localPosition = position;
            button.transform.localScale = Vector3.one;

            button.GetComponent<UISprite>().width = size.x;
            button.GetComponent<UISprite>().height = size.y;
            button.GetComponent<BoxCollider>().size = size;
            GameObject.Destroy(button.GetComponent<ButtonController>());

            // For some reason the buttons have two labels? One is disabled (Button/Label) and the other one is the one being used (Button/Background/Label).
            // UPDATE: We'll still be using that one, for SOME FUCKING REASON if you change the label the button colors start to behave weird... idk...
            UILabel buttonLabel = button.GetChildAt("Background/Label").GetComponent<UILabel>();
            GameObject.Destroy(buttonLabel.GetComponent<UILocalize>());
            buttonLabel.text = text;
            buttonLabel.SetAnchor(button, 0, 0, 0, 0);
            // Just change the label anchor so its size is the same as the button size.

            return button;
        }
        public static GameObject CreateButtonWithSprite(Transform parent, Vector3 position, Vector3Int size, int buttonDepth, string spriteName, Vector2Int spriteSize)
        {
            GameObject button = GameObject.Instantiate(buttonTemplate, parent);
            button.transform.localPosition = position;
            button.transform.localScale = Vector3.one;

            button.GetComponent<UISprite>().width = size.x;
            button.GetComponent<UISprite>().height = size.y;
            button.GetComponent<BoxCollider>().size = size;
            GameObject.Destroy(button.GetComponent<ButtonController>());

            GameObject labelObj = button.GetChildAt("Background/Label");
            GameObject.Destroy(labelObj.GetComponent<UILocalize>());
            GameObject.Destroy(labelObj.GetComponent<UILabel>());
            UISprite sprite = labelObj.AddComponent<UISprite>();
            sprite.transform.localPosition = Vector3.zero;
            sprite.transform.parent.localPosition = Vector3.zero;
            sprite.SetExternalSprite(spriteName);
            sprite.width = spriteSize.x;
            sprite.height = spriteSize.y;
            sprite.depth = buttonDepth + 1;

            return button;
        }

        public static GameObject CreateButtonAsToggle(Transform parent, Vector3 position, Vector3Int size, string text = "", int toggleDepth = 0)
        {
            GameObject button = GameObject.Instantiate(buttonTemplate, parent);
            button.transform.localPosition = position;
            button.transform.localScale = Vector3.one;

            button.GetComponent<UISprite>().width = size.x;
            button.GetComponent<UISprite>().height = size.y;
            button.GetComponent<UISprite>().depth = toggleDepth;
            button.GetComponent<BoxCollider>().size = size;
            GameObject.Destroy(button.GetComponent<ButtonController>());
            button.AddComponent<UIButtonAsToggle>();

            // For some reason the buttons have two labels? One is disabled (Button/Label) and the other one is the one being used (Button/Background/Label).
            // UPDATE: We'll still be using that one, for SOME FUCKING REASON if you change the label the button colors start to behave weird... idk...
            UILabel buttonLabel = button.GetChildAt("Background/Label").GetComponent<UILabel>();
            GameObject.Destroy(buttonLabel.GetComponent<UILocalize>());
            buttonLabel.text = text;
            buttonLabel.SetAnchor(button, 0, 0, 0, 0);
            // Just change the label anchor so its size is the same as the button size.

            return button;
        }
        public static GameObject CreateButtonAsToggleWithSprite(Transform parent, Vector3 position, Vector3Int size, int toggleDepth, string spriteName, Vector2Int spriteSize)
        {
            GameObject button = GameObject.Instantiate(buttonTemplate, parent);
            button.transform.localPosition = position;
            button.transform.localScale = Vector3.one;

            button.GetComponent<UISprite>().width = size.x;
            button.GetComponent<UISprite>().height = size.y;
            button.GetComponent<UISprite>().depth = toggleDepth;
            button.GetComponent<BoxCollider>().size = size;
            GameObject.Destroy(button.GetComponent<ButtonController>());
            button.AddComponent<UIButtonAsToggle>();

            GameObject labelObj = button.GetChildAt("Background/Label");
            GameObject.Destroy(labelObj.GetComponent<UILocalize>());
            GameObject.Destroy(labelObj.GetComponent<UILabel>());
            UISprite sprite = labelObj.AddComponent<UISprite>();
            sprite.transform.localPosition = Vector3.zero;
            sprite.transform.parent.localPosition = Vector3.zero;
            sprite.SetExternalSprite(spriteName);
            sprite.width = spriteSize.x;
            sprite.height = spriteSize.y;
            sprite.depth = toggleDepth + 1;

            return button;
        }

        public static UILabel CreateLabel(Transform parent, Vector3 position, Vector3Int size, string text = "", NGUIText.Alignment alignment = NGUIText.Alignment.Left,
            UIWidget.Pivot pivot = UIWidget.Pivot.Left)
        {
            GameObject labelObj = GameObject.Instantiate(labelTemplate, parent);
            labelObj.name = "Label";
            labelObj.RemoveComponent<UILocalize>();

            UILabel label = labelObj.GetComponent<UILabel>();
            label.width = size.x;
            label.height = size.y;
            label.text = text;
            label.color = Color.white;
            label.alignment = alignment;
            label.pivot = pivot;

            labelObj.transform.localPosition = position;

            return label;
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
            if (!char.IsDigit(addedChar) && addedChar != '.')
            {
                return '\0';
            }

            if (addedChar == '.' && text.Contains('.'))
            {
                return '\0';
            }

            return addedChar;
        }
        public static char ValidateNonNegativeInt(string text, int charIndex, char addedChar)
        {
            if (char.IsDigit(addedChar))
            {
                return addedChar;
            }

            return '\0';
        }
        public static char ValidateNonNegativeFloatWithMaxDecimals(string text, int charIndex, char addedChar, int maxDecimals)
        {
            if (!char.IsDigit(addedChar) && addedChar != '.')
            {
                return '\0';
            }

            if (addedChar == '.' && text.Contains('.'))
            {
                return '\0';
            }

            int dotIndex = text.IndexOf('.');
            if (dotIndex != -1)
            {
                int decimals = text.Length - dotIndex;
                if (decimals > 2)
                    return '\0';
            }

            return addedChar;
        }
        public static char ValidateFloatWithMaxDecimals(string text, int charIndex, char addedChar, int maxDecimals)
        {
            // Only accept numbers, dots and negatives (duuno how that's called in english, forgive me lol).
            if (!char.IsDigit(addedChar) && addedChar != '.' && addedChar != '-')
                return '\0';

            // Only accept ONE dot.
            if (addedChar == '.')
            {
                if (text.Contains(".")) return '\0';
                else return addedChar;
            }

            // Only accept ONE negative when it's at the beginning.
            if (addedChar == '-')
            {
                if (text.Contains("-") || charIndex != 0) return '\0';
                else return addedChar;
            }

            // Only accept up to 2 decimals.
            int dotIndex = text.IndexOf('.');
            if (dotIndex != -1)
            {
                int decimals = text.Length - dotIndex;
                if (decimals > 2)
                    return '\0';
            }

            return addedChar;
        }
    }
}
