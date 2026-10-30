using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace OrpheusBelow.Tests
{
    public sealed class OrpheusRunRegistrationTests
    {
        [Serializable] private sealed class Registration { public Record[] records; }
        [Serializable] private sealed class Record
        {
            public string view, source;
            public int phase, sourceWidth, sourceHeight;
            public Vector2 neck, pelvis;
        }

        private static Registration Reviewed => JsonUtility.FromJson<Registration>(Resources.Load<TextAsset>(
            "Characters/Orpheus/BroadOil/NaturalRunV2/RunRegistration").text);
        private static OrpheusAnimationLibrary Library => Resources.Load<OrpheusAnimationLibrary>(OrpheusAnimationLibrary.ResourcePath);

        [Test]
        public void IndependentlyMarkedPaintedPelvisStaysOnActorRootThroughoutRunning()
        {
            Record[] records = Reviewed.records;
            Assert.That(records, Has.Length.EqualTo(20), "Every selected canonical key needs its own reviewed body root.");
            Assert.That(records.Select(record => record.view + ":" + record.phase).Distinct().Count(), Is.EqualTo(20));
            var failures = new List<string>();
            foreach (Record record in records)
            {
                Sprite sprite = SelectedPaint(record);
                float x = LocalPaintX(sprite, record, record.pelvis.x);
                if (Mathf.Abs(x) > .05f)
                    failures.Add(record.view + " phase " + record.phase + " pelvis is " + x.ToString("F3") +
                        " world units away from the actor root; support-boot centroids must not move the torso.");
            }
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [Test]
        public void IndependentNeckWitnessesDoNotLurchSidewaysBetweenRunKeys()
        {
            foreach (var view in Reviewed.records.GroupBy(record => record.view))
            {
                float[] positions = view.Select(record => LocalPaintX(SelectedPaint(record), record, record.neck.x)).ToArray();
                Assert.That(positions.Max() - positions.Min(), Is.LessThanOrEqualTo(.2f),
                    view.Key + " upper body lurches sideways between support legs.");
            }
        }

        private static Sprite SelectedPaint(Record record)
        {
            var facing = (EurydiceFacing)Enum.Parse(typeof(EurydiceFacing), record.view);
            Sprite sprite = Library.Frame(OrpheusAnimationState.Run, facing, record.phase);
            Assert.That(sprite, Is.Not.Null);
            Assert.That(Path.GetFileName(AssetDatabase.GetAssetPath(sprite.texture)), Is.EqualTo(record.source),
                "Anatomical witnesses must refer to the exact selected source painting.");
            return sprite;
        }

        private static float LocalPaintX(Sprite sprite, Record record, float nativeX)
        {
            // The witness was manually marked in the untouched full source PNG, not
            // inferred from the changing boot silhouette or from the baker's pivot.
            float importedX = nativeX * sprite.texture.width / record.sourceWidth;
            Assert.That(importedX, Is.InRange(sprite.rect.xMin, sprite.rect.xMax));
            return (importedX - sprite.rect.x - sprite.pivot.x) / sprite.pixelsPerUnit;
        }
    }
}
