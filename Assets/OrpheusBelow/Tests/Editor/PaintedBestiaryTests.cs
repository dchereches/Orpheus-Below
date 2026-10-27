using NUnit.Framework;
using UnityEngine;
using System;
using System.Reflection;
using Object = UnityEngine.Object;

namespace OrpheusBelow.Tests
{
    public class PaintedBestiaryTests
    {
        [Test]
        public void EverySuppliedEnemyHasACompletePaintedAnimationLibrary()
        {
            foreach (string name in new[] { "Cerberus", "HoodedSkeleton", "MaskedMonk", "MysticEyeWarden", "WingedSentinel", "RoosterRider", "Tripod", "Shepherd" })
            {
                var library = Resources.Load<EnemyArtLibrary>("Enemies/" + name + "/Library");
                Assert.That(library, Is.Not.Null, name + " still lacks painted animation.");
                Assert.That(library.IsComplete, Is.True, name);
                for (int row = 0; row < 8; row++)
                {
                    Assert.That(library.FrameCount((EnemyMotion)row), Is.GreaterThanOrEqualTo(2));
                    for (int frame = 0; frame < library.FrameCount((EnemyMotion)row); frame++)
                        Assert.That(library.Frame((EnemyMotion)row, frame), Is.Not.Null);
                }
                Assert.That(typeof(PaintedEnemyActor).Assembly.GetType("OrpheusBelow." + name + "Attack"), Is.Not.Null, name + " has no attack pattern.");
            }
        }

        [Test]
        public void AllRequestedBossAndMiniBossChambersHaveAnEncounter()
        {
            for (int i = 1; i <= 6; i++)
            {
                var layout = Resources.Load<PaintedChamberDefinition>("ChamberLayouts/Chamber_" + i.ToString("D2"));
                Assert.That(layout.hasGuardian, Is.True, "Missing authored encounter in chamber " + i);
            }
        }

        [Test]
        public void PaintedBulletSweepsThinCover()
        {
            var wall = new GameObject("PaintedBulletCover"); wall.transform.position = Vector3.right * 40005 + Vector3.up * .7f;
            wall.AddComponent<BoxCollider>().size = new Vector3(.2f,3,3); wall.AddComponent<IsoObstacle>();
            PaintedEnemyProjectile shot = null;
            try
            {
                shot = PaintedEnemyProjectile.Spawn(PaintedEnemyKind.HoodedSkeleton,0,Vector3.right*40000+Vector3.up*.7f,Vector3.right,120,12);
                Physics.SyncTransforms(); shot.Advance(.1f);
                Assert.That(shot == null || shot.Removed, Is.True, "A fast painted bullet must stop at solid cover.");
            }
            finally { if (shot != null) Object.DestroyImmediate(shot.gameObject); Object.DestroyImmediate(wall); }
        }

        [Test]
        public void CerberusWaitsAndWarnsBeforeOneCommittedBiteAndCancelDisarmsIt()
        {
            var playerGo = new GameObject("PaintedBitePlayer"); var enemy = new GameObject("PaintedBiteEnemy");
            float previousTime = Time.timeScale; Time.timeScale = 1;
            try
            {
                var player = playerGo.AddComponent<OrpheusPlayerController>();
                typeof(OrpheusPlayerController).GetProperty("Instance").SetValue(null,player); player.RestoreTravelHealth(player.maxHealth);
                playerGo.transform.position = Vector3.right * 20000;
                enemy.transform.position = playerGo.transform.position + UnderworldCamera2D.IsoRightXZ;
                var actor = enemy.AddComponent<PaintedEnemyActor>(); actor.Configure(PaintedEnemyKind.Cerberus);
                var bite = (CerberusAttack)actor.Pattern; float hp = player.CurrentHealth;
                actor.Tick(1f); Assert.That(bite.Phase, Is.EqualTo(CerberusAttack.AttackPhase.Waiting));
                actor.Tick(.5f); Assert.That(bite.Phase, Is.EqualTo(CerberusAttack.AttackPhase.Windup));
                actor.Tick(.5f); Assert.That(player.CurrentHealth, Is.EqualTo(hp));
                actor.Cancel(); Assert.That(bite.HasPendingDamage, Is.False);
                actor.Tick(.1f); Assert.That(player.CurrentHealth, Is.EqualTo(hp));
            }
            finally { Object.DestroyImmediate(enemy); Object.DestroyImmediate(playerGo); typeof(OrpheusPlayerController).GetProperty("Instance").SetValue(null,null); Time.timeScale = previousTime; }
        }

