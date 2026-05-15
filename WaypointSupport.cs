using FS_LevelEditor.Editor;
using FS_LevelEditor.Misc;
using FS_LevelEditor.Playmode;
using FS_LevelEditor.SaveSystem.Converters;
using FS_LevelEditor.SaveSystem.SerializableTypes;
using Il2Cpp;
using MelonLoader;
using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;

namespace FS_LevelEditor
{
    [Serializable]
    public class WaypointData
    {
        public Vector3Serializable position { get; set; }
        public Vector3Serializable rotation { get; set; }
        public Vector3Serializable scale { get; set; } = Vector3.one;

        [JsonConverter(typeof(LEPropertiesConverterNew))]
        public Dictionary<string, object> properties { get; set; } = new Dictionary<string, object>();

        public WaypointData() { }
        public WaypointData(WaypointData original)
        {
            position = original.position;
            rotation = original.rotation;
            scale = original.scale;
            properties = new Dictionary<string, object>(original.properties);
        }
    }
    public enum WaypointMode
    {
        NONE,
        TRAVEL_BACK,
        LOOP
    }

    [MelonLoader.RegisterTypeInIl2Cpp]
    public class WaypointSupport : MonoBehaviour
    {
        public LE_Object targetObject;

        public Transform waypointsParent;
        public List<LE_Waypoint> spawnedWaypoints = new List<LE_Waypoint>();
        public LE_Waypoint firstWaypoint;
        public LineRenderer editorLine;

        Coroutine moveObjectCoroutine;
        int currentWaypointID;
        LE_Waypoint currentWaypoint;
        bool currentlyMoving = false;
        public bool IsCurrentlyMoving => moveObjectCoroutine != null;

        bool hasAlreadyMovedOnce = false;
        Vector3 startPosition;
        Vector3[] cachedWaypointPositions;
        Vector3[] cachedWaypointRotations;
        Vector3[] cachedWaypointScales;

        float currentMovingSpeed;

        public virtual List<WaypointData> targetWaypointsData => targetObject.waypoints;
        public virtual LE_Object.ObjectType waypointTypeToUse => LE_Object.ObjectType.WAYPOINT;
        public virtual bool needsEmptyWaypointAtStart => false;
        public virtual Vector3 waypointsPositionOffsetInPlaymode => Vector3.zero;
        public virtual bool usesCustomMoveSystem => false;
        public virtual Color editorLineColor => Color.white;
        public virtual GameObject waypointTemplate => null; // If null (by default), it'll create a copy of the main object.
        public virtual int? maxWaypointsCount => null;
        public virtual bool showWaypointsOnPlaymode => false;
        public virtual bool alwaysShowOnEditor => false; // This will also make the waypoints opaque (non-transparent).

        bool playerIsAbove = false;
        public static WaypointSupport objectWithPlayerAbove = null;
        List<GameObject> objectsToMove = new List<GameObject>();

        public Vector3 currentVelocity;

        void Awake()
        {
            targetObject = GetComponent<LE_Object>();
            CreateWaypointsParent();
            if (EditorController.Instance) CreateEditorLine();
        }
        void OnDestroy()
        {
            if (moveObjectCoroutine != null)
                StopObjectMovement();

            waypointsParent = null;
            spawnedWaypoints.Clear();
            spawnedWaypoints = null;
            firstWaypoint = null;
            editorLine = null;

            currentWaypoint = null;
            objectsToMove.Clear();
            objectsToMove = null;
        }

        void CreateWaypointsParent()
        {
            waypointsParent = new GameObject("Waypoints").transform;
            waypointsParent.parent = targetObject.transform;
            waypointsParent.localPosition = Vector3.zero;
            waypointsParent.localEulerAngles = Vector3.zero;
            waypointsParent.localScale = Vector3.one;
            GlobalScaleChanger.AddTo(waypointsParent.gameObject, waypointsParent.parent, Vector3.one, true); // Avoid inheriting scale from parent.
            if (EditorController.Instance)
            {
                waypointsParent.gameObject.SetActive(false); // Disabled by default, until the user selects it.
            }
            else if (PlayModeController.Instance)
            {
                // They're empty in playmode, no problem with that.
                waypointsParent.gameObject.SetActive(true);
            }
        }
        void CreateEditorLine()
        {
            if (!editorLine)
            {
                editorLine = Instantiate(Core.LoadOtherObjectInBundle("EditorLine"), transform).GetComponent<LineRenderer>();
                editorLine.transform.localPosition = Vector3.zero;
                editorLine.transform.localScale = Vector3.one;
                editorLine.startColor = editorLineColor;
                editorLine.endColor = editorLineColor;
                editorLine.gameObject.SetActive(false);
            }
        }

