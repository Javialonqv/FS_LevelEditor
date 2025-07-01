using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Il2CppSystem.Linq.Expressions.Interpreter.CastInstruction.CastInstructionNoT;

namespace FS_LevelEditor.UI_Related
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class UICustomInputField : MonoBehaviour
    {
        public enum UIInputType
        {
            HEX_COLOR,
            NON_NEGATIVE_INT,
            NON_NEGATIVE_FLOAT,
            INT,
            FLOAT,
            PLAIN_TEXT
        }

        public UIInput input { get; private set; }
        public UIInputType inputType { get; private set; }
        public bool isValid { get; private set; }
        bool initialized = false;

        UISprite fieldSprite;
        public Color validValueColor { get; private set; } = new Color(0.0588f, 0.3176f, 0.3215f, 0.9412f);
        public Color invalidValueColor { get; private set; } = new Color(0.3215f, 0.2156f, 0.0588f, 0.9415f);
        public bool setFieldColorAutomatically = true;

        public Action onChange;
        public Action onSubmit;

        public UICustomInputField(IntPtr ptr) : base(ptr) { }

        void Awake()
        {
            input = GetComponent<UIInput>();
            fieldSprite = GetComponent<UISprite>();
        }

        public void Setup(UIInputType type, string? defaultText = null, int maxDecimals = 0)
        {
            inputType = type;

            if (!input)
            {
                input = GetComponent<UIInput>();
                fieldSprite = GetComponent<UISprite>();
            }

            switch (type)
            {
                case UIInputType.HEX_COLOR:
                    input.validation = UIInput.Validation.Alphanumeric;
                    input.characterLimit = 6;
                    break;

                case UIInputType.NON_NEGATIVE_INT:
                    input.onValidate = (UIInput.OnValidate)NGUI_Utils.ValidateNonNegativeInt;
                    break;

                case UIInputType.NON_NEGATIVE_FLOAT:
                    if (maxDecimals <= 0)
                    {
                        input.onValidate = (UIInput.OnValidate)NGUI_Utils.ValidateNonNegativeFloat;
                    }
                    else
                    {
                        input.onValidate += (UIInput.OnValidate)((text, index, ch) => NGUI_Utils.ValidateNonNegativeFloatWithMaxDecimals(text, index, ch, maxDecimals));
                    }
                    break;

                case UIInputType.INT:
                    input.validation = UIInput.Validation.Integer;
                    break;

                case UIInputType.FLOAT:
                    if (maxDecimals <= 0)
                    {
                        input.validation = UIInput.Validation.Float;
                    }
                    else
                    {
                        input.onValidate += (UIInput.OnValidate)((text, index, ch) => NGUI_Utils.ValidateFloatWithMaxDecimals(text, index, ch, maxDecimals));
                    }
                    break;

                case UIInputType.PLAIN_TEXT:
                    input.validation = UIInput.Validation.None;
                    break;
            }
            if (defaultText != null) input.defaultText = defaultText;

            if (!initialized)
            {
                EventDelegate.Add(input.onChange, new EventDelegate(this, nameof(OnChange)));
                EventDelegate.Add(input.onSubmit, new EventDelegate(this, nameof(OnSubmit)));
            }

            initialized = true;
        }

        void OnChange()
        {
            if (setFieldColorAutomatically)
            {
                Set(IsValueValid());
            }

            if (onChange != null)
            {
                onChange.Invoke();
            }
        }
        void OnSubmit()
        {
            if (onSubmit != null)
            {
                onSubmit.Invoke();
            }
        }

        public void Set(bool newState)
        {
            isValid = newState;

            if (newState)
            {
                fieldSprite.color = validValueColor;
            }
            else
            {
                fieldSprite.color = invalidValueColor;
            }
        }

        bool IsValueValid()
        {
            switch (inputType)
            {
                case UIInputType.HEX_COLOR:
                    return Utilities.HexToColor(GetText(), false, null) != null;

                case UIInputType.NON_NEGATIVE_INT:
                    if (int.TryParse(GetText(), out int intResult))
                    {
                        return intResult >= 0;
                    }
                    return false;

                case UIInputType.NON_NEGATIVE_FLOAT:
                    if (Utilities.TryParseFloat(GetText(), out float floatResult))
                    {
                        return floatResult >= 0;
                    }
                    return false;

                case UIInputType.INT:
                    return int.TryParse(GetText(), out int intResult2);

                case UIInputType.FLOAT:
                    return Utilities.TryParseFloat(GetText(), out float floatResult2);

                case UIInputType.PLAIN_TEXT:
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the current text of the UIInput, if empty, returns the default text value.
        /// </summary>
        /// <returns></returns>
        public string GetText()
        {
            if (!string.IsNullOrEmpty(input.text))
            {
                return input.text;
            }
            else
            {
                return input.defaultText;
            }
        }

        public void SetText(string newText)
        {
            if (input.selected) return;

            input.text = newText;
        }
        public void SetText(float value)
        {
            if (input.selected) return;

            input.text = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        public void SetText(float value, int maxDecimals)
        {
            if (input.selected) return;

            string format = "0";
            if (maxDecimals > 0)
                format += "." + new string('#', maxDecimals);

            input.text = value.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
