using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OrpheusBelow
{
    public static class UnderworldArtLibrary
    {
        private static readonly Dictionary<string, Sprite> PaintedSprites = new Dictionary<string, Sprite>();

        public static Sprite PaintedSprite(string name, float worldHeight = 8f, bool panel = false)
        {
            if (PaintedSprites.TryGetValue(name, out Sprite cached) && cached != null) return cached;
            Texture2D texture = Resources.Load<Texture2D>("Art/" + name);
            if (texture == null) return null;
            float border = panel ? texture.width * .045f : 0f;
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                panel ? Vector2.one * .5f : new Vector2(.5f, 0f), texture.height / worldHeight,
                0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            sprite.name = name;
            PaintedSprites[name] = sprite;
            return sprite;
        }

        public static void StylePanel(Image image, float opacity = .97f)
        {
            image.sprite = PaintedSprite("PaintedSlatePanel", 8f, true);
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1.5f;
            image.color = new Color(1f, 1f, 1f, opacity);
            image.raycastTarget = false;
        }

        public static SpriteRenderer CreatePaintedFloor(Transform parent, float width, float depth, bool basalt)
        {
            var go = new GameObject("QuietPaintedFloor");
            go.transform.SetParent(parent, false);
            go.transform.rotation = RenderPipeline2DSetup.GroundDecalRotation;
            var renderer = go.AddComponent<SpriteRenderer>();
            // Center the tiled sprite on the slab. Floor art stays at a consistent world texel scale.
            go.transform.localPosition = new Vector3(0f, .012f, -depth * .5f);
            renderer.sprite = PaintedSprite(basalt ? "QuietBasaltFloor" : "QuietSlateFloor");
            renderer.sharedMaterial = RenderPipeline2DSetup.Get2DSpriteMaterial();
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = new Vector2(width, depth);
            renderer.color = new Color(.66f, .66f, .70f, 1f);
            renderer.sortingOrder = -1;
            return renderer;
        }

        public static GameObject CreateBeaconTower(Transform parent, Vector3 position)
        {
            var tower = new GameObject("DarkBeaconTower");
            tower.transform.SetParent(parent, false);
            tower.transform.localPosition = position;
            var collider = tower.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 1.8f, 0f);
            collider.size = new Vector3(.9f, 3.6f, .9f);
            tower.AddComponent<IsoObstacle>().kind = IsoObstacleKind.SolidWall;

            var art = new GameObject("PaintedBeacon");
            art.transform.SetParent(tower.transform, false);
            // Offset the padded bottom of the source sprite just below the floor.
            art.transform.localPosition = Vector3.down * .08f;
            RenderPipeline2DSetup.AttachBillboardSprite(art, PaintedSprite("DarkBeaconTower", 4.7f), Color.white, 5);

            var lightPool = new GameObject("WarmPaintedLightPool");
            lightPool.transform.SetParent(tower.transform, false);
            lightPool.transform.localPosition = Vector3.up * .025f;
            lightPool.transform.localScale = new Vector3(3.5f, 3.5f, 1f);
            RenderPipeline2DSetup.AttachGroundDecal(lightPool, RenderPipeline2DSetup.SoftGlowSprite,
                new Color(.85f, .55f, .23f, .17f), 0);
            return tower;
        }

        private static readonly Sprite[] Creatures = new Sprite[8];
        // Measured padded regions in the generated 1536x1024 atlas (top-left coordinates).
        // Explicit regions retain wisps between rows without leaking them into adjacent characters.
        private static readonly Rect[] Regions =
        {
            new Rect(96, 166, 242, 333), new Rect(453, 64, 249, 457),
            new Rect(822, 125, 266, 374), new Rect(1236, 132, 233, 390),
            new Rect(71, 566, 266, 389), new Rect(472, 565, 227, 387),
            new Rect(833, 589, 246, 368), new Rect(1196, 614, 281, 331)
        };
        public static Sprite Creature(int index)
        {
            index = Mathf.Clamp(index, 0, 7);
            if (Creatures[index] != null) return Creatures[index];
            Texture2D atlas = Resources.Load<Texture2D>("Art/UnderworldCreatures");
            if (atlas == null) return null;
            Rect region = Regions[index];
            float sx = atlas.width / 1536f, sy = atlas.height / 1024f;
            var rect = new Rect(region.x * sx, (1024f - region.yMax) * sy, region.width * sx, region.height * sy);
            Creatures[index] = Sprite.Create(atlas, rect, new Vector2(0.5f, 0f), rect.height / 2.4f, 0, SpriteMeshType.FullRect);
            Creatures[index].name = "UnderworldCreature_" + index;
            return Creatures[index];
        }
    }
}