        public void OnInstantiated(LEScene scene)
        {
            if (targetWaypointsData.Count > 0) LoadWaypointsFromSave();
        }
        public void LoadWaypointsFromSave()
        {
            List<WaypointData> waypoints = targetWaypointsData;

            if (PlayModeController.Instance)
            {
                if (needsEmptyWaypointAtStart) CreateFirstWaypointEver(waypoints);

                switch (GetWaypointMode())
                {
                    case WaypointMode.LOOP: CreateLoopWaypoint(waypoints); break;
                    case WaypointMode.TRAVEL_BACK: CreateTravelBackWaypoints(waypoints); break;
                }
            }

            for (int i = 0; i < waypoints.Count; i++)
            {
                var waypointData = waypoints[i];
                LE_Waypoint createdWaypoint = AddWaypoint(true);

                createdWaypoint.transform.localPosition = waypointData.position;
                if (PlayModeController.Instance) createdWaypoint.transform.localPosition += waypointsPositionOffsetInPlaymode;
                createdWaypoint.transform.localEulerAngles = waypointData.rotation;
                createdWaypoint.transform.localScale = waypointData.scale;
                foreach (var property in waypointData.properties)
                {
                    createdWaypoint.SetProperty(property.Key, property.Value);
                }
            }
            // Init the components NOW before SetupForCustomSystem() is called.
            if (PlayModeController.Instance) spawnedWaypoints.ForEach(x => x.InitComponent());
        }
        // --------------------------------------------------
        void CreateFirstWaypointEver(List<WaypointData> originalList)
        {
            WaypointData firstWaypoint = new WaypointData();
            // Waypoints positions are relative to the main object position, Vector3.zero means the waypoint will be in the same positions as the main object.
            firstWaypoint.position = Vector3.zero;
            firstWaypoint.rotation = Vector3.zero;
            firstWaypoint.scale = transform.localScale; // Same as the object at start.
            if (targetObject.waypointMode == WaypointMode.TRAVEL_BACK)
            {
                firstWaypoint.properties["WaitTime"] = targetObject.waitTime;
            }
            else
            {
                firstWaypoint.properties["WaitTime"] = 0f;
            }

            firstWaypoint.properties["MoveSpeed"] = targetObject.movingSpeed;

            originalList.Insert(0, firstWaypoint);
        }
        void CreateLoopWaypoint(List<WaypointData> originalList)
        {
            WaypointData finalWaypoint = new WaypointData();
            // Waypoints positions are relative to the main object position, Vector3.zero means the waypoint will be in the same positions as the main object.
            finalWaypoint.position = Vector3.zero;
            finalWaypoint.rotation = Vector3.zero;
            finalWaypoint.scale = transform.localScale; // Same as the object at start.
            // Use the original object's "Wait Time" attribute since the user won't be able to select/change this final waypoint.
            finalWaypoint.properties["WaitTime"] = targetObject.waitTime;
            finalWaypoint.properties["MoveSpeed"] = targetObject.movingSpeed;

            originalList.Add(finalWaypoint);
        }
        void CreateTravelBackWaypoints(List<WaypointData> originalList)
        {
            for (int i = originalList.Count - 2; i >= 0; i--)
            {
                WaypointData data = new WaypointData();
                data.position = originalList[i].position;
                data.rotation = originalList[i].rotation;
                data.scale = originalList[i].scale;
                foreach (var property in originalList[i].properties) data.properties[property.Key] = property.Value;

                originalList.Add(data);
            }

            if (!needsEmptyWaypointAtStart)
            {
                // Create the last waypoint so the object goes to its original position.
                WaypointData lastWaypoint = new WaypointData();
                lastWaypoint.position = Vector3.zero;
                lastWaypoint.rotation = Vector3.zero;
                lastWaypoint.scale = transform.localScale; // Same as the object at start.
                // Use the original object's "Wait Time" attribute since the user won't be able to select/change this last waypoint.
                lastWaypoint.properties["WaitTime"] = targetObject.waitTime;
                lastWaypoint.properties["MoveSpeed"] = targetObject.movingSpeed;
                originalList.Add(lastWaypoint);
            }
        }

