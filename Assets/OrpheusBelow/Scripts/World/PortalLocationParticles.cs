using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>Small, clean upward motes make chamber exits legible without obscuring the portal art.</summary>
    [DisallowMultipleComponent]
    public sealed class PortalLocationParticles : MonoBehaviour
    {
        private ParticleSystem _particles;
        private bool _isUnlocked = true;

        private void Awake()
        {
            EnsureInitialized();
        }

        public void EnsureInitialized()
        {
            if (_particles != null) return;
            _particles = GetComponent<ParticleSystem>();
            if (_particles == null) _particles = gameObject.AddComponent<ParticleSystem>();
            _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = _particles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.duration = 2f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 1.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.065f, 0.13f);
            main.startColor = new Color(0.5f, 0.95f, 0.8f, 0.72f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 36;

            var emission = _particles.emission;
            emission.rateOverTime = 12f;
            emission.enabled = _isUnlocked;

            var shape = _particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.52f;
            shape.radiusThickness = 0.35f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var velocity = _particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.y = new ParticleSystem.MinMaxCurve(0.35f, 0.75f);
            velocity.x = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);

            var size = _particles.sizeOverLifetime;
            size.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.35f);
            sizeCurve.AddKey(0.2f, 1.0f);
            sizeCurve.AddKey(0.7f, 0.85f);
            sizeCurve.AddKey(1f, 0.0f);
            size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var color = _particles.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(0.75f, 1f, 0.92f), 0f), new GradientColorKey(new Color(0.25f, 0.85f, 0.72f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.72f, 0.18f), new GradientAlphaKey(0.55f, 0.65f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;

            var renderer = _particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 16;

            Material sprMat = RenderPipeline2DSetup.Get2DSpriteMaterial();
            if (sprMat != null)
            {
                Material pMat = new Material(sprMat) { name = "PortalParticles_MoteMat" };
                if (RenderPipeline2DSetup.SoftGlowSprite != null)
                    pMat.mainTexture = RenderPipeline2DSetup.SoftGlowSprite.texture;
                renderer.material = pMat;
            }
        }

        public void SetUnlocked(bool unlocked)
        {
            _isUnlocked = unlocked;
            EnsureInitialized();
            if (_particles == null) return;

            var emission = _particles.emission;
            emission.enabled = unlocked;
            if (unlocked)
            {
                if (!_particles.isPlaying) _particles.Play();
            }
            else
            {
                _particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        public void SetAccent(Color accent)
        {
            EnsureInitialized();
            if (_particles == null) return;
            var main = _particles.main;
            Color bright = new Color(Mathf.Clamp01(accent.r + 0.28f), Mathf.Clamp01(accent.g + 0.28f), Mathf.Clamp01(accent.b + 0.28f));
            main.startColor = new Color(bright.r, bright.g, bright.b, 0.78f);
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(bright, 0f), new GradientColorKey(accent, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.72f, 0.18f), new GradientAlphaKey(0.55f, 0.65f), new GradientAlphaKey(0f, 1f) });
            var color = _particles.colorOverLifetime;
            color.color = gradient;
        }
    }
}
