using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.InputSystem;

namespace OrpheusBelow
{
    public enum VoiceLineId
    {
        None = 0,

        // Narrator (replaces Artemis)
        Narrator_ClearChambers,
        Narrator_DefendYourself,
        Narrator_RepellingMove,
        Narrator_CombosFlee,
        Narrator_PressF,
        Narrator_PluckCadences,
        Narrator_FoundHer,

        // Eurydice
        Eurydice_Orpheus,
        Eurydice_CannotFeel,
        Eurydice_IamHere,
        Eurydice_HearTheNote,
        Eurydice_RightHere,
        Eurydice_AreYouStillThere,
        Eurydice_LookAtMe,

        // Lord Hades
        Hades_ABreath,
        Hades_YourStrings,
        Hades_YouPacified,
        Hades_YouDare,
        Hades_YouSingOf,
        Hades_Silence,
        Hades_TakeHer,
        Hades_StepIntoTheDark,
        Hades_HearHow,
        Hades_APoets,
        Hades_TheLight,
        Hades_AMortals
    }

    public sealed class VoiceLineData
    {
        public VoiceLineId id;
        public string speaker;
        public Color speakerColor;
        public string resourcePath;
        public string subtitleText;
        public float duration;
    }

    /// <summary>
    /// Central manager for voice lines and narrative audio.
    /// Serializes full voice lines/sequences, locks gameplay, and renders synchronized subtitles.
    /// </summary>
    public sealed class VoiceDirector : MonoBehaviour
    {
        public static VoiceDirector Instance { get; private set; }
        public const float VoiceGain = 1f;
        public static bool HasPendingSpeech => Instance != null && Instance._queue.IsBlocking;
        public static bool BlocksGameplay => HasPendingSpeech && !Instance._interactiveInstruction;
        public static bool IsVoicePlaying => Instance != null && (Instance.CurrentLine != VoiceLineId.None || HasPendingSpeech || (Instance._voiceSource != null && Instance._voiceSource.isPlaying));

        private static readonly Color NarratorColor = new Color(0.95f, 0.82f, 0.44f, 1f); // Warm amber
        private static readonly Color HadesColor = new Color(0.96f, 0.32f, 0.32f, 1f);    // Regal crimson
        private static readonly Color EurydiceColor = new Color(1f, 0.72f, 0.76f, 1f);    // Soft rose gold

        private AudioSource _voiceSource;
        private readonly Dictionary<VoiceLineId, VoiceLineData> _lineRegistry = new Dictionary<VoiceLineId, VoiceLineData>();
        private readonly Dictionary<VoiceLineId, AudioClip> _clipCache = new Dictionary<VoiceLineId, AudioClip>();

        private Coroutine _activePlaybackRoutine;
        private readonly VoicePlaybackQueue _queue = new VoicePlaybackQueue();
        private bool _workerRunning, _ownsWorldPause;
        private bool _interactiveInstruction;
        private bool _skipRequested;
        public VoiceLineId CurrentLine { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticSession()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _voiceSource = gameObject.AddComponent<AudioSource>();
            _voiceSource.playOnAwake = false;
            _voiceSource.spatialBlend = 0f; // 2D voice
            _voiceSource.priority = 40;     // High priority: never culled by combat foley
            _voiceSource.ignoreListenerPause = true;

            BuildRegistry();
        }

        private void Start()
        {
            PrewarmAudioClips();
        }