        public void ObjectStart(LEScene scene)
        {
            if (scene == LEScene.Playmode && spawnedWaypoints != null && spawnedWaypoints.Count > 0)
            {
                if (usesCustomMoveSystem)
                {
                    SetupForCustomSystem();
                }
                else if (targetObject.startMovingAtStart) // Default system for global waypoints.
                {
                    StartObjectMovement();
                }
            }

            if (scene == LEScene.Editor && alwaysShowOnEditor)
            {
                ShowWaypoints(true);
            }
        }
        public void StartObjectMovement()
        {
            if (usesCustomMoveSystem) return;
            if (moveObjectCoroutine != null) return; // There's already a coroutine running, don't do shit.

            moveObjectCoroutine = (Coroutine)MelonCoroutines.Start(MoveObject());
            Logger.Log("Started waypoint movement for object object: " + gameObject.name);
        }
        IEnumerator MoveObject()
        {
            if (cachedWaypointPositions == null || cachedWaypointRotations == null || cachedWaypointScales == null)
            {
                startPosition = transform.position;

                cachedWaypointPositions = spawnedWaypoints.Select(x => x.transform.position).ToArray();
                cachedWaypointRotations = spawnedWaypoints.Select(x => x.transform.eulerAngles).ToArray();
                cachedWaypointScales = spawnedWaypoints.Select(x => x.transform.localScale).ToArray();
            }

            hasAlreadyMovedOnce = true;

            yield return new WaitForSeconds(targetObject.startDelay);

            currentMovingSpeed = targetObject.movingSpeed;

            string name = this.name;
            for (/*Do not reset currentWaypointID to 0*/; currentWaypointID < spawnedWaypoints.Count; currentWaypointID++)
            {
                currentWaypoint = spawnedWaypoints[currentWaypointID];

                Vector3 totalPosDistance = cachedWaypointPositions[currentWaypointID] - transform.position;
                Vector3 totalRotDiff = new Vector3(
                    Mathf.DeltaAngle(transform.eulerAngles.x, cachedWaypointRotations[currentWaypointID].x),
                    Mathf.DeltaAngle(transform.eulerAngles.y, cachedWaypointRotations[currentWaypointID].y),
                    Mathf.DeltaAngle(transform.eulerAngles.z, cachedWaypointRotations[currentWaypointID].z)
                );
                Vector3 totalScaleDiff = cachedWaypointScales[currentWaypointID] - transform.localScale;

                float totalPosDuration = totalPosDistance.magnitude > 0.001f ? totalPosDistance.magnitude / currentMovingSpeed : 0f;
                float totalRotDuration = totalRotDiff.magnitude > 0.001f ? totalRotDiff.magnitude / currentMovingSpeed : 0f;
                float totalScaleDuration = totalScaleDiff.magnitude > 0.001f ? totalScaleDiff.magnitude / currentMovingSpeed : 0f;

                // Use distance diff by default, if 0, then use the max value of either rotation or scale duration.
                float totalDuration = totalPosDuration;
                if (totalDuration == 0)
                {
                    totalDuration = Mathf.Max(totalPosDuration, totalRotDuration, totalScaleDuration);
                }

                // Start rotation and scale tween now, so they keep running on background.
                RotationTweener tweenRotation = RotationTweener.RotateTo(gameObject, cachedWaypointRotations[currentWaypointID], totalDuration, RotationPath.Shortest);

                TweenScale tweenScale = TweenScale.Begin(gameObject, totalDuration, cachedWaypointScales[currentWaypointID]);
                tweenScale.ignoreTimeScale = false; // Avoid object scaling while the game's paused.

                // Do the movement by steps, so we can also apply the position to the objects to move (cubes).
                currentlyMoving = true;
                while (Vector3.Distance(cachedWaypointPositions[currentWaypointID], transform.position) > 0.01f)
                {
                    Vector3 oldPos = transform.position;
                    float posSpeed = totalPosDuration > 0.001f ? totalPosDistance.magnitude / totalDuration : 0f;
                    Vector3 newPos = Vector3.MoveTowards(transform.position, cachedWaypointPositions[currentWaypointID], Time.deltaTime * currentMovingSpeed);
                    Vector3 difference = newPos - oldPos;
                    currentVelocity = difference;

                    transform.position = newPos;
                    bool exec = targetObject.objectFullNameWithID == "Moving Platform 2";

                    // Move every object attached to this platform.
                    foreach (var obj in objectsToMove)
                    {
                        if (obj.TryGetComponent<Rigidbody>(out var rb))
                        {
                            obj.transform.position += difference;
                            rb.MovePosition(obj.transform.position);
                        }
                    }

                    if (playerIsAbove)
                    {
                        // Move the player directly using character.Move instead of setting m_currentMovingPlatformMovement, since this coroutine is not syncted with the Update() function, which can cause some mismovement issues.

                        // Only move the player in the X and Z axis, Y axis is managed differently.
                        Vector3 differenceForPlayer = new Vector3(difference.x, 0f, difference.z);
                        Controls.Instance.character.Move(differenceForPlayer);
                        Controls.Instance.transform.position += new Vector3(0, difference.y, 0);
                    }

                    yield return null; // Skip frame.
                }
                currentlyMoving = false;
                currentVelocity = Vector3.zero;

                while (tweenRotation.isPlaying || tweenScale.enabled)
                {
                    yield return null;
                }

                currentWaypoint.ExecuteWhenReachedEvents();

                currentMovingSpeed = currentWaypoint.GetProperty<float>("MoveSpeed");

                yield return new WaitForSeconds(currentWaypoint.GetProperty<float>("WaitTime"));

                if (currentWaypoint.GetProperty<bool>("StopHere"))
                {
                    currentWaypointID++;
                    StopObjectMovement();
                    yield break;
                }

                if (currentWaypointID == spawnedWaypoints.Count - 1 && (targetObject.waypointMode == WaypointMode.LOOP || targetObject.waypointMode == WaypointMode.TRAVEL_BACK))
                {
                    currentWaypointID = -1; // the 'for' loop will automatically add 1 in the next iteration, converting 'i' to 0.
                }
            }

            StopObjectMovement();
        }
        public void StopObjectMovement()
        {
            if (moveObjectCoroutine == null) return; // Just in case trying to stop a null coroutine throws an error.

            MelonCoroutines.Stop(moveObjectCoroutine);
            moveObjectCoroutine = null;
            currentVelocity = Vector3.zero;
            Logger.Log("Waypoint movement stopped for object: " + gameObject.name);
        }
        public void ResetMovement()
        {
            if (!hasAlreadyMovedOnce)
                return;

            StopObjectMovement();

            Vector3 difference = startPosition - transform.position;

            // Move every object attached to this platform.
            foreach (var obj in objectsToMove)
            {
                if (obj.TryGetComponent<Rigidbody>(out var rb))
                {
                    obj.transform.position += difference;
                    rb.MovePosition(obj.transform.position);
                }
            }

            if (playerIsAbove)
            {
                Vector3 differenceForPlayer = new Vector3(difference.x, 0f, difference.z);
                Controls.Instance.character.Move(differenceForPlayer);
                Controls.Instance.transform.position += new Vector3(0, difference.y, 0);
            }

            transform.position = startPosition;
            currentWaypointID = 0;
        }
        // --------------------------------------------------
        public virtual void SetupForCustomSystem()
        {

        }
        public virtual WaypointMode GetWaypointMode()
        {
            return targetObject.waypointMode;
        }
        float GetObjectMoveSpeed(LE_Object obj)
        {
            if (obj is LE_Waypoint)
            {
                return obj.GetProperty<float>("MoveSpeed");
            }
            else
            {
                return obj.movingSpeed;
            }
        }

