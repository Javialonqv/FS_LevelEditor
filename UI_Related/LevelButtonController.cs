using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LevelButtonController : MonoBehaviour
    {
        public string levelFileNameWithoutExtension = "";
        public string levelName = "";
        public int objectsCount = 0;

        public void OnClick()
        {
            // Don't enter editor while renaming a level - clicking on the input field should just position the cursor.
            if (LE_MenuUIManager.Instance.isRenamingLevel) return;

            LE_MenuUIManager.Instance.EnterEditor(true, levelFileNameWithoutExtension, levelName);
        }
    }
}
