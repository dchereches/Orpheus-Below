using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>A deliberate gunslinger: hold aim, shoot, then make room before aiming again.</summary>
    public sealed class HoodedSkeletonAttack : PaintedAttackPattern
    {
        public const float AimDuration = .68f;
        public const float RecoilDuration = .30f;
        public const float RecoveryDuration = .36f;
        public const float ShotCooldown = 1.6f;
        public const float FanAngle = 14f;

        [SerializeField] private float moveSpeed = 2.6f;
        [SerializeField] private float projectileSpeed = 13f;
        [SerializeField] private float shotDamage = 12f;

        private const float RetreatRange = 4.8f;
        private const float PreferredRange = 8f;
        private const float MaximumShotRange = 14f;
        private Vector3 _aimDirection;
        private float _cooldown;
        private float _phaseRemaining;
        private int _volleyCount;
        private bool _fanPending;

        public EnemyMotion Phase { get; private set; } = EnemyMotion.Idle;
        public float PhaseRemaining => _phaseRemaining;
        public float CooldownRemaining => _cooldown;
        public bool HasPendingShot => Phase == EnemyMotion.Windup;
        public int VolleyCount => _volleyCount;

        public override void Initialize(PaintedEnemyActor actor)
        {
            base.Initialize(actor);
            Cancel();
            _cooldown = .65f;
        }

        public override void Tick(float dt)
        {
            if (Actor == null) return;
            var player = OrpheusPlayerController.Instance;
            if (player == null || player.IsGameOver || player.IsVictory)
            {
                Cancel();
                return;
            }
            if (Time.timeScale <= 0f || dt <= 0f || float.IsNaN(dt) || float.IsInfinity(dt)) return;

            _cooldown = Mathf.Max(0f, _cooldown - dt);
            if (Phase == EnemyMotion.Windup)
            {
                // Aim is committed at the start of the telegraph, so a sidestep or dash can evade it.
                Actor.Face(Actor.transform.position + _aimDirection);
                _phaseRemaining = Mathf.Max(0f, _phaseRemaining - dt);
                if (_phaseRemaining <= 0f) Fire();
                return;
            }
            if (Phase == EnemyMotion.Attack || Phase == EnemyMotion.Attack2)
            {
                _phaseRemaining = Mathf.Max(0f, _phaseRemaining - dt);
                if (_phaseRemaining <= 0f) SetPhase(EnemyMotion.Recover, RecoveryDuration);
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
                Actor.MoveToward(Actor.transform.position - direction * 2f, moveSpeed * .9f, dt,navigate:true);
                SetLocomotion(EnemyMotion.Move);
            }
            else if (distance > PreferredRange)
            {
                Actor.MoveToward(player.transform.position, moveSpeed, dt,navigate:true);
                SetLocomotion(EnemyMotion.Move);
            }
            else SetLocomotion(EnemyMotion.Idle);
            Actor.Face(player.transform.position);

            if (_cooldown > 0f || distance > MaximumShotRange) return;
            _aimDirection = direction;
            _fanPending = (_volleyCount & 1) != 0;
            SetPhase(EnemyMotion.Windup, AimDuration);
        }

        private void Fire()
        {
            Vector3 muzzle = Actor.transform.position + Vector3.up * .78f + _aimDirection * .55f;
            if (_fanPending)
            {
                SpawnBullet(muzzle, Quaternion.Euler(0f, -FanAngle, 0f) * _aimDirection);
                SpawnBullet(muzzle, _aimDirection);
                SpawnBullet(muzzle, Quaternion.Euler(0f, FanAngle, 0f) * _aimDirection);
            }
            else SpawnBullet(muzzle, _aimDirection);

            Actor.Art?.PlayEffect(1, muzzle, .95f, .18f);
            Actor.Art?.PlayEffect(3, muzzle, .65f, .55f);
            _volleyCount++;
            _cooldown = ShotCooldown;
            SetPhase(_fanPending ? EnemyMotion.Attack2 : EnemyMotion.Attack, RecoilDuration);
            _fanPending = false;
        }

        private void SpawnBullet(Vector3 muzzle, Vector3 direction)
        {
            // Shared projectile owns swept solid-cover collision, dash immunity, and effect row 2 impact.
            PaintedEnemyProjectile.Spawn(PaintedEnemyKind.HoodedSkeleton, 0, muzzle, direction,
                projectileSpeed, shotDamage, .20f, 2.5f).Owner = Actor;
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
            _aimDirection = Vector3.zero;
            _cooldown = 0f;
            _phaseRemaining = 0f;
            _fanPending = false;
            _volleyCount = 0;
            Phase = EnemyMotion.Idle;
            if (Actor != null) Actor.Art?.Play(EnemyMotion.Idle, .8f, true);
        }

        protected override void OnDisable() => Cancel();
    }
}
