using System;
using System.Collections.Generic;
using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>
    /// 2.5D Isometric HADES Guardian Boss: 3-Headed Cerberus!
    /// Combines isometric Hades floor telegraphs (Paw-Slam Ground Circles & 3-Way Fireball Fans)
    /// with 104 BPM Sonic Beat Rings (Gold = Soft Lyre Parry, Crimson = HARD BOOM Counter!).
    /// </summary>
    public class CerberusRhythmBoss : MonoBehaviour, IOrphicTarget
    {
        public static CerberusRhythmBoss ActiveBoss { get; private set; }

        [Header("Boss Stats & Rhythm")]
        public float maxHealth = 440f;
        public float bpm = 104f;
        public float noteTravelSeconds = 1.65f;
        public float hitWindowSeconds = 0.24f;

        public float CurrentHealth { get; private set; }
        public bool IsBattleActive { get; private set; }
        public bool IsDefeated { get; private set; }
        public string LastJudgement { get; private set; } = "";

        public event Action OnCerberusDefeated;

        private Transform _billboardRoot;
        private Transform[] _heads;
        private SpriteRenderer[] _headRenderers;
        private Transform _hitRingFloor;

        private float _beatInterval;
        private float _nextBeatTime;
        private int _beatCounter;

        private class IsoBeatRing
        {
            public bool requiresHardBoom;
            public float targetHitTime;
            public Transform visual;
            public bool resolved;
        }

        private readonly List<IsoBeatRing> _notes = new List<IsoBeatRing>();
        private static readonly int[] PhrasePattern = { 1, 1, 2, 0,  1, 2, 0, 2,  1, 1, 2, 0,  2, 2, 0, 0 };

        private void Awake()
        {
            ActiveBoss = this;
            CurrentHealth = maxHealth;
            _beatInterval = 60f / Mathf.Max(40f, bpm);

            SphereCollider sc = GetComponent<SphereCollider>();
            if (sc == null) sc = gameObject.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 1.85f;
            sc.center = new Vector3(0f, 1.2f, 0f);

            BuildBossVisuals();
        }

        private void BuildBossVisuals()
        {
            // Ground ritual shadow under Cerberus
            GameObject shadow = new GameObject("CerberusFloorAura");
            shadow.transform.SetParent(transform, false);
            shadow.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            shadow.transform.localScale = new Vector3(4.2f, 4.2f, 1f);
            RenderPipeline2DSetup.AttachGroundDecal(shadow, RenderPipeline2DSetup.SoftGlowSprite, new Color(1.8f, 0.15f, 0.25f, 0.45f), 2);

            // Golden Rhythm Target Ring on the arena floor in front of Cerberus!
            GameObject hitZone = new GameObject("FloorRhythmTargetRing");
            hitZone.transform.SetParent(transform, false);
            hitZone.transform.localPosition = new Vector3(-5.2f, 0.05f, -5.2f);
            hitZone.transform.localScale = new Vector3(2.4f, 2.4f, 1f);
            RenderPipeline2DSetup.AttachGroundDecal(hitZone, RenderPipeline2DSetup.RingSprite, new Color(2.5f, 2.0f, 0.45f, 0.90f), 5);
            _hitRingFloor = hitZone.transform;

            // 2.5D Billboarded 3-Headed Cerberus Silhouette
            _billboardRoot = new GameObject("CerberusBillboard25D").transform;
            _billboardRoot.SetParent(transform, false);
            _billboardRoot.rotation = RenderPipeline2DSetup.IsoCameraRotation;

            GameObject torso = new GameObject("CerberusBody");
            torso.transform.SetParent(_billboardRoot, false);
            torso.transform.localPosition = new Vector3(0f, 1.55f, 0.05f);
            torso.transform.localScale = new Vector3(3.2f, 3.1f, 1f);
            RenderPipeline2DSetup.AttachSprite(torso, RenderPipeline2DSetup.DiamondSprite, new Color(0.26f, 0.07f, 0.16f, 1f), 9);

            _heads = new Transform[3];
            _headRenderers = new SpriteRenderer[3];
            Vector3[] headOffsets =
            {
                new Vector3(-1.15f, 2.25f, 0f),
                new Vector3( 0.00f, 2.95f, -0.02f),
                new Vector3( 1.15f, 2.25f, 0f)
            };

            for (int i = 0; i < 3; i++)
            {
                GameObject h = new GameObject("CerberusHead_" + i);
                h.transform.SetParent(_billboardRoot, false);
                h.transform.localPosition = headOffsets[i];
                h.transform.localScale = new Vector3(1.35f, 1.25f, 1f);
                _headRenderers[i] = RenderPipeline2DSetup.AttachSprite(h, RenderPipeline2DSetup.DiamondSprite, new Color(1.55f, 0.18f, 0.26f, 1f), 11 + i);

                GameObject eyes = new GameObject("Eyes");
                eyes.transform.SetParent(h.transform, false);
                eyes.transform.localPosition = new Vector3(0f, 0.08f, -0.01f);
                eyes.transform.localScale = new Vector3(0.42f, 0.14f, 1f);
                RenderPipeline2DSetup.AttachSprite(eyes, RenderPipeline2DSetup.SquareSprite, new Color(2.9f, 2.0f, 0.35f, 1f), 15);

                _heads[i] = h.transform;
            }
        }

        public void ActivateGuardianDuel()
        {
            if (IsBattleActive || IsDefeated) return;
            IsBattleActive = true;
            _nextBeatTime = Time.time + 0.65f;
            OrpheusHud.Instance?.ShowBanner("CERBERUS, GUARDIAN OF STYX!  Gold Ring = Soft Strike  |  Crimson Ring = HARD BOOM!", 4.5f);
        }

        private void Update()
        {
            if (_billboardRoot != null) _billboardRoot.rotation = RenderPipeline2DSetup.IsoCameraRotation;
            if (IsDefeated || !IsBattleActive) return;

            OrpheusPlayerController player = OrpheusPlayerController.Instance;
            if (player == null || player.IsGameOver || player.IsVictory) return;

            float beatPhase = (Time.time / _beatInterval) * Mathf.PI * 2f;
            Vector3[] basePos =
            {
                new Vector3(-1.15f, 2.25f, 0f),
                new Vector3( 0.00f, 2.95f, -0.02f),
                new Vector3( 1.15f, 2.25f, 0f)
            };
            for (int i = 0; i < 3; i++)
            {
                _heads[i].localPosition = basePos[i] + new Vector3(0f, Mathf.Sin(beatPhase + i * 1.1f) * 0.18f, 0f);
                _headRenderers[i].color = Color.Lerp(_headRenderers[i].color, new Color(1.55f, 0.18f, 0.26f, 1f), Time.deltaTime * 8f);
            }

            if (Time.time >= _nextBeatTime)
            {
                int pat = PhrasePattern[_beatCounter % PhrasePattern.Length];
                _beatCounter++;
                _nextBeatTime += _beatInterval;

                ProceduralLyreAudio.Instance?.PlayRhythmBeat(pat == 2);
                if (pat != 0)
                {
                    SpawnFloorBeatNote(pat == 2, _nextBeatTime + noteTravelSeconds - _beatInterval);
                }
                else if (_beatCounter % 4 == 0)
                {
                    // Every 4th rest beat, launch a Hades ground Paw-Slam telegraph at Orpheus's position!
                    CerberusPawSlamHazard.Spawn(player.transform.position);
                }
            }

            UpdateBeatNotes(player);
        }

        private void SpawnFloorBeatNote(bool hardBoomRequired, float hitTime)
        {
            GameObject go = new GameObject(hardBoomRequired ? "IsoBeat_BOOM" : "IsoBeat_SOFT");
            go.transform.position = transform.position + new Vector3(0f, 0.07f, 0f);
            go.transform.localScale = hardBoomRequired ? new Vector3(1.8f, 1.8f, 1f) : new Vector3(1.1f, 1.1f, 1f);

            RenderPipeline2DSetup.AttachGroundDecal(
                go,
                hardBoomRequired ? RenderPipeline2DSetup.RingSprite : RenderPipeline2DSetup.DiamondSprite,
                hardBoomRequired ? new Color(2.9f, 0.28f, 0.25f, 1f) : new Color(2.5f, 2.0f, 0.42f, 1f),
                12);

            _notes.Add(new IsoBeatRing
            {
                requiresHardBoom = hardBoomRequired,
                targetHitTime = hitTime,
                visual = go.transform,
                resolved = false
            });
        }

        private void UpdateBeatNotes(OrpheusPlayerController player)
        {
            Vector3 startPos = transform.position + new Vector3(0f, 0.07f, 0f);
            Vector3 endPos = _hitRingFloor.position + new Vector3(0f, 0.02f, 0f);

            for (int i = _notes.Count - 1; i >= 0; i--)
            {
                IsoBeatRing n = _notes[i];
                if (n.resolved)
                {
                    if (n.visual != null) Destroy(n.visual.gameObject);
                    _notes.RemoveAt(i);
                    continue;
                }

                float rem = n.targetHitTime - Time.time;
                float u = 1f - (rem / noteTravelSeconds);
                if (n.visual != null) n.visual.position = Vector3.LerpUnclamped(startPos, endPos, u);

                if (rem < -hitWindowSeconds)
                {
                    n.resolved = true;
                    LastJudgement = "MISSED BEAT! Dash the Hellfire Fan!";
                    FireThreeWayHellfire(player.transform.position);
                    if (n.visual != null) Destroy(n.visual.gameObject);
                    _notes.RemoveAt(i);
                }
            }
        }

        public void OnPlayerRhythmPulse(float intensity, bool isHardBoom)
        {
            if (!IsBattleActive || IsDefeated) return;

            IsoBeatRing best = null;
            float bestAbs = hitWindowSeconds;

            foreach (IsoBeatRing n in _notes)
            {
                if (n.resolved) continue;
                float dt = Mathf.Abs(n.targetHitTime - Time.time);
                if (dt <= bestAbs)
                {
                    bestAbs = dt;
                    best = n;
                }
            }

            if (best == null) return;

            best.resolved = true;
            if (best.requiresHardBoom == isHardBoom)
            {
                float dmg = isHardBoom ? 62f : 34f;
                CurrentHealth = Mathf.Max(0f, CurrentHealth - dmg);
                LastJudgement = isHardBoom ? "PERFECT ORPHIC BOOM!! (-62)" : "PERFECT LYRE CHORD! (-34)";

                GroundShockwaveRing.Spawn(_hitRingFloor.position, isHardBoom ? 4.4f : 2.8f,
                    isHardBoom ? new Color(2.9f, 0.45f, 0.25f, 1f) : new Color(2.6f, 2.1f, 0.45f, 1f), 0.28f);
                GroundShockwaveRing.Spawn(transform.position + new Vector3(0f, 0.06f, 0f), 4.8f, new Color(2.7f, 1.85f, 0.4f, 1f), 0.30f);

                foreach (var sr in _headRenderers) sr.color = new Color(3f, 3f, 3f, 1f);
                UnderworldCamera2D.Instance?.AddShake(isHardBoom ? 0.60f : 0.22f);

                if (CurrentHealth <= 0f) DefeatCerberus();
            }
            else
            {
                LastJudgement = best.requiresHardBoom ? "TOO SOFT! Needed HARD BOOM!" : "TOO HARD! Needed SOFT STRIKE!";
                if (OrpheusPlayerController.Instance != null)
                {
                    FireThreeWayHellfire(OrpheusPlayerController.Instance.transform.position);
                }
            }
        }

        public void OnOrphicHit(float damage, float intensity, bool isHardBoom, Vector3 hitOrigin)
        {
            if (!IsBattleActive || IsDefeated) return;
            // Direct melee/boom hits also deal real combat damage alongside rhythm parries!
            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage * 0.45f);
            foreach (var sr in _headRenderers) sr.color = new Color(2.6f, 1.5f, 1.5f, 1f);
            if (CurrentHealth <= 0f) DefeatCerberus();
        }

        private void FireThreeWayHellfire(Vector3 targetPos)
        {
            Vector3 toPlayer = targetPos - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude < 0.01f) toPlayer = -UnderworldCamera2D.IsoUpXZ;
            toPlayer.Normalize();

            float[] angles = { -22f, 0f, 22f };
            foreach (float ang in angles)
            {
                Vector3 dir = Quaternion.Euler(0f, ang, 0f) * toPlayer;
                StygianOrbProjectile.Spawn(transform.position + new Vector3(0f, 0.65f, 0f) + dir * 1.2f, dir, 8.8f, 14f);
            }
        }

        private void DefeatCerberus()
        {
            IsDefeated = true;
            IsBattleActive = false;
            LastJudgement = "CERBERUS CHARMED INTO SLUMBER!";
            foreach (IsoBeatRing n in _notes)
            {
                if (n.visual != null) Destroy(n.visual.gameObject);
            }
            _notes.Clear();

            UnderworldCamera2D.Instance?.AddShake(0.9f);
            GroundShockwaveRing.Spawn(transform.position + new Vector3(0f, 0.08f, 0f), 9.5f, new Color(2.9f, 2.2f, 0.5f, 1f), 0.65f);
            OrpheusHud.Instance?.ShowBanner("CERBERUS SLUMBERS! Step through the Golden Archway into Sunlight!", 5.5f);

            OnCerberusDefeated?.Invoke();
            Destroy(gameObject, 0.35f);
        }
    }

    /// <summary>
    /// Hades Cerberus Floor Paw-Slam Telegraph Circle — charges for 0.85s then detonates (Dash out!).
    /// </summary>
    public class CerberusPawSlamHazard : MonoBehaviour
    {
        private Transform _fillDisc;
        private float _timer;
        private const float ChargeDuration = 0.85f;
        private const float Radius = 2.35f;

        public static void Spawn(Vector3 worldPos)
        {
            GameObject go = new GameObject("CerberusPawSlamTelegraph");
            go.transform.position = new Vector3(worldPos.x, 0.03f, worldPos.z);

            GameObject outer = new GameObject("OuterRing");
            outer.transform.SetParent(go.transform, false);
            outer.transform.localScale = new Vector3(Radius * 2f, Radius * 2f, 1f);
            RenderPipeline2DSetup.AttachGroundDecal(outer, RenderPipeline2DSetup.RingSprite, new Color(2.6f, 0.25f, 0.22f, 0.85f), 4);

            GameObject inner = new GameObject("ChargingFill");
            inner.transform.SetParent(go.transform, false);
            inner.transform.localScale = new Vector3(0.1f, 0.1f, 1f);
            RenderPipeline2DSetup.AttachGroundDecal(inner, RenderPipeline2DSetup.CircleSprite, new Color(2.2f, 0.20f, 0.18f, 0.32f), 3);

            CerberusPawSlamHazard haz = go.AddComponent<CerberusPawSlamHazard>();
            haz._fillDisc = inner.transform;
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            float u = Mathf.Clamp01(_timer / ChargeDuration);
            if (_fillDisc != null)
            {
                float d = Radius * 2f * u;
                _fillDisc.localScale = new Vector3(d, d, 1f);
            }

            if (_timer >= ChargeDuration)
            {
                GroundShockwaveRing.Spawn(transform.position + new Vector3(0f, 0.04f, 0f), Radius * 2.2f, new Color(2.9f, 0.38f, 0.18f, 1f), 0.25f);
                UnderworldCamera2D.Instance?.AddShake(0.28f);

                OrpheusPlayerController p = OrpheusPlayerController.Instance;
                if (p != null && !p.IsDashing)
                {
                    Vector3 diff = p.transform.position - transform.position;
                    diff.y = 0f;
                    if (diff.magnitude <= Radius)
                    {
                        p.TakeDamage(16f, transform.position);
                    }
                }
                Destroy(gameObject);
            }
        }
    }
}
