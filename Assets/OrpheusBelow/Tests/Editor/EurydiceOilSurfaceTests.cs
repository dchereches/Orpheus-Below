using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEngine;

namespace OrpheusBelow.Tests
{
    public sealed class EurydiceOilSurfaceTests
    {
        // The user explicitly chose the e0d1adc artwork/proportions as the immutable base.
        [TestCase("Idle","565f9e25dab6d31ae4dabf30dfffa5b4baaa46520353a21b5a2edde81b4a44d4")]
        [TestCase("Walk","00e1bc26ba7ff686f7663dd2a85b9478d9768b1c6c2f7801018cb5d6990f58ee")]
        [TestCase("Reactions","70675a89aae5d757ff5e7b1bbec64531b63d1d3670f35a7f72b415ece3340491")]
        [TestCase("Stairs","f8c40ec440cb717b15008db97e72a60839cdd9b061983f745fc274604a05e361")]
        [TestCase("Effects","4b59d168f51702678db68e41ea6ae35b950124c11b664400c210e770e3acd1be")]
        public void CommittedProportionsAndAnimationSourcesRemainIntact(string name,string expected)
        {
            using(var sha=SHA256.Create())
            {
                byte[] hash=sha.ComputeHash(File.ReadAllBytes("Assets/OrpheusBelow/Resources/Characters/Eurydice/Look02/"+name+".png"));
                Assert.That(System.BitConverter.ToString(hash).Replace("-","").ToLowerInvariant(),Is.EqualTo(expected));
            }
        }
        private static void Apply(SpriteRenderer renderer)
        {
            var type=typeof(EurydiceSpriteAnimator).Assembly.GetType("OrpheusBelow.EurydiceOilSurface");
            Assert.That(type,Is.Not.Null,"A surface-only paint treatment must preserve the original sprite geometry.");
            type.GetMethod("Apply",BindingFlags.Static|BindingFlags.Public).Invoke(null,new object[]{renderer});
        }
        [Test]
        public void PaintTreatmentCannotReplaceOrScaleTheCommittedFigure()
        {
            var go=new GameObject("LockedEurydiceSurface");
            try
            {
                var sprite=Resources.Load<EurydiceAnimationLibrary>(EurydiceAnimationLibrary.ResourcePath).idle[0];
                var renderer=go.AddComponent<SpriteRenderer>(); renderer.sprite=sprite;
                var vertices=sprite.vertices; var pivot=sprite.pivot; var rect=sprite.rect;
                Apply(renderer);
                Assert.That(renderer.sprite,Is.SameAs(sprite)); Assert.That(go.transform.localScale,Is.EqualTo(Vector3.one));
                Assert.That(sprite.pivot,Is.EqualTo(pivot)); Assert.That(sprite.rect,Is.EqualTo(rect));
                CollectionAssert.AreEqual(vertices,sprite.vertices);
                Assert.That(renderer.sharedMaterial.shader.name,Is.EqualTo("OrpheusBelow/EurydiceOilSurface"));
            }
            finally { Object.DestroyImmediate(go); }
        }
        [Test]
        public void FrameChangesUpdateTheSamplingBoundsWithoutChangingTheAnimation()
        {
            var go=new GameObject("LockedSurfaceFrameChange");
            try
            {
                var library=Resources.Load<EurydiceAnimationLibrary>(EurydiceAnimationLibrary.ResourcePath);
                var renderer=go.AddComponent<SpriteRenderer>();
                foreach(var sprite in new[]{library.idle[0],library.walk[7],library.reactions[12],library.stairs[18]})
                {
                    renderer.sprite=sprite; Apply(renderer);
                    var block=new MaterialPropertyBlock(); renderer.GetPropertyBlock(block);
                    var rect=sprite.rect; var expected=new Vector4(rect.x/sprite.texture.width,rect.y/sprite.texture.height,rect.width/sprite.texture.width,rect.height/sprite.texture.height);
                    Assert.That(block.GetVector("_FrameRect"),Is.EqualTo(expected));
                    Assert.That(renderer.sprite,Is.SameAs(sprite));
                }
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
