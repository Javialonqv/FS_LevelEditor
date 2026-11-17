using UnityEngine;
using Object = UnityEngine.Object;

namespace FS_LevelEditor
{
    /// <summary>
    /// Snow: big, soft, slow flakes that drift down around the player. Same idea as <see cref="LevelRain"/> (follows the player, sheltered by structures,
    /// doesn't fall in water) without the sound/splashes. Controlled by the "GlobalSnow" global property.
    /// The flake shader is in Misc/LevelSnow.shader.txt (compiled in the embedded "level_fog" bundle).
    /// </summary>
    public static class LevelSnow
    {
        static GameObject snowObject;
        static Material snowMaterial;

        public static bool IsRunning => snowObject != null;

        public static bool IsEnabled(Dictionary<string, object> globalProperties)
        {
            return globalProperties != null && globalProperties.TryGetValue("GlobalSnow", out var value) && value is bool enabled && enabled;
        }

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

        public static void Start()
        {
            Stop();

            try
            {
                if (Controls.Instance == null)
                {
                    Logger.Error("[LevelSnow] Controls.Instance is null, can't start the snow.");
                    return;
                }

                Shader shader = AssetBundleLoader.LoadAsset<Shader>("LevelSnow", "level_fog");
                if (shader == null)
                {
                    Logger.Error("[LevelSnow] Couldn't load the snow shader from the level_fog bundle, can't start the snow.");
                    return;
                }

                snowMaterial = new Material(shader);

                // Parented to the player so it lives (and dies) with it, see LevelRain for why.
                snowObject = new GameObject("LE_Snow");
                snowObject.transform.SetParent(Controls.Instance.transform, false);
                LevelSnowFollower follower = snowObject.AddComponent<LevelSnowFollower>();
                follower.target = Controls.Instance.transform;
                follower.UpdatePosition();

                ParticleSystem ps = CreateSnowSystem(snowObject);
                ps.Play();
                follower.snow = ps;

                Logger.Log("[LevelSnow] Snow started.");
            }
            catch (System.Exception e)
            {
                Logger.Error("[LevelSnow] Failed to start the snow: " + e);
                Stop();
            }
        }

        static ParticleSystem CreateSnowSystem(GameObject target)
        {
            ParticleSystem ps = target.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 5f;
            main.loop = true;
            main.playOnAwake = true;
            main.prewarm = false;
            main.startLifetime = 12f; // Slowest flake: 14 units at 1 u/s.
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.maxParticles = 5000; // Must be > rate x lifetime (300 x 12).

            var emission = ps.emission;
            emission.rateOverTime = 300f;

            // Flat area above the player.
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 1f;
            shape.radiusThickness = 1f;
            shape.scale = new Vector3(28f, 0.05f, 28f);

            // Slow fall with a bit of sideways drift...
            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.3f, 0.6f);
            velocity.y = new ParticleSystem.MinMaxCurve(-2.2f, -1f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.6f);

            // ...and turbulence, so they swirl a little instead of falling in straight lines.
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.5f;
            noise.frequency = 0.25f;
            noise.scrollSpeed = 0.2f;
            noise.damping = true;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient fadeIn = new Gradient();
            fadeIn.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.05f), new GradientAlphaKey(1f, 1f) });
            colorOverLifetime.color = fadeIn;

            // Flakes die when they hit the level (what the player can stand on is solid), so structures shelter what's under them.
            var collision = ps.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.quality = ParticleSystemCollisionQuality.High;
            collision.collidesWith = Controls.Instance.m_groundCheckLayerMask;
            collision.dampen = 1f;
            collision.bounce = 0f;
            collision.lifetimeLoss = 1f;
            collision.radiusScale = 0.05f;

            // Water volumes are triggers (particle collisions ignore those): flakes that enter or are inside one die. The follower assigns the colliders.
            var trigger = ps.trigger;
            trigger.enabled = true;
            trigger.enter = ParticleSystemOverlapAction.Kill;
            trigger.inside = ParticleSystemOverlapAction.Kill;
            trigger.exit = ParticleSystemOverlapAction.Ignore;
            trigger.outside = ParticleSystemOverlapAction.Ignore;

            var renderer = target.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.minParticleSize = 0.0008f;
            renderer.maxParticleSize = 0.5f;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sharedMaterial = snowMaterial;

            return ps;
        }

        public static void Stop()
        {
            if (snowObject != null) Object.Destroy(snowObject);
            if (snowMaterial != null) Object.Destroy(snowMaterial);
            snowObject = null;
            snowMaterial = null;
        }
    }

    /// <summary>
    /// Keeps the snow emitter above the player (in world space, without inheriting the player's rotation) and the particle system playing and out of the water.
    /// </summary>
    public class LevelSnowFollower : MonoBehaviour
    {
        public const float HEIGHT = 14f;

        public Transform target;
        public ParticleSystem snow;

        float nextWaterRefresh;
        readonly List<BoxCollider> waterColliders = new List<BoxCollider>();

        void LateUpdate()
        {
            // The player can get replaced (respawn/scene reload), pick the new one up instead of stopping the snow.
            if (target == null && Controls.Instance != null) target = Controls.Instance.transform;

            UpdatePosition();

            if (snow != null && !snow.isPlaying)
            {
                Logger.Warning("[LevelSnow] The snow had stopped playing, restarting it.");
                snow.Play();
            }

            if (Time.time >= nextWaterRefresh && snow != null && target != null)
            {
                nextWaterRefresh = Time.time + 1f;
                WeatherWater.RefreshTriggerColliders(snow, target.position, waterColliders);
            }
        }

        public void UpdatePosition()
        {
            if (target != null) transform.SetPositionAndRotation(target.position + Vector3.up * HEIGHT, Quaternion.identity);
        }
    }
}