        void Update()
        {
            // Update the editor link every frame while it's active.
            if (firstWaypoint && editorLine && editorLine.gameObject.active)
            {
                if (!editorLine.enabled) editorLine.enabled = true;
                editorLine.SetPosition(0, transform.position);
                editorLine.SetPosition(1, firstWaypoint.transform.position);
            }
            if (!firstWaypoint && editorLine) editorLine.enabled = false;
        }

        public void OnSelect()
        {
            ShowWaypoints(true);
        }
        public void OnDeselect()
        {
            ShowWaypoints(false);
        }
        public void BeforeSave()
        {
            // Since the waypoints aren't saved automatically, call the method manually in them.
            spawnedWaypoints.ForEach(x => x.BeforeSave());
        }

        public void ShowWaypoints(bool show)
        {
            if (alwaysShowOnEditor && !show) show = true;

            if (show)
            {
                waypointsParent.gameObject.SetActive(true);
                if (editorLine) editorLine.gameObject.SetActive(true); // Technically this can only be called when we're on the editor, but just in case.
            }
            else if (!EditorController.Instance.showAllWaypoints)
            {
                waypointsParent.gameObject.SetActive(false);
                if (editorLine) editorLine.gameObject.SetActive(false); // Technically this can only be called when we're on the editor, but just in case.
            }

            if (!alwaysShowOnEditor)
            {
                // Set the transparent materials in the waypoints just in case.
                foreach (var waypoint in spawnedWaypoints)
                {
                    waypoint.gameObject.SetTransparentMaterials();
                }
            }
        }
        public LE_Waypoint AddWaypoint(bool fromSave = false, bool selectIfNotFromSave = true)
        {
            if (maxWaypointsCount != null && spawnedWaypoints.Count >= maxWaypointsCount)
            {
                Logger.Warning($"Tried to add a waypoint in {GetType().Name} but max count ({maxWaypointsCount}) has been already reached!");
                return null;
            }

            GameObject waypoint = null;
            if (EditorController.Instance)
            {
                GameObject template = waypointTemplate ? waypointTemplate : EditorController.Instance.allCategoriesObjects[targetObject.objectType.Value];

                waypoint = Instantiate(template, waypointsParent);
                waypoint.SetActive(true); // Ensure the waypoint is enabled, since the template obj may not.
                if (!alwaysShowOnEditor) waypoint.SetTransparentMaterials();
                if(targetObject.objectType == LE_Object.ObjectType.CEILING_LIGHT || targetObject.objectType == LE_Object.ObjectType.POINT_LIGHT 
                    || targetObject.objectType == LE_Object.ObjectType.DIRECTIONAL_LIGHT)
                {
                    waypoint.GetComponentInChildren<Light>().range = targetObject.GetProperty<float>("Range");
                    targetObject.TryGetProperty("Intensity", out object intensity);
                    if(intensity != null)
                    {
						waypoint.GetComponentInChildren<Light>().intensity = (float)intensity;
					}
				}
                // DESTROY EVERY FUCKING RIGIDBODY WE FIND.
                foreach (var rigidBody in waypoint.TryGetComponents<Rigidbody>(true))
                {
                    Destroy(rigidBody);
                }
            }
            else
            {
                if (showWaypointsOnPlaymode)
                {
                    GameObject template = waypointTemplate ? waypointTemplate : PlayModeController.Instance.allCategoriesObjects[targetObject.objectType.Value];

                    waypoint = Instantiate(template, waypointsParent);
                    waypoint.SetActive(true); // Ensure the waypoint is enabled, since the template obj may not.
                }
                else // We don't need any meshes or shit in playmode, just create an empty object.
                {
                    waypoint = new GameObject("Waypoint"); // AddComponentToObject will overwrite the name, so fuck it.
                    waypoint.transform.parent = waypointsParent;
                }
            }

            waypoint.transform.localPosition = Vector3.zero;
            waypoint.transform.localEulerAngles = Vector3.zero;
            waypoint.transform.localScale = transform.localScale;

            LE_Waypoint waypointComp = (LE_Waypoint)LE_Object.AddComponentToObject(waypoint, waypointTypeToUse);
            waypointComp.waypointIndex = spawnedWaypoints.Count;

            if (!firstWaypoint)
            {
                firstWaypoint = waypointComp;
                waypointComp.previousWaypoint = this;
            }
            else
            {
                waypointComp.previousWaypoint = spawnedWaypoints.Last();
                spawnedWaypoints.Last().nextWaypoint = waypointComp;
            }

            spawnedWaypoints.Add(waypointComp);

            if (!fromSave) // Create a new WaypointData, link it and everything.
            {
                WaypointData data = new WaypointData();
                waypointComp.attachedData = data;
                targetWaypointsData.Add(data);

                if (EditorController.Instance && selectIfNotFromSave)
                {
                    EditorController.Instance.SetSelectedObj(waypoint);
                }

                // Force the Awake() call when loading from save since it won't be called until the user selects the main object and the waypoints are enabled for the first time.
                waypointComp.CallMethod("Awake");
            }
            else // Just link the ALREADY EXISTING data to the created waypoint.
            {
                waypointComp.attachedData = targetWaypointsData[spawnedWaypoints.Count - 1];

                // Force the Awake() call when loading from save since it won't be called until the user selects the main object and the waypoints are enabled for the first time.
                waypointComp.CallMethod("Awake");
            }

            if (EditorController.Instance)
            {
                // In the editor, the waypoints have a mesh, disable the colliders on the content object, so only EditorCollider works.
                waypointComp.SetCollidersState(false);
            }

            return waypointComp;
        }

