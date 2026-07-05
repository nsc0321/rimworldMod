using HarmonyLib;
using Verse;
using RimWorld;

namespace MaidAndroidMod
{
    [HarmonyPatch(typeof(StartingPawnUtility), nameof(StartingPawnUtility.GeneratePossessions))]
    public static class Patch_StartingPawnUtility_GeneratePossessions
    {
        [HarmonyPrefix]
        public static bool Prefix(Pawn pawn)
        {
            if (pawn == null)
            {
                Log.Warning("[MaidAndroidMod] GeneratePossessions called with null pawn!");
                return false;
            }
            if (pawn.story == null)
            {
                Log.Warning($"[MaidAndroidMod] GeneratePossessions bypassed for non-humanlike or story-less pawn: {pawn.def?.defName}");
                return false;
            }
            return true;
        }
    }
}
