using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor.Editor
{
    public enum GizmosArrow { None, X, Y, Z }

    [MelonLoader.RegisterTypeInIl2Cpp]
    public class EditorGizmo : MonoBehaviour
    {
        public EditorGizmo Instance;

        GameObject xObj, yObj, zObj;

        GizmosArrow currentHighlightedArrow = GizmosArrow.None;
        static readonly Dictionary<GizmosArrow, Color> arrowBaseColors = new Dictionary<GizmosArrow, Color>()
        {
            { GizmosArrow.X, new Color(0.89f, 0.27f, 0.20f, 1f) },
            { GizmosArrow.Y, new Color(0.25f, 0.78f, 0.35f, 1f) },
            { GizmosArrow.Z, new Color(0.20f, 0.52f, 0.89f, 1f) },
        };
        static readonly Dictionary<GizmosArrow, Color> arrowHighlightedColors = new Dictionary<GizmosArrow, Color>()
        {
            { GizmosArrow.X, new Color(0.956f, 0.708f, 0.68f) },
            { GizmosArrow.Y, new Color(0.7f, 0.912f, 0.74f) },
            { GizmosArrow.Z, new Color(0.68f, 0.808f, 0.956f) },
        };

        void Awake()
        {
            Instance = this;

            xObj = transform.GetChild(0).gameObject;
            yObj = transform.GetChild(1).gameObject;
            zObj = transform.GetChild(2).gameObject;
        }

        void OnDestroy()
        {
            Instance = null;
        }

        public GizmosArrow GetHoveredArrow(out Ray usedRay)
        {
             usedRay = Camera.main.ScreenPointToRay(Input.mousePosition);

            return GetHoveredArrow(usedRay);
        }
        public GizmosArrow GetHoveredArrow(Ray ray)
        {
            if (!gameObject.activeSelf) return GizmosArrow.None;

            string collidedArrowName = "";

            RaycastHit[] hits = Physics.RaycastAll(ray);
            foreach (var hit in hits)
            {
                string hitName = hit.collider.name;
                if ((hitName == "Shaft" || hitName == "Cone") && hit.transform.parent)
                {
                    collidedArrowName = hit.transform.parent.name;
                    break;
                }
            }

            GizmosArrow arrow;
            switch (collidedArrowName)
            {
                case "X": arrow = GizmosArrow.X; break;
                case "Y": arrow = GizmosArrow.Y; break;
                case "Z": arrow = GizmosArrow.Z; break;
                default: return GizmosArrow.None;
            }

            return arrow;
        }

        public void HighlightArrow(GizmosArrow arrow, bool unhighlightOthers = true)
        {
            if (unhighlightOthers)
            {
                UnhighlightAllArrows();
            }

            if (arrow == GizmosArrow.None) return;

            GameObject targetArrow = null;
            switch (arrow)
            {
                case GizmosArrow.X: targetArrow = xObj; break;
                case GizmosArrow.Y: targetArrow = yObj; break;
                case GizmosArrow.Z: targetArrow = zObj; break;
            }

            MeshRenderer shaftRenderer = targetArrow.transform.GetChild(0).GetComponent<MeshRenderer>();
            MeshRenderer coneRenderer = targetArrow.transform.GetChild(1).GetComponent<MeshRenderer>();

            shaftRenderer.material.color = arrowHighlightedColors[arrow];
            coneRenderer.material.color = arrowHighlightedColors[arrow];
        }
        public void UnhighlightArrow(GizmosArrow arrow)
        {
            if (arrow == GizmosArrow.None) return;

            GameObject targetArrow = null;
            switch (arrow)
            {
                case GizmosArrow.X: targetArrow = xObj; break;
                case GizmosArrow.Y: targetArrow = yObj; break;
                case GizmosArrow.Z: targetArrow = zObj; break;
            }

            MeshRenderer shaftRenderer = targetArrow.transform.GetChild(0).GetComponent<MeshRenderer>();
            MeshRenderer coneRenderer = targetArrow.transform.GetChild(1).GetComponent<MeshRenderer>();

            shaftRenderer.material.color = arrowBaseColors[arrow];
            coneRenderer.material.color = arrowBaseColors[arrow];
        }
        public void UnhighlightAllArrows()
        {
            UnhighlightArrow(GizmosArrow.X);
            UnhighlightArrow(GizmosArrow.Y);
            UnhighlightArrow(GizmosArrow.Z);
        }

        public void SetPosition(Vector3 newPosition)
        {
            transform.position = newPosition;
        }
        public void SetRotation(Quaternion newRotation)
        {
            transform.rotation = newRotation;
        }
        public void SetScale(Vector3 newScale)
        {
            transform.localScale = newScale;
        }

        public void ScaleRelativeToCamera(Transform currentSelectedObj)
        {
            float distance = Vector3.Distance(Camera.main.transform.position, transform.position);
            float baseArrowScale = 2f;
            float scaleFactor = Mathf.Max(0.1f, distance * 0.15f);

            SetScale(Vector3.one * baseArrowScale * scaleFactor);
        }
    }
}