        public void RecalculateWaypoints()
        {
            for (int i = 0; i < targetWaypointsData.Count; i++)
            {
                var waypointData = targetWaypointsData[i];
                var waypoint = spawnedWaypoints[i];

                if (i == 0)
                {
                    firstWaypoint = waypoint;
                    waypoint.previousWaypoint = this;
                }
                else
                {
                    spawnedWaypoints[i - 1].nextWaypoint = waypoint;
                    waypoint.previousWaypoint = spawnedWaypoints[i - 1];
                }
            }
        }

        public void OnPlatformProxyEntered(MovingPlatformProxy proxy)
        {
            if (proxy && !objectsToMove.Contains(proxy.gameObject))
            {
                objectsToMove.Add(proxy.gameObject);
                proxy.hasActivePlatform = true;
                proxy.gameObject.AddComponent<MovingPlatformProxyWithCustomPlatform>().attachedWaypointObj = this;
            }
        }
        public void OnPlatformProxyExited(MovingPlatformProxy proxy)
        {
            if (proxy && objectsToMove.Contains(proxy.gameObject))
            {
                objectsToMove.Remove(proxy.gameObject);
                proxy.hasActivePlatform = false;
                proxy.gameObject.RemoveComponent<MovingPlatformProxyWithCustomPlatform>();
            }
        }

