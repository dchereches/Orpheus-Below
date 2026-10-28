using System.Collections.Generic;
using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>A gunslinger that commits its aim before a doubletap, open fan, or closing volley.</summary>
    public sealed class HoodedSkeletonAttack : PaintedAttackPattern
    {
        public const float AimDuration = .52f;
        public const float RecoilDuration = .24f;
        public const float RecoveryDuration = .22f;
        public const float ShotCooldown = .9f;
        public const float FanAngle = 12f;
        public const float DoubletapGap = .18f;
        public const float SuppressionGap = .20f;
        public const int MaximumActiveProjectiles = 12;

        public enum GunAttack { Doubletap, OpenFan, ClosingVolley }

        [SerializeField] private float moveSpeed = 2.9f;
        [SerializeField] private float projectileSpeed = 17f;
        [SerializeField] private float shotDamage = 12f;

        private const float RetreatRange = 4.8f;
        private const float PreferredRange = 8f;
        private const float MaximumShotRange = 14f;
        private readonly List<PaintedEnemyProjectile> _shots = new List<PaintedEnemyProjectile>(MaximumActiveProjectiles);
        private readonly List<GameObject> _ownedEffects = new List<GameObject>(16);
        private Vector3 _aimDirection;
        private float _cooldown, _phaseRemaining, _burstRemaining;
        private int _volleyCount, _pendingBursts, _burstIndex;
        private GameObject _charge;

        public EnemyMotion Phase { get; private set; } = EnemyMotion.Idle;
        public GunAttack Variant { get; private set; }
        public float PhaseRemaining => _phaseRemaining;
        public float CooldownRemaining => _cooldown;
        public bool HasPendingShot => Phase == EnemyMotion.Windup || _pendingBursts > 0;
        public int VolleyCount => _volleyCount;
        public int ShotCount { get; private set; }
        public Vector3 LockedDirection => _aimDirection;
        public int PlannedProjectileCount => Variant == GunAttack.Doubletap ? 2 : Variant == GunAttack.OpenFan ? 4 : 6;
        public int PendingProjectileCount => Phase == EnemyMotion.Windup ? PlannedProjectileCount :
            _pendingBursts * (Variant == GunAttack.ClosingVolley ? 2 : 1);
        public int ActiveProjectileCount
        {
            get
            {
                int count = 0;
                foreach (var shot in _shots) if (shot != null && !shot.Removed) count++;
                return count;
            }
        }
        public int OwnedEffectCount
        {
            get
            {
                int count = 0;
                foreach (var effect in _ownedEffects) if (effect != null) count++;
                return count;
            }
        }

        public override void Initialize(PaintedEnemyActor actor)
        {
            Cancel();
            base.Initialize(actor);
            _cooldown = .4f;
        }

        public override void Tick(float dt)
        {
            if (Actor == null) return;
            var player = OrpheusPlayerController.Instance;
            if (player == null || player.IsGameOver || player.IsVictory ||
                (Actor.OwnerBoss != null && Actor.OwnerBoss.IsDefeated))
            {
                Cancel();
                return;
            }
            if (Time.timeScale <= 0f || dt <= 0f || float.IsNaN(dt) || float.IsInfinity(dt) ||
                UnderworldFrontEnd.BlocksGameplay || OrpheusHud.IsTutorialModalActive) return;

            // A slow frame must extend the committed warning rather than consume it unseen.
            dt = Mathf.Min(dt, .1f);
            _shots.RemoveAll(shot => shot == null || shot.Removed);
            _ownedEffects.RemoveAll(effect => effect == null);
            _cooldown = Mathf.Max(0f, _cooldown - dt);
            if (Phase == EnemyMotion.Windup)
            {
                Actor.Face(Actor.transform.position + _aimDirection);
                if (_charge != null) _charge.transform.position = Muzzle();
                _phaseRemaining = Mathf.Max(0f, _phaseRemaining - dt);
                if (_phaseRemaining <= 0f) Fire();
                return;
            }
            if (Phase == EnemyMotion.Attack || Phase == EnemyMotion.Attack2)
            {
                Actor.Face(Actor.transform.position + _aimDirection);
                _phaseRemaining = Mathf.Max(0f, _phaseRemaining - dt);
                if (_pendingBursts > 0)
                {
                    _burstRemaining -= dt;
                    if (_burstRemaining <= 0f)
                    {
                        EmitBurst(++_burstIndex);
                        _pendingBursts--;
                        _burstRemaining = BurstGap;
                    }
                }
                if (_pendingBursts == 0 && _phaseRemaining <= 0f) SetPhase(EnemyMotion.Recover, RecoveryDuration);
                return;
            }
            if (Phase == EnemyMotion.Recover)
            {
                _phaseRemaining = Mathf.Max(0f, _phaseRemaining - dt);
                if (_phaseRemaining <= 0f) SetPhase(EnemyMotion.Idle, .8f, true);
                return;
            }

            Vector3 toPlayer = player.transform.position - Actor.transform.position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;
            Vector3 direction = distance > .01f ? toPlayer / distance : Vector3.right;

            if (distance < RetreatRange)
            {
                Actor.MoveToward(Actor.transform.position - direction * 2f, moveSpeed * .9f, dt, navigate: true);
                SetLocomotion(EnemyMotion.Move);
            }
            else if (distance > PreferredRange)
            {
                Actor.MoveToward(player.transform.position, moveSpeed, dt, navigate: true);
                SetLocomotion(EnemyMotion.Move);
            }
            else SetLocomotion(EnemyMotion.Idle);
            Actor.Face(player.transform.position);

            if (_cooldown > 0f || distance > MaximumShotRange) return;
            _aimDirection = direction;
            Variant = (GunAttack)(_volleyCount % 3);
            SetPhase(EnemyMotion.Windup, AimDuration);
            _charge = Actor.Art?.PlayEffect(3, Muzzle(), .65f, AimDuration);
            TrackEffect(_charge);
        }

        private float BurstGap => Variant == GunAttack.ClosingVolley ? SuppressionGap : DoubletapGap;
        private Vector3 Muzzle() => Actor.transform.position + Vector3.up * .78f + _aimDirection * .55f;

        private void Fire()
        {
            DisposeEffect(_charge);
            _charge = null;
            _burstIndex = 0;
            _pendingBursts = Variant == GunAttack.Doubletap ? 1 : Variant == GunAttack.ClosingVolley ? 2 : 0;
            _burstRemaining = BurstGap;
            EmitBurst(0);
            _volleyCount++;
            _cooldown = ShotCooldown;
            SetPhase(Variant == GunAttack.Doubletap ? EnemyMotion.Attack : EnemyMotion.Attack2,
                RecoilDuration + _pendingBursts * BurstGap);
        }

        private void EmitBurst(int index)
        {
            Vector3 muzzle = Muzzle();
            if (Variant == GunAttack.OpenFan)
            {
                // Two broad lanes on each side leave a readable central escape lane.
                SpawnBullet(muzzle, -32f); SpawnBullet(muzzle, -FanAngle);
                SpawnBullet(muzzle, FanAngle); SpawnBullet(muzzle, 32f);
            }
            else if (Variant == GunAttack.ClosingVolley)
            {
                // The side lanes narrow in three beats without ever tracking a dodge.
                float angle = 24f - index * 8f;
                SpawnBullet(muzzle, -angle); SpawnBullet(muzzle, angle);
            }
            else SpawnBullet(muzzle, 0f);

            TrackEffect(Actor.Art?.PlayEffect(1, muzzle, .95f, .18f));
            TrackEffect(Actor.Art?.PlayEffect(3, muzzle, .65f, .35f));
        }

        private void SpawnBullet(Vector3 muzzle, float angle)
        {
            _shots.RemoveAll(shot => shot == null || shot.Removed);
            if (_shots.Count >= MaximumActiveProjectiles) return;
            // Shared projectile owns swept solid-cover collision, dash immunity, and row 2 impact.
            var shot = PaintedEnemyProjectile.Spawn(PaintedEnemyKind.HoodedSkeleton, 0, muzzle,
                Quaternion.Euler(0f, angle, 0f) * _aimDirection, projectileSpeed, shotDamage, .20f, 2f);
            shot.Owner = Actor;
            _shots.Add(shot);
            ShotCount++;
        }

        private void TrackEffect(GameObject effect)
        {
            if (effect == null) return;
            _ownedEffects.RemoveAll(item => item == null);
            if (_ownedEffects.Count >= 16)
            {
                DisposeEffect(_ownedEffects[0]);
                _ownedEffects.RemoveAt(0);
            }
            _ownedEffects.Add(effect);
        }

        private static void DisposeEffect(GameObject effect)
        {
            if (effect == null) return;
            effect.SetActive(false);
            if (Application.isPlaying) Destroy(effect); else DestroyImmediate(effect);
        }

        private void SetLocomotion(EnemyMotion motion)
        {
            if (Phase != motion) SetPhase(motion, motion == EnemyMotion.Move ? .65f : .8f, true);
        }

        private void SetPhase(EnemyMotion motion, float duration, bool loop = false)
        {
            Phase = motion;
            _phaseRemaining = loop ? 0f : duration;
            Actor.Art?.Play(motion, duration, loop);
        }

        public override void Cancel()
        {
            foreach (var shot in _shots) if (shot != null && !shot.Removed) shot.Remove();
            _shots.Clear();
            foreach (var effect in _ownedEffects) DisposeEffect(effect);
            _ownedEffects.Clear();
            _charge = null;
            _aimDirection = Vector3.zero;
            _cooldown = _phaseRemaining = _burstRemaining = 0f;
            _pendingBursts = _burstIndex = _volleyCount = 0;
            ShotCount = 0;
            Variant = GunAttack.Doubletap;
            Phase = EnemyMotion.Idle;
            if (Actor != null) Actor.Art?.Play(EnemyMotion.Idle, .8f, true);
        }

        protected override void OnDisable() => Cancel();
    }
}