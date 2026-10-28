using System;
using NUnit.Framework;
using UnityEngine;

namespace OrpheusBelow.Tests
{
    public class IntroCinematicTests
    {
        [Test] public void ApprovedTimelineCoversWholeNarrationAndStrike()
        {
            var timeline = IntroCinematicTimeline.Load();
            Assert.That(timeline.shots.Length, Is.EqualTo(42));
            Assert.That(timeline.duration, Is.EqualTo(223.553f).Within(.001f));
            Assert.That(timeline.FindShot(0), Is.EqualTo(0));
            Assert.That(timeline.FindShot(112.58f), Is.EqualTo(20));
            Assert.That(timeline.FindShot(223.552f), Is.EqualTo(41));
            for (int i=0;i<42;i++)
                Assert.That(Resources.Load<Texture2D>(timeline.shots[i].resource), Is.Not.Null, timeline.shots[i].resource);
            Assert.That(Resources.Load<AudioClip>("Cinematic/Intro/Narration"), Is.Not.Null);
        }

        [Test] public void IncomingDissolveAndFinalBlackUseAudioPosition()
        {
            var timeline = IntroCinematicTimeline.Load();
            var shot = timeline.shots[1];
            Assert.That(timeline.Alpha(1,shot.start), Is.EqualTo(0));
            Assert.That(timeline.Alpha(1,shot.start+shot.fade/2), Is.EqualTo(.5f).Within(.001));
            Assert.That(timeline.Alpha(1,shot.start+shot.fade), Is.EqualTo(1).Within(.001));
            Assert.That(timeline.BlackAlpha(timeline.duration), Is.EqualTo(1));
        }

        [TestCase(false,false,true)] [TestCase(true,false,false)] [TestCase(true,true,true)]
        public void OnlyNewJourneyOrExplicitReplayStartsIntro(bool inRun,bool replay,bool expected)
        { Assert.That(IntroCinematicTimeline.ShouldPlay(inRun,replay), Is.EqualTo(expected)); }

        [Test] public void InvalidTimelineIsRejectedBeforePlayback()
        {
            Assert.Throws<InvalidOperationException>(()=>new IntroCinematicTimeline().Validate());
        }

        [TestCase(100f,.02f,false)] [TestCase(223.52f,.04f,true)]
        [TestCase(0f,.3f,false)]
        public void StoppedAudioSucceedsOnlyAtTheNarratedEnding(float clock,float delta,bool expected)
        { Assert.That(IntroCinematicTimeline.ReachedEnd(clock,delta,223.553f),Is.EqualTo(expected)); }

    }
}