        // WARNING: 90% of this code is copied from Controls.SetAsMovingPlatform from 0.603's code. I can't even understand it, but it works. - Jav.
        public static void SetPlayerAbove(WaypointSupport newObjectWithPlayerAbove)
        {
            bool isOnObjectNow = newObjectWithPlayerAbove;
            bool thereWasOldObject = objectWithPlayerAbove;
            bool newObjIsDifferent = newObjectWithPlayerAbove != objectWithPlayerAbove;

            if (newObjIsDifferent)
            {
                if (isOnObjectNow && thereWasOldObject && newObjIsDifferent)
                {
                    Controls.Instance.m_currentMovingPlatformMovement = Vector3.zero;
                    objectWithPlayerAbove.playerIsAbove = false;
                    Controls.Instance.currentMovingPlatform = null;
                    newObjectWithPlayerAbove.playerIsAbove = true;
                    Controls.Instance.playerOnMovingPlatform = true;
                    Controls.Instance.m_currentWalkingDeceleration = Controls.Instance.m_groundedDeceleration;
                    Controls.Instance.m_currentWalkingAcceleration = Controls.Instance.m_groundedAcceleration;
                    Controls.Instance.m_movingPlatformMomentumMovement = Vector3.zero;
                    Controls.Instance.m_isJumping = false;
                }
                if (isOnObjectNow && newObjIsDifferent)
                {
                    Controls.Instance.playerOnMovingPlatform = true;
                    newObjectWithPlayerAbove.playerIsAbove = true;
                    Controls.Instance.currentMovingPlatform = null;
                    Controls.Instance.m_movingPlatformMomentumMovement = Vector3.zero;
                    Controls.Instance.m_currentWalkingDeceleration = Controls.Instance.m_groundedDeceleration;
                    Controls.Instance.m_currentWalkingAcceleration = Controls.Instance.m_groundedAcceleration;
                    Controls.Instance.m_isJumping = false;
                }
                else if (thereWasOldObject && !isOnObjectNow)
                {
                    Controls.Instance.m_currentMovingPlatformMovement = Vector3.zero;
                    Controls.Instance.CurrentPlatformVelocity = Vector3.zero;
                    if (Controls.Instance.currentGround == null)
                    {
                        Controls.Instance.m_currentWalkingDeceleration = Controls.Instance.m_airDeceleration;
                        Controls.Instance.m_currentWalkingAcceleration = Controls.Instance.m_airAcceleration;
                        if (objectWithPlayerAbove.currentlyMoving && Controls.Instance.m_movingPlatformMomentumMovement == Vector3.zero)
                        {
                            Controls.Instance.m_movingPlatformMomentumMovement = objectWithPlayerAbove.currentVelocity.normalized * objectWithPlayerAbove.currentMovingSpeed;
                            Controls.Instance.m_movingPlatformMomentumMovement.Set(Controls.Instance.m_movingPlatformMomentumMovement.x, 0f, Controls.Instance.m_movingPlatformMomentumMovement.z);
                            if (TimeManipulator.Exists && TimeManipulator.Instance.m_inPlayerPosession && TimeManipulator.Instance.IsCurrentlyActive())
                            {
                                Controls.Instance.m_movingPlatformMomentumMovement *= TimeManipulator.Instance.m_slowDownTimeValue;
                            }
                        }
                    }
                    objectWithPlayerAbove.playerIsAbove = false;
                    Controls.Instance.playerOnMovingPlatform = false;
                    Controls.Instance.currentMovingPlatform = null;
                    Controls.Instance.m_currentControllerColliderHit = null;
                }
            }
            else if (Controls.Instance.playerOnMovingPlatform && !isOnObjectNow)
            {
                Controls.Instance.playerOnMovingPlatform = false;
                Controls.Instance.currentMovingPlatform = null;
                Controls.Instance.m_currentControllerColliderHit = null;
                Controls.Instance.m_currentMovingPlatformMovement = Vector3.zero;
                Controls.Instance.m_movingPlatformMomentumMovement = Vector3.zero;
                Controls.Instance.CurrentPlatformVelocity = Vector3.zero;
            }

            objectWithPlayerAbove = newObjectWithPlayerAbove;
        }
    }