        [Test]
        public void AnimationPausesWithoutChangingFrameAndUsesEveryCleanMotionRow()
        {
            var go = new GameObject("PaintedAnimationClock");
            try
            {
                var animator = go.AddComponent<EnemyArtAnimator>(); animator.Configure(EnemyArtLibrary.Load(PaintedEnemyKind.RoosterRider));
                Assert.That(animator.IsReady, Is.True);
                animator.Play(EnemyMotion.Attack2,1); Sprite first = animator.Renderer.sprite;
                animator.Advance(0); Assert.That(animator.Renderer.sprite, Is.SameAs(first));
                animator.Advance(.8f); Assert.That(animator.Renderer.sprite, Is.Not.SameAs(first));
                Assert.That(animator.Library.FrameCount(EnemyMotion.Attack2), Is.EqualTo(2));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void PaintDamageRespectsDashAndSolidCover()
        {
            var playerGo = new GameObject("PaintedDamagePlayer"); var enemy = new GameObject("PaintedDamageOwner"); var wall = new GameObject("AttackCover");
            try
            {
                var player = playerGo.AddComponent<OrpheusPlayerController>(); playerGo.transform.position = Vector3.right * 30000;
                typeof(OrpheusPlayerController).GetProperty("Instance").SetValue(null,player);
                enemy.transform.position = playerGo.transform.position - Vector3.right * 2;
                var actor = enemy.AddComponent<PaintedEnemyActor>(); actor.Configure(PaintedEnemyKind.Cerberus);
                wall.transform.position = playerGo.transform.position - Vector3.right;
                wall.AddComponent<BoxCollider>().size = new Vector3(.2f,3,3); wall.AddComponent<IsoObstacle>(); Physics.SyncTransforms();
                Assert.That(actor.TryHitPlayer(enemy.transform.position,4,20), Is.False);
                Object.DestroyImmediate(wall); wall = null;
                typeof(OrpheusPlayerController).GetField("_isDashing",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(player,true);
                Assert.That(actor.TryHitPlayer(enemy.transform.position,4,20), Is.False);
            }
            finally { if(wall!=null) Object.DestroyImmediate(wall); Object.DestroyImmediate(enemy); Object.DestroyImmediate(playerGo); typeof(OrpheusPlayerController).GetProperty("Instance").SetValue(null,null); }
        }

        [Test]
        public void MovedGunslingerFiresFromItsLivePositionAndOwnsTheBullet()
        {
            var go = new GameObject("MovedGunslinger");
            try
            {
                var actor = go.AddComponent<PaintedEnemyActor>(); actor.Configure(PaintedEnemyKind.HoodedSkeleton);
                go.transform.position = Vector3.right * 45000;
                var shot = (HoodedSkeletonAttack)actor.Pattern;
                const BindingFlags hidden = BindingFlags.Instance|BindingFlags.NonPublic;
                typeof(HoodedSkeletonAttack).GetField("_aimDirection",hidden).SetValue(shot,Vector3.forward);
                typeof(HoodedSkeletonAttack).GetMethod("Fire",hidden).Invoke(shot,null);
                var bullets = Object.FindObjectsByType<PaintedEnemyProjectile>(FindObjectsSortMode.None);
                Assert.That(bullets.Length, Is.EqualTo(1));
                Assert.That(bullets[0].Owner, Is.SameAs(actor));
                Assert.That(Vector3.Distance(bullets[0].transform.position,go.transform.position), Is.LessThan(1));
                actor.Cancel(); Assert.That(Object.FindObjectsByType<PaintedEnemyProjectile>(FindObjectsSortMode.None), Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(go);
                foreach(var effect in Object.FindObjectsByType<EnemyPaintedEffects>(FindObjectsSortMode.None)) Object.DestroyImmediate(effect.gameObject);
            }
        }
    }
}
