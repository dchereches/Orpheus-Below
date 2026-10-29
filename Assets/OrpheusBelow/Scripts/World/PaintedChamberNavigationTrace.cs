using System;
using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>Reviewed original-painting coordinates, shared by authoring and verification.</summary>
    [Serializable]
    public sealed class PaintedChamberNavigationTrace
    {
        [Serializable] public sealed class Probe { public string name; public float x, y; public Vector2 Point => new Vector2(x, y); }
        [Serializable] public sealed class OcclusionProbe
        { public string name, coverName; public Vector2 behind, front; }

        public int chamberIndex, imageWidth, imageHeight;
        public Vector2[] walkableBoundary;
        public PaintedChamberDefinition.Cover[] cover;
        public Vector2 entrance, exit, returnExit, returnSpawn, encounterCenter, reunionPoint;
        public Vector2[] enemySpawns;
        public Probe[] walkableProbes, blockedProbes;
        public OcclusionProbe[] occlusionProbes;
        public string[] notes;

        public static PaintedChamberNavigationTrace Load(int index)
        {
            var asset = Resources.Load<TextAsset>("ChamberNavigation/Chamber_" + index.ToString("D2"));
            if (asset == null) throw new InvalidOperationException("Missing reviewed navigation trace for chamber " + index);
            var trace = JsonUtility.FromJson<PaintedChamberNavigationTrace>(asset.text);
            if (trace == null || trace.chamberIndex != index || trace.imageWidth != 1536 || trace.imageHeight != 1024)
                throw new InvalidOperationException("Navigation trace must match its chamber and original 1536 x 1024 painting: " + index);
            return trace;
        }

        public void ApplyTo(PaintedChamberDefinition definition)
        {
            if (definition == null || definition.index != chamberIndex) throw new ArgumentException("Trace/layout index mismatch.");
            definition.walkableBoundary = walkableBoundary;
            definition.cover = cover;
            definition.entrance = entrance;
            definition.exit = exit;
            definition.returnExit = returnExit;
            definition.returnSpawn = returnSpawn;
            definition.encounterCenter = encounterCenter;
            definition.enemySpawns = enemySpawns;
            definition.reunionPoint = reunionPoint == Vector2.zero ? exit : reunionPoint;
            // Healing wells no longer exist. Retain valid legacy metadata without placing an object.
            if (!definition.IsWalkable(definition.ImageToGround(definition.well), .4f))
                definition.well = encounterCenter;
        }
    }
}