        private void BuildRegistry()
        {
            // --- NARRATOR (replaces Artemis) ---
            Register(VoiceLineId.Narrator_ClearChambers, "Narrator", NarratorColor, "Voices/Narrator/clear_chambers",
                "Clear chambers to move further into the underworld! Move with W, A, S, D.", 6.80f);

            Register(VoiceLineId.Narrator_DefendYourself, "Narrator", NarratorColor, "Voices/Narrator/todefendyourself",
                "To defend yourself with a Quaver, press J or Left Click (or make a gentle sound if using the mic) to strike shades and hold the tempo.", 9.76f);

            Register(VoiceLineId.Narrator_RepellingMove, "Narrator", NarratorColor, "Voices/Narrator/tocauseabig",
                "To cause a big repelling move, press K or Right Click (or make a loud sound if using the mic) to unleash a Boom Fermata—blasting enemies away and shattering armor!", 13.20f);

            Register(VoiceLineId.Narrator_CombosFlee, "Narrator", NarratorColor, "Voices/Narrator/comboscancause",
                "Combos can force enemies to flee! Press F to draw your Aulos and see the combo needed for the Cataclysmic Requiem.", 8.40f);

            Register(VoiceLineId.Narrator_PressF, "Narrator", NarratorColor, "Voices/Narrator/press_f",
                "Press F to holster your Aulos when ready. Space or Shift grants two invulnerable dashes to evade strikes and reposition safely.", 9.52f);

            Register(VoiceLineId.Narrator_PluckCadences, "Narrator", NarratorColor, "Voices/Narrator/pluckcadances",
                "Pluck cadences to restore breath and crack grief-armor. Clear the chamber to unseal the exit!", 8.24f);

            Register(VoiceLineId.Narrator_FoundHer, "Narrator", NarratorColor, "Voices/Narrator/youhavefoundher",
                "You have found her, Orpheus! Lead her back through the chambers to the upper light. Trust the voice behind you, do not look back! Wait at each passage until she is near.", 11.76f);

            // --- EURYDICE ---
            Register(VoiceLineId.Eurydice_Orpheus, "Eurydice", EurydiceColor, "Voices/Eurydice/orpheus",
                "Orpheus...? Is that your lyre I hear through the frost? It is truly you... You came into the dark for me.", 8.24f);

            Register(VoiceLineId.Eurydice_CannotFeel, "Eurydice", EurydiceColor, "Voices/Eurydice/icannotfeel",
                "I cannot feel my hands, my love, but when you play, the warmth returns. Walk ahead, I will follow your music.", 9.20f);

            Register(VoiceLineId.Eurydice_IamHere, "Eurydice", EurydiceColor, "Voices/Eurydice/IamHere",
                "I am here, Orpheus. Right behind you.", 3.76f);

            Register(VoiceLineId.Eurydice_HearTheNote, "Eurydice", EurydiceColor, "Voices/Eurydice/Ihearthenote",
                "I hear the note... I am keeping pace.", 5.44f);

            Register(VoiceLineId.Eurydice_RightHere, "Eurydice", EurydiceColor, "Voices/Eurydice/righthere",
                "Right here, my love. Do not turn.", 3.28f);

            Register(VoiceLineId.Eurydice_AreYouStillThere, "Eurydice", EurydiceColor, "Voices/Eurydice/areyoustillthere",
                "Are you still there? Play for me... let me know you haven't abandoned me.", 5.36f);

            Register(VoiceLineId.Eurydice_LookAtMe, "Eurydice", EurydiceColor, "Voices/Eurydice/lookatme",
                "Orpheus... look at me... just one look to know you are real...", 4.88f);

            // --- LORD HADES ---
            Register(VoiceLineId.Hades_ABreath, "Hades", HadesColor, "Voices/Hades/abreath",
                "A breath of living air stumbles down my halls. Turn back, son of Apollo. The earth does not yield what it has swallowed.", 11.92f);

            Register(VoiceLineId.Hades_YourStrings, "Hades", HadesColor, "Voices/Hades/YourStrings",
                "Your strings will fray before the weight of my kingdom softens. Mourn above like all the rest.", 8.64f);

            Register(VoiceLineId.Hades_YouPacified, "Hades", HadesColor, "Voices/Hades/YouPacified",
                "You pacified my beast with a lullaby, insolent child? Step onto my carpet then. Let us see if your little song can move stone.", 14.00f);

            Register(VoiceLineId.Hades_YouDare, "Hades", HadesColor, "Voices/Hades/YouDare",
                "You dare stand before the throne of the Unseen? No living feet tread this carpet, Orpheus. Speak. Why should the King of Shadows release what fate has sealed?", 15.28f);

            Register(VoiceLineId.Hades_YouSingOf, "Hades", HadesColor, "Voices/Hades/YouSingOf",
                "You sing of springtime upon cold slate, minstrel. Here, every blossom has already rotted.", 9.04f);

            Register(VoiceLineId.Hades_Silence, "Hades", HadesColor, "Voices/Hades/Silence",
                "Silence... Even the Furies weep into the snakes. It has been an age since a melody pierced my heart. Very well... take her.", 15.68f);

            Register(VoiceLineId.Hades_TakeHer, "Hades", HadesColor, "Voices/Hades/TakeHer",
                "Take her, minstrel. But hear the law of Tartarus carved into the roots of the world: Walk ahead. She shall follow in your shadow. If you turn your head to look upon her before the sun strikes your brow—she is mine forever.", 23.36f);

            Register(VoiceLineId.Hades_StepIntoTheDark, "Hades", HadesColor, "Voices/Hades/StepIntoTheDark",
                "Step into the dark. Do not falter, do not doubt. And above all... do not look back.", 11.68f);

            Register(VoiceLineId.Hades_HearHow, "Hades", HadesColor, "Voices/Hades/HearHow",
                "Hear how quietly she walks behind you, Orpheus. Is she truly there? Or do you lead an empty shadow?", 9.84f);

            Register(VoiceLineId.Hades_APoets, "Hades", HadesColor, "Voices/Hades/APoets",
                "A poet's greatest curse is curiosity. How easy it would be to glance over your shoulder... just once.", 11.44f);

            Register(VoiceLineId.Hades_TheLight, "Hades", HadesColor, "Voices/Hades/TheLight",
                "The light is so close, musician. Can you trust what you cannot see?", 7.04f);

            Register(VoiceLineId.Hades_AMortals, "Hades", HadesColor, "Voices/Hades/AMortals",
                "A mortal's faith is brittle as dried reed. She returns to the dark, Orpheus. And you shall weep upon the earth alone.", 12.88f);
        }

