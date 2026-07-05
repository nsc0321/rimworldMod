using HarmonyLib;
using RimWorld;
using Verse;

namespace MaidAndroidMod
{
    [HarmonyPatch(typeof(Need_MechEnergy), "CurLevel", MethodType.Setter)]
    public static class Patch_Need_MechEnergy_CurLevel_Setter
    {
        [HarmonyPrefix]
        public static void Prefix(Need_MechEnergy __instance, ref float value)
        {
            float cur = __instance.CurLevel;
            if (value > cur) // Energy is increasing (charging)
            {
                Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
                if (pawn != null && MaidUtility.IsMaid(pawn))
                {
                    var hediff = pawn.health?.hediffSet?.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamed("MaidMod_FastCharger", false));
                    if (hediff != null)
                    {
                        float factor = 1.0f;
                        if (hediff.Severity >= 3.9f) factor = 2.0f;       // Ultra: +100%
                        else if (hediff.Severity >= 2.9f) factor = 1.70f; // High: +70%
                        else if (hediff.Severity >= 1.9f) factor = 1.40f; // Standard: +40%
                        else if (hediff.Severity >= 0.9f) factor = 1.15f; // Basic: +15%

                        float diff = value - cur;
                        value = cur + diff * factor;
                    }
                }
            }
        }
    }
}
