using FS_LevelEditor.Editor;
using FS_LevelEditor.Playmode;
using Il2Cpp;
using Il2CppDiscord;
using UnityEngine;

namespace FS_LevelEditor
{
    [MelonLoader.RegisterTypeInIl2Cpp]
    public class LE_Saw : LE_Object
    {
        ScieScript script;

        public LE_Saw_Waypoint nextWaypoint;
        public GameObject waypointsParent;
        public List<GameObject> waypointsGOs = new List<GameObject>();
        public List<LE_Saw_Waypoint> waypointsComps = new List<LE_Saw_Waypoint>();

        ScieScript sawScript;

        void Awake()
        {
            properties = new Dictionary<string, object>()
            {
                { "ActivateOnStart", true },
                { "TravelBack", true },
                { "Loop", false },
                { "waypoints", new List<LE_SawWaypointSerializable>() },
                { "Damage", 50 },
                { "WaitTime", 0f }
            };
            

            waypointsParent = gameObject.GetChildWithName("Waypoints");
        }

        public override void OnInstantiated(LEScene scene)
        {
            if (scene == LEScene.Editor)
            {
                // Set the saw on or off.
                SetMeshOnEditor((bool)GetProperty("ActivateOnStart"));
            }

            // Load the waypoints NOW after all of the other attributes were fully loaded.
            var waypoints = GetProperty<List<LE_SawWaypointSerializable>>("waypoints");
            LoadWaypointsFromSave(waypoints);

            base.OnInstantiated(scene);
        }
        public override void ObjectStart(LEScene scene)
        {
            if (scene == LEScene.Playmode)
            {
                // If it's false, that means the saw wasn't really spawned at the start of the level, activate it again to avoid bugs.
                if (!setActiveAtStart)
                {
                    // There's a good reason for this, I swear, the Activate and Deactivate functions are just inverting the enabled bool in the saw LOL, the both functions
                    // do the same thing, so first enable it, and then disable if needed, cause if we don't do anything, there's a bug with the saw animation.
                    script.Activate();
                    bool activateOnStart = (bool)GetProperty("ActivateOnStart");
                    if (!activateOnStart)
                    {
                        script.Deactivate();
                    }
                }
            }
        }

