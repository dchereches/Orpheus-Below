using System.Collections.Generic;
using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>
    /// 5-Layer 2D Parallax Backdrop & Foreground Vignette Rig for Orpheus Below.
    /// Drop AI-generated 2D PNGs onto the public Sprite slots in the Inspector at any time!
    /// Rises smoothly into place when the camera shifts from 2.5D Isometric into the 2D Boss Rhythm View,
    /// sliding each depth plane at authentic horizontal parallax speeds as Orpheus moves & dashes.
    /// </summary>
    public class UnderworldParallaxRig : MonoBehaviour
    {
        [Header("Drop AI-Generated 2D Layer PNGs Here (Optional — Procedural Art Used Until Assigned)")]
        public Sprite farAbyssSkySprite;
        public Sprite distantPeaksSprite;
        public Sprite midColonnadeSprite;
        public Sprite nearChainsSprite;
        public Sprite foregroundArchSprite;

        [Header("Theme Palette")]
        public Color abyssGlowTint = new Color(0.42f, 0.10f, 0.32f, 0.85f);
        public Color midArchitectureTint = new Color(0.14f, 0.08f, 0.22f, 0.94f);
        public Color accentFireTint = new Color(1.8f, 0.45f, 0.22f, 0.40f);

        private class ParallaxPlane
        {
            public Transform root;
            public float parallaxMultiplier; // 0 = locked to horizon, 1 = moves 1:1 with stage, >1 = foreground faster!
            public float depthAlongIsoUp;
            public float baseHeightY;
            public List<SpriteRenderer> renderers = new List<SpriteRenderer>();
            public List<float> baseAlphas = new List<float>();
        }

        private readonly List<ParallaxPlane> _planes = new List<ParallaxPlane>();
        private Vector3 _stageCenter;
        private float _visibility01;

        public void Initialize(Vector3 stageCenter, Color glowTint, Color archTint, Color fireTint)
        {
            _stageCenter = stageCenter;
            abyssGlowTint = glowTint;
            midArchitectureTint = archTint;
            accentFireTint = fireTint;
            transform.position = stageCenter;

            BuildPlanes();
            ApplyVisibility(0f, 0f);
        }

        private void BuildPlanes()
        {
            _planes.Clear();
            Quaternion rot = Quaternion.Euler(4f, 45f, 0f);
            bool hasFullBackdrop = farAbyssSkySprite != null;

            // PLANE 0: Deep Abyss Sky & Stygian Nebula (furthest back, slow 0.08x drift)
            ParallaxPlane p0 = CreatePlane("Parallax0_FarAbyss", depth: 18f, baseY: 4.2f, mult: 0.08f, rot);
            if (farAbyssSkySprite != null)
            {
                AddElement(p0, "CustomSky", farAbyssSkySprite, Vector3.zero, new Vector3(36f, 18f, 1f), Color.white, -40);
            }
            else
            {
                AddElement(p0, "AbyssBackdrop", RenderPipeline2DSetup.SquareSprite, Vector3.zero, new Vector3(44f, 22f, 1f), new Color(0.03f, 0.015f, 0.05f, 0.96f), -45);
                AddElement(p0, "NebulaLeft", RenderPipeline2DSetup.SoftGlowSprite, new Vector3(-7f, 2.2f, 0f), new Vector3(22f, 13f, 1f), abyssGlowTint, -42);
                AddElement(p0, "NebulaRight", RenderPipeline2DSetup.SoftGlowSprite, new Vector3(6.5f, 1.0f, 0f), new Vector3(20f, 12f, 1f), accentFireTint, -41);
            }

            // PLANE 1: Distant Jagged Tartarus Spires (0.22x parallax)
            ParallaxPlane p1 = CreatePlane("Parallax1_DistantPeaks", depth: 13.5f, baseY: 2.6f, mult: 0.22f, rot);
            if (distantPeaksSprite != null)
            {
                AddElement(p1, "CustomPeaks", distantPeaksSprite, Vector3.zero, new Vector3(32f, 14f, 1f), Color.white, -35);
            }
            else if (!hasFullBackdrop)
            {
                for (int i = -4; i <= 4; i++)
                {
                    float h = 7.5f + Mathf.Abs((i * 37) % 5) * 1.4f;
                    AddElement(p1, "TartarusSpire_" + i, RenderPipeline2DSetup.DiamondSprite,
                        new Vector3(i * 4.4f, h * 0.25f, 0f), new Vector3(5.8f, h, 1f),
                        new Color(midArchitectureTint.r * 0.65f, midArchitectureTint.g * 0.65f, midArchitectureTint.b * 0.72f, 0.92f), -35);
                }
            }

            // PLANE 2: Mid-Ground Ruined Greek Colonnade (0.46x parallax)
            ParallaxPlane p2 = CreatePlane("Parallax2_MidColonnade", depth: 9.0f, baseY: 3.1f, mult: 0.46f, rot);
            if (midColonnadeSprite != null)
            {
                AddElement(p2, "CustomColonnade", midColonnadeSprite, Vector3.zero, new Vector3(28f, 13f, 1f), Color.white, -28);
            }
            else if (!hasFullBackdrop)
            {
                for (int i = -4; i <= 4; i++)
                {
                    AddElement(p2, "DoricColumn_" + i, RenderPipeline2DSetup.SquareSprite,
                        new Vector3(i * 3.8f, 1.2f, 0f), new Vector3(0.95f, 11.5f, 1f), midArchitectureTint, -28);
                    AddElement(p2, "ColumnGlow_" + i, RenderPipeline2DSetup.SoftGlowSprite,
                        new Vector3(i * 3.8f, -1.6f, 0f), new Vector3(3.6f, 3.6f, 1f),
                        new Color(accentFireTint.r, accentFireTint.g, accentFireTint.b, 0.22f), -27);
                }
            }

            // PLANE 3: Near Hanging Chains & Silhouettes (0.74x parallax)
            ParallaxPlane p3 = CreatePlane("Parallax3_NearChains", depth: 4.8f, baseY: 4.4f, mult: 0.74f, rot);
            if (nearChainsSprite != null)
            {
                AddElement(p3, "CustomChains", nearChainsSprite, Vector3.zero, new Vector3(26f, 12f, 1f), Color.white, -18);
            }
            else if (!hasFullBackdrop)
            {
                for (int i = -3; i <= 3; i++)
                {
                    if (i == 0) continue;
                    AddElement(p3, "HangingChain_" + i, RenderPipeline2DSetup.SquareSprite,
                        new Vector3(i * 4.2f, 2.2f, 0f), new Vector3(0.18f, 9.5f, 1f),
                        new Color(0.06f, 0.03f, 0.09f, 0.85f), -18);
                }
            }

            // PLANE 4: Dark Foreground Vignette Framing Pillars (in FRONT of stage, 1.32x reverse parallax!)
            ParallaxPlane p4 = CreatePlane("Parallax4_ForegroundFrame", depth: -4.2f, baseY: 3.4f, mult: 1.32f, rot);
            if (foregroundArchSprite != null)
            {
                AddElement(p4, "CustomForeground", foregroundArchSprite, Vector3.zero, new Vector3(26f, 13f, 1f), Color.white, 32);
            }
            else
            {
                AddElement(p4, "FgSilhouetteLeft", RenderPipeline2DSetup.SquareSprite,
                    new Vector3(-11.8f, 0.5f, 0f), new Vector3(2.8f, 15f, 1f), new Color(0.02f, 0.01f, 0.035f, 0.90f), 32);
                AddElement(p4, "FgSilhouetteRight", RenderPipeline2DSetup.SquareSprite,
                    new Vector3(11.8f, 0.5f, 0f), new Vector3(2.8f, 15f, 1f), new Color(0.02f, 0.01f, 0.035f, 0.90f), 32);
            }
        }

        private ParallaxPlane CreatePlane(string name, float depth, float baseY, float mult, Quaternion rot)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.rotation = rot;

            ParallaxPlane p = new ParallaxPlane
            {
                root = go.transform,
                depthAlongIsoUp = depth,
                baseHeightY = baseY,
                parallaxMultiplier = mult
            };
            _planes.Add(p);
            return p;
        }

        private void AddElement(ParallaxPlane plane, string name, Sprite sprite, Vector3 localPos, Vector3 scale, Color col, int sortOrder)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(plane.root, false);
            go.transform.localPosition = localPos;
            // Treat scale as desired world dimensions, independent of imported pixels/PPU.
            Vector2 nativeSize = sprite.bounds.size;
            go.transform.localScale = new Vector3(scale.x / Mathf.Max(0.001f, nativeSize.x),
                scale.y / Mathf.Max(0.001f, nativeSize.y), scale.z);
            SpriteRenderer sr = RenderPipeline2DSetup.AttachSprite(go, sprite, col, sortOrder);
            plane.renderers.Add(sr);
            plane.baseAlphas.Add(col.a);
        }

        private void LateUpdate()
        {
            UnderworldCamera2D cam = UnderworldCamera2D.Instance;
            if (cam == null) return;

            // When a 2D Boss Battle is active, keep background pure void black (no nebulae, spires, or colonnades)
            if (CerberusRhythmBoss.ActiveBoss != null && CerberusRhythmBoss.ActiveBoss.IsBattleActive)
            {
                _visibility01 = 0f;
                ApplyVisibility(0f, 0f);
                return;
            }

            // Only show parallax rig when this chamber is the active one and Side2DBlend > 0
            float distToCamFocus = Vector3.Distance(
                new Vector3(cam.chamberCenter.x, 0f, cam.chamberCenter.z),
                new Vector3(_stageCenter.x, 0f, _stageCenter.z));

            float targetVis = distToCamFocus < 25f ? cam.SideViewBlend01 : 0f;
            _visibility01 = Mathf.MoveTowards(_visibility01, targetVis, Time.unscaledDeltaTime * 3.5f);

            ApplyVisibility(_visibility01, cam.CurrentSideParallaxOffset);
        }

        private void ApplyVisibility(float vis01, float camHorizOffset)
        {
            bool active = vis01 > 0.005f;
            for (int i = 0; i < _planes.Count; i++)
            {
                ParallaxPlane p = _planes[i];
                if (p.root == null) continue;
                p.root.gameObject.SetActive(active);
                if (!active) continue;

                // True multi-plane parallax shift along IsoRightXZ!
                float shift = -camHorizOffset * (p.parallaxMultiplier - 0.5f) * 0.85f;
                float riseOffset = (1f - vis01) * -3.5f;
                p.root.position = _stageCenter
                                + UnderworldCamera2D.IsoUpXZ * p.depthAlongIsoUp
                                + UnderworldCamera2D.IsoRightXZ * shift
                                + new Vector3(0f, p.baseHeightY + riseOffset, 0f);
                p.root.rotation = UnderworldCamera2D.CurrentBillboardRotation;

                for (int r = 0; r < p.renderers.Count; r++)
                {
                    SpriteRenderer sr = p.renderers[r];
                    if (sr == null) continue;
                    Color c = sr.color;
                    c.a = p.baseAlphas[r] * vis01;
                    sr.color = c;
                }
            }
        }
    }
}
