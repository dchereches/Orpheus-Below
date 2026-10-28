using System;
using UnityEngine;

namespace OrpheusBelow
{
    [Serializable]
    public sealed class IntroCinematicTimeline
    {
        [Serializable] public sealed class Shot
        {
            public string resource, camera;
            public float start, fade;
        }
        public float duration;
        public Shot[] shots;
        public static bool ShouldPlay(bool inRun, bool replay) => replay || !inRun;
        public static bool ReachedEnd(float lastClock, float frameDelta, float duration) => lastClock+Mathf.Max(0,frameDelta)>=duration-.05f;
        public static IntroCinematicTimeline Load()
        {
            var asset = Resources.Load<TextAsset>("Cinematic/Intro/Timeline");
            if (asset == null) throw new InvalidOperationException("Intro timeline is missing.");
            var result = JsonUtility.FromJson<IntroCinematicTimeline>(asset.text);
            if (result == null) throw new InvalidOperationException("Intro timeline is invalid.");
            result.Validate();
            return result;
        }
        public void Validate()
        {
            if (shots == null || shots.Length != 42 || !float.IsFinite(duration) || duration <= 0)
                throw new InvalidOperationException("The intro requires42 shots and a valid duration.");
            for (int i=0;i<shots.Length;i++)
            {
                var s = shots[i];
                float end = i+1<shots.Length ? shots[i+1]?.start ?? -1 : duration;
                if (s == null || string.IsNullOrWhiteSpace(s.resource) || !s.resource.StartsWith("Cinematic/Intro/", StringComparison.Ordinal)
                    || !float.IsFinite(s.start) || !float.IsFinite(s.fade) || s.fade <= 0
                    || (i==0 && s.start!=0) || end<=s.start || s.fade>end-s.start)
                    throw new InvalidOperationException("Invalid intro shot " + i);
            }
        }
        public int FindShot(float time)
        {
            for (int i=shots.Length-1;i>=0;i--) if (time>=shots[i].start) return i;
            return 0;
        }
        public float Alpha(int index, float time) => Mathf.Clamp01((time-shots[index].start)/shots[index].fade);
        public float BlackAlpha(float time) => Mathf.Max(1-Mathf.Clamp01(time/.6f), Mathf.Clamp01((time-222.4f)/(duration-222.4f)));
    }
}
