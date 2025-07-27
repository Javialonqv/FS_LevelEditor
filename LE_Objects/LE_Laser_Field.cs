using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Laser_Field : LE_Object
    {
        public override void InitComponent()
        {
            GameObject content = gameObject.GetChild("Content");

            content.SetActive(false);

            KillPlaneController script = content.AddComponent<KillPlaneController>();
            script.activationAllowed = true;
            script.currentState = true;
            script.destroyCubes = true;
            script.destroyOnlyIfNotInHands = true;
            script.fakeZeroScale = Vector3.one * 0.0001f;
            script.generalAnimator = content.GetComponent<Animator>();
            script.m_desiredScale = Vector3.one * 0.4f;
            script.m_light = content.GetChild("Light").GetComponent<Light>();
            script.m_onTurnOff = new UnityEngine.Events.UnityEvent();
            script.m_onTurnOn = new UnityEngine.Events.UnityEvent();
            script.m_scaleSpeed = 0.25f;
            script.onLightIntensity = -1;

            // ---------- SETUP TAGS & LAYERS ----------

            content.GetChild("KillPlane_Mesh").layer = LayerMask.NameToLayer("TransparentFX");
            content.GetChild("KillZone").tag = "KillZone";
            content.GetChild("KillZone").layer = LayerMask.NameToLayer("Ignore Raycast");
            content.GetChildAt("KillZone/InteractionOccluder1").tag = "InteractionOccluder_ALL";
            content.GetChildAt("KillZone/InteractionOccluder1").layer = LayerMask.NameToLayer("ActivableCheck");

            content.SetActive(true);

            initialized = true;
        }
    }
}
