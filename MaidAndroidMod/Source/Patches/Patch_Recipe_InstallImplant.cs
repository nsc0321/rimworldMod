using System.Collections.Generic;
using HarmonyLib;
using Verse;
using RimWorld;

namespace MaidAndroidMod
{
    [HarmonyPatch(typeof(Recipe_InstallImplant), nameof(Recipe_InstallImplant.ApplyOnPawn))]
    public static class Patch_Recipe_InstallImplant_ApplyOnPawn
    {
        [HarmonyPostfix]
        public static void Postfix(Recipe_InstallImplant __instance, Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (pawn == null || pawn.def == null || !MaidUtility.IsMaid(pawn)) return;

            if (__instance.recipe != null && __instance.recipe.defName.StartsWith("InstallMaidModule_"))
            {
                string moduleSuffix = __instance.recipe.defName.Substring("InstallMaidModule_".Length);
                string baseSuffix = MaidUtility.GetBaseModuleSuffix(moduleSuffix);
                MaidUtility.EnableWorkType(pawn, baseSuffix);
            }
        }
    }
}
