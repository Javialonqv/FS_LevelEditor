using FS_LevelEditor.Editor;
using FS_LevelEditor.Editor.UI;
using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FS_LevelEditor.Grouping
{
    [RegisterTypeInIl2Cpp]
    public class GroupManager : MonoBehaviour
    {
        public static GroupManager Instance { get; private set; }

        public List<LE_Group> allGroups = new List<LE_Group>();
        public LE_Group currentlyEditingGroup = null;
        public bool isEditingGroup => currentlyEditingGroup != null;

        // Store original material states for objects made transparent during group editing
        private Dictionary<LE_Object, bool> objectsOriginalTransparentState = new Dictionary<LE_Object, bool>();

        public GroupManager(IntPtr ptr) : base(ptr) { }

        void Awake()
        {
            Instance = this;
        }

        public void RegisterGroup(LE_Group group)
        {
            if (!allGroups.Contains(group))
            {
                allGroups.Add(group);
            }
        }

        public void UnregisterGroup(LE_Group group)
        {
            allGroups.Remove(group);
        }

        /// <summary>
        /// Creates a new group from the currently selected objects
        /// </summary>
        public LE_Group CreateGroupFromSelection()
        {
            var editor = EditorController.Instance;
            if (editor == null) return null;

            List<LE_Object> objectsToGroup = new List<LE_Object>();

            if (editor.multipleObjectsSelected)
            {
                foreach (var objComp in editor.currentSelectedObjsComponents)
                {
                    if (objComp != null && !objComp.isDeleted)
                    {
                        // Don't allow grouping waypoints alone - they should stay with their parent
                        if (LE_Object.IsWaypoint(objComp.objectType.Value)) continue;
                        // Don't allow grouping player spawn
                        if (objComp.objectType == LE_Object.ObjectType.PLAYER_SPAWN) continue;

                        objectsToGroup.Add(objComp);
                    }
                }
            }
            else if (editor.currentSelectedObjComponent != null)
            {
                var objComp = editor.currentSelectedObjComponent;
                if (!objComp.isDeleted && !LE_Object.IsWaypoint(objComp.objectType.Value) &&
                    objComp.objectType != LE_Object.ObjectType.PLAYER_SPAWN)
                {
                    objectsToGroup.Add(objComp);
                }
            }

            if (objectsToGroup.Count < 2)
            {
                Utils.ShowCustomNotificationRed("Select at least 2 objects to create a group", 2f);
                return null;
            }

            // Deselect current selection first
            editor.SetSelectedObj(null);

            // Remove objects from their current parents (unparent from multipleSelectedObjsParent if needed)
            foreach (var obj in objectsToGroup)
            {
                obj.transform.SetParent(editor.levelObjectsParent.transform);
            }

            // Create the group GameObject
            GameObject groupObj = new GameObject();
            groupObj.transform.SetParent(editor.levelObjectsParent.transform);

            LE_Group group = groupObj.AddComponent<LE_Group>();
            group.Initialize(objectsToGroup);

            editor.levelHasBeenModified = true;

            Utils.ShowCustomNotificationRed($"Created {group.groupName}", 1.5f);
            Logger.Log($"Created group \"{group.groupName}\" with {objectsToGroup.Count} objects.");

            return group;
        }

        /// <summary>
        /// Ungroups all objects from a group
        /// </summary>
        public void UngroupObjects(LE_Group group)
        {
            if (group == null) return;

            var editor = EditorController.Instance;
            if (editor == null) return;

            // Exit group editing mode if we're editing this group
            if (currentlyEditingGroup == group)
            {
                ExitGroupEditMode();
            }

            // Deselect if this group is selected
            if (IsGroupSelected(group))
            {
                editor.SetSelectedObj(null);
            }

            string groupName = group.groupName;
            group.Ungroup();

            editor.levelHasBeenModified = true;
            Utils.ShowCustomNotificationRed($"Ungrouped {groupName}", 1.5f);
        }

        /// <summary>
        /// Extends an existing group with additional objects from the current selection
        /// </summary>
        public void ExtendGroupFromSelection()
        {
            var editor = EditorController.Instance;
            if (editor == null || !editor.multipleObjectsSelected) return;

            // Find the group and other objects in the selection
            LE_Group targetGroup = null;
            List<LE_Object> objectsToAdd = new List<LE_Object>();

            foreach (var obj in editor.currentSelectedObjsComponents)
            {
                if (obj == null) continue;

                // Check if this object is part of a group (the group itself)
                var group = obj.transform.parent?.GetComponent<LE_Group>();
                if (group != null && targetGroup == null)
                {
                    targetGroup = group;
                }
                else if (group == null)
                {
                    // This object is not in a group, so it can be added
                    // But first check if it's not already in another group
                    var containingGroup = GetGroupContaining(obj);
                    if (containingGroup == null)
                    {
                        objectsToAdd.Add(obj);
                    }
                }
            }

            // Also check if any selected object IS a group (selected via its gameObject)
            foreach (var selectedObj in editor.currentSelectedObjects)
            {
                if (selectedObj == null) continue;
                var group = selectedObj.GetComponent<LE_Group>();
                if (group != null && targetGroup == null)
                {
                    targetGroup = group;
                }
            }

            if (targetGroup == null)
            {
                Utils.ShowCustomNotificationRed("No group found in selection", 2f);
                return;
            }

            if (objectsToAdd.Count == 0)
            {
                Utils.ShowCustomNotificationRed("No objects to add to group", 2f);
                return;
            }

            // Deselect current selection
            editor.SetSelectedObj(null);

            // Add objects to the group
            foreach (var obj in objectsToAdd)
            {
                targetGroup.AddObject(obj);
            }

            // Select the group
            editor.SetSelectedObj(targetGroup.gameObject);

            editor.levelHasBeenModified = true;
            Utils.ShowCustomNotificationRed($"Added {objectsToAdd.Count} object(s) to {targetGroup.groupName}", 1.5f);
            Logger.Log($"Extended group \"{targetGroup.groupName}\" with {objectsToAdd.Count} objects.");
        }

        /// <summary>
        /// Checks if the current selection contains a group and other non-grouped objects
        /// </summary>
        public bool CanExtendGroup()
        {
            var editor = EditorController.Instance;
            if (editor == null || !editor.multipleObjectsSelected) return false;

            bool hasGroup = false;
            bool hasNonGroupedObjects = false;

            foreach (var obj in editor.currentSelectedObjsComponents)
            {
                if (obj == null) continue;

                // Check if this object is part of a group
                var group = obj.transform.parent?.GetComponent<LE_Group>();
                if (group != null)
                {
                    hasGroup = true;
                }
                else
                {
                    // Check if it's not already in another group
                    var containingGroup = GetGroupContaining(obj);
                    if (containingGroup == null)
                    {
                        hasNonGroupedObjects = true;
                    }
                }
            }

            // Also check if any selected object IS a group
            foreach (var selectedObj in editor.currentSelectedObjects)
            {
                if (selectedObj == null) continue;
                var group = selectedObj.GetComponent<LE_Group>();
                if (group != null)
                {
                    hasGroup = true;
                }
            }

            return hasGroup && hasNonGroupedObjects;
        }

        /// <summary>
        /// Enters group editing mode - makes objects outside the group transparent
        /// </summary>
        public void EnterGroupEditMode(LE_Group group)
        {
            if (group == null || isEditingGroup) return;

            currentlyEditingGroup = group;
            objectsOriginalTransparentState.Clear();

            var editor = EditorController.Instance;
            if (editor == null) return;

            // Make all objects outside the group transparent
            foreach (var obj in editor.currentInstantiatedObjects)
            {
                if (obj == null || obj.isDeleted) continue;

                // Skip objects that are part of the group being edited
                if (group.groupedObjects.Contains(obj)) continue;

                // Skip objects that are already transparent (like inactive objects or waypoints)
                bool wasTransparent = !obj.setActiveAtStart;
                objectsOriginalTransparentState[obj] = wasTransparent;

                if (!wasTransparent)
                {
                    obj.gameObject.SetTransparentMaterials();
                }
            }

            // Also handle other groups
            foreach (var otherGroup in allGroups)
            {
                if (otherGroup == group || otherGroup == null || otherGroup.isDeleted) continue;

                foreach (var obj in otherGroup.groupedObjects)
                {
                    if (obj == null) continue;

                    bool wasTransparent = !obj.setActiveAtStart;
                    objectsOriginalTransparentState[obj] = wasTransparent;

                    if (!wasTransparent)
                    {
                        obj.gameObject.SetTransparentMaterials();
                    }
                }
            }

            // Deselect the group and allow selection of individual objects within
            editor.SetSelectedObj(null);

            Utils.ShowCustomNotificationRed($"Editing {group.groupName} - Click outside to exit", 2f);
            Logger.Log($"Entered edit mode for group \"{group.groupName}\"");
        }

        /// <summary>
        /// Exits group editing mode - restores transparency states
        /// </summary>
        public void ExitGroupEditMode()
        {
            if (!isEditingGroup) return;

            var editor = EditorController.Instance;

            // Restore original transparency states
            foreach (var kvp in objectsOriginalTransparentState)
            {
                if (kvp.Key == null) continue;

                // Only restore opaque if it wasn't originally transparent
                if (!kvp.Value)
                {
                    kvp.Key.gameObject.SetOpaqueMaterials();
                }
            }

            objectsOriginalTransparentState.Clear();

            Logger.Log($"Exited edit mode for group \"{currentlyEditingGroup?.groupName}\"");
            currentlyEditingGroup = null;

            Utils.ShowCustomNotificationRed("Exited group editing", 1f);
        }

        /// <summary>
        /// Checks if a click is inside the currently editing group
        /// </summary>
        public bool IsClickInsideEditingGroup(GameObject clickedObj)
        {
            if (!isEditingGroup || clickedObj == null) return false;

            // Check if clicked object is part of the editing group
            var leObj = clickedObj.GetComponent<LE_Object>();
            if (leObj != null)
            {
                return currentlyEditingGroup.groupedObjects.Contains(leObj);
            }

            // Check if it's a child of any object in the group
            foreach (var groupedObj in currentlyEditingGroup.groupedObjects)
            {
                if (groupedObj != null && clickedObj.transform.IsChildOf(groupedObj.transform))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the group that a LE_Object belongs to, if any
        /// </summary>
        public LE_Group GetGroupContaining(LE_Object obj)
        {
            if (obj == null) return null;

            foreach (var group in allGroups)
            {
                if (group != null && !group.isDeleted && group.groupedObjects.Contains(obj))
                {
                    return group;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the group that is currently selected in the editor
        /// </summary>
        public LE_Group GetSelectedGroup()
        {
            var editor = EditorController.Instance;
            if (editor == null || editor.currentSelectedObj == null) return null;

            return editor.currentSelectedObj.GetComponent<LE_Group>();
        }

        /// <summary>
        /// Checks if a specific group is currently selected
        /// </summary>
        public bool IsGroupSelected(LE_Group group)
        {
            if (group == null) return false;

            var editor = EditorController.Instance;
            return editor != null && editor.currentSelectedObj == group.gameObject;
        }

        /// <summary>
        /// Selects a group in the editor
        /// </summary>
        public void SelectGroup(LE_Group group)
        {
            if (group == null) return;

            var editor = EditorController.Instance;
            if (editor == null) return;

            // Exit group edit mode if we're editing a different group
            if (isEditingGroup && currentlyEditingGroup != group)
            {
                ExitGroupEditMode();
            }

            editor.SetSelectedObj(group.gameObject);
        }

        /// <summary>
        /// Called when level is being loaded to reset state
        /// </summary>
        public void OnLevelLoad()
        {
            allGroups.Clear();
            currentlyEditingGroup = null;
            objectsOriginalTransparentState.Clear();
            LE_Group.nextGroupID = 1;
        }

        /// <summary>
        /// Gets all groups for saving
        /// </summary>
        public List<LE_GroupData> GetGroupsDataForSave()
        {
            var groupsData = new List<LE_GroupData>();

            foreach (var group in allGroups)
            {
                if (group == null || group.isDeleted) continue;

                group.BeforeSave();
                groupsData.Add(new LE_GroupData(group));
            }

            return groupsData;
        }

        /// <summary>
        /// Restores groups from save data
        /// </summary>
        public void LoadGroupsFromData(List<LE_GroupData> groupsData, List<LE_Object> allObjects)
        {
            if (groupsData == null) return;

            foreach (var groupData in groupsData)
            {
                // Find the objects that belong to this group
                var objectsForGroup = new List<LE_Object>();
                foreach (var objRef in groupData.groupedObjectRefs)
                {
                    var matchingObj = allObjects.FirstOrDefault(o =>
                        o != null &&
                        o.objectType == objRef.objectType &&
                        o.objectID == objRef.objectID);

                    if (matchingObj != null)
                    {
                        objectsForGroup.Add(matchingObj);
                    }
                }

                if (objectsForGroup.Count == 0) continue;

                // Create the group
                var editor = EditorController.Instance;
                if (editor == null) continue;

                GameObject groupObj = new GameObject();
                groupObj.transform.SetParent(editor.levelObjectsParent.transform);
                groupObj.transform.localPosition = groupData.groupPosition;
                groupObj.transform.localEulerAngles = groupData.groupRotation;
                groupObj.transform.localScale = groupData.groupScale;

                LE_Group group = groupObj.AddComponent<LE_Group>();
                group.groupID = groupData.groupID;
                group.groupName = groupData.groupName;
                group.setActiveAtStart = groupData.setActiveAtStart;
                group.collision = groupData.collision;
                group.gameObject.name = groupData.groupName;

                // Ensure next ID is always higher than any loaded ID
                if (groupData.groupID >= LE_Group.nextGroupID)
                {
                    LE_Group.nextGroupID = groupData.groupID + 1;
                }

                // Add objects to group
                foreach (var obj in objectsForGroup)
                {
                    obj.transform.SetParent(group.transform);
                    group.groupedObjects.Add(obj);
                }

                RegisterGroup(group);
            }
        }

        void OnDestroy()
        {
            Instance = null;
        }
    }
}
