using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OrpheusBelow.Tests
{
    public sealed class WorldEffectPauseTests : CombatGameplayTestContext
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _root;

        [TearDown]
        public void TearDown() { if (_root != null) Object.DestroyImmediate(_root); }

        [TestCase(typeof(PacifiedSpiritVessel), "_elapsed", "_soulSr")]
        [TestCase(typeof(GroundShockwaveRing), "_elapsed", "_sr")]
        [TestCase(typeof(GroundSlashArcVfx), "_life", "_sr")]
        [TestCase(typeof(WeepingShadeFleeSink), "_elapsed", "_bodySr")]
        [TestCase(typeof(RisingAshParticle), "_life", "_sr")]
        [TestCase(typeof(GoldenResonanceBeamVfx), "_life", "_outerSr")]
        [TestCase(typeof(SirenConvergenceVortexVfx), "_elapsed", "_ringA")]
        public void MenuPausePreservesEffectPoseOpacityAndLifetime(Type type, string timer, string rendererField)
        {
            _root = new GameObject("PausedWorldEffect");
            Component effect = _root.AddComponent(type);
            var visual = new GameObject("EffectVisual").AddComponent<SpriteRenderer>();
            visual.transform.SetParent(_root.transform, false);
            visual.color = new Color(.4f, .5f, .6f, .73f);
            visual.transform.localScale = new Vector3(2, 3, 1);
            _root.transform.SetPositionAndRotation(new Vector3(4, 5, 6), Quaternion.Euler(12, 24, 36));
            Set(effect, rendererField, visual);
            Set(effect, timer, .2f);
            SetIfPresent(effect, "_duration", 1f);
            SetIfPresent(effect, "_maxLife", 1f);
            SetIfPresent(effect, "_baseScale", new Vector3(2, 3, 1));
            SetIfPresent(effect, "_startBodyScale", new Vector3(2, 3, 1));
            SetIfPresent(effect, "_startColor", visual.color);
            SetIfPresent(effect, "_col", visual.color);
            SetIfPresent(effect, "_targetDiameter", 4f);
            SetIfPresent(effect, "_maxDiameter", 4f);
            SetIfPresent(effect, "_length", 3f);
            Vector3 position = _root.transform.position;
            Quaternion rotation = _root.transform.rotation;
            Vector3 scale = _root.transform.localScale;
            Vector3 visualScale = visual.transform.localScale;
            Color color = visual.color;
            MethodInfo update = type.GetMethod("Update", Hidden);

            SetMenuPause(true);
            update.Invoke(effect, null);
            update.Invoke(effect, null);

            Assert.That(_root != null, Is.True);
            Assert.That(type.GetField(timer, Hidden).GetValue(effect), Is.EqualTo(.2f), "Pausing must retain the effect's remaining lifetime.");
            Assert.That(_root.transform.position, Is.EqualTo(position));
            Assert.That(_root.transform.rotation, Is.EqualTo(rotation));
            Assert.That(_root.transform.localScale, Is.EqualTo(scale));
            Assert.That(visual.transform.localScale, Is.EqualTo(visualScale));
            Assert.That(visual.color, Is.EqualTo(color), "A paused chamber must retain the effect's exact opacity.");

            // EditMode can retain a frame delta longer than these effects' whole lifetimes after
            // a long synchronous suite. Only manually resume effects with adjustable lifetimes;
            // constant-duration ascension/sink/vortex expiration belongs in PlayMode verification.
            if (timer == "_elapsed" && type.GetField("_duration", Hidden) == null) return;
            float safeLifetime = Mathf.Max(1000f, Time.unscaledDeltaTime * 4f);
            if (timer == "_life") Set(effect, timer, safeLifetime);
            SetIfPresent(effect, "_maxLife", safeLifetime * 2f);
            SetIfPresent(effect, "_duration", safeLifetime);
            SetMenuPause(false);
            update.Invoke(effect, null);
            Assert.That(visual.color, Is.Not.EqualTo(color), "The existing visual animation must resume after the menu closes.");
        }

        private static void SetMenuPause(bool paused) => typeof(UnderworldFrontEnd)
            .GetField("_menuBlocksGameplay", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, paused);
        private static void Set(Component effect, string name, object value) => effect.GetType().GetField(name, Hidden).SetValue(effect, value);
        private static void SetIfPresent(Component effect, string name, object value) => effect.GetType().GetField(name, Hidden)?.SetValue(effect, value);
    }
}
