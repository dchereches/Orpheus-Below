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
        private bool _used;

        public void SetupVisuals(SpriteRenderer portalSr, SpriteRenderer floorBeaconSr)
        {
            _portalSr = portalSr; _floorBeaconSr = floorBeaconSr;
            SetUnlocked(false);
        }

        public void SetUnlocked(bool unlocked)
        {
            IsUnlocked = unlocked;
            if (_portalSr != null)
                _portalSr.color = unlocked
                    ? (isFinalSunlightExit ? new Color(3.2f, 2.6f, .9f, .98f) : new Color(.45f, 2.6f, 1.55f, .95f))
                    : new Color(1.5f, .15f, .25f, .45f);
            if (_floorBeaconSr != null)
                _floorBeaconSr.color = unlocked ? new Color(.6f, 2.4f, 1.5f, .85f) : new Color(.9f, .15f, .22f, .30f);
        }

        private void OnTriggerEnter(Collider other)
            => TryEnter(other);
        private void OnTriggerStay(Collider other)
            => TryEnter(other);

        private void TryEnter(Collider other)
        {
            if (!IsUnlocked || _used) return;
            var player = other.GetComponent<OrpheusPlayerController>();
            if (player == null || player.IsGameOver) return;
            if (UnderworldFrontEnd.BlocksGameplay || OrpheusHud.IsTutorialModalActive) return;
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
