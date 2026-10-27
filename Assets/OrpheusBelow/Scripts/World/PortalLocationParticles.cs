using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>Small upward motes make chamber exits legible without obscuring the portal art.</summary>
    [DisallowMultipleComponent]
    public sealed class PortalLocationParticles : MonoBehaviour
    {
        private ParticleSystem _particles;

        private void Awake()
        {
            _particles = gameObject.AddComponent<ParticleSystem>();
            var main = _particles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.duration = 2f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.65f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 0.95f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.055f, 0.13f);
            main.startColor = new Color(1f, 0.77f, 0.42f, 0.68f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 48;
            var emission = _particles.emission;
            emission.rateOverTime = 12f;
            var shape = _particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.46f;
            shape.radiusThickness = 0.25f;
            var velocity = _particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.y = new ParticleSystem.MinMaxCurve(0.25f, 0.75f);
            var color = _particles.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.95f, 0.72f), 0f), new GradientColorKey(new Color(1f, 0.42f, 0.16f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.72f, 0.2f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;
            var renderer = _particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 12;
        }

        public void SetAccent(Color accent)
        {
            if (_particles == null) return;
            var main = _particles.main;
            Color bright = new Color(Mathf.Clamp01(accent.r + 0.28f), Mathf.Clamp01(accent.g + 0.28f), Mathf.Clamp01(accent.b + 0.28f));
            main.startColor = new Color(bright.r, bright.g, bright.b, 0.78f);
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(bright, 0f), new GradientColorKey(accent, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.72f, 0.2f), new GradientAlphaKey(0f, 1f) });
            var color = _particles.colorOverLifetime;
            color.color = gradient;
        }
    }
}
