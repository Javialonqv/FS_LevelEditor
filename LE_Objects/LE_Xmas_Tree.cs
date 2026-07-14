using FS_LevelEditor.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Xmas_Tree : LE_Object
    {
        GameObject presentsParent;

        public static Dictionary<string, object> GetDefaultProperties()
        {
            return new Dictionary<string, object>
            {
                { "Presents", true }
            };
        }

        void Awake()
        {
            presentsParent = contentObject.GetChild("PresentsPack");
        }

        public override void ObjectStart(LEScene scene)
        {
            SetPresentsState(GetProperty<bool>("Presents"));

            base.ObjectStart(scene);
        }

        public override bool SetProperty(string name, object value)
        {
            if (name == "Presents")
            {
                if (value is bool boolValue)
                {
                    properties["Presents"] = boolValue;
                    if (EditorController.Instance)
                        SetPresentsState(boolValue);
                    return true;
                }
            }

            return base.SetProperty(name, value);
        }

        void SetPresentsState(bool enabled)
        {
            presentsParent.SetActive(enabled);
        }
    }
}
