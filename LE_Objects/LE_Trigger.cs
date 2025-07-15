using FS_LevelEditor.Editor.UI;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Trigger : LE_Object
    {
        void Awake()
        {
            properties = new Dictionary<string, object>
            {
                { "OnEnter", new List<LE_Event>() },
                { "OnExit", new List<LE_Event>() }
            };
        }

        public override void OnInstantiated(LEScene scene)
        {
            if (scene == LEScene.Playmode)
            {
                gameObject.GetChildAt("Content/Mesh").SetActive(false);
            }

            base.OnInstantiated(scene);
        }

        public override void InitComponent()
        {
            GameObject triggerObj = gameObject.GetChildAt("Content/LE_Trigger");
            triggerObj.tag = "Trigger";
            triggerObj.layer = LayerMask.NameToLayer("Ignore Raycast");

            TriggerScript trigger = triggerObj.AddComponent<TriggerScript>();
            trigger.onEnter = new UnityEvent();
            trigger.onEnter.AddListener((UnityAction)ExecuteOnEnterEvents);
            trigger.onExit = new UnityEngine.Events.UnityEvent();
            trigger.onExit.AddListener((UnityAction)ExecuteOnExitEvents);
            trigger.onDestroy = new UnityEvent();
            trigger.BlocSwitchs = new GameObject[0];
            trigger.objectsToActivate = new GameObject[0];
            trigger.objectsToDeactivate = new GameObject[0];
            trigger.objectsToEnableOnly = new GameObject[0];
            trigger.objectsToDestroy = new GameObject[0];
            trigger.doorsToClose = new GameObject[0];
            trigger.lasersToEnable = new Laser_H_Controller[0];
            trigger.lasersToDisable = new Laser_H_Controller[0];
            trigger.dialogToActivate = new string[0];
            trigger.m_messages = new Messenger[0];
            trigger.keepActivated = true;

            initialized = true;
        }

        public override bool SetProperty(string name, object value)
        {
            if (GetAvailableEventsIDs().Contains(name))
            {
                if (value is List<LE_Event>)
                {
                    properties[name] = (List<LE_Event>)value;
                }
            }

            return base.SetProperty(name, value);
        }
        public override bool TriggerAction(string actionName)
        {
            if (actionName == "ManageEvents")
            {
                EventsUIPageManager.Instance.ShowEventsPage(this);
                return true;
            }
            else if (actionName == "OnEventsTabClose")
            {
                eventExecuter.CreateInEditorLinksToTargetObjects();
                return true;
            }

            return base.TriggerAction(actionName);
        }

        public override List<string> GetAvailableEventsIDs()
        {
            return new List<string>()
            {
                "OnEnter",
                "OnExit"
            };
        }

        void ExecuteOnEnterEvents()
        {
            eventExecuter.ExecuteEvents((List<LE_Event>)properties["OnEnter"]);
        }
        void ExecuteOnExitEvents()
        {
            eventExecuter.ExecuteEvents((List<LE_Event>)properties["OnExit"]);
        }

        public static new Color GetDefaultObjectColor(LEObjectContext context)
        {
            return new Color(1f, 1f, 0.07843138f);
        }
    }
}