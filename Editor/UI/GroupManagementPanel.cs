using FS_LevelEditor.Grouping;
using FS_LevelEditor.UI_Related;
using MelonLoader;
using System;
using UnityEngine;

namespace FS_LevelEditor.Editor.UI
{
    /// <summary>
    /// Placeholder for future group management UI functionality.
    /// Currently, group management is handled through the SelectedObjPanel.
    /// </summary>
    [RegisterTypeInIl2Cpp]
    public class GroupManagementPanel : MonoBehaviour
    {
        public static GroupManagementPanel Instance { get; private set; }

        public GroupManagementPanel(IntPtr ptr) : base(ptr) { }

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            Instance = null;
        }
    }
}
