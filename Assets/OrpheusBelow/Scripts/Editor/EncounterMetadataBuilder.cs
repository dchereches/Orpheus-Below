using UnityEditor;
using UnityEngine;

namespace OrpheusBelow.EditorTools
{
    public static class EncounterMetadataBuilder
    {
        public static void Update()
        {
            for(int i=0;i<UnderworldJourney.ChamberCount;i++)
            {
                var layout=Resources.Load<PaintedChamberDefinition>("ChamberLayouts/Chamber_"+i.ToString("D2"));
                layout.hasGuardian=ChamberEncounterPlan.IsMajorBoss(i);
                EditorUtility.SetDirty(layout);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Encounter integration] Saved three immediate major bosses and three chamber-floor mini-boss encounters.");
        }
    }
}
