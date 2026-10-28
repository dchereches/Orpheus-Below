using System;
using System.Collections.Generic;
using UnityEngine;

namespace OrpheusBelow
{
    /// <summary>The roster used by the actual chamber director, rather than a separate art showcase.</summary>
    public static class ChamberEncounterPlan
    {
        public static bool IsMajorBoss(int depth) => depth == 1 || depth == 3 || depth == 6;
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
            if(number != 1) return result;
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
            for(int i=0;i<roster.Count;i++)
            {
                float angle = i * Mathf.PI * 2 / Mathf.Max(1,roster.Count);
                result.Add(new HadesChamberDirector.WaveSpawnSpec { archetype=roster[i], localOffsetXZ=new Vector2(Mathf.Cos(angle)*6,Mathf.Sin(angle)*6) });
            }
            return result;
        }
        public static string Intro(int depth)
        {
            switch(depth)
            {
                case 0: return "The hooded skeleton raises its pistol before firing. The Monk marks a dark circle before stone falls. Step clear of their warnings, use cover against shots, and answer with your lyre.";
                case 1: return "The Tripod's rings gather a sonic wave. Dash through it as it reaches you. A second pulse may follow; save your other dash.";
                case 2: return "The Masked Monk calls stone from above. A dark circle locks onto the floor before each boulder falls. Leave the circle, then strike while he recovers.";
                case 3: return "The Mystic Eye Warden commits his aim before throwing the eye-staff. Dash past the staff, then answer while he recalls it.";
                case 4: return "The Winged Sentinel raises his sword slowly. Its shadow marks the full sweep. Leave that area or time a dash through the blow, then punish the recovery.";
                case 5: return "The Shepherd's bell calls a small flock. Each sheep warns its dash before charging. Step aside, scatter the flock with music, and reach the Shepherd between summons.";
                case 6: return "Cerberus waits, then commits to a bite or a broad tail whip. Watch his stance and the shadow, evade the strike, and play during his recovery.";
                default: return string.Empty;
            }
        }
    }
}
