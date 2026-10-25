using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>
    /// 2.5D Isometric HADES Lyre Combat driven by Piezo / Breath / Mouse / Mic (SensorPulseInput):
    ///   - SOFT (<0.60): 3-Hit Hades Combo (Crescent Ground Slash + Forward Micro-Step + Piercing Golden Bolt),
    ///     automatically upgrading into a high-speed DASH-STRIKE when triggered out of a Dash!
    ///   - HARD BOOM (>=0.60): Massive XZ Ground Shockwave + Hitstop + Armor/Pillar Shatter + WALL-SLAM Knockback!
    /// </summary>
    public class OrpheusLyreCombat : MonoBehaviour
    {
        [Header("Soft Hades Strike & Bolt (< 0.60)")]
        public float softBaseDamage = 20f;
        public float dashStrikeMultiplier = 1.45f;
        public float meleeSlashRadius = 2.85f;
        public float meleeSlashAngle = 128f;
        public float softBoltSpeed = 21f;

        [Header("Hard Orphic BOOM (>= 0.60)")]
        public float hardBoomMinDamage = 44f;
        public float hardBoomMaxDamage = 72f;
        public float hardBoomMinRadius = 4.3f;
        public float hardBoomMaxRadius = 6.4f;

        private OrpheusPlayerController _player;
        private int _comboStep;
        private float _comboResetTimer;
        private bool _inHitstop;

        private static readonly Collider[] HitBuf = new Collider[40];

        private void Awake()
        {
            _player = GetComponent<OrpheusPlayerController>();
        }

        private void Start()
        {
            if (SensorPulseInput.Instance != null)
            {
                SensorPulseInput.Instance.OnAttackPulse += HandleAttackPulse;
            }
        }

        private void OnDestroy()
        {
            if (SensorPulseInput.Instance != null)
            {
                SensorPulseInput.Instance.OnAttackPulse -= HandleAttackPulse;
            }
        }

        private void Update()
        {
            if (_comboResetTimer > 0f)
            {
                _comboResetTimer -= Time.deltaTime;
                if (_comboResetTimer <= 0f) _comboStep = 0;
            }
        }

        private void HandleAttackPulse(float intensity, bool isHardBoom)
        {
            if (_player == null || _player.IsGameOver || _player.IsVictory) return;

            bool dashStrike = _player.IsInDashStrikeWindow;

            if (CerberusRhythmBoss.ActiveBoss != null && CerberusRhythmBoss.ActiveBoss.IsBattleActive)
            {
                CerberusRhythmBoss.ActiveBoss.OnPlayerRhythmPulse(intensity, isHardBoom);
            }

            if (isHardBoom)
            {
                ExecuteHardBoom(intensity, dashStrike);
            }
            else
            {
                ExecuteSoftComboStrike(intensity, dashStrike);
            }
        }

        private void ExecuteSoftComboStrike(float intensity, bool isDashStrike)
        {
            ProceduralLyreAudio.Instance?.PlaySoftAttack(intensity);

            Vector3 aimDir = _player.AimDirectionXZ;
            if (aimDir.sqrMagnitude < 0.001f) aimDir = UnderworldCamera2D.IsoUpXZ;
            aimDir.Normalize();

            // Crisp Hades forward micro-step (or long Dash-Strike lunge!)
            if (isDashStrike)
            {
                _player.TriggerAttackLunge(aimDir, 18.5f, 0.13f);
            }
            else
            {
                _player.TriggerAttackLunge(aimDir, 7.8f, 0.075f);
            }

            float comboScale = _comboStep == 2 ? 1.25f : 1.0f;
            float dmg = softBaseDamage * comboScale * Mathf.Lerp(0.90f, 1.30f, intensity / 0.6f);
            if (isDashStrike) dmg *= dashStrikeMultiplier;

            Vector3 slashOrigin = transform.position + new Vector3(0f, 0.06f, 0f);
            float reach = isDashStrike ? meleeSlashRadius * 1.22f : meleeSlashRadius;

            // Flat 2.5D sweeping crescent slash on the chamber floor
            Color slashCol = isDashStrike
                ? new Color(2.8f, 1.35f, 0.35f, 0.95f)
                : (_comboStep == 2 ? new Color(2.6f, 2.05f, 0.50f, 0.92f) : new Color(2.2f, 1.75f, 0.42f, 0.88f));

            GroundSlashArcVfx.Spawn(slashOrigin + aimDir * 0.45f, aimDir, reach * 1.65f, slashCol, _comboStep % 2 == 1);
            UnderworldCamera2D.Instance?.AddShake(isDashStrike ? 0.16f : 0.07f);

            // Hit enemies & enemy projectiles in melee arc
            HitConeXZ(transform.position, aimDir, reach, meleeSlashAngle, dmg, intensity, isHardBoom: false);

            // Fire fast piercing Golden Lyre Note along the XZ floor
            SpawnIsoLyreBolt(transform.position + aimDir * 0.65f + new Vector3(0f, 0.55f, 0f), aimDir, dmg * 0.75f, intensity, isDashStrike);

            _comboStep = (_comboStep + 1) % 3;
            _comboResetTimer = 0.68f;
        }

        private void ExecuteHardBoom(float intensity, bool isDashStrike)
        {
            ProceduralLyreAudio.Instance?.PlayHardBoom(intensity);

            Vector3 aimDir = _player.AimDirectionXZ;
            if (isDashStrike)
            {
                _player.TriggerAttackLunge(aimDir, 15.0f, 0.11f);
            }

            float t = Mathf.InverseLerp(0.60f, 1.0f, intensity);
            float radius = Mathf.Lerp(hardBoomMinRadius, hardBoomMaxRadius, t);
            float dmg = Mathf.Lerp(hardBoomMinDamage, hardBoomMaxDamage, t) * (isDashStrike ? 1.2f : 1.0f);

            Vector3 groundCenter = transform.position + new Vector3(0f, 0.05f, 0f);

            // Triple layered HDR Crimson-Gold Ground Shockwave Rings on XZ plane!
            GroundShockwaveRing.Spawn(groundCenter, radius * 2.0f, new Color(2.9f, 0.42f, 0.22f, 1.0f), 0.34f);
            GroundShockwaveRing.Spawn(groundCenter, radius * 1.42f, new Color(2.7f, 1.95f, 0.45f, 0.95f), 0.24f);
            GroundShockwaveRing.Spawn(groundCenter, radius * 0.80f, new Color(2.6f, 2.6f, 2.6f, 0.90f), 0.15f);

            UnderworldCamera2D.Instance?.AddShake(Mathf.Lerp(0.50f, 0.85f, t));

            int hits = HitRadiusXZ(transform.position, radius, dmg, intensity, isHardBoom: true);
            if (hits > 0 && !_inHitstop)
            {
                StartCoroutine(HitstopRoutine(0.048f));
            }
        }

        private int HitConeXZ(Vector3 origin, Vector3 forwardXZ, float radius, float fullAngleDeg, float dmg, float intensity, bool isHardBoom)
        {
            int n = Physics.OverlapSphereNonAlloc(origin + new Vector3(0f, 0.5f, 0f), radius, HitBuf, ~0, QueryTriggerInteraction.Collide);
            HashSet<IOrphicTarget> visited = new HashSet<IOrphicTarget>();
            float halfAngle = fullAngleDeg * 0.5f;
            int hits = 0;

            for (int i = 0; i < n; i++)
            {
                Collider c = HitBuf[i];

                // Deflect/destroy enemy Stygian orbs with your melee swing (just like Hades!)
                StygianOrbProjectile orb = c.GetComponent<StygianOrbProjectile>();
                if (orb != null)
                {
                    orb.ShatterOrb();
                    continue;
                }

                IOrphicTarget tgt = c.GetComponentInParent<IOrphicTarget>();
                if (tgt == null || !visited.Add(tgt)) continue;

                Vector3 toTgt = c.transform.position - origin;
                toTgt.y = 0f;
                if (toTgt.magnitude <= 0.85f || Vector3.Angle(forwardXZ, toTgt) <= halfAngle)
                {
                    tgt.OnOrphicHit(dmg, intensity, isHardBoom, origin);
                    hits++;
                }
            }
            return hits;
        }

        private int HitRadiusXZ(Vector3 origin, float radius, float dmg, float intensity, bool isHardBoom)
        {
            int n = Physics.OverlapSphereNonAlloc(origin + new Vector3(0f, 0.5f, 0f), radius, HitBuf, ~0, QueryTriggerInteraction.Collide);
            HashSet<IOrphicTarget> visited = new HashSet<IOrphicTarget>();
            int hits = 0;

            for (int i = 0; i < n; i++)
            {
                Collider c = HitBuf[i];

                StygianOrbProjectile orb = c.GetComponent<StygianOrbProjectile>();
                if (orb != null)
                {
                    orb.ShatterOrb();
                    continue;
                }

                IOrphicTarget tgt = c.GetComponentInParent<IOrphicTarget>();
                if (tgt != null && visited.Add(tgt))
                {
                    tgt.OnOrphicHit(dmg, intensity, isHardBoom, origin);
                    hits++;
                }
            }
            return hits;
        }

        private void SpawnIsoLyreBolt(Vector3 pos, Vector3 dirXZ, float dmg, float intensity, bool empowered)
        {
            GameObject bolt = new GameObject("IsoLyreBolt");
            bolt.transform.position = pos;

            // Ground-aligned glowing diamond spearhead pointing along dirXZ!
            GameObject vis = new GameObject("BoltVisual");
            vis.transform.SetParent(bolt.transform, false);
            vis.transform.localScale = empowered ? new Vector3(0.48f, 0.92f, 1f) : new Vector3(0.36f, 0.72f, 1f);
            RenderPipeline2DSetup.AttachGroundDecal(vis, RenderPipeline2DSetup.DiamondSprite,
                empowered ? new Color(2.8f, 1.6f, 0.4f, 1f) : new Color(2.4f, 1.95f, 0.45f, 1f), 14);
            bolt.transform.rotation = Quaternion.LookRotation(dirXZ, Vector3.up);

            SphereCollider sc = bolt.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.34f;

            bolt.AddComponent<OrphicNoteProjectile>().Init(dirXZ, softBoltSpeed, dmg, intensity);
        }

        private IEnumerator HitstopRoutine(float realSeconds)
        {
            _inHitstop = true;
            if (Time.timeScale > 0.2f) Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(realSeconds);
            if (_player != null && !_player.IsGameOver && _player.LookBackProgress01 <= 0.01f)
            {
                Time.timeScale = 1f;
            }
            _inHitstop = false;
        }
    }

    public class OrphicNoteProjectile : MonoBehaviour
    {
        private Vector3 _dirXZ;
        private float _speed;
        private float _dmg;
        private float _intensity;
        private float _life = 0.72f;
        private readonly HashSet<IOrphicTarget> _hitSet = new HashSet<IOrphicTarget>();

        public void Init(Vector3 dirXZ, float speed, float dmg, float intensity)
        {
            _dirXZ = dirXZ.normalized;
            _speed = speed;
            _dmg = dmg;
            _intensity = intensity;
        }

        private void Update()
        {
            float step = _speed * Time.deltaTime;
            transform.position += _dirXZ * step;
            _life -= Time.deltaTime;
            if (_life <= 0f) Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<OrpheusPlayerController>() != null) return;

            IsoObstacle obs = other.GetComponent<IsoObstacle>();
            if (obs != null && obs.kind == IsoObstacleKind.SolidWall)
            {
                GroundShockwaveRing.Spawn( new Vector3(transform.position.x, 0.06f, transform.position.z), 0.9f, new Color(2.0f, 1.6f, 0.4f, 0.8f), 0.14f);
                Destroy(gameObject);
                return;
            }

            IOrphicTarget tgt = other.GetComponentInParent<IOrphicTarget>();
            if (tgt != null && _hitSet.Add(tgt))
            {
                tgt.OnOrphicHit(_dmg, _intensity, false, transform.position - _dirXZ * 0.5f);
                GroundShockwaveRing.Spawn(new Vector3(transform.position.x, 0.06f, transform.position.z), 1.35f, new Color(2.3f, 1.85f, 0.5f, 0.9f), 0.16f);
                Destroy(gameObject);
            }
        }
    }
}
