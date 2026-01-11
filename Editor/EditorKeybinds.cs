using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor.Editor
{
    public static class EditorKeybinds
    {
        // Number keys shortcuts.
        public static bool BuildingMode => Input.GetKeyDown(KeyCode.Alpha1);                                                // 1
        public static bool SelectionMode => Input.GetKeyDown(KeyCode.Alpha2);                                               // 2
        public static bool ToggleWaypointsVisibility => Input.GetKeyDown(KeyCode.Alpha3);                                   // 3
        public static bool ToggleGlobalGizmos => Input.GetKeyDown(KeyCode.Alpha4);                                          // 4
        public static bool ShowLevelMetadataPopup => Input.GetKeyDown(KeyCode.Alpha5) && !Input.GetKey(KeyCode.LeftShift);  // 5
        public static bool ToggleEditorLighting => Input.GetKeyDown(KeyCode.Alpha6);                                        // 6
        public static bool ToggleStatsVisibility => Input.GetKeyDown(KeyCode.Alpha7);                                       // 7

        // In-Editor shortcuts (object manipulation).
        public static bool DuplicateCurrentObject => Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.D);
        public static bool SelectAllObjects => Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.A);
        public static bool ToggleStartSpawnState => Input.GetKeyDown(KeyCode.Space);

        // In-Editor shortcuts.
        public static bool SaveLevel => Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.S);
        public static bool EnterPlaymode => Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.P);

        // UI shortcuts.
        public static bool ToggleHelpPanel => Input.GetKeyDown(KeyCode.F1);
        public static bool HideOrShowCategoryButtons => Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.H);
        public static bool ToggleGlobalProperties => Input.GetKeyDown(KeyCode.O);

        // Grid shortcuts.
        public static bool ToggleGridVisibility => Input.GetKeyDown(KeyCode.G) && !Input.GetKey(KeyCode.LeftShift);
        public static bool ToggleGridState => Input.GetKeyDown(KeyCode.G) && Input.GetKey(KeyCode.LeftShift);
        public static bool AllowedToAdjustGrid => !Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt);
        public static bool ChangeGridSize(out float scrollDelta)
        {
            scrollDelta = Input.GetAxis("Mouse ScrollWheel");

            return Input.GetKey(KeyCode.LeftControl) && Mathf.Abs(scrollDelta) > 0.0001f;
        }
        public static bool ChangeGridHeight(out float scrollDelta)
        {
            scrollDelta = Input.GetAxis("Mouse ScrollWheel");

            return !Input.GetKey(KeyCode.LeftControl) && Mathf.Abs(scrollDelta) > 0.0001f;
        }
        public static bool AdjustGridSizePrecisly => Input.GetKey(KeyCode.LeftShift);
    }
}
