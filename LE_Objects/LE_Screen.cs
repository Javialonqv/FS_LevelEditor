using FS_LevelEditor.Editor;
using FS_LevelEditor.Editor.UI;
using Il2Cpp;
using Il2CppTMPro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor
{
    public enum ScreenColorType
    {
        CYAN = 0,
        GREEN = 1,
        RED = 2
    }

    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Screen : LE_Object
    {
        public static Color textCyanColor = new Color(0.184f, 0.9297f, 1f);
        public static Color textGreenColor = new Color(0.3255f, 1f, 0.5765f);
        public static Color textRedColor = new Color(1, 0.4f, 0.3765f);

        ScreenController screen;

        GameObject wholeMesh;
        GameObject greenMesh, redMesh;
        TextMeshPro screenText;

        void Awake()
        {
            properties = new Dictionary<string, object>()
            {
                { "ColorType", ScreenColorType.CYAN },
                { "InvisibleMesh", false },
                { "InvertWithGravity", true },
                { "AutoFontSize", true },
                { "FontSize", 185f },
                { "MinFontSize", 60f },
                { "MaxFontSize", 185f },
                { "Text", "Text" }
            };

            wholeMesh = gameObject.GetChildAt("Content/Mesh");
            greenMesh = gameObject.GetChildAt("Content/Mesh/GreenPlane");
            redMesh = gameObject.GetChildAt("Content/Mesh/RedPlane");
            screenText = gameObject.GetChildAt("Content/Content/Label/MainLabel").GetComponent<TextMeshPro>();
        }

        public override void ObjectStart(LEScene scene)
        {
            // No matter the scene (Editor/Playmode) change the mesh.
            SetScreenMeshVisibility(GetProperty<bool>("InvisibleMesh"));
            SetScreenColor(GetProperty<ScreenColorType>("ColorType"));
            SetScreenText(GetProperty<string>("Text"));
            UpdateScreenTextFont();
        }

        public override void InitComponent()
        {
            GameObject content = gameObject.GetChildWithName("Content");

            content.SetActive(false);

            screen = content.AddComponent<ScreenController>();
            screen.m_content = content.GetChildWithName("Content").transform;
            screen.m_contentAnim = content.GetChildWithName("Content").GetComponent<Animation>();
            screen.m_screenRenderer = content.GetChildWithName("Mesh").GetComponent<MeshRenderer>();
            screen.redColorPlane = content.GetChildAt("Mesh/RedPlane");
            screen.greenColorPlane = content.GetChildAt("Mesh/GreenPlane");
            screen.m_mainLabelTMP = content.GetChildAt("Content/Label/MainLabel").GetComponent<TextMeshPro>();
            screen.m_mainLabelRenderer = content.GetChildAt("Content/Label/MainLabel").GetComponent<MeshRenderer>();
            screen.m_secondaryLabelTMP = content.GetChildAt("Content/Label/SecondaryLabel").GetComponent<TextMeshPro>();
            screen.m_secondaryLabelRenderer = content.GetChildAt("Content/Label/SecondaryLabel").GetComponent<MeshRenderer>();
            screen.m_lockdownLabelTMP = content.GetChildAt("Content/Label/LockdownLabel").GetComponent<TextMeshPro>();
            screen.m_lockdownLabelRenderer = content.GetChildAt("Content/Label/LockdownLabel").GetComponent<MeshRenderer>();
            screen.firstEnableEver = true;
            screen.redColor = t_screen.redColor;
            screen.greenColor = t_screen.greenColor;
            screen.cyanColor = t_screen.cyanColor;
            screen.whiteColor = Color.white;
            screen.useColorTint = true;
            screen.currentColor = ConvertColorToFSType(GetProperty<ScreenColorType>("ColorType"));

            screen.m_contentAnim.clip = t_screen.m_contentAnim.clip;
            foreach (var clip in t_screen.m_contentAnim)
            {
                AnimationState state = clip.Cast<AnimationState>();
                screen.m_contentAnim.AddClip(state.clip, state.name);
            }

            screen.m_mainLabelRenderer.material = t_screen.m_mainLabelRenderer.material;
            screen.m_mainLabelTMP.m_fontAsset = t_screen.m_mainLabelTMP.m_fontAsset;
            screen.m_mainLabelTMP.m_sharedMaterial = t_screen.m_mainLabelTMP.m_sharedMaterial;

            screen.m_secondaryLabelRenderer.material = t_screen.m_secondaryLabelRenderer.material;
            screen.m_secondaryLabelTMP.m_fontAsset = t_screen.m_secondaryLabelTMP.m_fontAsset;
            screen.m_secondaryLabelTMP.m_sharedMaterial = t_screen.m_secondaryLabelTMP.m_sharedMaterial;

            screen.m_lockdownLabelRenderer.material = t_screen.m_lockdownLabelRenderer.material;
            screen.m_lockdownLabelTMP.m_fontAsset = t_screen.m_lockdownLabelTMP.m_fontAsset;
            screen.m_lockdownLabelTMP.m_sharedMaterial = t_screen.m_lockdownLabelTMP.m_sharedMaterial;

            content.SetActive(true);

            initialized = true;
        }

        public override bool SetProperty(string name, object value)
        {
            if (name == "ColorType")
            {
                if (value is int)
                {
                    properties["ColorType"] = (ScreenColorType)value;
                    SetScreenColor((ScreenColorType)value);
                    return true;
                }
                else if (value is ScreenColorType)
                {
                    properties["ColorType"] = value;
                    SetScreenColor((ScreenColorType)value);
                    return true;
                }
            }
            else if (name == "InvisibleMesh")
            {
                if (value is bool)
                {
                    properties["InvisibleMesh"] = (bool)value;
                    SetScreenMeshVisibility((bool)value);
                    return true;
                }
            }
            else if (name == "InvertWithGravity")
            {
                if (value is bool)
                {
                    properties["InvertWithGravity"] = (bool)value;
                    return true;
                }
            }
            else if (name == "AutoFontSize")
            {
                if (value is bool)
                {
                    properties["AutoFontSize"] = (bool)value;
                    return true;
                }
            }
            else if (name == "FontSize")
            {
                if (value is string)
                {
                    if (Utilities.TryParseFloat((string)value, out float result))
                    {
                        properties["FontSize"] = result;
                        return true;
                    }
                }
                else if (value is float)
                {
                    properties["FontSize"] = (float)value;
                    return true;
                }
            }
            else if (name == "MinFontSize")
            {
                if (value is string)
                {
                    if (Utilities.TryParseFloat((string)value, out float result))
                    {
                        properties["MinFontSize"] = result;
                        return true;
                    }
                }
                else if (value is float)
                {
                    properties["MinFontSize"] = (float)value;
                    return true;
                }
            }
            else if (name == "MaxFontSize")
            {
                if (value is string)
                {
                    if (Utilities.TryParseFloat((string)value, out float result))
                    {
                        properties["MaxFontSize"] = result;
                        return true;
                    }
                }
                else if (value is float)
                {
                    properties["MaxFontSize"] = (float)value;
                    return true;
                }
            }
            else if (name == "Text")
            {
                properties["Text"] = value.ToString();
                if (PlayModeController.Instance)
                {
                    SetScreenText(value.ToString()); // Only requires manually update in playmode.
                }

                // Since this will convert the value to string no matter what, it'll catch the JsonElement before base.SetProperty() does, so, skip the warning in case it is
                // JsonElement.
                if (value is not string && value is not JsonElement)
                {
                    Logger.Warning($"The value wasn't a string, that's not expected, the value type was \"{value.GetType().Name}\".");
                }
            }

            return base.SetProperty(name, value);
        }
        public override bool TriggerAction(string actionName)
        {
            if (actionName == "EditText")
            {
                TextEditorUI.Instance.ShowTextEditor(this);
                return true;
            }
            else if (actionName == "OnTextEditorClose")
            {
                // NOT update the screen mesh color since that's in another property that's NOT in the text editor.
                SetScreenText(GetProperty<string>("Text"));
                UpdateScreenTextFont();
                return true;
            }

            else if (actionName == "InvertText")
            {
                if (screen.isInverted)
                {
                    screen.SetNormalLabel();
                }
                else
                {
                    screen.SetInvertedLabel();
                }
            }

            return base.TriggerAction(actionName);
        }

        void SetScreenMeshVisibility(bool setInvisible)
        {
            wholeMesh.SetActive(!setInvisible);
        }
        void SetScreenColor(ScreenColorType colorType)
        {
            if (EditorController.Instance)
            {
                if (colorType == ScreenColorType.CYAN)
                {
                    greenMesh.SetActive(false);
                    redMesh.SetActive(false);

                    screenText.color = textCyanColor;
                }
                else if (colorType == ScreenColorType.GREEN)
                {
                    greenMesh.SetActive(true);
                    redMesh.SetActive(false);

                    screenText.color = textGreenColor;
                }
                else // Only RED is left.
                {
                    greenMesh.SetActive(false);
                    redMesh.SetActive(true);

                    screenText.color = textRedColor;
                }
            }
            else if (screen)
            {
                screen.currentColor = ConvertColorToFSType(colorType);

                switch (colorType)
                {
                    case ScreenColorType.CYAN:
                        screen.SetCyanBG();
                        break;

                    case ScreenColorType.GREEN:
                        screen.SetGreenBG();
                        break;

                    case ScreenColorType.RED:
                        screen.SetRedBG();
                        break;
                }
            }
        }
        ScreenController.ColorType ConvertColorToFSType(ScreenColorType colorType)
        {
            switch (colorType)
            {
                case ScreenColorType.CYAN:
                    return ScreenController.ColorType.CYAN;

                case ScreenColorType.GREEN:
                    return ScreenController.ColorType.GREEN;

                case ScreenColorType.RED:
                    return ScreenController.ColorType.RED;

                default:
                    return ScreenController.ColorType.CYAN;
            }
        }

        void UpdateScreenTextFont()
        {
            screenText.enableAutoSizing = GetProperty<bool>("AutoFontSize");

            if (GetProperty<bool>("AutoFontSize"))
            {
                screenText.fontSizeMin = GetProperty<float>("MinFontSize");
                screenText.fontSizeMax = GetProperty<float>("MaxFontSize");
            }
            else
            {
                screenText.fontSize = GetProperty<float>("FontSize");
            }
        }
        void SetScreenText(string newText)
        {
            screenText.text = newText;
        }
    }
}