        public override void InitComponent()
        {
            GameObject content = gameObject.GetChildWithName("Content");

            content.SetActive(false);
            content.tag = "Scie";

            content.GetComponent<AudioSource>().outputAudioMixerGroup = t_saw.GetComponent<AudioSource>().outputAudioMixerGroup;

            RotationScie rotationScie = content.GetChildWithName("Scie_OFF").AddComponent<RotationScie>();
            rotationScie.vitesseRotation = 500;

            script = content.AddComponent<ScieScript>();
            script.doesDamage = true;
            script.damage = (int)GetProperty("Damage");
            script.rotationScript = rotationScie;
            script.m_damageCollider = content.GetComponent<BoxCollider>();
            script.m_audioSource = content.GetComponent<AudioSource>();
            script.movingSaw = false;
            script.movingSpeed = 10;
            if (waypointsGOs.Count > 0)
            {
                script.currentWaypoint = waypointsGOs[0];
                script.movingSaw = true;
                script.forcedHeading = true;
                script.allowSideRotation = false;
                script.sideSpeedMultiplier = 5;
            }
            script.scieSound = t_saw.scieSound;
            script.offMesh = content.GetChildWithName("Scie_OFF").GetComponent<MeshRenderer>();
            script.onMesh = content.GetChildAt("Scie_OFF/Scie_ON").GetComponent<MeshRenderer>();
            script.m_collision = content.GetChildWithName("Collision").GetComponent<BoxCollider>();
            script.physicsCollider = content.GetChildWithName("Saw_PhysicsCollider").GetComponent<MeshCollider>();
            script.m_damageCollider = content.GetChildWithName("Saw_DamageCollider").GetComponent<MeshCollider>();

            ForwardTriggerCollision damageTrigger = script.m_damageCollider.gameObject.AddComponent<ForwardTriggerCollision>();
            damageTrigger.target = script.gameObject;
            damageTrigger.triggerEnter = true;
            damageTrigger.triggerExit = true;

            script.particlesHolder = content.GetChildWithName("SawParticlesHolder");
            script.particles1 = content.GetChildAt("SawParticlesHolder/SawSparks_1").GetComponent<ParticleSystem>();
            script.particles2 = content.GetChildAt("SawParticlesHolder/SawSparks_2").GetComponent<ParticleSystem>();
            script.particles3 = content.GetChildAt("SawParticlesHolder/SawSparks_3").GetComponent<ParticleSystem>();
            script.particles4 = content.GetChildAt("SawParticlesHolder/SawSparks_4").GetComponent<ParticleSystem>();
            script.particles1.GetComponent<ParticleSystemRenderer>().sharedMaterial = t_saw.particles1.GetComponent<ParticleSystemRenderer>().sharedMaterial;
            script.particles2.GetComponent<ParticleSystemRenderer>().sharedMaterial = t_saw.particles2.GetComponent<ParticleSystemRenderer>().sharedMaterial;
            script.particles3.GetComponent<ParticleSystemRenderer>().sharedMaterial = t_saw.particles3.GetComponent<ParticleSystemRenderer>().sharedMaterial;
            script.particles4.GetComponent<ParticleSystemRenderer>().sharedMaterial = t_saw.particles4.GetComponent<ParticleSystemRenderer>().sharedMaterial;


            if (setActiveAtStart) // Only do this if it's meant to be enabled at start, otherwise, the saw will be bugged.
            {
                // There's a good reason for this, I swear, the Activate and Deactivate functions are just inverting the enabled bool in the saw LOL, the both functions
                // do the same thing, so first enable it, and then disable if needed, cause if we don't do anything, there's a bug with the saw animation.
                script.Activate();
                bool activateOnStart = (bool)GetProperty("ActivateOnStart");
                if (!activateOnStart)
                {
                    script.Deactivate();
                }
            }

            content.SetActive(true);

            // --------- SETUP TAGS & LAYERS ---------

            script.m_collision.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            script.physicsCollider.gameObject.tag = "Saw_PhysicsCollider";
            script.physicsCollider.gameObject.layer = LayerMask.NameToLayer("AllExceptPlayer");
            script.m_damageCollider.gameObject.tag = "Scie";
            script.m_damageCollider.gameObject.layer = LayerMask.NameToLayer("PlayerCollisionOnly");

            script.particles1.gameObject.layer = LayerMask.NameToLayer("TransparentFX");
            script.particles2.gameObject.layer = LayerMask.NameToLayer("TransparentFX");
            script.particles3.gameObject.layer = LayerMask.NameToLayer("TransparentFX");
            script.particles4.gameObject.layer = LayerMask.NameToLayer("TransparentFX");

            initialized = true;
        }

        public override bool SetProperty(string name, object value)
        {
            if (name == "ActivateOnStart")
            {
                if (value is bool)
                {
                    if (EditorController.Instance != null) SetMeshOnEditor((bool)value);
                    properties["ActivateOnStart"] = (bool)value;
                    return true;
                }
            }
            else if (name == "TravelBack")
            {
                if (value is bool)
                {
                    properties["TravelBack"] = (bool)value;
                    return true;
                }
            }
            else if (name == "Loop")
            {
                if (value is bool)
                {
                    properties["Loop"] = (bool)value;
                    return true;
                }
            }
            else if (name == "waypoints") // For now, this is only called when loading the level...
            {
                if (value is List<LE_SawWaypointSerializable>)
                {
                    // Set the list and AFTER all of the other attributes are set, THEN create the waypoints OnInstantiated().
                    properties["waypoints"] = value;
                }
            }
            else if (name == "Damage")
            {
                if (value is string)
                {
                    if (int.TryParse((string)value, out int result))
                    {
                        properties["Damage"] = result;
                        return true;
                    }
                }
                else if (value is int)
                {
                    properties["Damage"] = (int)value;
                    return true;
                }
            }
            else if (name == "WaitTime")
            {
                if (value is string)
                {
                    if (Utilities.TryParseFloat((string)value, out float result))
                    {
                        properties["WaitTime"] = result;
                        return true;
                    }
                }
                else if (value is float)
                {
                    properties["WaitTime"] = (float)value;
                    return true;
                }
            }

            return base.SetProperty(name, value);
        }
        public override bool TriggerAction(string actionName)
        {
            if (actionName == "AddWaypoint")
            {
                AddWaypoint();
                return true;
            }
            else if (actionName == "Activate")
            {
                script.Activate();
                return true;
            }
            else if (actionName == "Deactivate")
            {
                script.Deactivate();
                return true;
            }
            else if (actionName == "ToggleActivated")
            {
                if (script.activated)
                {
                    script.Deactivate();
                }
                else
                {
                    script.Activate();
                }
                return true;
            }

            return base.TriggerAction(actionName);
        }

