using UnityEngine;

namespace OrpheusBelow
{
    public enum JourneyDoorRole { Legacy, Deeper, Surfaceward, Reunion }

    public sealed class ChamberExitDoor : MonoBehaviour
    {
        public int targetChamberIndex;
        public bool isFinalSunlightExit;
        public JourneyDoorRole journeyRole;
        public bool IsUnlocked { get; private set; }
        [SerializeField] private SpriteRenderer _portalSr;
        [SerializeField] private SpriteRenderer _floorBeaconSr;
        [SerializeField] private PortalLocationParticles _exitParticles;
        [SerializeField] private BoxCollider _lockedThreshold;
        private bool _used;

        private void Awake()
        {
            EnsureParticles();
            EnsureSolidThreshold();
        }

        public void SetupVisuals(SpriteRenderer portalSr, SpriteRenderer floorBeaconSr)
        {
            _portalSr = portalSr;
            _floorBeaconSr = floorBeaconSr;
            EnsureParticles();
            SetUnlocked(false);
        }

        public void EnsureParticles()
        {
            if (_exitParticles != null) return;
            _exitParticles = GetComponent<PortalLocationParticles>();
            if (_exitParticles == null) _exitParticles = GetComponentInChildren<PortalLocationParticles>();
            if (_exitParticles == null && _portalSr != null) _exitParticles = _portalSr.GetComponent<PortalLocationParticles>();
            if (_exitParticles == null)
            {
                var pGo = new GameObject("ExitDoorParticles");
                pGo.transform.SetParent(transform, false);
                pGo.transform.localPosition = new Vector3(0f, 0.08f, 0f);
                _exitParticles = pGo.AddComponent<PortalLocationParticles>();
            }
        }

        public void SetUnlocked(bool unlocked)
        {
            IsUnlocked = unlocked;
            EnsureSolidThreshold();
            if (_lockedThreshold != null) _lockedThreshold.enabled = !unlocked;
            if (_portalSr != null)
                _portalSr.color = unlocked
                    ? (isFinalSunlightExit ? new Color(3.2f, 2.6f, .9f, .98f) : new Color(.45f, 2.6f, 1.55f, .95f))
                    : new Color(1.5f, .15f, .25f, .45f);
            if (_floorBeaconSr != null)
                _floorBeaconSr.color = unlocked ? new Color(.6f, 2.4f, 1.5f, .85f) : new Color(.9f, .15f, .22f, .30f);

            EnsureParticles();
            if (_exitParticles != null)
            {
                Color accent = isFinalSunlightExit
                    ? new Color(1.0f, 0.85f, 0.42f)
                    : new Color(0.35f, 0.95f, 0.72f);
                _exitParticles.SetAccent(accent);
                _exitParticles.SetUnlocked(unlocked);
            }
        }

        private void EnsureSolidThreshold()
        {
            if (_lockedThreshold != null || GetComponentInParent<PaintedChamberMap>() == null) return;
            var threshold = new GameObject("LockedPassage_SolidThreshold");
            threshold.transform.SetParent(transform, false);
            _lockedThreshold = threshold.AddComponent<BoxCollider>();
            _lockedThreshold.center = Vector3.up * 1.25f;
            _lockedThreshold.size = new Vector3(5.5f, 3f, .6f);
            threshold.AddComponent<IsoObstacle>().kind = IsoObstacleKind.SolidWall;
            _lockedThreshold.enabled = !IsUnlocked;
        }

        private void Update()
        {
            if (!IsUnlocked || _used) return;
            var player = OrpheusPlayerController.Instance;
            if (player == null || player.IsGameOver) return;
            if (UnderworldFrontEnd.BlocksGameplay || OrpheusHud.IsTutorialModalActive || VoiceDirector.BlocksGameplay) return;

            // Proximity failsafe: walking anywhere within 3.5m of the gate threshold activates it seamlessly
            float dx = transform.position.x - player.transform.position.x;
            float dz = transform.position.z - player.transform.position.z;
            float distSq = dx * dx + dz * dz;
            if (distSq <= 12.25f && Mathf.Abs(transform.position.y - player.transform.position.y) < 3.5f)
            {
                TryEnter(player.GetComponent<Collider>());
            }
        }

        private void OnTriggerEnter(Collider other)
            => TryEnter(other);
        private void OnTriggerStay(Collider other)
            => TryEnter(other);

        private void TryEnter(Collider other)
        {
            if (!IsUnlocked || _used) return;
            var player = other != null ? other.GetComponent<OrpheusPlayerController>() : OrpheusPlayerController.Instance;
            if (player == null || player.IsGameOver) return;
            if (UnderworldFrontEnd.BlocksGameplay || OrpheusHud.IsTutorialModalActive || VoiceDirector.BlocksGameplay) return;
            if (journeyRole != JourneyDoorRole.Legacy)
            {
                _used = HadesChamberDirector.Instance != null && HadesChamberDirector.Instance.TryJourneyPassage(this);
                return;
            }
            _used = true;
            if (isFinalSunlightExit) player.TriggerRunVictory();
            else HadesChamberDirector.Instance?.TransitionToChamber(targetChamberIndex);
        }
    }
}
