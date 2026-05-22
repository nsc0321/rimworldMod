using HarmonyLib;
using RimWorld;
using Verse;

namespace HiveMod
{
    [HarmonyPatch(typeof(BodyPartDef), "GetMaxHealth")]
    public static class HarmonyPatch_BodyPartHealth
    {
        [HarmonyPostfix]
        public static void Postfix(BodyPartDef __instance, Pawn pawn, ref float __result)
        {
            if (pawn == null || pawn.genes == null) return;

            // Check if the pawn has the Hive Seed gene and is deployed
            GeneDef hiveSeedDef = DefDatabase<GeneDef>.GetNamedSilentFail("Gene_HiveSeed");
            if (hiveSeedDef == null) return;

            var deployGene = pawn.genes.GetGene(hiveSeedDef) as Gene_Deploy;
            if (deployGene != null && deployGene.isDeployed)
            {
                // Override Torso health to 500
                if (__instance.defName == "Torso")
                {
                    __result = 500f;
                }
                // Override Brain health to 10
                else if (__instance.defName == "Brain")
                {
                    __result = 10f;
                }
            }
        }
    }
}
