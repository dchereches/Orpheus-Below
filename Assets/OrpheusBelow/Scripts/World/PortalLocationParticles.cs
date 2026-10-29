using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>Floor and airborne dust streams into the physical passage.</summary>
    [DisallowMultipleComponent]
    public sealed class PortalLocationParticles : MonoBehaviour
    {
        private const int Capacity = 48;
        private const float EmissionInterval = 1f / 20f;
        private readonly ParticleSystem.Particle[] _buffer = new ParticleSystem.Particle[Capacity];
        private readonly Mote[] _motes = new Mote[Capacity];
        private ParticleSystem _particles;
        private Material _material;
        private Transform _doorway;
        private bool _isUnlocked;
        private float _emissionClock;
        private uint _sequence;
        private Color _accent = new Color(.95f, .84f, .58f);

        private struct Mote
        {
            public Vector3 start, target;
            public float age, lifetime, size;
            public uint seed;
        }

        public Vector3 AttractionPoint => (_doorway != null ? _doorway.position : transform.position) + Vector3.up * 1.1f;

        private void Awake() => EnsureInitialized();

        public void SetDoorway(Transform doorway)
        {
            if (_doorway == doorway) return;
            _doorway = doorway;
            ClearMotes();
        }

        public void EnsureInitialized()
        {
            if (_particles != null) return;
            _particles = GetComponent<ParticleSystem>();
            if (_particles == null) _particles = gameObject.AddComponent<ParticleSystem>();
            // Serialized legacy systems may already be playing. Stop before changing duration.
            _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = _particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 3f;
            main.startSpeed = 0f;
            main.startLifetime = 3f;
            main.startSize = .1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.useUnscaledTime = false;
            main.maxParticles = Capacity;
            var emission = _particles.emission; emission.enabled = false;
            var shape = _particles.shape; shape.enabled = false;
            var velocity = _particles.velocityOverLifetime; velocity.enabled = false;
            var force = _particles.forceOverLifetime; force.enabled = false;
            var noise = _particles.noise; noise.enabled = false;
            var size = _particles.sizeOverLifetime; size.enabled = false;
            var color = _particles.colorOverLifetime; color.enabled = false;
            var trails = _particles.trails; trails.enabled = false;
            var renderer = _particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 16;
            renderer.SetActiveVertexStreams(new System.Collections.Generic.List<ParticleSystemVertexStream>
                { ParticleSystemVertexStream.Position, ParticleSystemVertexStream.Color, ParticleSystemVertexStream.UV });
            // Sprite shaders require per-SpriteRenderer uniforms that particle renderers do not provide.
            Shader shader = Resources.Load<Shader>("Art/DoorwayDust");
            if (shader != null)
            {
                _material = new Material(shader) { name = "DoorwaySuction_MoteMat" };
                if (RenderPipeline2DSetup.SoftGlowSprite != null)
                    _material.mainTexture = RenderPipeline2DSetup.SoftGlowSprite.texture;
                renderer.sharedMaterial = _material;
            }
        }

        public void SetUnlocked(bool unlocked)
        {
            EnsureInitialized();
            if (_isUnlocked == unlocked) { if (!unlocked) ClearMotes(); return; }
            _isUnlocked = unlocked;
            ClearMotes();
            if (unlocked && isActiveAndEnabled) PrepareRenderer();
        }

        public void SetAccent(Color accent) => _accent = accent;

        private void PrepareRenderer()
        {
            EnsureInitialized();
            // SetParticles supplies every position and lifetime. Keep native simulation paused
            // so its automatic clock cannot move them twice or continue through a menu/voice.
            _particles.Play(false);
            _particles.Pause(false);
            var map = GetComponentInParent<PaintedChamberMap>();
            if (map != null && map.definition != null)
                _particles.GetComponent<ParticleSystemRenderer>().sortingOrder = PaintedChamberMap.DepthOrder(map.ToImage(AttractionPoint).y) + 2;
        }

        private void OnEnable() { if (_isUnlocked) PrepareRenderer(); }
        private void OnDisable() => ClearMotes();
        private void OnDestroy()
        {
            if (_material == null) return;
            if (Application.isPlaying) Destroy(_material);
            else DestroyImmediate(_material);
        }
        private void Update() => Advance(Time.deltaTime);

        private void Advance(float deltaTime)
        {
            if (!_isUnlocked || !isActiveAndEnabled || deltaTime <= 0f || Time.timeScale <= 0f ||
                UnderworldFrontEnd.BlocksGameplay || OrpheusHud.IsTutorialModalActive || VoiceDirector.BlocksGameplay) return;
            _emissionClock += deltaTime;
            int count = 0;
            for (int i = 0; i < Capacity; i++)
            {
                Mote mote = _motes[i];
                mote.age += deltaTime;
                if (mote.lifetime <= 0f || mote.age >= mote.lifetime)
                {
                    if (_emissionClock < EmissionInterval) { _motes[i] = default; continue; }
                    _emissionClock -= EmissionInterval;
                    mote = SpawnMote(++_sequence);
                }
                _motes[i] = mote;
                float t = Mathf.Clamp01(mote.age / mote.lifetime);
                Color tint = _accent;
                tint.a = Mathf.SmoothStep(0f, .8f, t / .16f)
                    * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.82f, 1f, t)));
                _buffer[count++] = new ParticleSystem.Particle
                {
                    position = EvaluatePath(mote.start, mote.target, t),
                    startColor = tint,
                    startSize = mote.size * Mathf.Lerp(1f, .25f, t * t),
                    startLifetime = mote.lifetime,
                    remainingLifetime = Mathf.Max(.01f, mote.lifetime - mote.age),
                    randomSeed = mote.seed
                };
            }
            _emissionClock = Mathf.Min(_emissionClock, EmissionInterval * 2f);
            _particles.SetParticles(_buffer, count);
        }

        private Mote SpawnMote(uint seed)
        {
            Transform door = _doorway != null ? _doorway : transform;
            // A broad volume on the chamber side, with no circular emission shape.
            Vector3 offset = door.right * Mathf.Lerp(-3.1f, 3.1f, Sample(seed, 1))
                - door.forward * Mathf.Lerp(1.8f, 5.8f, Sample(seed, 2))
                + Vector3.up * Mathf.Lerp(.08f, 2.5f, Sample(seed, 3));
            return new Mote
            {
                start = door.position + offset,
                target = AttractionPoint,
                lifetime = Mathf.Lerp(1.35f, 2.1f, Sample(seed, 4)),
                size = Mathf.Lerp(.12f, .20f, Sample(seed, 5)),
                seed = seed
            };
        }

        // Fixed endpoints ignore emitter scale. Increasing speed makes the final approach suction.
        public static Vector3 EvaluatePath(Vector3 start, Vector3 target, float normalizedAge)
            => Vector3.LerpUnclamped(start, target, Mathf.Pow(Mathf.Clamp01(normalizedAge), 1.65f));

        private static float Sample(uint seed, uint channel)
        {
            uint value = seed * 747796405u + channel * 2891336453u;
            value = ((value >> (int)((value >> 28) + 4)) ^ value) * 277803737u;
            value = (value >> 22) ^ value;
            return (value & 0x00ffffff) / 16777215f;
        }

        private void ClearMotes()
        {
            System.Array.Clear(_motes, 0, _motes.Length);
            _emissionClock = 0f;
            _sequence = 0;
            if (_particles != null) _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
