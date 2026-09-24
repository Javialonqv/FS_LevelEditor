using System;
using System.Collections.Generic;
using UnityEngine;

namespace FS_LevelEditor.LE_Objects
{
    public class RegisteredObjectData
    {
        // Intended to be used so all the hardcodded stuff (LE_Laser, for e.g., for whatever reason)
        public string Classname { get; set; }
        // Shows up in BuildUI
        public string DisplayName { get; set; }
        // It's in the name
        public string SnapCategory { get; set; }
        // The name of the prefab in the .JSON.
        public GameObject PrefabTemplate { get; set; }
        // Reference for the .dll with the behavior
        public Type LogicComponentType { get; set; }
        // In case it needs one
        public Type CustomWaypointSupportType { get; set; }
        // Self-explainatory
        public Vector3 DefaultScale { get; set; }
        // Events that can be gone with the object
        public string[] EventsIDs { get; set; }
        // Default settings
        public Dictionary<string, object> DefaultProperties { get; set; }
    }
}