        public override void OnSelect()
        {
            base.OnSelect();
            HideOrShowAllWaypointsInEditor(true);
        }
        public override void OnDeselect(GameObject nextSelectedObj)
        {
            base.OnDeselect(nextSelectedObj);
            HideOrShowAllWaypointsInEditor(false);
        }

        void SetMeshOnEditor(bool isSawOn)
        {
            gameObject.GetChildAt("Content/Scie_OFF").GetComponent<MeshRenderer>().enabled = !isSawOn;
            gameObject.GetChildAt("Content/Scie_OFF/Scie_ON").GetComponent<MeshRenderer>().enabled = isSawOn;
        }


        void LoadWaypointsFromSave(List<LE_SawWaypointSerializable> waypoints)
        {
            // Foreach value in the saved list, create a new waypoint in the editor.
            for (int i = 0; i < waypoints.Count; i++)
            {
                var waypoint = waypoints[i];
                bool isTheFirstWaypoint = i == 0;
                bool isTheLastWaypoint = i == waypoints.Count - 1;

                // Set the waypoint values, we don't need to worry about the waypoints list since when loading data, the data is not altered.
                LE_Saw_Waypoint instance = AddWaypoint(true);
                instance.transform.localPosition = waypoint.waypointPosition;
                instance.transform.localEulerAngles = waypoint.waypointRotation;

                if (isTheFirstWaypoint) // When it's the first waypoint, the wait time is in the SAW properties.
                {
                    instance.SetProperty("WaitTime", this.GetProperty<float>("WaitTime"));
                }
                else
                {
                    // The saw will loop by default, however, if loop is disabled, set wait time -1 for the last waypoint.
                    if (isTheLastWaypoint && !GetProperty<bool>("Loop") && !GetProperty<bool>("TravelBack") && PlayModeController.Instance)
                    {
                        instance.SetProperty("WaitTime", -1f); // IMPORTANT: ALWAYS, ALWAYS put the damn value like a FLOAT.
                    }
                    else
                    {
                        instance.SetProperty("WaitTime", waypoint.waitTime);
                    }
                }

                if (isTheFirstWaypoint) instance.isTheFirstWaypoint = true;

                // The next waypoint and previows waypoint references and all that shit is already set by AddWaypoint().
            }

            // If it's in the editor, hide all, the links and the waypoints.
            if (EditorController.Instance)
            {
                HideOrShowAllWaypointsInEditor(false);
            }

            if (PlayModeController.Instance && GetProperty<bool>("TravelBack"))
            {
                CreateTravelBackWaypointsInPlaymode();
            }
        }
        void CreateTravelBackWaypointsInPlaymode()
        {
            for (int i = waypointsGOs.Count - 2; i >= 1; i--)
            {
                LE_Saw_Waypoint waypoint = AddWaypoint(true);

                waypoint.transform.localPosition = waypointsGOs[i].transform.localPosition;
                waypoint.transform.localRotation = waypointsGOs[i].transform.localRotation;

                waypoint.SetProperty("WaitTime", waypointsComps[i].GetProperty<float>("WaitTime"));
            }
        }

