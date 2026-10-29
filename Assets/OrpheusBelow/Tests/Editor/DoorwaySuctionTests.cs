using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OrpheusBelow.Tests
{
    public sealed class DoorwaySuctionTests : CombatGameplayTestContext
    {
        private GameObject _root;
        private float _previousScale;
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

        [SetUp]
        public void SetUp()
        {
            _previousScale = Time.timeScale;
            Time.timeScale = 1f;
            _root = new GameObject("DoorwaySuctionContract");
            _root.transform.position = new Vector3(20f, 0f, -12f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Time.timeScale = _previousScale;
        }

        [Test]
        public void DustMaterialConsumesParticleColorWithoutSpriteRendererUniforms()
        {
            var motes = _root.AddComponent<PortalLocationParticles>();
            motes.EnsureInitialized();
            var renderer = _root.GetComponent<ParticleSystemRenderer>();
            Assert.That(renderer.sharedMaterial, Is.Not.Null);
            Assert.That(renderer.sharedMaterial.shader.name, Is.EqualTo("OrpheusBelow/DoorwayDust"));
            Assert.That(renderer.sharedMaterial.mainTexture, Is.Not.Null);
            Assert.That(renderer.sharedMaterial.HasProperty("_Flip"), Is.False);
            Assert.That(renderer.sharedMaterial.HasProperty("_RendererColor"), Is.False);
            var streams = new System.Collections.Generic.List<ParticleSystemVertexStream>();
            renderer.GetActiveVertexStreams(streams);
            Assert.That(streams, Does.Contain(ParticleSystemVertexStream.Color));
            Assert.That(streams, Does.Contain(ParticleSystemVertexStream.UV));
            motes.SetUnlocked(true);
            Advance(motes, .1f);
            var particles = new ParticleSystem.Particle[48];
            int count = _root.GetComponent<ParticleSystem>().GetParticles(particles);
            Assert.That(count, Is.GreaterThan(0));
            for (int i = 0; i < count; i++)
                Assert.That(particles[i].startSize, Is.InRange(.05f, .2f), "Dust must remain small but visible at the gameplay camera scale.");
        }

        [Test]
        public void LegacyMarkersStayHiddenThroughUnlockWithoutRemovingDoorCollidersOrArtwork()
        {
            _root.AddComponent<PaintedChamberMap>();
            var doorObject = new GameObject("Door");
            doorObject.transform.SetParent(_root.transform, false);
            var trigger = doorObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            var markers = new[] {
                Sprite(doorObject.transform, "ExitThreshold"),
                Sprite(doorObject.transform, "DoorThresholdRune"),
                Sprite(doorObject.transform, "ArchwayPortalGlow")
            };
            markers[1].gameObject.SetActive(false);
            var artwork = Sprite(doorObject.transform, "PaintedDoorArtwork");
            // Serialized references can also point at sprites outside the door hierarchy.
            var referencedGlow = Sprite(_root.transform, "OldReferencedGlow");
            var referencedBeacon = Sprite(_root.transform, "OldReferencedBeacon");
            var door = doorObject.AddComponent<ChamberExitDoor>();
            door.SetupVisuals(referencedGlow, referencedBeacon);

            foreach (bool unlocked in new[] { false, true, false, true })
            {
                door.SetUnlocked(unlocked);
                foreach (var marker in markers) Assert.That(marker.enabled, Is.False, marker.name);
                Assert.That(referencedGlow.enabled, Is.False);
                Assert.That(referencedBeacon.enabled, Is.False);
                Assert.That(artwork.enabled, Is.True);
                Assert.That(trigger.enabled && trigger.isTrigger, Is.True);
                var threshold = doorObject.transform.Find("LockedPassage_SolidThreshold").GetComponent<BoxCollider>();
                Assert.That(threshold.enabled, Is.EqualTo(!unlocked));
                Assert.That(threshold.isTrigger, Is.False);
                Assert.That(door.GetComponentsInChildren<PortalLocationParticles>(true).Length, Is.EqualTo(1));
            }
        }

        [Test]
        public void PathConvergesMonotonicallyAndAcceleratesIntoTheDoorway()
        {
            Vector3 start = new Vector3(4f, .1f, -6f), target = new Vector3(0f, 1.1f, 0f);
            Assert.That(PortalLocationParticles.EvaluatePath(start, target, -1f), Is.EqualTo(start));
            Assert.That(PortalLocationParticles.EvaluatePath(start, target, 2f), Is.EqualTo(target));
            float previousDistance = Vector3.Distance(start, target);
            for (int step = 1; step <= 20; step++)
            {
                Vector3 point = PortalLocationParticles.EvaluatePath(start, target, step / 20f);
                float distance = Vector3.Distance(point, target);
                Assert.That(distance, Is.LessThan(previousDistance), "Every step must approach the passage.");
                Assert.That(Vector3.Cross(point - start, target - start).magnitude, Is.LessThan(.0001f));
                previousDistance = distance;
            }
            float earlyTravel = Vector3.Distance(start, PortalLocationParticles.EvaluatePath(start, target, .1f));
            float lateTravel = Vector3.Distance(PortalLocationParticles.EvaluatePath(start, target, .9f), target);
            Assert.That(lateTravel, Is.GreaterThan(earlyTravel));
        }

        [Test]
        public void ActualMotesMoveInwardToDoorAboveThresholdDespiteLegacyEmitterOffsetAndScale()
        {
            _root.transform.rotation = Quaternion.Euler(0f, 67f, 0f);
            var emitter = new GameObject("LegacyScaledPortalEmitter");
            emitter.transform.SetParent(_root.transform, false);
            emitter.transform.localPosition = new Vector3(-.3f, 1.45f, -.3f);
            emitter.transform.localScale = new Vector3(2.2f, 2.9f, 1f);
            var motes = emitter.AddComponent<PortalLocationParticles>();
            motes.SetDoorway(_root.transform);
            motes.SetUnlocked(true);
            Advance(motes, .05f);
            var system = emitter.GetComponent<ParticleSystem>();
            var before = new ParticleSystem.Particle[48];
            int initialCount = system.GetParticles(before);
            Assert.That(initialCount, Is.GreaterThan(0));
            Vector3 target = new Vector3(20f, 1.1f, -12f);
            Assert.That(Vector3.Distance(motes.AttractionPoint, target), Is.LessThan(.0001f));
            for (int i = 0; i < initialCount; i++)
            {
                Assert.That(before[i].startSize, Is.InRange(.12f, .20f),
                    "New motes must read at the normal gameplay camera distance.");
                Assert.That(Vector3.Dot(before[i].position - _root.transform.position, _root.transform.forward),
                    Is.LessThan(-1f), "Motes must originate on the room side of the opening.");
            }

            Advance(motes, .6f);
            var after = new ParticleSystem.Particle[48];
            int count = system.GetParticles(after);
            for (int i = 0; i < initialCount; i++)
            {
                int match = System.Array.FindIndex(after, 0, count, particle => particle.randomSeed == before[i].randomSeed);
                Assert.That(match, Is.GreaterThanOrEqualTo(0));
                Assert.That(Vector3.Distance(after[match].position, target),
                    Is.LessThan(Vector3.Distance(before[i].position, target)));
                Assert.That(after[match].startSize, Is.LessThan(before[i].startSize));
                Assert.That(((Color)after[match].startColor).a, Is.GreaterThan(.65f),
                    "Midlife motes must reach readable opacity before fading near the doorway.");
            }
            Assert.That(system.isPaused, Is.True, "Native particle time must not advance paths a second time.");
            Assert.That(system.main.scalingMode, Is.EqualTo(ParticleSystemScalingMode.Shape),
                "Legacy portal scale must not enlarge the dust into broad glow blobs.");
        }

        [Test]
        public void PauseFreezesMotesAndLockOrDisableClearsThemImmediately()
        {
            var motes = _root.AddComponent<PortalLocationParticles>();
            motes.SetUnlocked(true);
            Advance(motes, .1f);
            var system = _root.GetComponent<ParticleSystem>();
            var before = new ParticleSystem.Particle[48];
            int count = system.GetParticles(before);
            Assert.That(count, Is.GreaterThan(0));
            Time.timeScale = 0f;
            Advance(motes, .5f);
            var after = new ParticleSystem.Particle[48];
            Assert.That(system.GetParticles(after), Is.EqualTo(count));
            for (int i = 0; i < count; i++)
            {
                Assert.That(after[i].position, Is.EqualTo(before[i].position));
                Assert.That(after[i].remainingLifetime, Is.EqualTo(before[i].remainingLifetime));
            }
            motes.SetUnlocked(false);
            Assert.That(system.particleCount, Is.Zero);
            Time.timeScale = 1f;
            Advance(motes, 1f);
            Assert.That(system.particleCount, Is.Zero);
            motes.SetUnlocked(true);
            Advance(motes, .1f);
            Assert.That(system.particleCount, Is.GreaterThan(0));
            _root.SetActive(false);
            Assert.That(system.particleCount, Is.Zero);
        }

        private static SpriteRenderer Sprite(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<SpriteRenderer>();
        }

        private static void Advance(PortalLocationParticles motes, float deltaTime) =>
            typeof(PortalLocationParticles).GetMethod("Advance", Hidden).Invoke(motes, new object[] { deltaTime });
    }
}
