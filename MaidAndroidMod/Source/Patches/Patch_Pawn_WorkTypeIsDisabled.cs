using HarmonyLib;
using Verse;
using RimWorld;

namespace MaidAndroidMod
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.WorkTypeIsDisabled))]
    public static class Patch_Pawn_WorkTypeIsDisabled
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn __instance, WorkTypeDef w, ref bool __result)
        {
            if (__instance == null || __instance.def == null || __instance.health == null) return;

            if (MaidUtility.IsMaid(__instance))
            {
                // Core lock checks for Maid Modules
                if (w.defName == "Cooking" && !MaidUtility.HasMaidModule(__instance, "Domestic"))
                {
                    __result = true;
                }
                else if (w.defName == "Growing" && !MaidUtility.HasMaidModule(__instance, "Agriculture"))
                {
                    __result = true;
                }
                else if (w.defName == "Mining" && !MaidUtility.HasMaidModule(__instance, "Work"))
                {
                    __result = true;
                }
                else if (w.defName == "Construction" && !MaidUtility.HasMaidModule(__instance, "Work"))
                {
                    __result = true;
                }
                else if (w.defName == "Art" && !MaidUtility.HasMaidModule(__instance, "Crafting"))
                {
                    __result = true;
                }
                else if (w.defName == "Warden" && !MaidUtility.HasMaidModule(__instance, "Domestic"))
                {
                    __result = true;
                }
                else if (w.defName == "Research" && !MaidUtility.HasMaidModule(__instance, "Research"))
                {
                    __result = true;
                }
                else if (w.defName == "DarkStudy" && !MaidUtility.HasMaidModule(__instance, "Research"))
                {
                    __result = true;
                }
                else if (w.defName == "Doctor" && !MaidUtility.HasMaidModule(__instance, "Rescue"))
                {
                    __result = true;
                }
                else if (w.defName == "Firefighter" && !MaidUtility.HasMaidModule(__instance, "Rescue"))
                {
                    __result = true;
                }
                else if (w.defName == "Childcare" && !MaidUtility.HasMaidModule(__instance, "Domestic"))
                {
                    __result = true;
                }
                else if (w.defName == "Handling" && !MaidUtility.HasMaidModule(__instance, "Agriculture"))
                {
                    __result = true;
                }
                else if (w.defName == "Hunting" && !MaidUtility.HasMaidModule(__instance, "Combat"))
                {
                    __result = true;
                }
                else if ((w.defName == "Crafting" || w.defName == "Smithing" || w.defName == "Tailoring") &&
                         !MaidUtility.HasMaidModule(__instance, "Crafting") &&
                         !MaidUtility.HasMaidModule(__instance, "Domestic"))
                {
                    __result = true;
                }
                else if (w.defName == "PlantCutting" &&
                         !MaidUtility.HasMaidModule(__instance, "Work") &&
                         !MaidUtility.HasMaidModule(__instance, "Agriculture"))
                {
                    __result = true;
                }
            }
        }
    }
}
