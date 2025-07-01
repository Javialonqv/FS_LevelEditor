using Il2Cpp;
using Il2CppTMPro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Screen : LE_Object
    {
        ScreenController screen;

        public override void ObjectStart(LEScene scene)
        {
            //screen.PlayShowAnimation();
        }

        public override void InitComponent()
        {
            GameObject content = gameObject.GetChildWithName("Content");

            content.SetActive(false);

            screen = content.AddComponent<ScreenController>();
            screen.m_content = content.GetChildWithName("Content").transform;
            screen.m_contentAnim = content.GetChildWithName("Content").GetComponent<Animation>();
            screen.m_screenRenderer = content.GetChildWithName("Mesh").GetComponent<MeshRenderer>();
            screen.redColorPlane = content.GetChildAt("Mesh/RedPlane");
            screen.greenColorPlane = content.GetChildAt("Mesh/GreenPlane");
            screen.m_mainLabelTMP = content.GetChildAt("Content/Label/MainLabel").GetComponent<TextMeshPro>();
            screen.m_mainLabelRenderer = content.GetChildAt("Content/Label/MainLabel").GetComponent<MeshRenderer>();
            screen.m_secondaryLabelTMP = content.GetChildAt("Content/Label/SecondaryLabel").GetComponent<TextMeshPro>();
            screen.m_secondaryLabelRenderer = content.GetChildAt("Content/Label/SecondaryLabel").GetComponent<MeshRenderer>();
            screen.m_lockdownLabelTMP = content.GetChildAt("Content/Label/LockdownLabel").GetComponent<TextMeshPro>();
            screen.m_lockdownLabelRenderer = content.GetChildAt("Content/Label/LockdownLabel").GetComponent<MeshRenderer>();
            screen.firstEnableEver = true;
            screen.redColor = t_screen.redColor;
            screen.greenColor = t_screen.greenColor;
            screen.cyanColor = t_screen.cyanColor;
            screen.whiteColor = Color.white;
            screen.useColorTint = true;
            screen.currentColor = ScreenController.ColorType.CYAN;

            screen.m_contentAnim.clip = t_screen.m_contentAnim.clip;
            foreach (var clip in t_screen.m_contentAnim)
            {
                AnimationState state = clip.Cast<AnimationState>();
                screen.m_contentAnim.AddClip(state.clip, state.name);
            }

            screen.m_mainLabelRenderer.material = t_screen.m_mainLabelRenderer.material;
            screen.m_mainLabelTMP.m_fontAsset = t_screen.m_mainLabelTMP.m_fontAsset;
            screen.m_mainLabelTMP.m_sharedMaterial = t_screen.m_mainLabelTMP.m_sharedMaterial;
            FormatLabel mainLabelFormat = screen.m_mainLabelTMP.gameObject.AddComponent<FormatLabel>();
            mainLabelFormat.textMesh = screen.m_mainLabelTMP;
            mainLabelFormat.localizationKey = "test";

            screen.m_secondaryLabelRenderer.material = t_screen.m_secondaryLabelRenderer.material;
            screen.m_secondaryLabelTMP.m_fontAsset = t_screen.m_secondaryLabelTMP.m_fontAsset;
            screen.m_secondaryLabelTMP.m_sharedMaterial = t_screen.m_secondaryLabelTMP.m_sharedMaterial;
            FormatLabel secondaryLabelFormat = screen.m_secondaryLabelTMP.gameObject.AddComponent<FormatLabel>();
            secondaryLabelFormat.textMesh = screen.m_secondaryLabelTMP;

            screen.m_lockdownLabelRenderer.material = t_screen.m_lockdownLabelRenderer.material;
            screen.m_lockdownLabelTMP.m_fontAsset = t_screen.m_lockdownLabelTMP.m_fontAsset;
            screen.m_lockdownLabelTMP.m_sharedMaterial = t_screen.m_lockdownLabelTMP.m_sharedMaterial;
            FormatLabel lockdownLabelFormat = screen.m_lockdownLabelTMP.gameObject.AddComponent<FormatLabel>();
            lockdownLabelFormat.textMesh = screen.m_lockdownLabelTMP;

            screen.m_mainLabelFormatter = mainLabelFormat;
            screen.m_secondaryLabelFormatter = secondaryLabelFormat;
            screen.m_lockdownLabelFormatter = lockdownLabelFormat;

            content.SetActive(true);

            initialized = true;
        }
    }
}
