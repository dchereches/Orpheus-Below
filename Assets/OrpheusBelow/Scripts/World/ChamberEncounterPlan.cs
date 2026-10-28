using System;
using System.Collections.Generic;
using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>The roster used by the actual chamber director, rather than a separate art showcase.</summary>
    public static class ChamberEncounterPlan
    {
        public static bool IsMajorBoss(int depth) => depth == 1 || depth == 3 || depth == 6;
        public static int WaveCount(int depth, bool ascending) => !ascending && IsMajorBoss(depth) ? 0 : ascending || depth >= 4 ? 3 : 2;
        public static BossIdentity BossAt(int depth) => depth == 1 ? BossIdentity.Tripod : depth == 3 ? BossIdentity.MysticEyeWarden : BossIdentity.CerberusHoundOfHades;
        public static bool IsMiniBoss(ShadeArchetype type) => type == ShadeArchetype.MaskedMonk || type == ShadeArchetype.WingedSentinel || type == ShadeArchetype.Shepherd;
        public static PaintedEnemyKind ArtFor(ShadeArchetype type)
        {
            switch(type)
            {
                case ShadeArchetype.BrimstoneCaster: return PaintedEnemyKind.HoodedSkeleton;
                case ShadeArchetype.MourningAbductor: return PaintedEnemyKind.RoosterRider;
                case ShadeArchetype.SummonedSheep: return PaintedEnemyKind.Sheep;
                case ShadeArchetype.MaskedMonk: return PaintedEnemyKind.MaskedMonk;
                case ShadeArchetype.Shepherd: return PaintedEnemyKind.Shepherd;
                case ShadeArchetype.WingedSentinel:
                case ShadeArchetype.WretchLunger:
                case ShadeArchetype.ArmoredWarden: return PaintedEnemyKind.WingedSentinel;
                default: throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
        public static List<HadesChamberDirector.WaveSpawnSpec> Wave(int depth,int number,bool ascending)
        {
            var result = new List<HadesChamberDirector.WaveSpawnSpec>();
            if(number < 1 || number > WaveCount(depth, ascending)) return result;
            var roster = new List<ShadeArchetype>();
            if(ascending)
            {
                roster.Add(ShadeArchetype.MourningAbductor);
                if(depth >= 4) roster.Add(ShadeArchetype.MourningAbductor);
                roster.Add(depth % 3 == 0 ? ShadeArchetype.MaskedMonk : depth % 3 == 1 ? ShadeArchetype.WingedSentinel : ShadeArchetype.Shepherd);
                if(depth > 0) roster.Add(depth % 3 == 0 ? ShadeArchetype.Shepherd : depth % 3 == 1 ? ShadeArchetype.MaskedMonk : ShadeArchetype.WingedSentinel);
                if(depth % 3 == 0) roster.Add(ShadeArchetype.BrimstoneCaster);
            }
            else if(IsMajorBoss(depth)) return result;
            else
            {
                switch(depth)
                {
                    case 0:
                        roster.Add(ShadeArchetype.BrimstoneCaster); roster.Add(ShadeArchetype.MaskedMonk); break;
                    case 2:
                        roster.Add(ShadeArchetype.MaskedMonk); roster.Add(ShadeArchetype.WingedSentinel); roster.Add(ShadeArchetype.BrimstoneCaster); break;
                    case 4:
                        roster.Add(ShadeArchetype.WingedSentinel); roster.Add(ShadeArchetype.MaskedMonk); roster.Add(ShadeArchetype.WingedSentinel); break;
                    case 5:
                        roster.Add(ShadeArchetype.Shepherd); roster.Add(ShadeArchetype.WingedSentinel); roster.Add(ShadeArchetype.MaskedMonk); break;
                    case 7:
                        roster.Add(ShadeArchetype.BrimstoneCaster); roster.Add(ShadeArchetype.MaskedMonk); roster.Add(ShadeArchetype.WingedSentinel); roster.Add(ShadeArchetype.Shepherd); break;
                }
            }
            if (number > 1) roster.RemoveAll(type => type == ShadeArchetype.BrimstoneCaster);
            int desiredCount = !ascending && depth == 0 && number == 1 ? 3 : Mathf.Min(7, 4 + (depth >= 4 ? 1 : 0) + (number == 3 ? 1 : 0));
            while (roster.Count < desiredCount)
                roster.Add((roster.Count + number + depth) % 2 == 0 ? ShadeArchetype.MaskedMonk : ShadeArchetype.WingedSentinel);
            for(int i=0;i<roster.Count;i++)
            {
                float angle = i * Mathf.PI * 2 / Mathf.Max(1,roster.Count);
                result.Add(new HadesChamberDirector.WaveSpawnSpec { archetype=roster[i], localOffsetXZ=new Vector2(Mathf.Cos(angle)*6,Mathf.Sin(angle)*6) });
            }
            return result;
        }
        public static string Intro(int depth)
        {
            return string.Empty;
        }
    }
}