    #region Patches for objects with MovingPlatformProxy
    // Small class to register when a MovingPlatformProxy contacts with a custom object waypoint system instead of a normal MP.
    [RegisterTypeInIl2Cpp]
    public class MovingPlatformProxyWithCustomPlatform : MonoBehaviour
    {
        public WaypointSupport attachedWaypointObj;
    }

    [HarmonyPatch(typeof(MovingPlatformProxy), nameof(MovingPlatformProxy.OnCollisionEnter))]
    public static class OnCollisionEnterForPlatformProxyPatch
    {
        public static void Prefix(MovingPlatformProxy __instance, Collision collision)
        {
            if (PlayModeController.Instance)
            {
                LE_Object editorObjectComp = collision.gameObject.GetComponentInParent<LE_Object>();
                if (!editorObjectComp) return;
                WaypointSupport waypointSupport = null;

                if (editorObjectComp.customWaypointSupport && editorObjectComp.customWaypointSupport.targetWaypointsData.Count > 0) waypointSupport = editorObjectComp.customWaypointSupport;
                else if (editorObjectComp.waypointSupport && editorObjectComp.waypointSupport.targetWaypointsData.Count > 0) waypointSupport = editorObjectComp.waypointSupport;

                if (waypointSupport)
                {
                    waypointSupport.OnPlatformProxyEntered(__instance);
                }
            }
        }
    }
    [HarmonyPatch(typeof(MovingPlatformProxy), nameof(MovingPlatformProxy.OnCollisionExit))]
    public static class OnCollisionExitForPlatformProxyPatch
    {
        public static void Prefix(MovingPlatformProxy __instance, Collision collision)
        {
            if (PlayModeController.Instance)
            {
                LE_Object editorObjectComp = collision.gameObject.GetComponentInParent<LE_Object>();
                if (!editorObjectComp) return;
                WaypointSupport waypointSupport = null;

                if (editorObjectComp.customWaypointSupport && editorObjectComp.customWaypointSupport.targetWaypointsData.Count > 0) waypointSupport = editorObjectComp.customWaypointSupport;
                else if (editorObjectComp.waypointSupport && editorObjectComp.waypointSupport.targetWaypointsData.Count > 0) waypointSupport = editorObjectComp.waypointSupport;

                if (waypointSupport)
                {
                    waypointSupport.OnPlatformProxyExited(__instance);
                }
            }
        }
    }
    #endregion

