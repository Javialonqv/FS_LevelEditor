using Il2Cpp;
using Il2CppInterop.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UnityEngine;
using FS_LevelEditor.Editor;
using FS_LevelEditor.Playmode;
using Il2CppTMPro;
using System.Collections;
using MelonLoader;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Sequence_Screen : LE_Object
    {
        public GameObject screenObject;
        public GameObject LEDHolder;
        public GameObject LEDIndicatorPrefab;
        public LE_Sequence targetSequencer;

        public static Dictionary<string, object> GetDefaultProperties()
        {
            return new Dictionary<string, object>
            {
                { "SequencerID", 0 },
                { "InvertDisplayOrder", false },
                { "UseNumbers", false }
            };
        }

        void Awake()
        {
            screenObject = contentObject.GetChild("ScreenMesh");
            LEDHolder = contentObject.GetChild("LEDHolder");
            LEDIndicatorPrefab = contentObject.GetChild("LEDIndicatorPrefab");
            LEDIndicatorPrefab.GetChild("Mesh").GetComponent<MeshFilter>().mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        }

        public override void ObjectStart(LEScene scene)
        {
            objectLink.SetTargetObject(GetProperty<int>("SequencerID"));

            if (scene == LEScene.Playmode)
            {
                if (targetSequencer) MelonCoroutines.Start(WaitForSequenceInit());
            }

            base.ObjectStart(scene);
        }
        IEnumerator WaitForSequenceInit()
        {
            while (targetSequencer.sequence == null)
                yield return null;

            FixLEDs();
        }

        void FixLEDs()
        {
            if (targetSequencer == null || targetSequencer.sequence == null) return;

            // Force the first LED to be active
            targetSequencer.sequence.m_LEDIndicators[0].SetOnMaterial();

            // Force the LEDs to be in the right values
            foreach (var led in targetSequencer.sequence.m_LEDIndicators)
            {
                led.m_textMesh.transform.localPosition = new Vector3(-0.8f, 0, 0);
                led.m_textMesh.transform.localEulerAngles = new Vector3(0, 90, 0);
                led.m_textMesh.alignment = TextAlignmentOptions.Center;
            }
        }

        public override bool SetProperty(string name, object value)
        {
            if (name == "SequencerID")
            {
                if (value is string stringVal)
                {
                    if (int.TryParse(stringVal, out int parsedVal))
                    {
                        properties[name] = parsedVal;
                        return objectLink.SetTargetObject(parsedVal);
                    }
                }
                else if (value is int intVal)
                {
                    properties[name] = intVal;
                    return objectLink.SetTargetObject(intVal);
                }
            }
            else if (name == "InvertDisplayOrder")
            {
                if (value is bool boolValue)
                {
                    properties[name] = boolValue;
                    UpdateScreen();
                    return true;
                }
            }
            else if (name == "UseNumbers")
            {
                if (value is bool boolValue)
                {
                    properties[name] = boolValue;
                    UpdateScreen();
                    return true;
                }
            }

            return base.SetProperty(name, value);
        }

        public override void OnObjectLinkTargetChanged(LE_Object newTarget)
        {
            targetSequencer = objectLink.targetObject ? objectLink.targetObject as LE_Sequence : null;
            // This may have been called from SetProperty, which mean there's a chance the target sequencer has not been instantiated yet, wait till it's called from ObjectStart.
            if (!targetSequencer)
                return;

            if (EditorController.Instance)
                UpdateScreen(); // Custom-made update system for editor.
            else
                targetSequencer.FinishedSettingUpSteps(); // Use official one.
        }
        public void UpdateScreen()
        {
            if (!EditorController.Instance)
                return;

            // targetSequencer is assigned on ObjectStart, but since it may be executed AFTER this function, assign it NOW
            //if (!targetSequencer) targetSequencer = objectLink.targetObject ? objectLink.targetObject as LE_Sequence : null;

            LEDHolder.DeleteAllChildren();

            if (!targetSequencer) return;

            //var steps = targetSequencer.GetProperty<List<WaypointData>>("waypoints");
            //var stepsColors = steps.Select(step => (SequenceSwitchController.SwitchType)((JsonElement)step.properties["Color"]).GetInt32()).ToList();
            var steps = targetSequencer.customWaypointSupport.spawnedWaypoints;
            var stepsColors = steps.Select(step => step.GetProperty<SequenceSwitchController.SwitchType>("Color")).ToList();
            stepsColors.Insert(0, targetSequencer.GetProperty<SequenceSwitchController.SwitchType>("Color")); // Insert the first color, which is the main sequencer's.

            float screenSize = screenObject.transform.localScale.x;
            float LEDIndicatorSize = screenSize / (float)stepsColors.Count - 0.1f;
            float LEDindicatorSeparation = LEDIndicatorSize + 0.1f;

            LEDHolder.transform.localPosition = new Vector3(-screenSize * 0.5f + (LEDIndicatorSize * 0.5f + 0.05f), LEDHolder.transform.localPosition.y, LEDHolder.transform.localPosition.z);

            for (int i = 0; i < stepsColors.Count; i++)
            {
                GameObject createdLED = Instantiate(LEDIndicatorPrefab, LEDHolder.transform, false);
                createdLED.transform.localScale = new Vector3(createdLED.transform.localScale.x, createdLED.transform.localScale.y, LEDIndicatorSize);

                int num = i;
                if (GetProperty<bool>("InvertDisplayOrder"))
                {
                    num = stepsColors.Count - 1 - i;
                }
                createdLED.transform.localPosition = new Vector3((float)num * LEDindicatorSeparation, 0f, 0f);

                if (GetProperty<bool>("UseNumbers"))
                {
                    createdLED.GetChild("Mesh").GetComponent<MeshRenderer>().material = EditorController.Instance.GetMaterial($"NewProps_v1_Light_Black");
                    createdLED.GetChild("LEDTextMesh").gameObject.SetActive(true);
                    createdLED.GetChild("LEDTextMesh").GetComponent<TextMeshPro>().text = (i + 1) + "";
                }
                else
                {
                    SequenceSwitchController.SwitchType ledColor = stepsColors[i];
                    createdLED.GetChild("Mesh").GetComponent<MeshRenderer>().material = EditorController.Instance.GetMaterial($"NewProps_v1_Light_{ledColor}", true);
                    createdLED.GetChild("LEDTextMesh").gameObject.SetActive(false);
                }

                createdLED.SetActive(true);
            }
        }

        // Skip the LED indicators.
        public override void SetObjectColor(LEObjectContext context)
        {
            foreach (var renderer in gameObject.TryGetComponents<MeshRenderer>())
            {
                if (renderer.transform.IsChildOf(LEDHolder.transform) || renderer.transform.IsChildOf(LEDIndicatorPrefab.transform))
                    continue;

                // Skip waypoints
                if (canHaveWaypoints)
                {
                    if (waypointSupport && renderer.transform.IsChildOf(waypointSupport.waypointsParent)) continue;
                    if (customWaypointSupport && renderer.transform.IsChildOf(customWaypointSupport.waypointsParent)) continue;
                }

                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (!materials[i].HasProperty("_Color")) continue;

                    Color toSet = LE_Object.GetObjectColorForObject(objectType.Value, context);
                    toSet.a = materials[i].color.a;

                    materials[i] = MaterialUtils.GetMaterialWithColor(materials[i], toSet);
                }
                renderer.sharedMaterials = materials;
            }
        }
    }
}
