using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

namespace FS_LevelEditor
{
    /// <summary>
    /// The Intro chapter's rain, recreated: the game's "Rain_Player" is a particle system above the player that emits falling drops with a bit of wind,
    /// drawn as thin stretched streaks. Drops die when they hit the level (so structures shelter what's under them) and leave a splash ring there.
    /// The area is wider than the game's 10x10 so it keeps raining around/beside a roof instead of stopping as soon as you walk under one.
    /// The game's own rain material can't be reached outside of the Intro scene, so the visuals use small shaders of ours (Misc/LevelRain*.shader.txt,
    /// compiled in the embedded "level_fog" bundle). Controlled by the "GlobalRain" global property.
    /// </summary>
    public static class LevelRain
    {
        static GameObject rainObject;
        static Material rainMaterial;
        static Material splashMaterial;

        /// <summary>
        /// If the rain is currently falling (it can be turned on/off with events during playmode).
        /// </summary>
        public static bool IsRunning => rainObject != null;

        public static void SetEnabled(bool enabled)
        {
            if (enabled)
            {
                if (!IsRunning) Start();
            }
            else
            {
                Stop();
            }
        }

        public static bool IsEnabled(Dictionary<string, object> globalProperties)
        {
            return globalProperties != null && globalProperties.TryGetValue("GlobalRain", out var value) && value is bool enabled && enabled;
        }

        public static void Start()
        {
            Stop();

            try
            {
                if (Controls.Instance == null)
                {
                    Logger.Error("[LevelRain] Controls.Instance is null, can't start the rain.");
                    return;
                }

                Shader rainShader = AssetBundleLoader.LoadAsset<Shader>("LevelRain", "level_fog");
                Shader splashShader = AssetBundleLoader.LoadAsset<Shader>("LevelRainSplash", "level_fog");
                if (rainShader == null || splashShader == null)
                {
                    Logger.Error("[LevelRain] Couldn't load the rain shaders from the level_fog bundle, can't start the rain.");
                    return;
                }

                rainMaterial = new Material(rainShader);
                rainMaterial.SetColor("_Color", new Color(0.8f, 0.9f, 1f, 0.4f));
                splashMaterial = new Material(splashShader);

                // Parented to the player so it lives (and dies) with it, a loose root object could get destroyed by a scene change right after the level loads.
                // The follower puts it back above the player in world space every frame, ignoring the player's rotation.
                rainObject = new GameObject("LE_Rain");
                rainObject.transform.SetParent(Controls.Instance.transform, false);
                LevelRainFollower follower = rainObject.AddComponent<LevelRainFollower>();
                follower.target = Controls.Instance.transform;
                follower.UpdatePosition();

                ParticleSystem splash = CreateSplashSystem(rainObject.transform);
                ParticleSystem rain = CreateRainSystem(rainObject, splash);

                splash.Play();
                rain.Play();
                follower.rain = rain;
                follower.splash = splash;
                follower.SetupSound();

                Logger.Log("[LevelRain] Rain started.");
            }
            catch (System.Exception e)
            {
                Logger.Error("[LevelRain] Failed to start the rain: " + e);
                Stop();
            }
        }

        static ParticleSystem CreateRainSystem(GameObject target, ParticleSystem splash)
        {
            ParticleSystem ps = target.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 5f;
            main.loop = true;
            main.playOnAwake = true;
            main.prewarm = false;
            main.startLifetime = 3f; // Long enough to reach the ground from the emitter (15 up) even at the slowest speed, so drops don't vanish mid-air.
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.008f, 0.02f);
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 6000; // Must be > rate x lifetime (1700 x 3), otherwise the emitter starves and leaves gaps.
            main.scalingMode = ParticleSystemScalingMode.Local; // Ignore the scale of the player it's parented to.

            // About a third of the game's density (800/s over 10x10), over a 24x24 area: the game's looked like too much here.
            var emission = ps.emission;
            emission.rateOverTime = 1700f;

            // Flat area above the player (the game's is 10x10, 9.9 up).
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 1f;
            shape.radiusThickness = 1f;
            shape.scale = new Vector3(24f, 0.05f, 24f);

