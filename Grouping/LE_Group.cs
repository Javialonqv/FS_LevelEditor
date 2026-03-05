using FS_LevelEditor.Editor;
using FS_LevelEditor.Editor.UI;
using FS_LevelEditor.Playmode;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FS_LevelEditor.Grouping
{
    [RegisterTypeInIl2Cpp]
    public class LE_Group : MonoBehaviour
    {
        public static int nextGroupID = 1;

        public int groupID;
        public string groupName;
        public bool setActiveAtStart = true;
        public bool collision = true;

        public List<LE_Object> groupedObjects = new List<LE_Object>();
        public bool isDeleted = false;

        // For tracking original parents when grouping
        private Dictionary<LE_Object, Transform> originalParents = new Dictionary<LE_Object, Transform>();

        public LE_Group(IntPtr ptr) : base(ptr) { }

        public string GroupFullName => $"{groupName}";

        public void Initialize(List<LE_Object> objects, string customName = null)
        {
            groupID = nextGroupID++;
            groupName = customName ?? $"Group {groupID}";
            gameObject.name = groupName;

            // Store original parents and add objects to group
            foreach (var obj in objects)
            {
                if (obj == null || obj.isDeleted) continue;

                originalParents[obj] = obj.transform.parent;
                obj.transform.SetParent(transform);
                groupedObjects.Add(obj);
            }

            // Position the group at the center of all objects
            RecalculateCenter();

            // Register with GroupManager
            GroupManager.Instance?.RegisterGroup(this);

            // Register with EditorController's instantiated objects tracking
            if (EditorController.Instance != null)
            {
                // Groups are tracked separately in GroupManager
            }
        }

        public void RecalculateCenter()
        {
            if (groupedObjects.Count == 0) return;

            // Calculate center position
            Vector3 center = Vector3.zero;
            foreach (var obj in groupedObjects)
            {
                if (obj != null)
                    center += obj.transform.position;
            }
            center /= groupedObjects.Count;

            // Move group to center without moving children (relatively)
            Vector3 offset = center - transform.position;
            transform.position = center;

            // Adjust children positions to compensate
            foreach (var obj in groupedObjects)
            {
                if (obj != null)
                    obj.transform.position -= offset;
            }
        }

        public void AddObject(LE_Object obj)
        {
            if (obj == null || groupedObjects.Contains(obj)) return;

            originalParents[obj] = obj.transform.parent;
            obj.transform.SetParent(transform);
            groupedObjects.Add(obj);
            RecalculateCenter();
        }

        public void RemoveObject(LE_Object obj)
        {
            if (obj == null || !groupedObjects.Contains(obj)) return;

            if (originalParents.TryGetValue(obj, out Transform originalParent))
            {
                obj.transform.SetParent(originalParent ?? EditorController.Instance?.levelObjectsParent.transform);
                originalParents.Remove(obj);
            }
            else
            {
                obj.transform.SetParent(EditorController.Instance?.levelObjectsParent.transform);
            }

            groupedObjects.Remove(obj);

            // If group is empty, destroy it
            if (groupedObjects.Count == 0)
            {
                Ungroup();
            }
            else
            {
                RecalculateCenter();
            }
        }

        public void Ungroup()
        {
            // Restore all objects to their original parents
            foreach (var obj in groupedObjects.ToList())
            {
                if (obj == null) continue;

                if (originalParents.TryGetValue(obj, out Transform originalParent))
                {
                    obj.transform.SetParent(originalParent ?? EditorController.Instance?.levelObjectsParent.transform);
                }
                else
                {
                    obj.transform.SetParent(EditorController.Instance?.levelObjectsParent.transform);
                }
            }

            groupedObjects.Clear();
            originalParents.Clear();

            // Unregister from GroupManager
            GroupManager.Instance?.UnregisterGroup(this);

            Destroy(gameObject);
        }

        public void SetObjectsColor(LE_Object.LEObjectContext context)
        {
            foreach (var obj in groupedObjects)
            {
                if (obj != null)
                    obj.SetObjectColor(context);
            }
        }

        public void OnSelect()
        {
            SetObjectsColor(LE_Object.LEObjectContext.SELECT);

            // Make objects opaque if they were transparent
            foreach (var obj in groupedObjects)
            {
                if (obj != null && obj.canBeDisabledAtStart)
                    obj.gameObject.SetOpaqueMaterials();
            }
        }

        public void OnDeselect()
        {
            SetObjectsColor(LE_Object.LEObjectContext.NORMAL);

            // Restore transparency state based on setActiveAtStart
            foreach (var obj in groupedObjects)
            {
                if (obj != null && obj.canBeDisabledAtStart)
                {
                    if (!obj.setActiveAtStart)
                        obj.gameObject.SetTransparentMaterials();
                    else
                        obj.gameObject.SetOpaqueMaterials();
                }
            }
        }

        public void OnDelete()
        {
            isDeleted = true;
            gameObject.SetActive(false);

            // Mark all grouped objects as deleted too
            foreach (var obj in groupedObjects)
            {
                if (obj != null)
                {
                    obj.isDeleted = true;
                    obj.gameObject.SetActive(false);
                }
            }
        }

        public void OnUndoDeletion()
        {
            isDeleted = false;
            gameObject.SetActive(true);

            // Restore all grouped objects
            foreach (var obj in groupedObjects)
            {
                if (obj != null)
                {
                    obj.isDeleted = false;
                    obj.gameObject.SetActive(obj.setActiveAtStart);
                }
            }
        }

        public void SetActiveAtStart(bool active)
        {
            setActiveAtStart = active;

            // Also update all objects in the group
            foreach (var obj in groupedObjects)
            {
                if (obj != null)
                {
                    obj.setActiveAtStart = active;
                    if (obj.canBeDisabledAtStart)
                    {
                        if (!active)
                            obj.gameObject.SetTransparentMaterials();
                        else
                            obj.gameObject.SetOpaqueMaterials();
                    }
                }
            }
        }

        public void SetCollision(bool hasCollision)
        {
            collision = hasCollision;

            foreach (var obj in groupedObjects)
            {
                if (obj != null)
                {
                    obj.collision = hasCollision;
                    obj.SetCollidersState(hasCollision);
                }
            }
        }

        public void SetInvisibleMesh(bool invisible)
        {
            foreach (var obj in groupedObjects)
            {
                if (obj != null)
                {
                    obj.invisibleMesh = invisible;
                    obj.SetMeshRenderersState(!invisible);
                }
            }
        }

        /// <summary>
        /// Returns true if all objects in the group can have waypoints
        /// </summary>
        public bool CanHaveWaypoints()
        {
            return groupedObjects.All(obj => obj != null && obj.canHaveWaypoints);
        }

        /// <summary>
        /// Returns true if all objects in the group have the same invisibleMesh state
        /// </summary>
        public bool? GetInvisibleMeshState()
        {
            if (groupedObjects.Count == 0) return null;

            bool first = groupedObjects[0]?.invisibleMesh ?? false;
            foreach (var obj in groupedObjects)
            {
                if (obj != null && obj.invisibleMesh != first)
                    return null; // Mixed state
            }
            return first;
        }

        public void BeforeSave()
        {
            foreach (var obj in groupedObjects)
            {
                obj?.BeforeSave();
            }
        }

        /// <summary>
        /// Gets the bounding box of all objects in the group
        /// </summary>
        public Bounds GetGroupBounds()
        {
            if (groupedObjects.Count == 0)
                return new Bounds(transform.position, Vector3.zero);

            Bounds bounds = new Bounds();
            bool first = true;

            foreach (var obj in groupedObjects)
            {
                if (obj == null) continue;

                var renderers = obj.gameObject.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    if (first)
                    {
                        bounds = renderer.bounds;
                        first = false;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }
            }

            return bounds;
        }
    }
}
