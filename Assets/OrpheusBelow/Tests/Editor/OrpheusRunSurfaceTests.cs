using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace OrpheusBelow.Tests
{
    public class OrpheusRunSurfaceTests
    {
        private GameObject _root;
        private OrpheusSpriteAnimator _animator;
        private SpriteRenderer _body;
        private SpriteRenderer _effects;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("RunSurfaceContract");
            _animator = _root.AddComponent<OrpheusSpriteAnimator>();
            _body = _root.AddComponent<SpriteRenderer>();
            var effects = new GameObject("Effects"); effects.transform.SetParent(_root.transform);
            _effects = effects.AddComponent<SpriteRenderer>();
            _animator.Initialize(_body, _effects, null, Resources.Load<OrpheusAnimationLibrary>(OrpheusAnimationLibrary.ResourcePath));
        }

        [TearDown] public void TearDown() => Object.DestroyImmediate(_root);

        [Test]
        public void EveryRunPhaseAndFacingUsesTheExactIdleMaterialAndOpacityPolicy()
        {
            Material idleMaterial = _body.sharedMaterial;
            var tint = new Color(.8f, .65f, .4f, .47f);
            _body.color = tint; _body.sortingOrder = 27;
            for (int direction = 0; direction < 8; direction++)
            {
                float angle = direction * Mathf.PI * .25f;
                Vector3 travel = (UnderworldCamera2D.IsoRightXZ * Mathf.Sin(angle) - UnderworldCamera2D.IsoUpXZ * Mathf.Cos(angle)) * .21f;
                for (int phase = 0; phase < 24; phase++)
                {
                    _animator.Drive(new OrpheusAnimationInput { deltaTime = .035f, groundDelta = travel, movementRequested = true });
                    Assert.That(_body.sharedMaterial, Is.SameAs(idleMaterial));
                    Assert.That(_body.sharedMaterial.shader.name, Is.EqualTo("OrpheusBelow/OrpheusOilSurface"));
                    Assert.That(_body.sharedMaterial.HasProperty("_RunSoftness"), Is.False,
                        "No run-only blur, desaturation or alpha curve may be reintroduced.");
                    Assert.That(_body.color, Is.EqualTo(tint));
                    Assert.That(_body.sortingOrder, Is.EqualTo(27));
                    Assert.That(_body.transform.localScale, Is.EqualTo(Vector3.one));
                    Assert.That(_effects.enabled, Is.False);
                }
            }
            _animator.ResetMotion(); _animator.TriggerHit(false);
            Assert.That(_body.sharedMaterial, Is.SameAs(idleMaterial));
            Assert.That(_body.color, Is.EqualTo(tint));
            Assert.That(_root.GetComponentsInChildren<SpriteRenderer>(), Has.Length.EqualTo(2),
                "Locomotion must render one body and no cloned silhouettes.");
        }

        [Test]
        public void ReturningFromRunCannotChangeRenderedColourOrPartiallyTransparentEdges()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) Assert.Ignore("Surface pixel comparison requires a graphics device.");
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false, true);
            var pixels = new Color[32 * 32];
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
                pixels[y * 32 + x] = new Color(x % 4 < 2 ? .8f : .2f, .35f, .12f, .15f + .85f * x / 31f);
            texture.SetPixels(pixels); texture.Apply(); texture.filterMode = FilterMode.Bilinear;
            var sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), Vector2.one * .5f, 32f);
            var target = RenderTexture.GetTemporary(32, 32, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Texture2D idle = null, afterRun = null;
            try
            {
                _body.sprite = sprite; OrpheusOilSurface.Apply(_body, null); idle = Render(target);
                _animator.Drive(new OrpheusAnimationInput { deltaTime = .1f,
                    groundDelta = UnderworldCamera2D.IsoRightXZ * .6f, movementRequested = true });
                // Reuse identical reference paint after the runtime run state has applied its
                // surface. A hidden run-only filter or alpha policy must change these pixels.
                _body.sprite = sprite; _body.ResetLocalBounds(); OrpheusOilSurface.Apply(_body, null);
                afterRun = Render(target);
                Assert.That(afterRun.GetPixels32(), Is.EqualTo(idle.GetPixels32()),
                    "Reference paint, including its soft alpha edge, must render identically before and during locomotion.");
                Assert.That(idle.GetPixel(24, 16).a, Is.GreaterThan(.5f), "The pixel comparison must actually draw the reference.");
            }
            finally
            {
                Object.DestroyImmediate(idle); Object.DestroyImmediate(afterRun);
                Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); RenderTexture.ReleaseTemporary(target);
            }
        }

        private Texture2D Render(RenderTexture target)
        {
            using (var commands = new CommandBuffer())
            {
                commands.SetRenderTarget(target); commands.ClearRenderTarget(true, true, Color.clear);
                commands.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.Ortho(-.5f, .5f, -.5f, .5f, -10f, 10f));
                commands.DrawRenderer(_body, _body.sharedMaterial); Graphics.ExecuteCommandBuffer(commands);
            }
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = target;
                var image = new Texture2D(32, 32, TextureFormat.RGBA32, false, true);
                image.ReadPixels(new Rect(0, 0, 32, 32), 0, 0); image.Apply(); return image;
            }
            finally { RenderTexture.active = previous; }
        }
    }
}
