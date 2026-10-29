using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace OrpheusBelow.Tests
{
    public sealed class OrpheusRunSurfaceTests
    {
        private GameObject _root;
        private OrpheusSpriteAnimator _animator;
        private SpriteRenderer _body;
        private SpriteRenderer _effects;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("RunningPaintSurface");
            _animator = _root.AddComponent<OrpheusSpriteAnimator>();
            _body = _root.AddComponent<SpriteRenderer>();
            var effects = new GameObject("Effects"); effects.transform.SetParent(_root.transform);
            _effects = effects.AddComponent<SpriteRenderer>();
            _animator.Initialize(_body, _effects, null,
                Resources.Load<OrpheusAnimationLibrary>(OrpheusAnimationLibrary.ResourcePath));
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_root);

        [Test]
        public void RunningUsesTheSameSoftPaintTreatmentForEveryPhaseAndFacing()
        {
            for (int direction = 0; direction < 8; direction++)
            {
                Vector3 travel = Quaternion.AngleAxis(direction * 45f, Vector3.up) * Vector3.forward;
                for (int phase = 0; phase < 16; phase++)
                {
                    Drive(travel * .13f);
                    Assert.That(Softness(), Is.EqualTo(1f), "Running must not flicker between sharp and faded phases.");
                    Assert.That(_body.sharedMaterial.shader.name, Is.EqualTo("OrpheusBelow/OrpheusOilSurface"));
                    Assert.That(_effects.enabled, Is.False);
                }
            }
        }

        [Test]
        public void RunFinishResetsForIdleAndCannotChangeHitFlashDeathAlphaOrSorting()
        {
            Color tint = new Color(1f, .35f, .25f, .47f);
            _body.color = tint; _body.sortingOrder = 27;
            Drive(Vector3.right * .13f);
            Assert.That(Softness(), Is.EqualTo(1f));
            Assert.That(_body.color, Is.EqualTo(tint), "Paint softness must not overwrite combat tint or death opacity.");
            Assert.That(_body.sortingOrder, Is.EqualTo(27));
            _animator.ResetMotion();
            Assert.That(Softness(), Is.Zero, "The run finish must not leak into the authored idle paint.");
            Assert.That(_body.color, Is.EqualTo(tint));
            Drive(Vector3.right * .13f);
            _animator.TriggerHit(false);
            Assert.That(Softness(), Is.Zero);
            Assert.That(_body.color, Is.EqualTo(tint));
            Assert.That(_root.GetComponentsInChildren<SpriteRenderer>(), Has.Length.EqualTo(2),
                "Softening uses the existing body; it must not introduce a second silhouette.");
        }

        [Test]
        public void SofteningKeepsTheCompletePosedSpriteAndBounds()
        {
            var library = Resources.Load<OrpheusAnimationLibrary>(OrpheusAnimationLibrary.ResourcePath);
            for (int phase = 0; phase < 16; phase++)
            {
                Drive(Vector3.right * .13f);
                Sprite expected = library.Frame(OrpheusAnimationState.Run, _animator.Facing, _animator.CurrentFrame);
                Assert.That(_body.sprite, Is.SameAs(expected));
                foreach (Vector2 vertex in expected.vertices)
                {
                    Vector3 actual = new Vector3(_body.flipX ? -vertex.x : vertex.x, vertex.y, 0f);
                    Assert.That(_body.localBounds.Contains(actual), Is.True,
                        "The faded finish must keep the full reaching boot, coat and head visible.");
                }
                Assert.That(_body.transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(_body.color.a, Is.EqualTo(1f), "The run must remain a solid character, not a ghost.");
            }
        }

        [TestCase(2)]
        [TestCase(4)]
        public void RunningFinishReducesFineContrastWithoutGhostingOpaquePaint(int stripeWidth)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("This paint comparison needs a graphics device.");
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false, true);
            var pixels = new Color[32 * 32];
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
            {
                // Four-texel brush detail also needs softening: a one-texel filter can
                // pass the fine-stripe comparison while leaving the face too crisp.
                float value = x % (stripeWidth * 2) < stripeWidth ? .15f : .85f;
                pixels[y * 32 + x] = new Color(value, value, value, 1f);
            }
            texture.SetPixels(pixels); texture.Apply(); texture.filterMode = FilterMode.Bilinear;
            var sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), Vector2.one * .5f, 32f);
            var target = RenderTexture.GetTemporary(32, 32, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Texture2D idle = null, run = null;
            try
            {
                _body.sprite = sprite;
                OrpheusOilSurface.Apply(_body, null);
                idle = Render(target);
                OrpheusOilSurface.Apply(_body, null, true);
                run = Render(target);
                Assert.That(Contrast(run), Is.LessThan(Contrast(idle) * .9f),
                    "Running paint should lose the hard fine-detail contrast that made it pop against idle.");
                Assert.That(run.GetPixel(16, 16).a, Is.GreaterThan(.95f),
                    "Fading must keep the opaque figure solid instead of revealing scenery through its torso.");
            }
            finally
            {
                Object.DestroyImmediate(idle); Object.DestroyImmediate(run);
                Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture);
                RenderTexture.ReleaseTemporary(target);
            }
        }

        private Texture2D Render(RenderTexture target)
        {
            using (var commands = new CommandBuffer())
            {
                commands.SetRenderTarget(target);
                commands.ClearRenderTarget(true, true, Color.clear);
                commands.SetViewProjectionMatrices(Matrix4x4.identity,
                    Matrix4x4.Ortho(-.5f, .5f, -.5f, .5f, -10f, 10f));
                commands.DrawRenderer(_body, _body.sharedMaterial);
                Graphics.ExecuteCommandBuffer(commands);
            }
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = target;
                var image = new Texture2D(32, 32, TextureFormat.RGBA32, false, true);
                image.ReadPixels(new Rect(0, 0, 32, 32), 0, 0); image.Apply();
                return image;
            }
            finally { RenderTexture.active = previous; }
        }

        private static float Contrast(Texture2D image)
        {
            float minimum = 1f, maximum = 0f;
            for (int y = 8; y < 24; y++) for (int x = 8; x < 24; x++)
            {
                float value = image.GetPixel(x, y).grayscale;
                minimum = Mathf.Min(minimum, value); maximum = Mathf.Max(maximum, value);
            }
            return maximum - minimum;
        }

        private void Drive(Vector3 travel) => _animator.Drive(new OrpheusAnimationInput
        {
            deltaTime = .016f, groundDelta = travel, aimDirection = travel, movementRequested = true
        });

        private float Softness()
        {
            var properties = new MaterialPropertyBlock(); _body.GetPropertyBlock(properties);
            return properties.GetFloat("_RunSoftness");
        }
    }
}
