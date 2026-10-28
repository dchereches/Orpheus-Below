using NUnit.Framework;
using UnityEngine;

namespace OrpheusBelow.Tests
{
    public class VoiceBootPolishTests
    {
        [Test] public void VoiceRequestsQueueWithoutInterruptingAndKeepTheLock()
        {
            var queue=new VoicePlaybackQueue(); int completed=0;
            queue.Enqueue(new[]{VoiceLineId.Eurydice_IamHere},()=>completed++);
            queue.Enqueue(new[]{VoiceLineId.Eurydice_HearTheNote},()=>completed++);
            Assert.That(queue.IsBlocking,Is.True);
            Assert.That(queue.TryBegin(out var first),Is.True);
            Assert.That(first.lines[0],Is.EqualTo(VoiceLineId.Eurydice_IamHere));
            Assert.That(queue.TryBegin(out _),Is.False,"An active voice must not be replaced");
            queue.Complete(); Assert.That(completed,Is.EqualTo(1));
            Assert.That(queue.IsBlocking,Is.True,"Queued lines keep gameplay locked");
            Assert.That(queue.TryBegin(out var second),Is.True);
            Assert.That(second.lines[0],Is.EqualTo(VoiceLineId.Eurydice_HearTheNote));
            queue.Complete(); queue.Complete();
            Assert.That(completed,Is.EqualTo(2)); Assert.That(queue.IsBlocking,Is.False);
        }
        [Test] public void SequenceOwnsItsLockUntilTheWholeRequestCompletes()
        {
            var queue=new VoicePlaybackQueue();
            var lines=new[]{VoiceLineId.Hades_ABreath,VoiceLineId.Hades_YourStrings};
            queue.Enqueue(lines,null); lines[0]=VoiceLineId.None;
            queue.TryBegin(out var request);
            Assert.That(request.lines[0],Is.EqualTo(VoiceLineId.Hades_ABreath));
            Assert.That(request.lines.Length,Is.EqualTo(2)); Assert.That(queue.IsBlocking,Is.True);
            queue.Clear(); Assert.That(queue.IsBlocking,Is.False);
        }
        [Test] public void CompletionCanQueueAnotherVoiceWithoutDroppingTheLock()
        {
            var queue=new VoicePlaybackQueue();
            queue.Enqueue(new[]{VoiceLineId.Eurydice_IamHere},()=>queue.Enqueue(new[]{VoiceLineId.Eurydice_RightHere},null));
            queue.TryBegin(out _); queue.Complete();
            Assert.That(queue.IsBlocking,Is.True);
            Assert.That(queue.TryBegin(out var next),Is.True);
            Assert.That(next.lines[0],Is.EqualTo(VoiceLineId.Eurydice_RightHere));
        }
        [Test] public void EnablingAuthoredMapClearsSerializedNavigationOverlay()
        {
            var go=new GameObject("Map"); var overlay=new GameObject("Overlay");
            try
            {
                var map=go.AddComponent<PaintedChamberMap>(); map.overlayRoot=overlay.transform;
                overlay.SetActive(true); go.SetActive(false); go.SetActive(true);
                typeof(PaintedChamberMap).GetMethod("OnEnable",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
                    .Invoke(map,null); // EditMode does not dispatch PlayMode lifecycle callbacks.
                Assert.That(overlay.activeSelf,Is.False); Assert.That(map.showNavigationGizmos,Is.False);
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(overlay); }
        }
        [Test] public void VoiceMixIsFullLevelWhileAmbientIsStronglyDucked()
        {
            Assert.That(VoiceDirector.VoiceGain,Is.EqualTo(1));
            Assert.That(UnderworldSoundscape.DialogueAmbientGain,Is.LessThanOrEqualTo(.15f));
        }
        [Test] public void IntroHasBlackEndpointsAndGentleFades()
        {
            var timeline=IntroCinematicTimeline.Load();
            Assert.That(timeline.BlackAlpha(0),Is.EqualTo(1));
            Assert.That(timeline.BlackAlpha(.3f),Is.EqualTo(.5f).Within(.01));
            Assert.That(timeline.BlackAlpha(223f),Is.InRange(.4f,.7f));
            Assert.That(timeline.BlackAlpha(timeline.duration),Is.EqualTo(1));
        }
    }
}
