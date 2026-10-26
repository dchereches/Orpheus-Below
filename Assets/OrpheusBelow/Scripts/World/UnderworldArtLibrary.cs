using UnityEngine;

namespace OrpheusBelow
{
    public static class UnderworldArtLibrary
    {
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
