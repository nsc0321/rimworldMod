using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace HiveMod
{
    [HarmonyPatch(typeof(MentalStateHandler), "TryStartMentalState")]
    public static class HarmonyPatch_PreventHiveSeedMentalBreak
    {
        public static bool Prefix(Pawn ___pawn)
        {
            if (___pawn?.genes != null)
            {
                GeneDef hiveSeedDef = DefDatabase<GeneDef>.GetNamedSilentFail("Gene_HiveSeed");
                if (hiveSeedDef != null && ___pawn.genes.HasActiveGene(hiveSeedDef))
                {
                    // Prevent all mental breaks for pawns with the Hive Seed gene
                    return false;
                }
            }
            return true;
        }
    }
}
