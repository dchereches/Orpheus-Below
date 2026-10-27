using UnityEngine;

namespace OrpheusBelow
{
    public abstract class PaintedAttackPattern : MonoBehaviour
    {
        protected PaintedEnemyActor Actor;
        public virtual void Initialize(PaintedEnemyActor actor) { Actor = actor; }
        public abstract void Tick(float dt);
        public virtual void Cancel() { }
        protected virtual void OnDisable() { Cancel(); }
    }
}
