using FS_LevelEditor.Editor;
using FS_LevelEditor.Playmode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class SingleObjectLink : MonoBehaviour
    {
        public virtual LE_Object.ObjectType? targetObjectType => null;

        public LineRenderer editorLine;
        public LE_Object targetObject;
        public LE_Object mainObject => GetComponent<LE_Object>();

        void Awake()
        {
            if (EditorController.Instance) CreateEditorLine();
        }
        void CreateEditorLine()
        {
            if (!editorLine)
            {
                editorLine = Instantiate(Core.LoadOtherObjectInBundle("EditorLine"), transform).GetComponent<LineRenderer>();
                editorLine.transform.localPosition = Vector3.zero;
                editorLine.transform.localScale = Vector3.one;
                editorLine.startColor = Color.yellow;
                editorLine.endColor = Color.yellow;
                editorLine.gameObject.SetActive(false);
            }
        }

        public bool SetTargetObject(int objectID)
        {
            if (targetObject && objectID == targetObject.objectID) return true;

            List<LE_Object> objectsList = null;
            if (EditorController.Instance)
                objectsList = EditorController.Instance.currentInstantiatedObjects;
            else if (PlayModeController.Instance)
                objectsList = PlayModeController.Instance.currentInstantiatedObjects;

            if (objectsList == null) return false;

            LE_Object newTarget = objectsList.Find(obj => obj.objectType == targetObjectType && obj.objectID == objectID);

            if (targetObject)
            {
                targetObject.otherObjThisIsLinkedTo = null;
            }

            targetObject = newTarget;

            if (targetObject)
            {
                targetObject.otherObjThisIsLinkedTo = this;
            }

            mainObject.OnObjectLinkTargetChanged(newTarget);

            return newTarget != null;
        }

        void Update()
        {
            if (editorLine && targetObject)
            {
                if (!editorLine.enabled) editorLine.enabled = true;
                editorLine.SetPosition(0, transform.position);
                editorLine.SetPosition(1, targetObject.transform.position);
            }
            if (editorLine && !targetObject) editorLine.enabled = false;
        }
        public void OnSelect()
        {
            editorLine.gameObject.SetActive(true);
        }
        public void OnDeselect()
        {
            editorLine.gameObject.SetActive(false);
        }
    }
}
