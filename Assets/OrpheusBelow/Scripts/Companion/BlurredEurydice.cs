using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>
    /// Blurred Eurydice — Active Psychological & Escort Mechanic:
    ///   - Connected to Orpheus by a delicate Soul Tether thread (shimmers pale silver-gold normally;
    ///     turns jagged crimson when Mourning Abductor Shades latch onto her and drag her into the abyss!)
    ///   - Answers nearby Soft Lyre notes (or the B-S Sanctuary Cadence) with a rising musical hum + moonlit ripple
    ///     so you can verify the REAL Eurydice vs Underworld Mirage Decoys without ever pressing [Hold Q]!
    /// </summary>
    public class BlurredEurydice : MonoBehaviour
    {
        public Transform orpheus;
        public float followLagDistance = 3.8f;
        public float smoothSpeed = 4.8f;

        public bool IsVanished { get; private set; }
        public bool InTemptationZone { get; private set; }
        public string ActiveWhisper { get; private set; } = "";
        public float AbductionDanger01 { get; private set; }
        public bool HasSanctuaryShield => _sanctuaryTimer > 0f;
        public bool IsBeingAbducted => _activeAbductors.Count > 0;
        public bool IsTeachingPose { get; private set; }

        private readonly Queue<Vector3> _breadcrumbs = new Queue<Vector3>();
        private readonly HashSet<ShadeEnemy> _activeAbductors = new HashSet<ShadeEnemy>();
        private Vector3 _lastRecordedPos;
        private Vector3 _desiredPos;

        private Transform _billboardRoot;
        private Transform[] _blurLayers;
        private SpriteRenderer[] _blurRenderers;
        private SpriteRenderer _sharpCoreHead;
        private SpriteRenderer _sharpCoreRobe;
        private SpriteRenderer _generatedSpriteRenderer;
        private EurydiceSpriteAnimator _spriteAnimator;
        private SpriteRenderer _sanctuaryRingSr;
        private LineRenderer _tetherLine;

        private float _focus01;
        private float _sanctuaryTimer;
        private float _replyCooldown;
        private float _humPoseTimer;
        private bool _shownAbductorHint;
        private Vector3 _lastAnimationPosition;
        private readonly EurydiceAnimationBrain _animationBrain = new EurydiceAnimationBrain();
        private SpriteRenderer _paintedEffects;
        private float _joyTimer, _fearTimer, _reformTimer, _shieldIntroTimer, _threatScanTimer;
        private readonly Collider[] _nearby = new Collider[24];
        public EurydiceAnimationState CurrentAnimation => _spriteAnimator != null ? _spriteAnimator.CurrentState : EurydiceAnimationState.Idle;
        public EurydiceFacing Facing => _animationBrain.Facing;

        private void Start()
        {
            BuildBlurredVisualStack();
            BuildSoulTetherLine();
            var depth = gameObject.AddComponent<PaintedActorDepth>();
            depth.Exclude(_paintedEffects);
            depth.Exclude(_sanctuaryRingSr);
            if (orpheus != null)
            {
                SnapBehindOrpheus(orpheus.position - UnderworldCamera2D.IsoUpXZ * followLagDistance);
            }
            _lastAnimationPosition = transform.position;
        }

        public void SnapBehindOrpheus(Vector3 worldPos)
        {
            _breadcrumbs.Clear();
            _activeAbductors.Clear();
            AbductionDanger01 = 0f;
            worldPos.y = 0f;
            if (PaintedChamberMap.Active != null) worldPos = PaintedChamberMap.Active.NearestWalkable(worldPos, .5f);
            transform.position = worldPos;
            _desiredPos = worldPos;
            _lastAnimationPosition = worldPos;
            _animationBrain.ResetMotion();
            _spriteAnimator?.ResetMotion();
            _reformTimer = IsTeachingPose ? 0 : .9f; _joyTimer = 0; _fearTimer = 0; _humPoseTimer = 0;
            if (orpheus != null) _lastRecordedPos = orpheus.position;
            if (IsTeachingPose) SetTeachingPose(true);
        }

        private void BuildSoulTetherLine()
        {
            GameObject tetherGo = new GameObject("SoulTetherThread");
            tetherGo.transform.SetParent(transform, false);
            _tetherLine = tetherGo.AddComponent<LineRenderer>();
            _tetherLine.positionCount = 12;
            _tetherLine.useWorldSpace = true;
            _tetherLine.startWidth = 0.032f;
            _tetherLine.endWidth = 0.022f;
            _tetherLine.sharedMaterial = RenderPipeline2DSetup.Get2DSpriteMaterial();
            _tetherLine.sortingOrder = 5;
        }

        private void BuildBlurredVisualStack()
        {
            GameObject shadow = new GameObject("EurydiceMistAura");
            shadow.transform.SetParent(transform, false);
            shadow.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            shadow.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
            RenderPipeline2DSetup.AttachGroundDecal(shadow, RenderPipeline2DSetup.SoftGlowSprite, new Color(0.35f, 0.75f, 1.0f, 0.08f), 1);

            GameObject sancGo = new GameObject("SanctuaryAegisRing");
            sancGo.transform.SetParent(transform, false);
            sancGo.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            sancGo.transform.localScale = new Vector3(3.6f, 3.6f, 1f);
            _sanctuaryRingSr = RenderPipeline2DSetup.AttachGroundDecal(sancGo, RenderPipeline2DSetup.RingSprite, new Color(2.6f, 2.1f, 0.55f, 0f), 6);

            _billboardRoot = new GameObject("EurydiceBillboard25D").transform;
            _billboardRoot.SetParent(transform, false);
            _billboardRoot.localPosition = new Vector3(0f, 0.85f, 0f);
            _billboardRoot.rotation = RenderPipeline2DSetup.IsoCameraRotation;

            const int layerCount = 8;
            _blurLayers = new Transform[layerCount];
            _blurRenderers = new SpriteRenderer[layerCount];

            for (int i = 0; i < layerCount; i++)
            {
                GameObject layerGo = new GameObject("BlurGhostLayer_" + i);
                layerGo.transform.SetParent(_billboardRoot, false);
                layerGo.transform.localScale = new Vector3(1.25f + i * 0.13f, 2.0f + i * 0.11f, 1f);

                Color c = Color.Lerp(
                    new Color(0.52f, 0.86f, 1.15f, 0.20f),
                    new Color(1.05f, 0.76f, 0.95f, 0.13f),
                    i / (float)(layerCount - 1));

                _blurRenderers[i] = RenderPipeline2DSetup.AttachSprite(layerGo, RenderPipeline2DSetup.SoftGlowSprite, c, 6);
                _blurLayers[i] = layerGo.transform;
            }

            GameObject robeGo = new GameObject("EurydiceSharpRobe");
            robeGo.transform.SetParent(_billboardRoot, false);
            robeGo.transform.localPosition = new Vector3(0f, -0.12f, 0f);
            robeGo.transform.localScale = new Vector3(0.60f, 1.28f, 1f);
            _sharpCoreRobe = RenderPipeline2DSetup.AttachSprite(robeGo, RenderPipeline2DSetup.DiamondSprite, new Color(0.85f, 0.96f, 1.30f, 0f), 8);

            GameObject headGo = new GameObject("EurydiceSharpHead");
            headGo.transform.SetParent(_billboardRoot, false);
            headGo.transform.localPosition = new Vector3(0f, 0.68f, -0.01f);
            headGo.transform.localScale = new Vector3(0.42f, 0.42f, 1f);
            _sharpCoreHead = RenderPipeline2DSetup.AttachSprite(headGo, RenderPipeline2DSetup.CircleSprite, new Color(1.2f, 1.05f, 0.92f, 0f), 9);

            GameObject spriteGo = new GameObject("EurydiceDarkCrimsonSprite");
            spriteGo.transform.SetParent(_billboardRoot, false);
            spriteGo.transform.localPosition = Quaternion.Inverse(_billboardRoot.rotation) * (-Vector3.up * .85f) - Vector3.forward * .03f;
            spriteGo.transform.localScale = Vector3.one;
            _generatedSpriteRenderer = spriteGo.AddComponent<SpriteRenderer>();
            _generatedSpriteRenderer.sharedMaterial = RenderPipeline2DSetup.Get2DSpriteMaterial();
            _generatedSpriteRenderer.sortingOrder = 10;
            _generatedSpriteRenderer.color = new Color(1f, 1f, 1f, 0.82f);
            _spriteAnimator = spriteGo.AddComponent<EurydiceSpriteAnimator>();

            if (_spriteAnimator.IsReady)
            {
                _sharpCoreRobe.enabled = false;
                _sharpCoreHead.enabled = false;
                if (_spriteAnimator.UsesLook02)
                {
                    foreach (var layer in _blurRenderers) layer.enabled = false;
                    var effects = new GameObject("EurydicePaintedEffects");
                    effects.transform.SetParent(_billboardRoot, false);
                    effects.transform.localPosition = Quaternion.Inverse(_billboardRoot.rotation) * (-Vector3.up * .85f)
                        + new Vector3(0, .85f, .05f);
                    _paintedEffects = effects.AddComponent<SpriteRenderer>();
                    _paintedEffects.sharedMaterial = RenderPipeline2DSetup.Get2DSpriteMaterial();
                    _paintedEffects.sortingOrder = 8;
                    _spriteAnimator.AttachEffects(_paintedEffects);
                }
            }
            else
            {
                _generatedSpriteRenderer.enabled = false;
            }
            if (IsTeachingPose) SetTeachingPose(true);
        }

        public void RegisterAbductor(ShadeEnemy shade)
        {
            if (shade != null && _sanctuaryTimer <= 0f)
            {
                bool newlyAdded = _activeAbductors.Add(shade);
                if (newlyAdded && !_shownAbductorHint)
                {
                    _shownAbductorHint = true;
                    OrpheusHud.Instance?.ShowSubtleCallout(
                        "Eurydice: \"Orpheus, they have me! Break their hold before they pull me into the deep!\"", 4.2f);
                }
            }
        }

        public void SetTeachingPose(bool active)
        {
            IsTeachingPose = active && !IsVanished;
            if (IsTeachingPose)
            {
                _reformTimer = 0;
                _spriteAnimator?.SetState(IsBeingAbducted || _fearTimer > 0 ? EurydiceAnimationState.Fear
                    : HasSanctuaryShield && _shieldIntroTimer > 0 ? EurydiceAnimationState.Sanctuary : EurydiceAnimationState.Teaching);
            }
        }

        public void UnregisterAbductor(ShadeEnemy shade)
        {
            if (_activeAbductors.Remove(shade) && _activeAbductors.Count == 0 && !IsVanished) Celebrate(1.4f);
        }

        public void Celebrate(float duration = 1.6f) { if (!IsVanished) _joyTimer = Mathf.Max(_joyTimer, duration); }
        public void NotifyThreat(float duration = 1.2f) { if (!IsVanished && !HasSanctuaryShield) _fearTimer = Mathf.Max(_fearTimer, duration); }

        /// <summary>
        /// Triggered when Orpheus plays a Soft Ping near her or executes the B-S Sanctuary Cadence!
        /// </summary>
        public void TriggerAcousticReply(bool grantSanctuaryShield)
        {
            if (IsVanished) return;

            if (grantSanctuaryShield)
            {
                _sanctuaryTimer = 4.5f;
                _shieldIntroTimer = 1f;
                AbductionDanger01 = 0f;
                // Repel & pacify-damage any Abductor Shades clinging to Eurydice!
                List<ShadeEnemy> copy = new List<ShadeEnemy>(_activeAbductors);
                _activeAbductors.Clear();
                foreach (ShadeEnemy ab in copy)
                {
                    if (ab != null) ab.OnOrphicHit(55f, 0.85f, true, transform.position);
                }
                GroundShockwaveRing.Spawn(transform.position + new Vector3(0f, 0.06f, 0f), 5.0f, new Color(2.6f, 2.15f, 0.60f, 1f), 0.38f);
                OrpheusHud.Instance?.ShowSubtleCallout("Eurydice: \"The golden circle holds! Their claws cannot touch me!\"", 3.2f);
            }

            if (_replyCooldown <= 0f)
            {
                _replyCooldown = 0.9f;
                _humPoseTimer = 0.85f;
                StartCoroutine(HumReplyRoutine());
            }
        }

        private static readonly string[] AcousticPingReplies = new string[]
        {
            "Eurydice: \"I am here, Orpheus. Right behind you.\"",
            "Eurydice: \"I hear the note... I am keeping pace.\"",
            "Eurydice: \"Right here, my love. Do not turn.\""
        };
        private int _pingReplyIndex;

        private IEnumerator HumReplyRoutine()
        {
            yield return new WaitForSeconds(0.20f);
            if (IsVanished) yield break;
            ProceduralLyreAudio.Instance?.PlayEurydiceHumReply();
            GroundShockwaveRing.Spawn(transform.position + new Vector3(0f, 0.05f, 0f), 3.2f, new Color(0.75f, 1.85f, 2.4f, 0.90f), 0.42f);
            if (_sanctuaryTimer <= 0f)
            {
                VoiceDirector.Instance?.PlayNextPingReply();
            }
        }

        public void SetLookBackFocus(float focus01)
        {
            _focus01 = Mathf.Clamp01(focus01);
            if (_focus01 > 0.25f && !IsVanished)
            {
                VoiceDirector.Instance?.PlayLookBackTemptation();
            }
        }

        public void SetTemptationZone(bool active, string whisperText)
        {
            if (active && !InTemptationZone) NotifyThreat(1.5f);
            InTemptationZone = active;
            ActiveWhisper = active ? whisperText : "";
        }

        public void ShatterIntoAsh()
        {
            if (IsVanished) return;
            IsVanished = true;
            IsTeachingPose = false;
            _spriteAnimator?.SetState(EurydiceAnimationState.Shatter);
            _focus01 = 1f;
            if (_tetherLine != null) _tetherLine.enabled = false;
            StartCoroutine(ShatterRoutine());
        }

        private IEnumerator ShatterRoutine()
        {
            UpdateBlurVisuals();
            yield return new WaitForSeconds(1.1f);

            for (int i = 0; i < 42; i++)
            {
                GameObject ash = new GameObject("EurydiceAsh");
                ash.transform.position = transform.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0.2f, 1.5f), Random.Range(-0.5f, 0.5f));
                float sz = Random.Range(0.09f, 0.24f);
                ash.transform.localScale = new Vector3(sz, sz, 1f);
                Color col = Random.value > 0.4f ? new Color(1.7f, 1.15f, 0.45f, 0.9f) : new Color(0.7f, 0.95f, 1.45f, 0.85f);
                SpriteRenderer sr = RenderPipeline2DSetup.AttachBillboardSprite(ash, RenderPipeline2DSetup.SoftGlowSprite, col, 16);
                ash.AddComponent<RisingAshParticle>().Init(sr, driftUpSpeed: 1.8f, duration: 2.0f);
            }

            foreach (var r in _blurRenderers) if (r != null) r.enabled = false;
            if (_sharpCoreHead != null) _sharpCoreHead.enabled = false;
            if (_sharpCoreRobe != null) _sharpCoreRobe.enabled = false;
            if (_generatedSpriteRenderer != null) _generatedSpriteRenderer.enabled = false;
            if (_paintedEffects != null) _paintedEffects.enabled = false;
        }

        private void Update()
        {
            if (UnderworldFrontEnd.BlocksGameplay) return;
            if (IsVanished) return;
            if (_billboardRoot != null) _billboardRoot.rotation = UnderworldCamera2D.CurrentBillboardRotation;

            float dt = Time.deltaTime;
            if (dt <= 0) { _lastAnimationPosition = transform.position; return; }
            var livingLeader = OrpheusPlayerController.Instance;
            if (livingLeader != null && (livingLeader.IsGameOver || livingLeader.IsVictory))
                _activeAbductors.Clear();
            if (_replyCooldown > 0f) _replyCooldown -= dt;
            if (_humPoseTimer > 0f) _humPoseTimer -= dt;
            if (_sanctuaryTimer > 0f) _sanctuaryTimer -= dt;
            _joyTimer = Mathf.Max(0, _joyTimer - dt);
            _fearTimer = Mathf.Max(0, _fearTimer - dt);
            _reformTimer = Mathf.Max(0, _reformTimer - dt);
            _shieldIntroTimer = Mathf.Max(0, _shieldIntroTimer - dt);
            _threatScanTimer -= dt;
            if (_threatScanTimer <= 0 && !HasSanctuaryShield)
            {
                _threatScanTimer = .2f;
                int count = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up * .6f, 2.1f, _nearby, ~0, QueryTriggerInteraction.Collide);
                for (int i = 0; i < count; i++)
                    if (_nearby[i].GetComponentInParent<ShadeEnemy>() is ShadeEnemy shade && shade.CurrentHealth > 0) { NotifyThreat(.6f); break; }
            }

            if (_sanctuaryRingSr != null)
            {
                Color sc = _sanctuaryRingSr.color;
                sc.a = _sanctuaryTimer > 0f ? (0.55f + Mathf.Sin(Time.time * 8f) * 0.18f) : 0f;
                _sanctuaryRingSr.color = sc;
            }

            // Prune destroyed abductors
            _activeAbductors.RemoveWhere(a => a == null);

            if (orpheus != null)
            {
                OrpheusPlayerController pc = OrpheusPlayerController.Instance;
                if (pc != null && pc.IsIn2DBossStage)
                {
                    _activeAbductors.Clear();
                    AbductionDanger01 = 0f;
                    Vector3 stageLeftBg = orpheus.position
                                        - UnderworldCamera2D.IsoRightXZ * 3.1f
                                        + UnderworldCamera2D.IsoUpXZ * 2.2f;
                    float bob2D = Mathf.Sin(Time.time * 2.1f) * 0.12f;
                    transform.position = Vector3.Lerp(transform.position, new Vector3(stageLeftBg.x, bob2D, stageLeftBg.z), Time.unscaledDeltaTime * 3.5f);
                }
                else if (_activeAbductors.Count > 0 && _sanctuaryTimer <= 0f)
                {
                    // The rider leads toward the actual deeper passage; both actors respect cover.
                    Vector3 dragDir = -UnderworldCamera2D.IsoUpXZ;
                    foreach (var abductor in _activeAbductors)
                    {
                        var rider = abductor != null ? abductor.GetComponent<RoosterRiderAttack>() : null;
                        if (rider != null && rider.IsDragging && rider.DragTarget == this)
                        { dragDir = rider.RetreatDirection; break; }
                    }
                    MoveOnGround(transform.position + dragDir * (1.55f * dt));
                    AbductionDanger01 = Mathf.MoveTowards(AbductionDanger01, 1f, dt / 6.8f);

                    if (AbductionDanger01 >= 1f && pc != null && !pc.IsGameOver)
                    {
                        ShatterIntoAsh();
                        pc.TriggerEurydiceAbductedLoss();
                        return;
                    }
                }
                else
                {
                    AbductionDanger01 = Mathf.MoveTowards(AbductionDanger01, 0f, dt * 0.45f);

                    float targetLag = InTemptationZone ? followLagDistance + 1.4f : followLagDistance;
                    float curDist = GroundDistance(transform.position, orpheus.position);
                    bool nearby = curDist < followLagDistance * 1.6f;
                    if (curDist > 7.5f)
                    {
                        VoiceDirector.Instance?.PlaySeparationCallout();
                    }
                    bool gesture = IsTeachingPose || _reformTimer > 0 || _fearTimer > 0 || (pc != null && pc.IsVictory) ||
                        (nearby && (_joyTimer > 0 || _humPoseTimer > 0 || _shieldIntroTimer > 0));
                    if (gesture) RecordBreadcrumb();
                    else FollowBreadcrumbs(dt, targetLag);
                }

                UpdateSoulTetherVisual();
            }

            UpdateBlurVisuals();
            UpdateGeneratedSpriteAnimation(dt);
        }

        private void UpdateGeneratedSpriteAnimation(float dt)
        {
            if (_spriteAnimator == null || !_spriteAnimator.IsReady) return;
            if (dt <= 0) { _lastAnimationPosition = transform.position; return; }

            Vector3 delta = transform.position - _lastAnimationPosition;
            _lastAnimationPosition = transform.position;
            bool ascending = false;
            var map = PaintedChamberMap.Active;
            bool onStairs = map != null && map.definition.StairMotion(transform.position - map.transform.position, delta, out ascending);
            var pc = OrpheusPlayerController.Instance;
            bool grounded = new Vector2(delta.x, delta.z).magnitude / dt < .28f;
            _animationBrain.Step(new EurydiceAnimationInput
            {
                groundDelta = delta, deltaTime = dt,
                paused = UnderworldFrontEnd.BlocksGameplay || dt <= 0,
                vanished = IsVanished, reforming = _reformTimer > 0,
                threatened = IsBeingAbducted || (!HasSanctuaryShield && _fearTimer > 0) || (pc != null && pc.IsGameOver),
                shield = HasSanctuaryShield && _shieldIntroTimer > 0 && grounded, joy = (_joyTimer > 0 && grounded) || (pc != null && pc.IsVictory),
                teaching = IsTeachingPose, replying = _humPoseTimer > 0 && grounded, onStairs = onStairs, ascending = ascending
            });
            _spriteAnimator.SetShieldActive(HasSanctuaryShield, _joyTimer > 0, _humPoseTimer > 0);
            _spriteAnimator.Drive(_animationBrain);
        }

        private static float GroundDistance(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }
        private void RecordBreadcrumb()
        {
            if (orpheus != null && GroundDistance(orpheus.position, _lastRecordedPos) > .35f)
            { _breadcrumbs.Enqueue(orpheus.position); _lastRecordedPos = orpheus.position; }
        }

        private void FollowBreadcrumbs(float dt, float targetLag)
        {
            RecordBreadcrumb();
            var player = OrpheusPlayerController.Instance;
            float speed = Mathf.Max(smoothSpeed * 2.4f, player != null ? player.moveSpeed * 1.15f : 0);
            float budget = Mathf.Min(speed * dt, Mathf.Max(0, RemainingPathDistance() - targetLag));
            // Carry unused distance across waypoints while retaining every recorded corner.
            for (int guard = 0; guard < 64 && budget > .0001f; guard++)
            {
                float remaining = GroundDistance(transform.position, _desiredPos);
                if (remaining < .001f)
                {
                    if (_breadcrumbs.Count == 0) break;
                    _desiredPos = _breadcrumbs.Dequeue(); continue;
                }
                Vector3 before = transform.position;
                Vector3 direction = _desiredPos - before; direction.y = 0;
                float step = Mathf.Min(budget, remaining);
                Vector3 goal = before + direction.normalized * step;
                goal.y = Mathf.Sin(Time.time * 2.3f) * .035f;
                MoveOnGround(goal);
                float travelled = GroundDistance(before, transform.position);
                budget -= travelled;
                if (travelled < step * .5f) break;
            }
        }
        private float RemainingPathDistance()
        {
            float length = GroundDistance(transform.position, _desiredPos);
            Vector3 last = _desiredPos;
            foreach (Vector3 point in _breadcrumbs) { length += GroundDistance(last, point); last = point; }
            return length + (orpheus != null ? GroundDistance(last, orpheus.position) : 0);
        }

        private void MoveOnGround(Vector3 goal)
        {
            Vector3 pos = transform.position, delta = goal - pos; delta.y = 0;
            for (int i = 0; i < 2 && delta.sqrMagnitude > .000001f; i++)
            {
                if (!ChamberCollision.SweepObstacle(pos + Vector3.up * .5f, delta, .32f, out var hit)) { pos += delta; break; }
                float distance = Mathf.Max(0, hit.distance - .02f);
                Vector3 travel = delta.normalized * Mathf.Min(distance, delta.magnitude);
                pos += travel;
                Vector3 normal = hit.normal; normal.y = 0;
                delta = Vector3.ProjectOnPlane(delta - travel, normal.normalized);
            }
            var map = PaintedChamberMap.Active;
            if (map == null || map.IsWalkable(pos, .3f)) transform.position = new Vector3(pos.x, goal.y, pos.z);
        }

        private void UpdateSoulTetherVisual()
        {
            if (_tetherLine == null || orpheus == null) return;

            Vector3 a = orpheus.position + new Vector3(0f, 0.72f, 0f);
            Vector3 b = transform.position + new Vector3(0f, 0.78f, 0f);

            Color col = IsBeingAbducted
                ? Color.Lerp(new Color(2.4f, 0.25f, 0.35f, 0.65f), new Color(2.9f, 0.55f, 0.20f, 0.90f), Mathf.PingPong(Time.time * 6f, 1f))
                : (_sanctuaryTimer > 0f
                    ? new Color(2.2f, 1.85f, 0.55f, 0.48f)
                    : new Color(0.75f, 0.92f, 1.15f, 0.18f));

            _tetherLine.startColor = col;
            _tetherLine.endColor = new Color(col.r, col.g, col.b, col.a * 0.55f);

            int n = _tetherLine.positionCount;
            for (int i = 0; i < n; i++)
            {
                float u = i / (float)(n - 1);
                Vector3 pt = Vector3.Lerp(a, b, u);
                float sag = Mathf.Sin(u * Mathf.PI) * (IsBeingAbducted ? 0.04f : -0.22f);
                float jitter = IsBeingAbducted ? Mathf.Sin(Time.time * 38f + i * 2.1f) * 0.09f : 0f;
                pt.y += sag + jitter;
                _tetherLine.SetPosition(i, pt);
            }
        }

        private void UpdateBlurVisuals()
        {
            if (_blurLayers == null) return;

            float blurSpread = Mathf.Lerp(0.45f, 0.02f, _focus01);
            float t = Time.unscaledTime;

            for (int i = 0; i < _blurLayers.Length; i++)
            {
                float angle = t * (1.1f + i * 0.23f) + i * 1.57f;
                float r = blurSpread * (0.35f + 0.65f * (i / (float)_blurLayers.Length));
                _blurLayers[i].localPosition = new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle * 1.3f) * r * 0.8f, 0f);

                Color c = _blurRenderers[i].color;
                c.a = Mathf.Lerp(0.19f, 0.08f, _focus01);
                _blurRenderers[i].color = c;
            }

            float sharpAlpha = Mathf.SmoothStep(0f, 1f, (_focus01 - 0.25f) / 0.75f);
            if (_sharpCoreRobe != null)
            {
                Color rc = _sharpCoreRobe.color;
                rc.a = sharpAlpha;
                _sharpCoreRobe.color = rc;
            }
            if (_sharpCoreHead != null)
            {
                Color hc = _sharpCoreHead.color;
                hc.a = sharpAlpha;
                _sharpCoreHead.color = hc;
            }
            if (_generatedSpriteRenderer != null && _spriteAnimator != null && _spriteAnimator.IsReady)
            {
                Color gc = _generatedSpriteRenderer.color;
                gc.a = Mathf.Lerp(0.94f, 1f, _focus01);
                _generatedSpriteRenderer.color = gc;
            }
        }
    }

    /// <summary>
    /// Deceptive Underworld Mirage: mimics Blurred Eurydice's silhouette off to the side to bait [Hold Q]!
    /// Hit it with any Lyre note to expose & shatter the illusion without looking back.
    /// </summary>
    public class EurydiceMirageDecoy : MonoBehaviour, IOrphicTarget
    {
        private Transform _billboardRoot;
        private readonly List<Transform> _layers = new List<Transform>();

        public static void Spawn(Transform parent, Vector3 worldPos)
        {
            GameObject go = new GameObject("Eurydice_DeceptiveMirage");
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = worldPos;

            SphereCollider sc = go.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.85f;
            sc.center = new Vector3(0f, 0.85f, 0f);

            EurydiceMirageDecoy m = go.AddComponent<EurydiceMirageDecoy>();
            m.BuildMirage();
        }

        private void BuildMirage()
        {
            _billboardRoot = new GameObject("MirageBillboard").transform;
            _billboardRoot.SetParent(transform, false);
            _billboardRoot.localPosition = new Vector3(0f, 0.85f, 0f);

            for (int i = 0; i < 5; i++)
            {
                GameObject l = new GameObject("MirageBlur_" + i);
                l.transform.SetParent(_billboardRoot, false);
                l.transform.localScale = new Vector3(1.3f + i * 0.14f, 2.0f + i * 0.12f, 1f);
                RenderPipeline2DSetup.AttachSprite(l, RenderPipeline2DSetup.SoftGlowSprite, new Color(0.58f, 0.82f, 1.10f, 0.17f), 6);
                _layers.Add(l.transform);
            }
        }

        private void Update()
        {
            if (_billboardRoot != null) _billboardRoot.rotation = UnderworldCamera2D.CurrentBillboardRotation;
            float t = Time.time;
            for (int i = 0; i < _layers.Count; i++)
            {
                float a = t * (1.2f + i * 0.3f) + i;
                _layers[i].localPosition = new Vector3(Mathf.Cos(a) * 0.35f, Mathf.Sin(a * 1.4f) * 0.28f, 0f);
            }
        }

        public void OnOrphicHit(float damage, float intensity, bool isHardBoom, Vector3 hitOrigin)
        {
            // Shatter crimson — exposing the Underworld's lie!
            GroundShockwaveRing.Spawn(transform.position + new Vector3(0f, 0.06f, 0f), 2.8f, new Color(2.6f, 0.22f, 0.45f, 0.95f), 0.30f);
            OrpheusHud.Instance?.ShowSubtleCallout("Mirage Dispelled");
            Destroy(gameObject);
        }
    }

    public class RisingAshParticle : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private Vector3 _vel;
        private float _life;
        private float _maxLife;
        private float _startAlpha = 0.7f;

        public void Init(SpriteRenderer sr, float driftUpSpeed = 1.4f, float duration = 1.2f)
        {
            _sr = sr;
            _vel = new Vector3(Random.Range(-0.45f, 0.45f), driftUpSpeed * Random.Range(0.7f, 1.3f), Random.Range(-0.45f, 0.45f));
            _maxLife = Mathf.Max(0.08f, duration);
            _life = _maxLife;
            _startAlpha = sr != null ? sr.color.a : 0.7f;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            transform.position += _vel * dt;
            _life -= dt;
            if (_sr != null)
            {
                Color c = _sr.color;
                c.a = Mathf.Clamp01(_life / _maxLife) * _startAlpha;
                _sr.color = c;
            }
            if (_life <= 0f) Destroy(gameObject);
        }
    }
}