    #region Patches for player when he's above of an object
    // WARNING: This is executed constantly, but only when the player is colliding with at least something.
    [HarmonyPatch(typeof(Controls), nameof(Controls.CheckGround))]
    public static class CheckGroundForPlayerInWaypointsObjPatch // Detect when player CONTACTS with an obj with waypoint support.
    {
        // Use Postfix so Controls.currentGround is already set when called.
        public static void Postfix(Controls __instance)
        {
            //if (Controls.Instance.m_currentControllerColliderHit == null || Controls.Instance.m_currentControllerColliderHit.collider == null) return;
            if (!Controls.Instance.currentGround) return;

            if (PlayModeController.Instance && Controls.PlayerAtFinalSavedPos && Controls.QuickloadFinished && Time.timeSinceLevelLoad > 0.5f)
            {
                LE_Object editorObjectComp = Controls.Instance.currentGround.gameObject.GetComponentInParent<LE_Object>();
                if (!editorObjectComp) return;
                if (editorObjectComp.objectType == LE_Object.ObjectType.MOVING_PLATFORM && editorObjectComp.customWaypointSupport.targetWaypointsData.Count > 0)
                {
                    if (WaypointSupport.objectWithPlayerAbove != null)
                    {
                        Logger.DebugLog($"Player LOST collision with an object with global waypoints and went to a MP!");
                        WaypointSupport.SetPlayerAbove(null);
                        Controls.Instance.SetAsMovingPlatform(((LE_Moving_Platform)editorObjectComp).script);
                    }
                    return;
                }

                WaypointSupport waypointSupport = null;

                if (editorObjectComp.customWaypointSupport && editorObjectComp.customWaypointSupport.targetWaypointsData.Count > 0) waypointSupport = editorObjectComp.customWaypointSupport;
                else if (editorObjectComp.waypointSupport && editorObjectComp.waypointSupport.targetWaypointsData.Count > 0) waypointSupport = editorObjectComp.waypointSupport;

                if (waypointSupport) // Player collided with an object with waypoints.
                {
                    if (waypointSupport != WaypointSupport.objectWithPlayerAbove)
                    {
                        WaypointSupport.SetPlayerAbove(waypointSupport);
                        Logger.DebugLog($"Player is above an object with waypoints: {waypointSupport.targetObject.objectFullNameWithID}");
                    }
                }
                else
                {
                    MovingPlatformProxyWithCustomPlatform customProxy = Controls.Instance.currentGround.gameObject.GetComponentInParent<MovingPlatformProxyWithCustomPlatform>();
                    if (customProxy) // But he collided wih a proxy.
                    {
                        if (customProxy.attachedWaypointObj != WaypointSupport.objectWithPlayerAbove)
                        {
                            WaypointSupport.SetPlayerAbove(customProxy.attachedWaypointObj);
                            Logger.DebugLog($"Player is above a proxy object: {customProxy.attachedWaypointObj.targetObject.objectFullNameWithID}");
                        }
                    }
                    else // Didn't collide with any platform or related.
                    {
                        if (WaypointSupport.objectWithPlayerAbove != null)
                        {
                            Logger.DebugLog($"Player LOST collision with an object with waypoints!");
                            WaypointSupport.SetPlayerAbove(null);
                        }
                    }
                }
            }
        }
    }

    // Use this patch to also deattach the player from any platform when he's on mid air (not colliding with anything).
    [HarmonyPatch(typeof(Controls), nameof(Controls.UngroundInstantly))]
    public static class OnUngroundInstantlyForPlayerInWaypointsObjPatch // Detect when player LOSES contact with ANY object.
    {
        public static void Prefix()
        {
            if (PlayModeController.Instance)
            {
                if (!Controls.Instance.IsInZeroGravity() && WaypointSupport.objectWithPlayerAbove)
                {
                    Logger.DebugLog($"Player stopped touching ground and LOST collision with an object with waypoints!");
                    Controls.Instance.currentGround = null;
                    WaypointSupport.SetPlayerAbove(null);
                }
            }
        }
    }
    #endregion
}
