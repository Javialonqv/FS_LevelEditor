using FS_LevelEditor.SaveSystem.SerializableTypes;
using System;
using System.Collections.Generic;

namespace FS_LevelEditor.Grouping
{
    [Serializable]
    public class LE_GroupData
    {
        public int groupID { get; set; }
        public string groupName { get; set; }
        public bool setActiveAtStart { get; set; } = true;
        public bool collision { get; set; } = true;

        public Vector3Serializable groupPosition { get; set; }
        public Vector3Serializable groupRotation { get; set; }
        public Vector3Serializable groupScale { get; set; } = new Vector3Serializable(UnityEngine.Vector3.one);

        /// <summary>
        /// List of object identifiers (ObjectType + ObjectID) that belong to this group
        /// </summary>
        public List<GroupedObjectReference> groupedObjectRefs { get; set; } = new List<GroupedObjectReference>();

        public LE_GroupData() { }

        public LE_GroupData(LE_Group group)
        {
            groupID = group.groupID;
            groupName = group.groupName;
            setActiveAtStart = group.setActiveAtStart;
            collision = group.collision;

            groupPosition = group.transform.localPosition;
            groupRotation = group.transform.localEulerAngles;
            groupScale = group.transform.localScale;

            foreach (var obj in group.groupedObjects)
            {
                if (obj == null || !obj.objectType.HasValue) continue;

                groupedObjectRefs.Add(new GroupedObjectReference
                {
                    objectType = obj.objectType.Value,
                    objectID = obj.objectID
                });
            }
        }
    }

    [Serializable]
    public class GroupedObjectReference
    {
        public LE_Object.ObjectType objectType { get; set; }
        public int objectID { get; set; }
    }
}
