using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;

namespace FS_LevelEditor;

public class LE_Power_Reciever : LE_Object
{
    private InterrupteurController reciever;

    public override string[] EventsIDs =>
    [
        "WhenInvertingEvents",
        "WhenActivatingEvents",
        "WhenDeactivatingEvents"
    ];

    public static Dictionary<string, object> GetDefaultProperties()
    {
        return new Dictionary<string, object>
        {
            { "canBeUsed", true },
            { "RevertOnOff", false },
            { "WhenInvertingEvents", new List<LE_Event>() },
            { "WhenActivatingEvents", new List<LE_Event>() },
            { "WhenDeactivatingEvents", new List<LE_Event>() },
        };
    }

    public override void ObjectStart(LEScene scene)
    {
        base.ObjectStart(scene);
    }

    public override void InitComponent()
    {
        InterrupteurController ic = t_receiver;
        
        GameObject content = gameObject.GetChild("Content");
        content.SetActive(false);
        content.tag = "Interrupteur";
        
        reciever = content.AddComponent<InterrupteurController>();
        reciever.isLightLaserSwitch = true;
        reciever.canBeUsed = GetProperty<bool>("canBeUsed");
        reciever.onClipName = ic.onClipName;
        reciever.controlScript = Controls.Instance;
        AccessTools.Field(typeof(InterrupteurController), "hasOnMaterials").SetValue(reciever, false);
        reciever.iconDeactivationSound = ic.iconDeactivationSound; 
        reciever.iconActivationSound = ic.iconActivationSound;
        reciever.IGCType = ic.IGCType;
        reciever.ignoreColorPlane = true;
        reciever.ignoreLaser = true;
        reciever.interactableWhileDodge = true;
        reciever.isImportant = true;
        reciever.laserOnly = true;
        Animation anim = content.GetChild("LightSwitchMesh").GetComponent<Animation>();
        anim.playAutomatically = false;
        foreach (AnimationState state in ic.m_animationComp)
        {
            if (anim.GetClip(state.name) == null)
                anim.AddClip(state.clip, state.name);
        }
        Transform blocsOn = anim.transform.Find("LightSwitchBlocs_On");
        if (blocsOn) blocsOn.name = "LightSwitchBlocs_ON";
        reciever.m_animationComp = anim;
        reciever.m_ignoreMaterialsSystem = true;
        reciever.m_meshTransform = content.transform;
        reciever.m_onActivate = new UnityEngine.Events.UnityEvent();
        reciever.m_onActivate_HandOnly = new UnityEngine.Events.UnityEvent();
        reciever.m_onActivate_TaserOnly = new UnityEngine.Events.UnityEvent();
        reciever.messagesOnActivate = new Messenger[0];
        reciever.messagesOnDeactivate = new Messenger[0];
        reciever.dialogToActivate = new string[0];
        reciever.doorsToClose = new GameObject[0];
        reciever.toggleCanBeUsed = new GameObject[0];
        reciever.objectsToActivate = new GameObject[0];
        reciever.objectsToDestroy = new GameObject[0];
        reciever.objectsToEnableOnly = new GameObject[0];
        reciever.offColor = InterrupteurController.ColorType.RED;
        reciever.onColor = InterrupteurController.ColorType.GREEN;
        reciever.offMaterials = ic.offMaterials;
        reciever.onMaterials = ic.onMaterials;
        reciever.unusableColor = InterrupteurController.ColorType.BLACK;
        reciever.unusableMaterials = ic.unusableMaterials;

        Rotation rot = content.AddComponent<Rotation>();
        rot.z = true;
        rot.smoothSpeed = 0.8f;
        rot.smooth = false;
        
        ConfigureEvents(reciever);

        content.SetActive(true);

        base.InitComponent();
    }

    public override bool SetProperty(string name, object value)
    {
        if (name == "canBeUsed" || name == "RevertOnOff")
        {
            if (value is bool)
            {
                properties[name] = (bool)value;
                return true;
            }
        }
        else if (name == "WhenInvertingEvents" || name == "WhenActivatingEvents" || name == "WhenDeactivatingEvents")
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
        if (actionName == "Activate")
        {
            // Activate without running the events (same as LE_Switch).
            UnityEvent onActivate = reciever.m_onActivate;
            reciever.m_onActivate = new UnityEvent();
            reciever.ActivateSwitch();
            reciever.m_onActivate = onActivate;
            return true;
        }
        else if (actionName == "Deactivate")
        {
            UnityEvent onDeactivate = reciever.m_onDeactivate;
            reciever.m_onDeactivate = new UnityEvent();
            reciever.DeactivateSwitch();
            reciever.m_onDeactivate = onDeactivate;
            return true;
        }
        else if (actionName == "ToggleActivated")
        {
            return TriggerAction(reciever.activated ? "Deactivate" : "Activate");
        }
        else if (actionName == "ExecuteWhenActivatingActions")
        {
            ExecuteWhenActivatingEvents();
        }
        else if (actionName == "ExecuteWhenDeactivatingActions")
        {
            ExecuteWhenDeactivatingEvents();
        }
        else if (actionName == "ExecuteWhenInvertingActions")
        {
            ExecuteWhenInvertingEventsActivating();
        }
        else if (actionName == "SetCanBeUsed_True")
        {
            reciever.canBeUsed = true;
            return true;
        }
        else if (actionName == "SetCanBeUsed_False")
        {
            reciever.canBeUsed = false;
            return true;
        }
        else if (actionName == "ToggleCanBeUsed")
        {
            reciever.canBeUsed = !reciever.canBeUsed;
            return true;
        }

        return base.TriggerAction(actionName);
    }

    /// <summary>
    /// Called by the Power Beam that was activating this receiver when that beam turns off.
    /// The game's beam never deactivates the switch in that case, only when the beam moves away while still on.
    /// </summary>
    public void OnBeamTurnedOff()
    {
        if (reciever && reciever.activated && GetProperty<bool>("RevertOnOff"))
            reciever.DeactivateSwitch();
    }

    void ConfigureEvents(InterrupteurController controller)
    {
        controller.m_onActivate = new UnityEvent();
        controller.m_onActivate.AddListener((UnityAction)ExecuteWhenActivatingEvents);
        controller.m_onActivate.AddListener((UnityAction)ExecuteWhenInvertingEventsActivating);

        controller.m_onDeactivate = new UnityEvent();
        controller.m_onDeactivate.AddListener((UnityAction)ExecuteWhenDeactivatingEvents);
        controller.m_onDeactivate.AddListener((UnityAction)ExecuteWhenInvertingEventsDeactivating);
    }

    void ExecuteWhenActivatingEvents()
    {
        eventExecuter.ExecuteEventsWithAndLogic((List<LE_Event>)properties["WhenActivatingEvents"], "WhenActivatingEvents", true);
    }
    void ExecuteWhenDeactivatingEvents()
    {
        eventExecuter.ExecuteEventsWithAndLogic((List<LE_Event>)properties["WhenDeactivatingEvents"], "WhenDeactivatingEvents", false);
    }
    void ExecuteWhenInvertingEventsActivating()
    {
        eventExecuter.ExecuteEventsWithAndLogic((List<LE_Event>)properties["WhenInvertingEvents"], "WhenInvertingEvents", true);
    }
    void ExecuteWhenInvertingEventsDeactivating()
    {
        eventExecuter.ExecuteEventsWithAndLogic((List<LE_Event>)properties["WhenInvertingEvents"], "WhenInvertingEvents", false);
    }
    
}