        public LE_Saw_Waypoint AddWaypoint(bool isFromSavedData = false, bool isToCreateTheVeryFirstWaypoint = false)
        {
            // This is for creating an extra waypoint in the same position as the saw, but it'll NOT visible in the LE.
            // Also, this will not be executed when the waypoint is created from save since the save file already includes that extra waypoint, let it be LOL.
            if (nextWaypoint == null && !isToCreateTheVeryFirstWaypoint && !isFromSavedData)
            {
                LE_Saw_Waypoint firstWaypoint = AddWaypoint(isFromSavedData, true);
                firstWaypoint.isTheFirstWaypoint = true;

                nextWaypoint = firstWaypoint;
            }

            GameObject waypoint = Instantiate(Core.LoadOtherObjectInBundle("Saw Waypoint"), waypointsParent.transform);
            waypoint.transform.localPosition = Vector3.zero;
            waypoint.transform.localEulerAngles = Vector3.zero;
            waypoint.transform.localScale = Vector3.one;

            LE_Saw_Waypoint objComponent = (LE_Saw_Waypoint)LE_Object.AddComponentToObject(waypoint, LE_Object.ObjectType.SAW_WAYPOINT, true);

            if (nextWaypoint == null) // If null, then this waypoint will be the first one.
            {
                nextWaypoint = objComponent;
                objComponent.previousWaypoint = this;
                objComponent.waypointID = 0;
                objComponent.mainSaw = this;
                objComponent.SetProperty("WaitTime", 0f);
            }
            else
            {
                objComponent.previousWaypoint = GetLastWaypoint();
                GetLastWaypoint().nextWaypoint = objComponent;
                objComponent.waypointID = waypointsGOs.Count;
                objComponent.mainSaw = this;
            }

            // Register the new waypoint in these lists.
            if (!isFromSavedData)
            {
                AddNewWaypointToList(new LE_SawWaypointSerializable(objComponent));
            }
            objComponent.SetupObjectID();
            waypointsGOs.Add(objComponent.gameObject);
            waypointsComps.Add(objComponent);


            // Select the object only if the waypoint is NOT loaded from save data ;)
            if (!isFromSavedData && EditorController.Instance)
            {
                EditorController.Instance.SetSelectedObj(waypoint);
            }

            Logger.DebugLog($"Created waypoint! ID: {objComponent.objectID}, isFromSaveData: {isFromSavedData}, ignoreIfCurrentNextWaypointIsNull: {isToCreateTheVeryFirstWaypoint}.");
            return objComponent;
        }
        public void HideOrShowAllWaypointsInEditor(bool show)
        {
            if (nextWaypoint != null)
            {
                // This is a loop, when we call this in the next waypoint, it'll automatically call it in the OTHER waypoints till the end.
                nextWaypoint.HideOrShowSawInEditor(show);
            }
        }
        public void RecalculateWaypoints()
        {
            HideOrShowAllWaypointsInEditor(false);

            // Clear this waypoints list.
            ((List<LE_SawWaypointSerializable>)properties["waypoints"]).Clear();
            nextWaypoint = null;

            LE_Saw_Waypoint lastWaypoint = null;

            foreach (var waypoint in waypointsGOs)
            {
                LE_Saw_Waypoint waypointComp = waypoint.GetComponent<LE_Saw_Waypoint>();
                waypointComp.previousWaypoint = null;
                waypointComp.nextWaypoint = null;

                if (lastWaypoint != null)
                {
                    lastWaypoint.nextWaypoint = waypointComp;
                    waypointComp.previousWaypoint = lastWaypoint;
                }
                else // This is the very first waypoint ever.
                {
                    waypointComp.previousWaypoint = this;
                }

                AddNewWaypointToList(new LE_SawWaypointSerializable(waypointComp));

                // If the next waypoint from this saw is null, that means this is the first waypoint in this new list, assign it to this saw.
                if (nextWaypoint == null) nextWaypoint = waypointComp;

                lastWaypoint = waypointComp;
            }

            HideOrShowAllWaypointsInEditor(true);
        }

        void AddNewWaypointToList(LE_SawWaypointSerializable newWaypoint)
        {
            ((List<LE_SawWaypointSerializable>)properties["waypoints"]).Add(newWaypoint);
        }
        LE_Saw_Waypoint GetLastWaypoint()
        {
            if (nextWaypoint != null)
            {
                return nextWaypoint.GetLastWaypoint();
            }
            else
            {
                return null;
            }
        }
    }
}