        private void Register(VoiceLineId id, string speaker, Color color, string resPath, string subtitle, float duration)
        {
            _lineRegistry[id] = new VoiceLineData
            {
                id = id,
                speaker = speaker,
                speakerColor = color,
                resourcePath = resPath,
                subtitleText = subtitle,
                duration = duration
            };
        }

        private void PrewarmAudioClips()
        {
            foreach (var kvp in _lineRegistry)
            {
                GetOrLoadClip(kvp.Value);
            }
        }

        private static string FormatSubtitleText(string rawText)
        {
            if (string.IsNullOrEmpty(rawText)) return rawText;
            bool micActive = SensorPulseInput.GlobalSourceMode == SensorSourceMode.KeyboardPlusPCMicrophone;
            if (!micActive && rawText.Contains("mic"))
            {
                rawText = System.Text.RegularExpressions.Regex.Replace(rawText, @"\s*\([^)]*mic[^)]*\)", "");
            }
            return rawText;
        }

        public AudioClip GetOrLoadClip(VoiceLineData data)
        {
            if (data == null) return null;
            if (_clipCache.TryGetValue(data.id, out AudioClip cached) && cached != null)
                return cached;

            AudioClip clip = Resources.Load<AudioClip>(data.resourcePath);
            if (clip != null)
            {
                _clipCache[data.id] = clip;
                return clip;
            }

            // Direct file system fallback for Unity runtime
            string rel = data.resourcePath.StartsWith("Voices/") ? data.resourcePath.Substring("Voices/".Length) : data.resourcePath;
            string[] possiblePaths = new string[]
            {
                Path.Combine(Application.dataPath, "OrpheusBelow/Resources", data.resourcePath + ".mp3"),
                Path.Combine(Application.dataPath, "../voices", rel + ".mp3"),
                Path.Combine(Application.dataPath, "../voices", data.resourcePath + ".mp3"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Orpheus Below/voices", rel + ".mp3")
            };

            foreach (string p in possiblePaths)
            {
                if (File.Exists(p))
                {
                    StartCoroutine(LoadClipViaWeb(p, data.id));
                    break;
                }
            }

            return null;
        }

        private IEnumerator LoadClipViaWeb(string fullPath, VoiceLineId id)
        {
            string url = "file:///" + fullPath.Replace('\\', '/');
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
            {
                yield return www.SendWebRequest();
                if (www.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    if (clip != null)
                    {
                        clip.name = id.ToString();
                        _clipCache[id] = clip;
                    }
                }
            }
        }

        /// <summary>
        /// Queues a complete voice line; gameplay remains locked until queued speech finishes.
        /// Displays subtitles at the bottom of the screen.
        /// </summary>
        public void Play(VoiceLineId id, Action onComplete = null)
        {
            if (id == VoiceLineId.None || VoiceLinePolicy.IsRecognitionChatter(id) || !_lineRegistry.TryGetValue(id, out VoiceLineData data))
            {
                onComplete?.Invoke();
                return;
            }

            Enqueue(new[] { id }, onComplete);
        }

        private IEnumerator PlayRoutine(VoiceLineData data)
        {
            // Startup dialogue waits behind the title and narrated introduction.
            // Keep listener-pause bypass for voiced tutorial modals during a run.
            while (UnderworldFrontEnd.BlocksMenusOrTransitions) yield return null;
            CurrentLine = data.id;
            _skipRequested = false;
            _interactiveInstruction = VoiceLinePolicy.IsInteractiveInstruction(data.id);
            if (_interactiveInstruction) ReleaseWorldPause();
            else AcquireWorldPause();
            AudioClip clip = GetOrLoadClip(data);
            float duration = clip != null ? clip.length : data.duration;

            if (clip != null && _voiceSource != null)
            {
                // Master gain is already applied by AudioListener; don't apply it twice.
                _voiceSource.volume = VoiceGain;
                _voiceSource.clip = clip;
                _voiceSource.Play();
            }

            // Display literary subtitle at screen bottom
            string formattedText = FormatSubtitleText(data.subtitleText);
            OrpheusHud.Instance?.ShowSubtitle(data.speaker, data.speakerColor, formattedText, duration + 0.45f, data.id);

            // Real audio must finish; world simulation is frozen while this request owns the lock.
            float timer = duration;
            bool pausedForMenu = false;
            while (!_skipRequested && (timer > 0f || (_voiceSource != null && _voiceSource.isPlaying)))
            {
                if (UnderworldFrontEnd.BlocksMenusOrTransitions)
                {
                    if (!pausedForMenu && _voiceSource != null) _voiceSource.Pause();
                    pausedForMenu = true;
                    yield return null;
                    continue;
                }
                if (pausedForMenu && _voiceSource != null) _voiceSource.UnPause();
                pausedForMenu = false;
                timer -= Time.unscaledDeltaTime;
                yield return null;
            }
            if (_voiceSource != null) _voiceSource.Stop();
            OrpheusHud.Instance?.HideSubtitle();
            CurrentLine = VoiceLineId.None;
        }

        /// <summary>
        /// Plays a sequential chain of voice lines with natural pauses in real time.
        /// </summary>
        public void PlaySequence(VoiceLineId[] ids, Action onComplete = null)
        {
            if (ids == null || ids.Length == 0)
            {
                onComplete?.Invoke();
                return;
            }

            Enqueue(Array.FindAll(ids, id => id != VoiceLineId.None && !VoiceLinePolicy.IsRecognitionChatter(id) && _lineRegistry.ContainsKey(id)), onComplete);
        }

        private void Enqueue(VoiceLineId[] ids, Action onComplete)
        {
            _queue.Enqueue(ids, onComplete);
            if (_workerRunning || !_queue.IsBlocking) return;
            _workerRunning = true;
            _activePlaybackRoutine = StartCoroutine(DrainQueue());
        }

        private IEnumerator DrainQueue()
        {
            try
            {
                while (_queue.TryBegin(out var request))
                {
                    while (UnderworldFrontEnd.BlocksMenusOrTransitions) yield return null;
                    for (int i=0; i<request.lines.Length; i++)
                    {
                        yield return PlayRoutine(_lineRegistry[request.lines[i]]);
                        if (i+1<request.lines.Length)
                            for (float gap=0; gap<.4f;)
                            { if (!UnderworldFrontEnd.BlocksMenusOrTransitions) gap+=Time.unscaledDeltaTime; yield return null; }
                    }
                    try { _queue.Complete(); }
                    catch (Exception error) { Debug.LogException(error); }
                }
            }
            finally
            {
                _queue.Clear(); _workerRunning = false; _activePlaybackRoutine = null;
                CurrentLine = VoiceLineId.None; _interactiveInstruction = false;
                ReleaseWorldPause();
            }
        }

        public bool SkipCurrentLine()
        {
            if (CurrentLine == VoiceLineId.None || UnderworldFrontEnd.BlocksMenusOrTransitions) return false;
            _skipRequested = true;
            if (_voiceSource != null) _voiceSource.Stop();
            OrpheusHud.Instance?.HideSubtitle();
            return true;
        }

        public void CompleteTutorialInstruction(AsphodelTutorialStep completedStep)
        {
            if (CurrentLine == VoiceLinePolicy.InstructionFor(completedStep)) SkipCurrentLine();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
                SkipCurrentLine();
            if (_ownsWorldPause) Time.timeScale = 0;
        }

        private void AcquireWorldPause()
        {
            if (_ownsWorldPause) return;
            _ownsWorldPause = true;
            Time.timeScale = 0;
        }
        private void ReleaseWorldPause()
        {
            if (!_ownsWorldPause) return;
            _ownsWorldPause = false;
            if (!UnderworldFrontEnd.BlocksMenusOrTransitions && !OrpheusHud.IsTutorialModalActive)
                Time.timeScale = GameplayClock.ResumeScale;
        }
        private void OnDisable()
        {
            if (_activePlaybackRoutine != null) StopCoroutine(_activePlaybackRoutine);
            _activePlaybackRoutine = null; _workerRunning = false; _queue.Clear();
            if (_voiceSource != null) _voiceSource.Stop();
            ReleaseWorldPause();
        }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>
        /// Cycles through Eurydice's acoustic ping replies on lyre plucks.
        /// </summary>
        public void PlayNextPingReply() { }

        public void PlayLookBackTemptation() { }

        public void PlaySeparationCallout() { }

        /// <summary>
        /// Plays the full Chamber 7 divine reunion sequence in real-time.
        /// </summary>
        public void PlayReunionSequence(Action onComplete = null)
        {
            VoiceLineId[] sequence = new VoiceLineId[]
            {
                VoiceLineId.Hades_TakeHer,
                VoiceLineId.Eurydice_Orpheus,
                VoiceLineId.Eurydice_CannotFeel,
                VoiceLineId.Narrator_FoundHer,
                VoiceLineId.Hades_StepIntoTheDark
            };

            PlaySequence(sequence, onComplete);
        }

        /// <summary>
        /// Maps Artemis/Narrator tutorial text to the corresponding voiced clip.
        /// </summary>
        public void PlayNarratorTutorial(string text, Action onComplete = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                onComplete?.Invoke();
                return;
            }

            VoiceLineId id = VoiceLineId.None;
            if (text.Contains("Clear chambers") || text.Contains("move further") || text.Contains("W, A, S, D") || text.Contains("Artemis"))
                id = VoiceLineId.Narrator_ClearChambers;
            else if (text.Contains("defend yourself") || text.Contains("Quaver") || text.Contains("J"))
                id = VoiceLineId.Narrator_DefendYourself;
            else if (text.Contains("repelling") || text.Contains("Boom") || text.Contains("K"))
                id = VoiceLineId.Narrator_RepellingMove;
            else if (text.Contains("force enemies to flee") || text.Contains("combos") || text.Contains("Aulos") || text.Contains("Requiem"))
                id = VoiceLineId.Narrator_CombosFlee;
            else if (text.Contains("holster") || text.Contains("dash") || text.Contains("Space"))
                id = VoiceLineId.Narrator_PressF;
            else if (text.Contains("cadences") || text.Contains("restore breath") || text.Contains("unseal"))
                id = VoiceLineId.Narrator_PluckCadences;
            else if (text.Contains("found her") || text.Contains("sanctuary"))
                id = VoiceLineId.Narrator_FoundHer;

            if (id != VoiceLineId.None)
            {
                Play(id, onComplete);
            }
            else
            {
                // Fallback: show subtitle for unvoiced tutorial text without pausing
                OrpheusHud.Instance?.ShowSubtitle("NARRATOR", NarratorColor, text, 4.5f, id);
                onComplete?.Invoke();
            }
        }

