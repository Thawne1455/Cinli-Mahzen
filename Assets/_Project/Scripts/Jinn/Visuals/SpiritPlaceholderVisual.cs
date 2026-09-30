using UnityEngine;

namespace CinliMahzen.Jinn
{
    /// <summary>
    /// Placeholder spirit look (B0.2): the prefab holds a transparent sphere body with two eyes;
    /// this component adds a world-space particle trail and a cosmetic bob. Purely visual — the
    /// layer of the root (EvilJinnVisual / GoodJinnVisual) is copied to the trail so camera
    /// culling and VisibilityService treat it like the body.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SpiritPlaceholderVisual : MonoBehaviour
    {
        private const float FullCircle = 2f * Mathf.PI;

        [SerializeField] private Transform body;

        [Header("Trail")]
        [SerializeField] private Material trailMaterial;
        [SerializeField] private Color trailColor = new Color(0.6f, 0.2f, 1f, 0.6f);
        [Min(0f)] [SerializeField] private float trailLifetime = 0.8f;
        [Min(0f)] [SerializeField] private float trailSize = 0.18f;
        [Min(0f)] [SerializeField] private float trailEmitRadius = 0.15f;
        [Tooltip("Particles per metre travelled")]
        [Min(0f)] [SerializeField] private float trailRatePerMeter = 6f;
        [Tooltip("Particles per second while hovering in place")]
        [Min(0f)] [SerializeField] private float trailIdleRate = 4f;
        [Min(1)] [SerializeField] private int trailMaxParticles = 64;

        [Header("Bob")]
        [Min(0f)] [SerializeField] private float bobAmplitude = 0.06f;
        [Min(0f)] [SerializeField] private float bobFrequency = 1.2f;

        private Vector3 _bodyRestPosition;
        private float _phase;

        public ParticleSystem Trail { get; private set; }

        private void Awake()
        {
            if (body != null)
            {
                _bodyRestPosition = body.localPosition;
            }
            Trail = BuildTrail();
        }

        private void LateUpdate()
        {
            if (body == null || bobAmplitude <= 0f)
            {
                return;
            }
            _phase += Time.deltaTime * bobFrequency * FullCircle;
            if (_phase > FullCircle)
            {
                _phase -= FullCircle;
            }
            body.localPosition = _bodyRestPosition + new Vector3(0f, Mathf.Sin(_phase) * bobAmplitude, 0f);
        }

        private ParticleSystem BuildTrail()
        {
            var go = new GameObject("Trail");
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = trailLifetime;
            main.startSpeed = 0f;
            main.startSize = trailSize;
            main.startColor = trailColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = trailMaxParticles;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = trailIdleRate;
            emission.rateOverDistance = trailRatePerMeter;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = trailEmitRadius;

            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
            color.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            color.color = fade;

            var psRenderer = go.GetComponent<ParticleSystemRenderer>();
            psRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            psRenderer.receiveShadows = false;
            if (trailMaterial != null)
            {
                psRenderer.sharedMaterial = trailMaterial;
            }

            ps.Play();
            return ps;
        }
    }
}
