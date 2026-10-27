using UnityEngine;
using UnityEngine.SceneManagement;

namespace OrpheusBelow
{
    public sealed class PaintedChamberScene : MonoBehaviour
    {
        public int chamberIndex;
        public PaintedChamberMap map;
        private static bool _travelling;
        private static float _health;
        private static int _cadenceTier;
        private static bool _wardenHint;
        public static string ScenePath(int index) => "Assets/OrpheusBelow/Scenes/PaintedChamber_" + index.ToString("D2") + ".unity";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetTravel() { _travelling = false; }

        private void Start()
        {
            // Scene-loaded callbacks run only for the initial application scene in the old bootstrap.
            // Subsequent authored scenes start their own systems after all scene objects are enabled.
            if (HadesChamberDirector.Instance == null) OrpheusPrototypeBuilder.BuildPrototypeWorld();
        }

        public static bool Travel(int index, bool wardenHint)
        {
            if (index < 0 || index >= 5 || !Application.CanStreamedLevelBeLoaded(ScenePath(index))) return false;
            _travelling = true;
            _health = OrpheusPlayerController.Instance != null ? OrpheusPlayerController.Instance.CurrentHealth : 100f;
            _cadenceTier = OrpheusLyreCombat.Instance != null ? OrpheusLyreCombat.Instance.UnlockedCadenceTier : 1;
            _wardenHint = wardenHint;
            UnderworldFrontEnd.SkipTitleOnce = true;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            SceneManager.LoadSceneAsync(ScenePath(index), LoadSceneMode.Single);
            return true;
        }

        public static void RestoreTravel(HadesChamberDirector director)
        {
            if (!_travelling) return;
            _travelling = false;
            OrpheusPlayerController.Instance?.RestoreTravelHealth(_health);
            if (OrpheusLyreCombat.Instance != null) OrpheusLyreCombat.Instance.UnlockedCadenceTier = _cadenceTier;
            director.RestoreWardenHint(_wardenHint);
        }

        public static bool Restart(bool skipTitle)
        {
            if (Resources.Load<PaintedChamberDefinition>("ChamberLayouts/Chamber_00") == null ||
                !Application.CanStreamedLevelBeLoaded(ScenePath(0))) return false;
            _travelling = false;
            UnderworldFrontEnd.SkipTitleOnce = skipTitle;
            Time.timeScale = 1f; AudioListener.pause = false;
            SceneManager.LoadScene(ScenePath(0));
            return true;
        }
    }
}
