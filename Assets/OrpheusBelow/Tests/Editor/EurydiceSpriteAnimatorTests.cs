using NUnit.Framework;
using UnityEngine;

namespace OrpheusBelow.Tests
{
    public class EurydiceSpriteAnimatorTests
    {
        [Test]
        public void SlicesSixteenEqualFramesFromSquareSheet()
        {
            Texture2D texture = new Texture2D(2048, 2048);
            Sprite[] sprites = EurydiceSpriteAnimator.SliceSheet(texture);

            Assert.That(sprites, Has.Length.EqualTo(16));
            Assert.That(sprites[0].rect, Is.EqualTo(new Rect(0f, 1536f, 512f, 512f)));
            Assert.That(sprites[3].rect, Is.EqualTo(new Rect(1536f, 1536f, 512f, 512f)));
            Assert.That(sprites[12].rect, Is.EqualTo(new Rect(0f, 0f, 512f, 512f)));

            foreach (Sprite sprite in sprites) Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
        }

        [TestCase(EurydiceAnimationState.Idle, 0, 0)]
        [TestCase(EurydiceAnimationState.Glide, 2, 6)]
        [TestCase(EurydiceAnimationState.Run, 2, 10)]
        [TestCase(EurydiceAnimationState.Teaching, 3, 15)]
        [TestCase(EurydiceAnimationState.Shatter, 3, 15)]
        public void MapsAnimationStateToExpectedRow(EurydiceAnimationState state, int frame, int expectedIndex)
        {
            Assert.That(EurydiceSpriteAnimator.GetFrameIndex(state, frame), Is.EqualTo(expectedIndex));
        }

        [Test]
        public void ReturnsEmptyForNullOrNonDivisibleSheet()
        {
            Assert.That(EurydiceSpriteAnimator.SliceSheet(null), Is.Empty);

            Texture2D invalid = new Texture2D(1025, 1024);
            Assert.That(EurydiceSpriteAnimator.SliceSheet(invalid), Is.Empty);
            Object.DestroyImmediate(invalid);
        }
    }
}