            // Falling speed + a bit of wind, in world space.
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(2f, 5f);
            velocity.y = new ParticleSystem.MinMaxCurve(-22f, -12f);
            velocity.z = new ParticleSystem.MinMaxCurve(1f, 2f);

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.05f), new Keyframe(0.11f, 0.908f), new Keyframe(1f, 1f)));

            // Drops die when they hit the level, so structures shelter what's under them.
            var collision = ps.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.quality = ParticleSystemCollisionQuality.High; // Medium missed large/thin colliders like roofs.
            // Whatever the player can stand on is solid for the rain. (The game's rain only used layers 0 and 30, which isn't what the level's colliders use.)
            collision.collidesWith = Controls.Instance.m_groundCheckLayerMask;
            collision.dampen = 1f;
            collision.bounce = 0f;
            collision.lifetimeLoss = 1f;
            collision.radiusScale = 0.05f;

            // Water volumes are triggers (particle collisions ignore those), so they're handled with the trigger module instead:
            // drops that enter a water volume are reported to LevelRainFollower.OnParticleTrigger, which kills them and makes the splash.
            // The colliders are assigned by the follower (it keeps the nearest water volumes there).
            var trigger = ps.trigger;
            trigger.enabled = true;
            trigger.enter = ParticleSystemOverlapAction.Callback;
            trigger.exit = ParticleSystemOverlapAction.Ignore;
            // Anything already inside a water volume dies too (e.g. drops spawned inside a deep volume while the player is underwater, those never "enter" it).
            trigger.inside = ParticleSystemOverlapAction.Kill;
            trigger.outside = ParticleSystemOverlapAction.Ignore;

            // A splash on (some of) the drops that hit something.
            var subEmitters = ps.subEmitters;
            subEmitters.enabled = true;
            subEmitters.AddSubEmitter(splash, ParticleSystemSubEmitterType.Collision, ParticleSystemSubEmitterProperties.InheritNothing, 0.15f);

            var renderer = target.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2f;
            renderer.velocityScale = 0.06f; // Streak length = speed x this (~0.7-1.3 units): the game's 0.01 gave short specks that looked like snow.
            renderer.cameraVelocityScale = 0f;
            renderer.minParticleSize = 0.0015f; // So the thin drops stay visible from afar.
            renderer.maxParticleSize = 0.2f;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sharedMaterial = rainMaterial;

            return ps;
        }

        static ParticleSystem CreateSplashSystem(Transform parent)
        {
            GameObject splashObject = new GameObject("Splash");
            splashObject.transform.SetParent(parent, false);

            ParticleSystem ps = splashObject.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 1f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.22f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 800;
            main.scalingMode = ParticleSystemScalingMode.Local;

            // Only used as a sub emitter: it emits one ring per collision of the rain.
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });

            var shape = ps.shape;
            shape.enabled = false;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.15f), new Keyframe(1f, 1f)));

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = fade;

            var renderer = splashObject.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sharedMaterial = splashMaterial;

            return ps;
        }

        public static void Stop()
        {
            if (rainObject != null) Object.Destroy(rainObject);
            if (rainMaterial != null) Object.Destroy(rainMaterial);
            if (splashMaterial != null) Object.Destroy(splashMaterial);
            rainObject = null;
            rainMaterial = null;
            splashMaterial = null;
        }
    }

    /// <summary>
    /// Keeps the rain emitter above the player (in world space, without inheriting the player's rotation) and the particle system playing.
    /// Also makes the rain interact with the water volumes: the trigger module only supports 6 colliders, so the nearest water volumes are kept in it.
    /// </summary>
    public class LevelRainFollower : MonoBehaviour
    {
        public const float HEIGHT = 15f;
        const float SPLASH_CHANCE_ON_WATER = 0.15f;

        public Transform target;
        public ParticleSystem rain;
        public ParticleSystem splash;

        // Sound: the Intro's rain loop that gets quieter and muffled when there's a roof above (or when swimming).
        const float BASE_VOLUME = 0.3f;
        AudioSource audioSource;
        AudioLowPassFilter lowPass;
        float shelterTarget, shelterSmoothed, volumeSmoothed;
        float nextShelterCheck;
        static readonly Vector3[] shelterRays =
        {
            Vector3.up,
            new Vector3(0.64f, 0.77f, 0f), new Vector3(-0.64f, 0.77f, 0f),
            new Vector3(0f, 0.77f, 0.64f), new Vector3(0f, 0.77f, -0.64f),
        };

        float startTime;
        bool loggedFirst, loggedSecond;
        float nextWaterRefresh;
        readonly List<BoxCollider> waterColliders = new List<BoxCollider>();
        readonly List<ParticleSystem.Particle> enterParticles = new List<ParticleSystem.Particle>();

        void Awake()
        {
            startTime = Time.realtimeSinceStartup;
        }

        void LateUpdate()
        {
            // The player can get replaced (respawn/scene reload), pick the new one up instead of stopping the rain.
            if (target == null && Controls.Instance != null) target = Controls.Instance.transform;

            UpdatePosition();

            if (rain != null && !rain.isPlaying)
            {
                Logger.Warning("[LevelRain] The rain had stopped playing, restarting it.");
                rain.Play();
            }

            if (Time.time >= nextWaterRefresh)
            {
                nextWaterRefresh = Time.time + 1f;
                RefreshWaterColliders();
            }

            UpdateSound();
            LogStatus();
        }

        public void SetupSound()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = RainSound.CreateClip();
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D, it's everywhere around the player.
            audioSource.volume = 0f;

            // Goes through the game's "SFX" mixer group, so the SFX volume option (and the game's pause/underwater mixing) applies to it.
            AudioMixerGroup sfxGroup = FindSfxGroup();
            if (sfxGroup != null) audioSource.outputAudioMixerGroup = sfxGroup;
            else Logger.Warning("[LevelRain] Couldn't find the \"SFX\" audio mixer group, the rain sound will play outside of the mixer.");

            lowPass = gameObject.AddComponent<AudioLowPassFilter>();
            lowPass.cutoffFrequency = 16000f;
            audioSource.Play();
        }

        static AudioMixerGroup FindSfxGroup()
        {
            AudioMixer mixer = Controls.Instance != null ? Controls.Instance.m_audioMixer : null;
            if (mixer == null) return null;

            // "Master" matches the whole tree (all the groups are under it), then pick the SFX one by name.
            foreach (var group in mixer.FindMatchingGroups("Master"))
            {
                if (group.name == "SFX") return group;
            }
            foreach (var group in mixer.FindMatchingGroups("SFX"))
            {
                return group;
            }
            return null;
        }

        void UpdateSound()
        {
            if (audioSource == null || target == null) return;

            if (Time.time >= nextShelterCheck)
            {
                nextShelterCheck = Time.time + 0.2f;
                shelterTarget = ComputeShelter(target.position);
            }

            float dt = Time.unscaledDeltaTime;
            shelterSmoothed = Mathf.MoveTowards(shelterSmoothed, shelterTarget, dt * 1.5f);

            // Almost silent indoors (not completely, you'd still faintly hear it through the walls).
            float targetVolume = BASE_VOLUME * Mathf.Lerp(1f, 0.04f, shelterSmoothed);
            volumeSmoothed = Mathf.MoveTowards(volumeSmoothed, targetVolume, dt * 0.5f);
            audioSource.volume = volumeSmoothed;
            lowPass.cutoffFrequency = Mathf.Lerp(16000f, 600f, shelterSmoothed);
        }

        // 0 = open sky above the player, 1 = fully covered (or swimming). Uses a few rays pointing up, so a roof a bit to the side counts partially.
        static float ComputeShelter(Vector3 playerPosition)
        {
            if (Controls.IsSwimming()) return 1f;

            LayerMask mask = Controls.Instance.m_groundCheckLayerMask;
            Vector3 origin = playerPosition + Vector3.up * 0.8f;
            int blocked = 0;
            foreach (var direction in shelterRays)
            {
                if (Physics.Raycast(origin, direction, 25f, mask, QueryTriggerInteraction.Ignore)) blocked++;
            }

            float covered = blocked / (float)shelterRays.Length;
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.2f, 0.8f, covered));
        }

        public void UpdatePosition()
        {
            if (target != null) transform.SetPositionAndRotation(target.position + Vector3.up * HEIGHT, Quaternion.identity);
        }

        void RefreshWaterColliders()
        {
            if (rain == null || target == null) return;

            WeatherWater.RefreshTriggerColliders(rain, target.position, waterColliders);
        }

        // Called by Unity for the drops that entered a water volume: they die there and (some) make a splash on the water surface.
        void OnParticleTrigger()
        {
            if (rain == null) return;

            enterParticles.Clear();
            int count = rain.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, enterParticles);
            for (int i = 0; i < count; i++)
            {
                ParticleSystem.Particle particle = enterParticles[i];

                if (splash != null && UnityEngine.Random.value < SPLASH_CHANCE_ON_WATER)
                {
                    ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
                    emitParams.position = new Vector3(particle.position.x, GetWaterSurfaceY(particle.position), particle.position.z);
                    splash.Emit(emitParams, 1);
                }

                particle.remainingLifetime = 0f;
                enterParticles[i] = particle;
            }
            rain.SetTriggerParticles(ParticleSystemTriggerEventType.Enter, enterParticles);
        }

        float GetWaterSurfaceY(Vector3 position)
        {
            foreach (var collider in waterColliders)
            {
                if (collider == null) continue;
                Bounds bounds = collider.bounds;
                if (position.x >= bounds.min.x && position.x <= bounds.max.x && position.z >= bounds.min.z && position.z <= bounds.max.z)
                    return bounds.max.y + 0.02f; // Slightly above the surface so the ring isn't hidden by the water.
            }
            return position.y;
        }

        // Diagnostics: tells us if the rain is alive some time after starting (it should have thousands of particles).
        void LogStatus()
        {
            float elapsed = Time.realtimeSinceStartup - startTime;
            if ((!loggedFirst && elapsed > 1f) || (!loggedSecond && elapsed > 5f))
            {
                if (!loggedFirst) loggedFirst = true; else loggedSecond = true;
                var renderer = GetComponent<ParticleSystemRenderer>();
                Logger.Log($"[LevelRain] Status after {elapsed:F1}s: particles={(rain != null ? rain.particleCount : -1)}, playing={(rain != null && rain.isPlaying)}, " +
                    $"activeInHierarchy={gameObject.activeInHierarchy}, rendererEnabled={(renderer != null && renderer.enabled)}, pos={transform.position}, waterVolumes={waterColliders.Count}");
            }
        }
    }

    /// <summary>
    /// The Intro's own rain loop ("sfx_rain_loop", stored in the level_editor bundle), since the Intro scene isn't loaded outside of it.
    /// If it can't be loaded, falls back to a procedural loop (filtered noise with a slow variation).
    /// The procedural read callback runs on the audio thread: it only touches its own static state, nothing from Unity.
    /// </summary>
    public static class RainSound
    {
        const int SAMPLE_RATE = 44100;
        static uint seed = 2463534242;
        static float fast, slow, variation;

        public static AudioClip CreateClip()
        {
            AudioClip bundled = AssetBundleLoader.LoadAsset<AudioClip>("sfx_rain_loop", "level_editor", false);
            if (bundled != null) return bundled;

            Logger.Warning("[LevelRain] Couldn't load \"sfx_rain_loop\" from the level_editor bundle, using the procedural rain sound.");
            fast = slow = variation = 0f;
            return AudioClip.Create("LE_RainSound", SAMPLE_RATE, 1, SAMPLE_RATE, true, OnRead);
        }

        static float Random()
        {
            seed ^= seed << 13;
            seed ^= seed >> 17;
            seed ^= seed << 5;
            return (seed & 0xFFFFFF) / (float)0x7FFFFF - 1f; // -1 to 1.
        }

        static void OnRead(float[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                float white = Random();
                fast += (white - fast) * 0.45f;   // Low-pass: takes the harshest highs out.
                slow += (fast - slow) * 0.015f;   // Slower low-pass, subtracting it is a high-pass: takes the rumble out.
                variation += (Random() - variation) * 0.0008f;

                data[i] = (fast - slow) * 0.9f * (0.75f + 0.25f * variation);
            }
        }
    }

    /// <summary>
    /// Shared by the rain and the snow: the particle trigger module only supports 6 colliders, so the water volumes nearest to the player are the ones kept in it.
    /// </summary>
    public static class WeatherWater
    {
        const int MAX_TRIGGER_COLLIDERS = 6;

        public static void RefreshTriggerColliders(ParticleSystem particles, Vector3 playerPosition, List<BoxCollider> nearestWaterColliders)
        {
            nearestWaterColliders.Clear();
            foreach (var patch in Object.FindObjectsOfType<LE_Water.WaterPatch>())
            {
                if (patch != null && patch.volumeCollider != null && patch.volumeCollider.enabled) nearestWaterColliders.Add(patch.volumeCollider);
            }

            nearestWaterColliders.Sort((a, b) =>
                (a.ClosestPoint(playerPosition) - playerPosition).sqrMagnitude.CompareTo((b.ClosestPoint(playerPosition) - playerPosition).sqrMagnitude));
            if (nearestWaterColliders.Count > MAX_TRIGGER_COLLIDERS)
                nearestWaterColliders.RemoveRange(MAX_TRIGGER_COLLIDERS, nearestWaterColliders.Count - MAX_TRIGGER_COLLIDERS);

            var trigger = particles.trigger;
            for (int i = trigger.colliderCount - 1; i >= 0; i--) trigger.RemoveCollider(i);
            foreach (var collider in nearestWaterColliders) trigger.AddCollider(collider);
        }
    }
}
