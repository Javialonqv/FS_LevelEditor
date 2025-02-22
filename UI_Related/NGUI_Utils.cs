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
            bgSprite.color = new Color(0f, 0.2215f, 0.2264f, 0.9412f);
            bgSprite.width = size.x;
            bgSprite.height = size.y;

            UILabel label = inputField.AddComponent<UILabel>();
            label.font = GameObject.Find("MainMenu/Camera/Holder/Options/Game_Options/Buttons/Subtitles/Label").GetComponent<UILabel>().font;
            label.fontSize = fontSize;
            label.width = size.x;
            label.height = size.y;

            BoxCollider collider = inputField.AddComponent<BoxCollider>();
            collider.center = Vector3.zero;
            collider.size = size;

            UIInput input = inputField.AddComponent<UIInput>();
            input.label = label;

            return inputField;
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
    }
}
