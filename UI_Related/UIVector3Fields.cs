using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor.UI_Related
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class UIVector3Fields : MonoBehaviour
    {
        public UICustomInputField xField;
        public UICustomInputField yField;
        public UICustomInputField zField;

        public Action<string> onSelected;
        public Action<string> onChange;
        public Action<string> onDeselected;

        public UIVector3Fields(IntPtr ptr) : base(ptr) { }

        public void Assign(UICustomInputField xField, UICustomInputField yField, UICustomInputField zField)
        {
            this.xField = xField;
            this.yField = yField;
            this.zField = zField;

            SetupField(xField, "X");
            SetupField(yField, "Y");
            SetupField(zField, "Z");
        }
        void SetupField(UICustomInputField field, string axis)
        {
            field.onSelected += () => OnFieldSelected(true, axis);
            field.onChange += () => OnChange(axis);
            field.onDeselected += () => OnFieldSelected(false, axis);
        }

        void OnFieldSelected(bool selected, string axis)
        {
            if (selected && onSelected != null)
            {
                onSelected.Invoke(axis);
            }
            else if (onDeselected != null)
            {
                onDeselected.Invoke(axis);
            }
        }

        void OnChange(string axis)
        {
            if (onChange != null)
            {
                onChange.Invoke(axis);
            }
        }

        public Vector3 GetVector()
        {
            Utils.TryParseFloat(xField.GetText(), out float x);
            Utils.TryParseFloat(yField.GetText(), out float y);
            Utils.TryParseFloat(zField.GetText(), out float z);

            return new Vector3(x, y, z);
        }
        public void SetVector(Vector3 vector, int maxDecimals = 3, bool executeOnChange = true)
        {
            xField.SetText(vector.x, maxDecimals, executeOnChange);
            yField.SetText(vector.y, maxDecimals, executeOnChange);
            zField.SetText(vector.z, maxDecimals, executeOnChange);
        }
    }
}
