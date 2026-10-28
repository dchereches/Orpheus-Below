using System;
using System.Collections.Generic;

namespace OrpheusBelow
{
    /// <summary>A sequence is one atomic request; later speech never replaces it.</summary>
    public sealed class VoicePlaybackQueue
    {
        public sealed class Request
        {
            public readonly VoiceLineId[] lines;
            public readonly Action completed;
            public Request(VoiceLineId[] lines, Action completed)
            { this.lines=(VoiceLineId[])lines.Clone(); this.completed=completed; }
        }
        private readonly Queue<Request> _pending=new Queue<Request>();
        private Request _active;
        public bool IsBlocking => _active!=null || _pending.Count>0;
        public void Enqueue(VoiceLineId[] lines, Action completed)
        {
            if (lines==null || lines.Length==0) { completed?.Invoke(); return; }
            _pending.Enqueue(new Request(lines,completed));
        }
        public bool TryBegin(out Request request)
        {
            request=null;
            if (_active!=null || _pending.Count==0) return false;
            request=_active=_pending.Dequeue(); return true;
        }
        public void Complete()
        {
            var done=_active; _active=null; done?.completed?.Invoke();
        }
        public void Clear() { _active=null; _pending.Clear(); }
    }
}
