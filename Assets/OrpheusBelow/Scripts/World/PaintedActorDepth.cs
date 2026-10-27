using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace OrpheusBelow
{
    [DefaultExecutionOrder(200)]
    [RequireComponent(typeof(SortingGroup))]
    public sealed class PaintedActorDepth : MonoBehaviour
    {
        private SortingGroup _group;
        private readonly Dictionary<SpriteRenderer, Material> _originals = new Dictionary<SpriteRenderer, Material>();
        private readonly HashSet<SpriteRenderer> _excluded = new HashSet<SpriteRenderer>();
        private static Material _depthMaterial;
        public void Exclude(SpriteRenderer sprite) { if (sprite != null) _excluded.Add(sprite); }
        private void Awake()
        {
            _group = GetComponent<SortingGroup>();
            if (_group == null) _group = gameObject.AddComponent<SortingGroup>();
        }

        private void LateUpdate()
        {
            var map = PaintedChamberMap.Active;
            bool exploration = UnderworldCamera2D.Instance == null || UnderworldCamera2D.Instance.SideViewBlend01 < .1f;
            _group.sortingOrder = map != null && exploration ? PaintedChamberMap.DepthOrder(map.ToImage(transform.position).y) : 0;
            if (map != null && exploration)
            {
                if (_depthMaterial == null) _depthMaterial = new Material(Shader.Find("OrpheusBelow/PaintedCover")) { name = "PaintedActorDepth" };
                foreach (var sprite in GetComponentsInChildren<SpriteRenderer>())
                {
                    // Ground decals and translucent painted effects must not cut holes in the actor's body.
                    if (_excluded.Contains(sprite) || Vector3.Dot(sprite.transform.forward, RenderPipeline2DSetup.IsoCameraRotation * Vector3.forward) < .9f)
                    {
                        if (_originals.TryGetValue(sprite, out var original)) sprite.sharedMaterial = original;
                        continue;
                    }
                    if (!_originals.ContainsKey(sprite)) _originals.Add(sprite, sprite.sharedMaterial);
                    sprite.sharedMaterial = _depthMaterial;
                }
            }
            else
                foreach (var pair in _originals) if (pair.Key != null) pair.Key.sharedMaterial = pair.Value;
        }
    }
}
