using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace OrpheusBelow.Tests
{
    public class OrpheusAnimationTests
    {
        [Test]
        public void LibraryDeclaresEveryApprovedMotion()
        {
            var required = OrpheusAnimationLibrary.RequiredStates;
            Assert.That(required, Has.Length.GreaterThanOrEqualTo(48));
            Assert.That(required, Does.Contain(OrpheusAnimationState.RestlessIdle));
            Assert.That(required, Does.Contain(OrpheusAnimationState.DashStrike));
            Assert.That(required, Does.Contain(OrpheusAnimationState.SoftStrike3));
            Assert.That(required, Does.Contain(OrpheusAnimationState.HymnBeam));
            Assert.That(required, Does.Contain(OrpheusAnimationState.SingSustain));
            Assert.That(required, Does.Contain(OrpheusAnimationState.AulosLoop));
            Assert.That(required, Does.Contain(OrpheusAnimationState.Requiem));
            Assert.That(required, Does.Contain(OrpheusAnimationState.LookBackFatal));
            Assert.That(required, Does.Contain(OrpheusAnimationState.Victory));
            Assert.That(new System.Collections.Generic.HashSet<OrpheusAnimationState>(required).Count,
                Is.EqualTo(required.Length), "Every approved motion must appear exactly once.");
        }

        [Test]
        public void ControlledActionsRequireEightDirections()
        {
            foreach (var state in new[]
            {
                OrpheusAnimationState.Walk, OrpheusAnimationState.Run,
                OrpheusAnimationState.DashStrike, OrpheusAnimationState.SoftStrike1,
                OrpheusAnimationState.HardBoomRelease, OrpheusAnimationState.HymnBeam,
                OrpheusAnimationState.SingSustain, OrpheusAnimationState.AulosLoop
            })
                Assert.That(OrpheusAnimationLibrary.RequiresEightDirections(state), Is.True, state.ToString());

            Assert.That(OrpheusAnimationLibrary.RequiresEightDirections(OrpheusAnimationState.LookBackFatal), Is.False);
            Assert.That(OrpheusAnimationLibrary.DirectionCount(OrpheusAnimationState.LookBackFatal), Is.EqualTo(4));
        }

        [Test]
        public void VisibleHeightMatchesHumanoidEnemies()
        {
            var library = ScriptableObject.CreateInstance<OrpheusAnimationLibrary>();
            try { Assert.That(library.visibleHeight, Is.EqualTo(3.9f).Within(.001f)); }
            finally { UnityEngine.Object.DestroyImmediate(library); }
        }

        [Test]
        public void PriorityDefeatHurtAttackMusicDashLocomotionIdle()
        {
            var brain = new OrpheusAnimationBrain();
            var input = new OrpheusAnimationInput
            {
                deltaTime = .1f,
                groundDelta = UnderworldCamera2D.IsoRightXZ,
                aimDirection = UnderworldCamera2D.IsoUpXZ,
                dashing = true,
                action = OrpheusAnimationState.SingSustain
            };

            brain.Step(input); Assert.That(brain.State, Is.EqualTo(OrpheusAnimationState.SingSustain));
            input.action = OrpheusAnimationState.SoftStrike1;
            brain.Step(input); Assert.That(brain.State, Is.EqualTo(OrpheusAnimationState.SoftStrike1));
            input.lightHurt = true;
            brain.Step(input); Assert.That(brain.State, Is.EqualTo(OrpheusAnimationState.LightHit));
            input.heavyHurt = true;
            brain.Step(input); Assert.That(brain.State, Is.EqualTo(OrpheusAnimationState.HeavyHit));
            input.defeated = true;
            brain.Step(input); Assert.That(brain.State, Is.EqualTo(OrpheusAnimationState.Defeat));
            input.paused = true; input.defeated = false; input.heavyHurt = false; input.lightHurt = false;
            brain.Step(input); Assert.That(brain.State, Is.EqualTo(OrpheusAnimationState.Defeat));
        }

        [Test]
        public void FacingUsesAimDuringCombatAndMotionOtherwise()
        {
            var brain = new OrpheusAnimationBrain();
            brain.Step(new OrpheusAnimationInput
            {
                deltaTime = .25f,
                groundDelta = UnderworldCamera2D.IsoRightXZ,
                aimDirection = UnderworldCamera2D.IsoUpXZ
            });
            Assert.That(brain.Facing, Is.EqualTo(EurydiceFacing.East));

            brain.Step(new OrpheusAnimationInput
            {
                deltaTime = .1f,
                groundDelta = UnderworldCamera2D.IsoRightXZ,
                aimDirection = UnderworldCamera2D.IsoUpXZ,
                action = OrpheusAnimationState.SoftStrike1
            });
            Assert.That(brain.Facing, Is.EqualTo(EurydiceFacing.North));
        }

        [Test]
        public void FacingHysteresisPreventsBoundaryFlicker()
        {
            Vector3 east = UnderworldCamera2D.IsoRightXZ;
            Vector3 southEastNearEdge = Quaternion.AngleAxis(-24f, Vector3.up) * east;
            Assert.That(OrpheusAnimationBrain.FacingFor(southEastNearEdge, EurydiceFacing.East),
                Is.EqualTo(EurydiceFacing.East));
            Vector3 committedSouthEast = Quaternion.AngleAxis(-36f, Vector3.up) * east;
            Assert.That(OrpheusAnimationBrain.FacingFor(committedSouthEast, EurydiceFacing.East),
                Is.Not.EqualTo(EurydiceFacing.East));
        }

        [Test]
        public void TeleportResetReturnsIdleWithoutRestartNoise()
        {
            var brain = new OrpheusAnimationBrain();
            brain.Step(new OrpheusAnimationInput
            {
                deltaTime = .1f,
                action = OrpheusAnimationState.HardBoomRelease,
                aimDirection = UnderworldCamera2D.IsoUpXZ
            });
            Assert.That(brain.State, Is.EqualTo(OrpheusAnimationState.HardBoomRelease));
            brain.ResetMotion();
            int settledVersion = brain.StateVersion;
            Assert.That(brain.State, Is.EqualTo(OrpheusAnimationState.Idle));
            Assert.That(brain.GroundSpeed, Is.Zero);
            Assert.That(brain.ActionElapsed, Is.Zero);
            brain.ResetMotion();
            Assert.That(brain.StateVersion, Is.EqualTo(settledVersion), "Repeated reset must not restart idle.");
        }

        [Test]
        public void PlaybackFreezesWhenPausedAndDoesNotRestartIdenticalState()
        {
            var owned = new List<UnityEngine.Object>();
            OrpheusAnimationLibrary library = CreateCompleteLibrary(owned);
            var root = new GameObject("OrpheusPlayback");
            var fallback = new GameObject("Fallback");
            try
            {
                fallback.transform.SetParent(root.transform);
                var body = root.AddComponent<SpriteRenderer>();
                var fx = new GameObject("Effects").AddComponent<SpriteRenderer>();
                fx.transform.SetParent(root.transform);
                var animator = root.AddComponent<OrpheusSpriteAnimator>();
                animator.Initialize(body, fx, fallback, library);
                Assert.That(animator.UsesProductionArt, Is.True);
                Assert.That(fallback.activeSelf, Is.False);

                var idle = new OrpheusAnimationInput { deltaTime = .1f };
                animator.Drive(idle);
                animator.Advance(.3f);
                Assert.That(animator.CurrentFrame, Is.EqualTo(1));
                float elapsed = animator.Elapsed;
                animator.Drive(idle);
                Assert.That(animator.Elapsed, Is.EqualTo(elapsed), "Repeated idle input must not restart the clip.");
                animator.Advance(5f, true);
                Assert.That(animator.Elapsed, Is.EqualTo(elapsed));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                foreach (UnityEngine.Object item in owned) UnityEngine.Object.DestroyImmediate(item);
            }
        }

        [Test]
        public void FatalAndHurtOneShotsOverrideCosmeticAndAttackStates()
        {
            var owned = new List<UnityEngine.Object>();
            OrpheusAnimationLibrary library = CreateCompleteLibrary(owned);
            var root = new GameObject("OrpheusPriority");
            try
            {
                var body = root.AddComponent<SpriteRenderer>();
                var animator = root.AddComponent<OrpheusSpriteAnimator>();
                animator.Initialize(body, null, null, library);
                animator.TriggerSoftAttack(1);
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.SoftStrike1));
                animator.TriggerVictory();
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.SoftStrike1), "Cosmetic victory cannot interrupt committed combat.");
                animator.TriggerHit(true);
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.HeavyHit));
                animator.TriggerLookBack(true);
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.LookBackFatal));
                animator.TriggerHardBoom();
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.LookBackFatal));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                foreach (UnityEngine.Object item in owned) UnityEngine.Object.DestroyImmediate(item);
            }
        }

        [Test]
        public void ResetClearsOneShotAndReturnsIdleWithoutRestartingAgain()
        {
            var owned = new List<UnityEngine.Object>();
            OrpheusAnimationLibrary library = CreateCompleteLibrary(owned);
            var root = new GameObject("OrpheusReset");
            try
            {
                var animator = root.AddComponent<OrpheusSpriteAnimator>();
                animator.Initialize(root.AddComponent<SpriteRenderer>(), null, null, library);
                animator.TriggerDiscord();
                animator.Advance(.2f);
                animator.ResetMotion();
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.Idle));
                Assert.That(animator.Elapsed, Is.Zero);
                int version = animator.StateVersion;
                animator.ResetMotion();
                Assert.That(animator.StateVersion, Is.EqualTo(version));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                foreach (UnityEngine.Object item in owned) UnityEngine.Object.DestroyImmediate(item);
            }
        }

        [Test]
        public void MissingLibraryKeepsProceduralFallbackVisible()
        {
            var root = new GameObject("OrpheusFallback");
            var fallback = new GameObject("Fallback");
            try
            {
                fallback.transform.SetParent(root.transform);
                var body = root.AddComponent<SpriteRenderer>();
                var animator = root.AddComponent<OrpheusSpriteAnimator>();
                animator.Initialize(body, null, fallback, null);
                Assert.That(animator.UsesProductionArt, Is.False);
                Assert.That(fallback.activeSelf, Is.True);
                Assert.That(body.enabled, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void BossStageUsesSideFacing()
        {
            var owned = new List<UnityEngine.Object>();
            OrpheusAnimationLibrary library = CreateCompleteLibrary(owned);
            var root = new GameObject("OrpheusBossFacing");
            try
            {
                var animator = root.AddComponent<OrpheusSpriteAnimator>();
                animator.Initialize(root.AddComponent<SpriteRenderer>(), null, null, library);
                animator.Drive(new OrpheusAnimationInput
                {
                    deltaTime = .1f,
                    bossStage = true,
                    action = OrpheusAnimationState.SoftStrike1,
                    aimDirection = -UnderworldCamera2D.IsoRightXZ
                });
                Assert.That(animator.Facing, Is.EqualTo(EurydiceFacing.West));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                foreach (UnityEngine.Object item in owned) UnityEngine.Object.DestroyImmediate(item);
            }
        }

        [Test]
        public void PlayerCreatesAnimatorAndUsesInstalledProductionLibrary()
        {
            var root = new GameObject("OrpheusPlayerVisuals");
            try
            {
                var player = root.AddComponent<OrpheusPlayerController>();
                typeof(OrpheusPlayerController).GetMethod("Awake",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(player, null);
                var animator = root.GetComponent<OrpheusSpriteAnimator>();
                Assert.That(animator, Is.Not.Null);
                Assert.That(animator.UsesProductionArt, Is.True);
                Transform fallback = root.transform.Find("OrpheusBillboard25D/SpriteFlipRoot/ProceduralOrpheusVisual");
                Assert.That(fallback, Is.Not.Null);
                Assert.That(fallback.gameObject.activeSelf, Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void LyreCallbacksSelectComboBoomAndCadenceAnimations()
        {
            HashSet<int> before = SnapshotObjects();
            GameObject root = CreatePlayer(out OrpheusPlayerController player, out OrpheusSpriteAnimator animator);
            try
            {
                var combat = root.AddComponent<OrpheusLyreCombat>();
                Invoke(combat, "Awake");
                for (int combo = 1; combo <= 3; combo++)
                {
                    Invoke(combat, "ExecuteSoftComboStrike", .4f, false);
                    if (combo == 1)
                    {
                        Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.LyreDraw));
                        animator.Advance(.1f);
                    }
                    Assert.That(animator.CurrentState, Is.EqualTo(combo == 1 ? OrpheusAnimationState.SoftStrike1
                        : combo == 2 ? OrpheusAnimationState.SoftStrike2 : OrpheusAnimationState.SoftStrike3));
                    animator.ResetMotion();
                }
                Invoke(combat, "ExecuteHardBoom", .8f, false);
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.HardBoomWindup));
                animator.Advance(.1f);
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.HardBoomRelease));
                animator.ResetMotion();
                Invoke(combat, "ExecuteHymnOfDawnBeam");
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.FullVoiceRelease));
                animator.Advance(.1f);
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.HymnBeam));
                animator.ResetMotion();
                Invoke(combat, "ExecuteSanctuaryOfEurydice");
                animator.Advance(.1f);
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.SanctuaryCast));
                animator.ResetMotion();
                Invoke(combat, "ExecuteSirensConvergenceVortex");
                animator.Advance(.1f);
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.ConvergenceCast));
            }
            finally { DestroyNewObjects(before); }
        }

        [Test]
        public void CadenceWindowSustainsThenResolvesSinging()
        {
            HashSet<int> before = SnapshotObjects();
            GameObject root = CreatePlayer(out OrpheusPlayerController player, out OrpheusSpriteAnimator animator);
            try
            {
                var combat = root.AddComponent<OrpheusLyreCombat>();
                Invoke(combat, "Awake");
                Invoke(combat, "RecordCadenceNoteAndEvaluate", 'S');
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.SingSustain));
                combat.AdvanceState(combat.cadenceWindowSeconds + .01f);
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.PhraseResolve));
            }
            finally { DestroyNewObjects(before); }
        }

        [Test]
        public void FluteCallbacksSelectDrawNotesDiscordAndRequiem()
        {
            HashSet<int> before = SnapshotObjects();
            GameObject root = CreatePlayer(out OrpheusPlayerController player, out OrpheusSpriteAnimator animator);
            try
            {
                var flute = root.AddComponent<OrpheusFluteChannel>();
                Invoke(flute, "Awake");
                flute.EnterFluteStance();
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.AulosDraw));
                bool hard = flute.TargetSequence[0] == 'B';
                flute.ProcessFlutePulse(hard ? .8f : .3f, hard);
                Assert.That(animator.CurrentState, Is.EqualTo(hard ? OrpheusAnimationState.AulosNoteHard : OrpheusAnimationState.AulosNoteSoft));
                Invoke(flute, "CompleteMelodyOfSorrow");
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.Requiem));

                flute.ExitFluteStance(); animator.ResetMotion(); flute.EnterFluteStance();
                bool wrongHard = flute.TargetSequence[0] != 'B';
                flute.ProcessFlutePulse(wrongHard ? .8f : .3f, wrongHard);
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.Discord));
            }
            finally { DestroyNewObjects(before); }
        }

        [Test]
        public void DamageLossClearAndVictorySelectTheirAnimations()
        {
            HashSet<int> before = SnapshotObjects();
            GameObject root = CreatePlayer(out OrpheusPlayerController player, out OrpheusSpriteAnimator animator);
            try
            {
                player.TakeDamage(1f, root.transform.position - Vector3.right);
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.LightHit));
                animator.ResetMotion();
                Set(player, "_invulnTimer", 0f);
                player.TakeDamage(30f, root.transform.position - Vector3.right);
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.HeavyHit));
                animator.ResetMotion();
                player.TriggerChamberClear();
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.ChamberClear));
                animator.ResetMotion();
                player.TriggerRunVictory();
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.Victory));
            }
            finally { DestroyNewObjects(before); }

            before = SnapshotObjects();
            root = CreatePlayer(out player, out animator);
            try
            {
                player.TriggerEurydiceAbductedLoss();
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.EurydiceGrief));
            }
            finally { DestroyNewObjects(before); }

            before = SnapshotObjects();
            root = CreatePlayer(out player, out animator);
            try
            {
                Invoke(player, "TriggerLookBackLoss");
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.LookBackFatal));
            }
            finally { DestroyNewObjects(before); }
        }

        [Test]
        public void BreathExhaustionAndRecoveryHaveDistinctAnimations()
        {
            var root = new GameObject("BreathAnimation");
            try
            {
                var animator = root.AddComponent<OrpheusSpriteAnimator>();
                animator.Initialize(root.AddComponent<SpriteRenderer>(), null, null, null);
                animator.TriggerBreath(true);
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.BreathExhausted));
                animator.TriggerBreath(false);
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.BreathRecover));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void ChamberLightingNormalizesAuthoredDirectionAndFallsBackToUpRightLight()
        {
            var chamber = ScriptableObject.CreateInstance<PaintedChamberDefinition>();
            try
            {
                chamber.dominantLightDirection = new Vector2(8f, 6f);
                Assert.That(chamber.ResolvedDominantLightDirection.magnitude, Is.EqualTo(1f).Within(.0001f));
                Assert.That(chamber.ResolvedDominantLightDirection.x, Is.EqualTo(.8f).Within(.0001f));
                Assert.That(chamber.ResolvedDominantLightDirection.y, Is.EqualTo(.6f).Within(.0001f));
                Assert.That(OrpheusGroundShadow.ResolveCastDirection(chamber).x, Is.EqualTo(-.8f).Within(.0001f));
                Assert.That(OrpheusGroundShadow.ResolveCastDirection(chamber).y, Is.EqualTo(-.6f).Within(.0001f));

                chamber.dominantLightDirection = Vector2.zero;
                Vector2 fallbackCast = OrpheusGroundShadow.ResolveCastDirection(chamber);
                Assert.That(fallbackCast.x, Is.LessThan(0f));
                Assert.That(fallbackCast.y, Is.LessThan(0f));
                Assert.That(fallbackCast.magnitude, Is.EqualTo(1f).Within(.0001f));
            }
            finally { UnityEngine.Object.DestroyImmediate(chamber); }
        }

        [Test]
        public void EveryPaintedChamberHasDistinctAuthoredDominantLight()
        {
            var signatures = new HashSet<string>();
            for (int i = 0; i < UnderworldJourney.ChamberCount; i++)
            {
                var chamber = Resources.Load<PaintedChamberDefinition>("ChamberLayouts/Chamber_" + i.ToString("D2"));
                Assert.That(chamber, Is.Not.Null, "Missing chamber " + i);
                Assert.That(chamber.dominantLightDirection.sqrMagnitude, Is.GreaterThan(.01f), chamber.name);
                Assert.That(chamber.dominantLightIntensity, Is.InRange(.2f, 2f), chamber.name);
                signatures.Add($"{chamber.dominantLightDirection.x:F2}:{chamber.dominantLightDirection.y:F2}:" +
                    $"{chamber.dominantLightColor.r:F2}:{chamber.dominantLightColor.g:F2}:{chamber.dominantLightColor.b:F2}");
            }
            Assert.That(signatures.Count, Is.GreaterThanOrEqualTo(6));
        }

        [Test]
        public void GroundShadowCompressesForDashWidensForAttackAndClampsOpacity()
        {
            OrpheusShadowShape idle = OrpheusGroundShadow.Evaluate(false, false, -20f);
            OrpheusShadowShape dash = OrpheusGroundShadow.Evaluate(true, false, 1f);
            OrpheusShadowShape attack = OrpheusGroundShadow.Evaluate(false, true, 20f);
            Assert.That(dash.castScale.y, Is.LessThan(idle.castScale.y));
            Assert.That(dash.castScale.x, Is.GreaterThan(idle.castScale.x));
            Assert.That(attack.ambientScale.x, Is.GreaterThan(idle.ambientScale.x));
            Assert.That(idle.opacity, Is.InRange(.18f, .62f));
            Assert.That(attack.opacity, Is.InRange(.18f, .62f));
        }

        [Test]
        public void GroundShadowBuildsThreeLayersBelowTheActor()
        {
            var root = new GameObject("OrpheusShadowContract");
            try
            {
                var shadow = root.AddComponent<OrpheusGroundShadow>();
                shadow.Initialize(null);
                Assert.That(shadow.ContactRenderer, Is.Not.Null);
                Assert.That(shadow.AmbientRenderer, Is.Not.Null);
                Assert.That(shadow.CastRenderer, Is.Not.Null);
                Assert.That(shadow.ContactRenderer.sortingOrder, Is.LessThan(14));
                Assert.That(shadow.AmbientRenderer.sortingOrder, Is.LessThan(14));
                Assert.That(shadow.CastRenderer.sortingOrder, Is.LessThan(14));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void OilRimUsesPropertyBlocksWithoutChangingSpriteGeometryOrTransforms()
        {
            var texture = new Texture2D(4, 4);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(.25f, .1f), 4f);
            var a = new GameObject("OrpheusOilA");
            var b = new GameObject("OrpheusOilB");
            try
            {
                var ra = a.AddComponent<SpriteRenderer>(); ra.sprite = sprite;
                var rb = b.AddComponent<SpriteRenderer>(); rb.sprite = sprite;
                Vector3 scale = new Vector3(1.2f, .9f, 1f); a.transform.localScale = scale;
                Vector2 pivot = sprite.pivot; Rect rect = sprite.rect;
                OrpheusOilSurface.Apply(ra, Vector2.right, Color.red, .4f);
                OrpheusOilSurface.Apply(rb, Vector2.up, Color.blue, 1.7f);

                var blockA = new MaterialPropertyBlock(); ra.GetPropertyBlock(blockA);
                var blockB = new MaterialPropertyBlock(); rb.GetPropertyBlock(blockB);
                Assert.That(blockA.GetVector("_RimDirection"), Is.Not.EqualTo(blockB.GetVector("_RimDirection")));
                Assert.That(blockA.GetColor("_RimColor"), Is.Not.EqualTo(blockB.GetColor("_RimColor")));
                Assert.That(blockA.GetFloat("_RimIntensity"), Is.EqualTo(.4f).Within(.001f));
                Assert.That(blockB.GetFloat("_RimIntensity"), Is.EqualTo(1.7f).Within(.001f));
                Assert.That(ra.sprite, Is.SameAs(sprite));
                Assert.That(a.transform.localScale, Is.EqualTo(scale));
                Assert.That(sprite.pivot, Is.EqualTo(pivot));
                Assert.That(sprite.rect, Is.EqualTo(rect));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(a); UnityEngine.Object.DestroyImmediate(b);
                UnityEngine.Object.DestroyImmediate(sprite); UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void TerminalNarrativeOverridesSurviveDefeatedDrive()
        {
            var owned = new List<UnityEngine.Object>();
            OrpheusAnimationLibrary library = CreateCompleteLibrary(owned);
            var root = new GameObject("OrpheusTerminalPriority");
            try
            {
                var animator = root.AddComponent<OrpheusSpriteAnimator>();
                animator.Initialize(root.AddComponent<SpriteRenderer>(), null, null, library);
                animator.TriggerGrief();
                animator.Drive(new OrpheusAnimationInput { defeated = true, deltaTime = .1f });
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.EurydiceGrief));

                animator.ResetMotion();
                animator.TriggerLookBack(true);
                animator.Drive(new OrpheusAnimationInput { defeated = true, deltaTime = .1f });
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.LookBackFatal));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                foreach (UnityEngine.Object item in owned) UnityEngine.Object.DestroyImmediate(item);
            }
        }

        [Test]
        public void QueuedSingingAndNarrativeBeginAfterCommittedAttack()
        {
            var owned = new List<UnityEngine.Object>();
            OrpheusAnimationLibrary library = CreateCompleteLibrary(owned);
            var root = new GameObject("OrpheusQueuedPerformance");
            try
            {
                var animator = root.AddComponent<OrpheusSpriteAnimator>();
                animator.Initialize(root.AddComponent<SpriteRenderer>(), null, null, library);
                animator.TriggerSoftAttack(1);
                animator.SetSinging(true);
                animator.Advance(1f);
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.SingSustain));

                animator.ResetMotion();
                animator.TriggerSoftAttack(1);
                animator.TriggerChamberClear();
                animator.Advance(1f);
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.ChamberClear));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                foreach (UnityEngine.Object item in owned) UnityEngine.Object.DestroyImmediate(item);
            }
        }

        [Test]
        public void CombatStateSelectsTheMatchingPaintedEffectRow()
        {
            var owned = new List<UnityEngine.Object>();
            OrpheusAnimationLibrary library = CreateCompleteLibrary(owned);
            var effectTexture = new Texture2D(2, 2);
            var hardBoomEffect = Sprite.Create(effectTexture, new Rect(0, 0, 2, 2), Vector2.one * .5f, 1f);
            owned.Add(hardBoomEffect); owned.Add(effectTexture);
            library.effects = new Sprite[64];
            Array.Fill(library.effects, library.Frame(OrpheusAnimationState.Idle, EurydiceFacing.South, 0));
            library.effects[16] = hardBoomEffect;
            var root = new GameObject("OrpheusEffectPlayback");
            try
            {
                var body = root.AddComponent<SpriteRenderer>();
                var effects = new GameObject("Effects").AddComponent<SpriteRenderer>();
                effects.transform.SetParent(root.transform);
                var animator = root.AddComponent<OrpheusSpriteAnimator>();
                animator.Initialize(body, effects, null, library);
                animator.TriggerHardBoom();
                Assert.That(effects.sprite, Is.SameAs(hardBoomEffect));
                Assert.That(effects.enabled, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                foreach (UnityEngine.Object item in owned) UnityEngine.Object.DestroyImmediate(item);
            }
        }

        [Test]
        public void PlayerPreservesOilBodyMaterialAndFeedsAuthoredStairs()
        {
            HashSet<int> before = SnapshotObjects();
            GameObject root = CreatePlayer(out OrpheusPlayerController player, out OrpheusSpriteAnimator animator);
            var definition = ScriptableObject.CreateInstance<PaintedChamberDefinition>();
            try
            {
                var depth = root.GetComponent<PaintedActorDepth>();
                var excluded = (HashSet<SpriteRenderer>)typeof(PaintedActorDepth).GetField("_excluded",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(depth);
                Assert.That(excluded, Does.Contain(animator.BodyRenderer));

                definition.stairs = new[] { new PaintedChamberDefinition.StairZone
                {
                    polygon = new[] { Vector2.zero, new Vector2(1536, 0), new Vector2(1536, 1024), new Vector2(0, 1024) },
                    uphill = new Vector2(100, 0)
                } };
                var mapGo = new GameObject("StairMap");
                var map = mapGo.AddComponent<PaintedChamberMap>();
                map.definition = definition;
                map.MakeActive();
                Set(player, "_lastAnimationPosition", Vector3.zero);
                root.transform.position = UnderworldCamera2D.IsoRightXZ;
                Invoke(player, "UpdateVisualPose");
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.StairAscent));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
                DestroyNewObjects(before);
            }
        }

        [Test]
        public void MovementTransitionsAndDashPhasesAreReachable()
        {
            var brain = new OrpheusAnimationBrain();
            brain.Step(new OrpheusAnimationInput { groundDelta = Vector3.right, deltaTime = .1f });
            Assert.That(brain.State, Is.EqualTo(OrpheusAnimationState.Start));
            brain.Step(new OrpheusAnimationInput { groundDelta = Vector3.right, deltaTime = .3f });
            Assert.That(new[] { OrpheusAnimationState.Walk, OrpheusAnimationState.Run }, Does.Contain(brain.State));
            brain.Step(new OrpheusAnimationInput { deltaTime = .2f });
            Assert.That(brain.State, Is.EqualTo(OrpheusAnimationState.Stop));

            HashSet<int> before = SnapshotObjects();
            GameObject root = CreatePlayer(out OrpheusPlayerController player, out OrpheusSpriteAnimator animator);
            try
            {
                Invoke(player, "BeginDashCommon");
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.DashStart));
                Invoke(player, "TriggerDashSettleAnimation");
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.DashSettle));
            }
            finally { DestroyNewObjects(before); }
        }

        [Test]
        public void HeavyHitFlowsThroughGroundedHurtAndRise()
        {
            var owned = new List<UnityEngine.Object>();
            OrpheusAnimationLibrary library = CreateCompleteLibrary(owned);
            var root = new GameObject("OrpheusHeavyRecovery");
            try
            {
                var animator = root.AddComponent<OrpheusSpriteAnimator>();
                animator.Initialize(root.AddComponent<SpriteRenderer>(), null, null, library);
                animator.TriggerHit(true);
                animator.Advance(1f);
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.GroundedHurt));
                animator.Advance(1f);
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.Rise));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                foreach (UnityEngine.Object item in owned) UnityEngine.Object.DestroyImmediate(item);
            }
        }

        [Test]
        public void DamageCancelsQueuedSingingBeforeGroundRecovery()
        {
            var owned = new List<UnityEngine.Object>();
            OrpheusAnimationLibrary library = CreateCompleteLibrary(owned);
            var root = new GameObject("OrpheusInterruptedSong");
            try
            {
                var animator = root.AddComponent<OrpheusSpriteAnimator>();
                animator.Initialize(root.AddComponent<SpriteRenderer>(), null, null, library);
                animator.TriggerSoftAttack(1);
                animator.SetSinging(true);
                animator.TriggerHit(true);
                animator.Advance(1f);
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.GroundedHurt));
                animator.Advance(1f);
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.Rise));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                foreach (UnityEngine.Object item in owned) UnityEngine.Object.DestroyImmediate(item);
            }
        }

        [Test]
        public void DashStartYieldsToTravelBeforeGameplayDashEnds()
        {
            var owned = new List<UnityEngine.Object>();
            OrpheusAnimationLibrary library = CreateCompleteLibrary(owned);
            var root = new GameObject("OrpheusDashPhases");
            try
            {
                var animator = root.AddComponent<OrpheusSpriteAnimator>();
                animator.Initialize(root.AddComponent<SpriteRenderer>(), null, null, library);
                animator.TriggerDashStart();
                animator.Advance(.06f);
                animator.Drive(new OrpheusAnimationInput { dashing = true, deltaTime = .016f });
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.DashTravel));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                foreach (UnityEngine.Object item in owned) UnityEngine.Object.DestroyImmediate(item);
            }
        }

        [Test]
        public void EntranceKeepsItsFrameBeforeQueuedAlert()
        {
            var owned = new List<UnityEngine.Object>();
            OrpheusAnimationLibrary library = CreateCompleteLibrary(owned);
            var root = new GameObject("OrpheusEntranceAlert");
            try
            {
                var animator = root.AddComponent<OrpheusSpriteAnimator>();
                animator.Initialize(root.AddComponent<SpriteRenderer>(), null, null, library);
                animator.TriggerChamberEntrance();
                animator.TriggerAlert();
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.ChamberEntrance));
                animator.Advance(1f);
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.Inspect));
                animator.Advance(1f);
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.Alert));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                foreach (UnityEngine.Object item in owned) UnityEngine.Object.DestroyImmediate(item);
            }
        }

        [Test]
        public void GameplaySequencesReachEveryMusicalAndContextPose()
        {
            var owned = new List<UnityEngine.Object>();
            OrpheusAnimationLibrary library = CreateCompleteLibrary(owned);
            var root = new GameObject("OrpheusReachability");
            try
            {
                var animator = root.AddComponent<OrpheusSpriteAnimator>();
                animator.Initialize(root.AddComponent<SpriteRenderer>(), null, null, library);

                Invoke(animator, "TriggerSoftPerformance", 1);
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.LyreDraw));
                animator.Advance(.1f);
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.SoftStrike1));
                animator.Advance(1f);
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.SoftPluck));

                animator.ResetMotion();
                Invoke(animator, "TriggerSoftPerformance", 3);
                animator.Advance(1f);
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.SoftPluck));
                animator.Advance(1f);
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.SoftBolt));

                animator.ResetMotion();
                Invoke(animator, "TriggerHardBoomPerformance");
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.HardBoomWindup));
                animator.Advance(.1f);
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.HardBoomRelease));

                animator.ResetMotion();
                Invoke(animator, "TriggerCadencePerformance", OrpheusAnimationState.HymnBeam);
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.FullVoiceRelease));
                animator.Advance(.1f);
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.HymnBeam));

                animator.ResetMotion();
                animator.TriggerChamberEntrance();
                animator.Advance(1f);
                Assert.That(animator.CurrentState, Is.EqualTo(OrpheusAnimationState.Inspect));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                foreach (UnityEngine.Object item in owned) UnityEngine.Object.DestroyImmediate(item);
            }
        }

        [Test]
        public void ProductionLibraryInstallsEveryStateWithAtLeastSixHundredDistinctPaintedFrames()
        {
            OrpheusAnimationLibrary library = Resources.Load<OrpheusAnimationLibrary>(OrpheusAnimationLibrary.ResourcePath);
            Assert.That(library, Is.Not.Null, "The production Orpheus library must exist at its runtime Resources path.");
            Assert.That(library.IsComplete, Is.True);
            Assert.That(library.visibleHeight, Is.EqualTo(3.9f).Within(.001f));
            int installed = 0;
            var distinct = new HashSet<int>();
            foreach (OrpheusAnimationState state in OrpheusAnimationLibrary.RequiredStates)
            {
                OrpheusAnimationClip clip = library.FindClip(state);
                Assert.That(clip, Is.Not.Null, state.ToString());
                Assert.That(clip.directions, Is.EqualTo(OrpheusAnimationLibrary.DirectionCount(state)), state.ToString());
                Assert.That(clip.framesPerDirection, Is.GreaterThanOrEqualTo(2), state.ToString());
                installed += clip.frames.Length;
                foreach (Sprite frame in clip.frames) distinct.Add(frame.GetInstanceID());
            }
            foreach (Sprite effect in library.effects) if (effect != null) distinct.Add(effect.GetInstanceID());
            foreach (Sprite source in library.sourceFrames) if (source != null) distinct.Add(source.GetInstanceID());
            Assert.That(installed, Is.GreaterThanOrEqualTo(1200));
            Assert.That(library.effects, Has.Length.GreaterThanOrEqualTo(64));
            Assert.That(library.sourceFrames, Has.Length.GreaterThanOrEqualTo(640));
            Assert.That(distinct.Count, Is.GreaterThanOrEqualTo(700));
        }

        [Test]
        public void GeneratedAtlasesKeepRealTransparency()
        {
            foreach (string name in new[] { "IdleTransitions", "Walk", "Run", "DashStairs", "SoftCombat",
                "HardCadence", "LyreSinging", "Aulos", "Reactions", "Narrative", "Effects" })
            {
                Texture2D texture = Resources.Load<Texture2D>("Characters/Orpheus/BroadOil/" + name);
                Assert.That(texture, Is.Not.Null, name);
                Color32[] pixels = texture.GetPixels32();
                int clear = 0;
                foreach (Color32 pixel in pixels) if (pixel.a < 10) clear++;
                Assert.That(clear, Is.GreaterThan(pixels.Length / 5), name + " must have a genuinely transparent gutter/background.");
            }
        }

        [Test]
        public void BakedFramesHaveStableGroundPivotsAndSafeTransparentGutters()
        {
            OrpheusAnimationLibrary library = Resources.Load<OrpheusAnimationLibrary>(OrpheusAnimationLibrary.ResourcePath);
            var visited = new HashSet<int>();
            float worstEdgeRatio = 0f;
            string worstEdgeSprite = "";
            var unsafeEdges = new List<string>();
            foreach (OrpheusAnimationClip clip in library.clips)
                foreach (Sprite sprite in clip.frames)
                {
                    if (!visited.Add(sprite.GetInstanceID())) continue;
                    Assert.That(sprite.pivot.y / sprite.rect.height, Is.InRange(0f, .051f), sprite.name);
                    RectInt rect = new RectInt(Mathf.RoundToInt(sprite.rect.x), Mathf.RoundToInt(sprite.rect.y),
                        Mathf.RoundToInt(sprite.rect.width), Mathf.RoundToInt(sprite.rect.height));
                    Color32[] pixels = sprite.texture.GetPixels32();
                    int width = sprite.texture.width, opaqueEdge = 0, perimeter = rect.width * 2 + rect.height * 2;
                    for (int x = rect.xMin; x < rect.xMax; x++)
                    {
                        if (pixels[rect.yMin * width + x].a > 40) opaqueEdge++;
                        if (pixels[(rect.yMax - 1) * width + x].a > 40) opaqueEdge++;
                    }
                    for (int y = rect.yMin; y < rect.yMax; y++)
                    {
                        if (pixels[y * width + rect.xMin].a > 40) opaqueEdge++;
                        if (pixels[y * width + rect.xMax - 1].a > 40) opaqueEdge++;
                    }
                    float edgeRatio = opaqueEdge / (float)perimeter;
                    if (edgeRatio > worstEdgeRatio) { worstEdgeRatio = edgeRatio; worstEdgeSprite = sprite.name; }
                    if (edgeRatio >= .45f) unsafeEdges.Add(sprite.name + "=" + edgeRatio.ToString("F3"));
                }
            Assert.That(unsafeEdges, Is.Empty, worstEdgeSprite + " is grossly clipped or contaminated at its cell edge. Unsafe: " +
                string.Join(", ", unsafeEdges));
            Sprite idle = library.FindClip(OrpheusAnimationState.Idle).frames[0];
            Assert.That(idle.bounds.size.y, Is.EqualTo(3.9f).Within(.35f));
        }

        private static OrpheusAnimationLibrary CreateCompleteLibrary(List<UnityEngine.Object> owned)
        {
            var texture = new Texture2D(2, 2);
            texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(.5f, 0f), 1f);
            var library = ScriptableObject.CreateInstance<OrpheusAnimationLibrary>();
            library.clips = new OrpheusAnimationClip[OrpheusAnimationLibrary.RequiredStates.Length];
            for (int i = 0; i < library.clips.Length; i++)
            {
                OrpheusAnimationState state = OrpheusAnimationLibrary.RequiredStates[i];
                int directions = OrpheusAnimationLibrary.DirectionCount(state);
                int frames = state == OrpheusAnimationState.SoftStrike1 ? 3 : 2;
                var sprites = new Sprite[directions * frames];
                Array.Fill(sprites, sprite);
                library.clips[i] = new OrpheusAnimationClip
                {
                    state = state,
                    directions = directions,
                    framesPerDirection = frames,
                    frames = sprites,
                    fps = 4f,
                    loop = state == OrpheusAnimationState.Idle || state == OrpheusAnimationState.Walk ||
                        state == OrpheusAnimationState.Run || state == OrpheusAnimationState.SingSustain ||
                        state == OrpheusAnimationState.AulosLoop
                };
            }
            owned.Add(library); owned.Add(sprite); owned.Add(texture);
            return library;
        }

        private static GameObject CreatePlayer(out OrpheusPlayerController player, out OrpheusSpriteAnimator animator)
        {
            var root = new GameObject("OrpheusIntegration");
            player = root.AddComponent<OrpheusPlayerController>();
            Invoke(player, "Awake");
            animator = root.GetComponent<OrpheusSpriteAnimator>();
            return root;
        }

        private static object Invoke(object target, string method, params object[] args)
            => target.GetType().GetMethod(method, System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public).Invoke(target, args);

        private static void Set(object target, string field, object value)
            => target.GetType().GetField(field, System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic).SetValue(target, value);

        private static HashSet<int> SnapshotObjects()
        {
            var ids = new HashSet<int>();
            foreach (GameObject go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)) ids.Add(go.GetInstanceID());
            return ids;
        }

        private static void DestroyNewObjects(HashSet<int> before)
        {
            foreach (GameObject go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                if (!before.Contains(go.GetInstanceID())) UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