        /// <summary>
        /// Maps Hades dialogue text to the corresponding voiced clip.
        /// </summary>
        public void PlayHadesTutorial(string text, Action onComplete = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                onComplete?.Invoke();
                return;
            }

            VoiceLineId id = VoiceLineId.None;
            if (text.Contains("breath of living air") || text.Contains("swallowed"))
                id = VoiceLineId.Hades_ABreath;
            else if (text.Contains("strings will fray") || text.Contains("Mourn above"))
                id = VoiceLineId.Hades_YourStrings;
            else if (text.Contains("pacified my beast") || text.Contains("lullaby"))
                id = VoiceLineId.Hades_YouPacified;
            else if (text.Contains("dare stand") || text.Contains("Unseen"))
                id = VoiceLineId.Hades_YouDare;
            else if (text.Contains("springtime") || text.Contains("rotted"))
                id = VoiceLineId.Hades_YouSingOf;
            else if (text.Contains("Silence") || text.Contains("Furies weep"))
                id = VoiceLineId.Hades_Silence;
            else if (text.Contains("Take her") || text.Contains("law of Tartarus"))
                id = VoiceLineId.Hades_TakeHer;
            else if (text.Contains("Step into the dark") || text.Contains("do not look back"))
                id = VoiceLineId.Hades_StepIntoTheDark;
            else if (text.Contains("quietly she walks") || text.Contains("empty shadow"))
                id = VoiceLineId.Hades_HearHow;
            else if (text.Contains("curse is curiosity") || text.Contains("just once"))
                id = VoiceLineId.Hades_APoets;
            else if (text.Contains("light is so close") || text.Contains("cannot see"))
                id = VoiceLineId.Hades_TheLight;
            else if (text.Contains("mortal's faith") || text.Contains("brittle as dried reed"))
                id = VoiceLineId.Hades_AMortals;

