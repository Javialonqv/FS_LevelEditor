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
            LE_MenuUIManager.Instance.LoadLevel(levelFileNameWithoutExtension, levelName);
        }

        public void OnHover(bool _isHovered)
        {
            if (_isHovered)
            {
                LE_MenuUIManager.Instance.levelNameLabel.GetComponent<UILabel>().text = levelName;
                LE_MenuUIManager.Instance.levelNameLabel.SetActive(true);

                LE_MenuUIManager.Instance.levelObjectsLabel.GetComponent<UILabel>().text = $"Objects: {objectsCount}";
                LE_MenuUIManager.Instance.levelObjectsLabel.SetActive(true);
            }
            else
            {
                LE_MenuUIManager.Instance.levelNameLabel.SetActive(false);
                LE_MenuUIManager.Instance.levelObjectsLabel.SetActive(false);
            }
        }
    }
}
