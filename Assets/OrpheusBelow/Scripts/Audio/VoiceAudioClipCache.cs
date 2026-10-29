using System;
using System.Collections.Generic;
using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>Owns decoded speech copies, leaving imported media and the master listener gain intact.</summary>
    public sealed class VoiceAudioClipCache : IDisposable
    {
        private readonly Dictionary<int, AudioClip> _prepared = new Dictionary<int, AudioClip>();

        public AudioClip Prepare(AudioClip original)
        {
            if (original == null) return null;
            int id = original.GetInstanceID();
            if (_prepared.TryGetValue(id, out var cached) && cached != null) return cached;
            if (original.loadState != AudioDataLoadState.Loaded) original.LoadAudioData();
            var samples = new float[original.samples * original.channels];
            if (!original.GetData(samples, 0)) return original;
            VoiceLoudness.Normalize(samples);
            var clip = AudioClip.Create(original.name, original.samples, original.channels, original.frequency, false);
            clip.hideFlags = HideFlags.DontSave;
            clip.SetData(samples, 0);
            _prepared[id] = clip;
            return clip;
        }

        public void Dispose()
        {
            foreach (var clip in _prepared.Values)
                if (clip != null)
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(clip);
                    else UnityEngine.Object.DestroyImmediate(clip);
                }
            _prepared.Clear();
        }
    }
}
