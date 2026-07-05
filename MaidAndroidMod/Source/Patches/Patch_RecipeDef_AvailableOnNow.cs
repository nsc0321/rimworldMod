using HarmonyLib;
using Verse;
using RimWorld;
using System.Linq;

namespace MaidAndroidMod
{
    [HarmonyPatch(typeof(RecipeDef), nameof(RecipeDef.AvailableOnNow))]
    public static class Patch_RecipeDef_AvailableOnNow
    {
        [HarmonyPostfix]
        public static void Postfix(RecipeDef __instance, Thing thing, ref bool __result)
        {
            if (thing == null) return;

            // Case A: Surgery recipe targeting the Mech_Maid pawn
            if (thing is Pawn pawn && pawn.def != null && pawn.health != null)
            {
                if (MaidUtility.IsMaid(pawn) && __instance.defName.StartsWith("InstallMaidModule_"))
                {
                    int activeModules = 0;
                    foreach (var hediff in pawn.health.hediffSet.hediffs)
                    {
                        if (hediff.def != null && hediff.def.defName.StartsWith("MaidModule_"))
                        {
                            activeModules++;
                        }
                    }

                    int maxModules = MaidUtility.GetMaxModules(pawn);
                    if (activeModules >= maxModules)
                    {
                        __result = false;
                    }
                }
            }
            // Case B: Direct upgrade/removal recipe on the MaidModuleInstallationBench
            else if (thing is Building_WorkTable bench && bench.def.defName == "MaidModuleInstallationBench")
            {
                var comp = bench.GetComp<CompMaidAssembly>();
                if (__instance.defName.StartsWith("UpgradeMaid_"))
                {
                    if (comp == null || !comp.HasMaid)
                    {
                        __result = false;
                    }
                    else
                    {
                        Pawn presentMaid = comp.ContainedMaid;
                        string moduleSuffix = __instance.defName.Substring("UpgradeMaid_".Length);
                        string baseSuffix = MaidUtility.GetBaseModuleSuffix(moduleSuffix);
                        string basePref = "MaidModule_" + baseSuffix + "_";

                        // Calculate the incoming tier and defName
                        string hediffDefName = "MaidModule_" + moduleSuffix;
                        int incomingTier = MaidUtility.GetModuleTier(hediffDefName);

                        int activeModules = 0;
                        bool hasSameOrHigher = false;

                        foreach (var hediff in presentMaid.health.hediffSet.hediffs)
                        {
                            if (hediff.def != null && hediff.def.defName.StartsWith("MaidModule_"))
                            {
                                activeModules++;
                                if (MaidUtility.IsSameBaseModule(hediff.def.defName, baseSuffix))
                                {
                                    int existingTier = MaidUtility.GetModuleTier(hediff.def.defName);
                                    if (existingTier >= incomingTier)
                                    {
                                        hasSameOrHigher = true;
                                    }
                                    // Upgrading replaces the module, so it doesn't increase active count
                                    activeModules--;
                                }
                            }
                        }

                        int maxModules = MaidUtility.GetMaxModules(presentMaid);
                        if (activeModules >= maxModules || hasSameOrHigher)
                        {
                            __result = false;
                        }
                    }
                }
                else if (__instance.defName.StartsWith("RemoveMaid_"))
                {
                    if (comp == null || !comp.HasMaid)
                    {
                        __result = false;
                    }
                    else
                    {
                        Pawn presentMaid = comp.ContainedMaid;
                        string moduleSuffix = __instance.defName.Substring("RemoveMaid_".Length);
                        string baseSuffix = MaidUtility.GetBaseModuleSuffix(moduleSuffix);
                        string basePref = "MaidModule_" + baseSuffix + "_";

                        bool hasAnyTier = presentMaid.health.hediffSet.hediffs.Any(h => h.def != null && MaidUtility.IsSameBaseModule(h.def.defName, baseSuffix));
                        if (!hasAnyTier)
                        {
                            __result = false;
                        }
                    }
                }
            }
        }
    }
}