            if (id != VoiceLineId.None)
            {
                Play(id, onComplete);
            }
            else
            {
                OrpheusHud.Instance?.ShowSubtitle("HADES", HadesColor, text, 5.0f, id);
                onComplete?.Invoke();
            }
        }

        /// <summary>
        /// Maps Eurydice dialogue text to the corresponding voiced clip.
        /// </summary>
        public void PlayEurydiceTutorial(string text, Action onComplete = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                onComplete?.Invoke();
                return;
            }

            VoiceLineId id = VoiceLineId.None;
            if (text.Contains("frost") || text.Contains("truly you") || text.Contains("lyre I hear"))
                id = VoiceLineId.Eurydice_Orpheus;
            else if (text.Contains("cannot feel my hands") || text.Contains("warmth returns"))
                id = VoiceLineId.Eurydice_CannotFeel;
            else if (text.Contains("I am here") || text.Contains("Right behind you"))
                id = VoiceLineId.Eurydice_IamHere;
            else if (text.Contains("hear the note") || text.Contains("keeping pace"))
                id = VoiceLineId.Eurydice_HearTheNote;
            else if (text.Contains("Right here") || text.Contains("Do not turn"))
                id = VoiceLineId.Eurydice_RightHere;
            else if (text.Contains("still there") || text.Contains("abandoned"))
                id = VoiceLineId.Eurydice_AreYouStillThere;
            else if (text.Contains("look at me") || text.Contains("know you are real"))
                id = VoiceLineId.Eurydice_LookAtMe;

            if (id != VoiceLineId.None)
            {
                Play(id, onComplete);
            }
            else
            {
                OrpheusHud.Instance?.ShowSubtitle("EURYDICE", EurydiceColor, text, 4.5f, id);
                onComplete?.Invoke();
            }
        }
    }
}
