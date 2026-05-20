using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;
using HarmonyLib;

namespace MaidAndroidMod
{
    [StaticConstructorOnStartup]
    public static class ModLoader
    {
        static ModLoader()
        {
            try
            {
                var harmony = new Harmony("nsc.MaidAndroidMod");
                harmony.PatchAll();
                Log.Message("[MaidAndroidMod] Custom Maid Mechanoid Mod successfully loaded! Dynamic 올라운더 Work-locks, 3-Cap, & Weaponry active.");
            }
            catch (Exception ex)
            {
                Log.Error("[MaidAndroidMod] Failed to initialize Harmony Patches: " + ex);
            }
        }
    }

    // ==================== 1. DYNAMIC WORK LOCK / UNLOCK ENGINE ====================
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.WorkTypeIsDisabled))]
    public static class Patch_Pawn_WorkTypeIsDisabled
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn __instance, WorkTypeDef w, ref bool __result)
        {
            if (__instance == null || __instance.def == null || __instance.health == null) return;

            if (__instance.def.defName == "Mech_Maid")
            {
                // Core lock checks for Maid Modules (Artistic mapped to 'Art')
                if (w.defName == "Cooking" && !__instance.health.hediffSet.HasHediff(HediffDef.Named("MaidModule_Cook")))
                {
                    __result = true;
                }
                else if (w.defName == "Growing" && !__instance.health.hediffSet.HasHediff(HediffDef.Named("MaidModule_Grow")))
                {
                    __result = true;
                }
                else if (w.defName == "Crafting" && !__instance.health.hediffSet.HasHediff(HediffDef.Named("MaidModule_Craft")))
                {
                    __result = true;
                }
                else if (w.defName == "Mining" && !__instance.health.hediffSet.HasHediff(HediffDef.Named("MaidModule_Mine")))
                {
                    __result = true;
                }
                else if (w.defName == "Construction" && !__instance.health.hediffSet.HasHediff(HediffDef.Named("MaidModule_Construct")))
                {
                    __result = true;
                }
                else if (w.defName == "Art" && !__instance.health.hediffSet.HasHediff(HediffDef.Named("MaidModule_Art")))
                {
                    __result = true;
                }
                else if (w.defName == "Warden" && !__instance.health.hediffSet.HasHediff(HediffDef.Named("MaidModule_Warden")))
                {
                    __result = true;
                }
                else if (w.defName == "Research" && !__instance.health.hediffSet.HasHediff(HediffDef.Named("MaidModule_Research")))
                {
                    __result = true;
                }
                else if (w.defName == "Doctor" && !__instance.health.hediffSet.HasHediff(HediffDef.Named("MaidModule_Doctor")))
                {
                    __result = true;
                }
            }
        }
    }

    // ==================== 2. COMBAT MODULE WEAPONRY PIPELINE ====================
    [HarmonyPatch(typeof(EquipmentUtility), nameof(EquipmentUtility.CanEquip), new Type[] { typeof(Thing), typeof(Pawn), typeof(string), typeof(bool) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal })]
    public static class Patch_EquipmentUtility_CanEquip
    {
        [HarmonyPostfix]
        public static void Postfix(Thing thing, Pawn pawn, ref bool __result)
        {
            if (pawn == null || pawn.def == null || pawn.health == null) return;

            // Bypasses the vanilla mechanoid weapon restriction if Maid has Combat Module installed
            if (pawn.def.defName == "Mech_Maid")
            {
                if (pawn.health.hediffSet.HasHediff(HediffDef.Named("MaidModule_Combat")))
                {
                    __result = true;
                }
            }
        }
    }

    [HarmonyPatch(typeof(FloatMenuMakerMap), "GetOptions")]
    public static class Patch_FloatMenuMakerMap_GetOptions
    {
        [HarmonyPostfix]
        public static void Postfix(List<Pawn> selectedPawns, Vector3 clickPos, ref FloatMenuContext context, List<FloatMenuOption> __result)
        {
            if (selectedPawns == null || selectedPawns.Count == 0 || __result == null) return;

            foreach (var pawn in selectedPawns)
            {
                if (pawn == null || pawn.def == null || pawn.def.defName != "Mech_Maid" || pawn.Map == null) continue;

                // 1. Move to Mechanoid Workbench / Charger ordered action
                IntVec3 clickCell = IntVec3.FromVector3(clickPos);
                List<Thing> clickThings = clickCell.GetThingList(pawn.Map);
                for (int i = 0; i < clickThings.Count; i++)
                {
                    Thing t = clickThings[i];
                    if (t is Building b && (
                        b.def.defName.Contains("MechGestator") ||
                        b.def.defName.Contains("SubcoreEncoder") ||
                        b.def.defName.Contains("MechCharger") ||
                        b.def.defName.Contains("GestationVat") ||
                        b.def.defName.Contains("SubcoreScanner")
                    ))
                    {
                        string label = "작업대로 이동 (" + b.LabelShort + ")";
                        Action action = delegate
                        {
                            Job job = JobMaker.MakeJob(JobDefOf.Goto, b);
                            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                        };
                        __result.Add(new FloatMenuOption(label, action));
                    }
                }

                // 2. Unlock right-click Equip context action on ground weapons if she has Combat Module active
                if (pawn.health.hediffSet.HasHediff(HediffDef.Named("MaidModule_Combat")))
                {
                    for (int i = 0; i < clickThings.Count; i++)
                    {
                        Thing t = clickThings[i];
                        if (t.def.equipmentType == EquipmentType.Primary)
                        {
                            string label = "Equip " + t.LabelShort;
                            Action action = delegate
                            {
                                Job job = JobMaker.MakeJob(JobDefOf.Equip, t);
                                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                            };
                            __result.Add(new FloatMenuOption(label, action));
                        }
                    }
                }
            }
        }
    }

    // ==================== 3. THREE-MODULE HARD CAP SURGERY CONTROLLER ====================
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
                // Intercept our custom module installation recipes
                if (pawn.def.defName == "Mech_Maid" && __instance.defName.StartsWith("InstallMaidModule_"))
                {
                    int activeModules = 0;
                    foreach (var hediff in pawn.health.hediffSet.hediffs)
                    {
                        if (hediff.def != null && hediff.def.defName.StartsWith("MaidModule_"))
                        {
                            activeModules++;
                        }
                    }

                    // If she already has 3 or more modules, block further surgery options completely!
                    if (activeModules >= 3)
                    {
                        __result = false;
                    }
                }
            }
            // Case B: Direct upgrade recipe on the MaidAssemblyBench
            else if (thing is Building_WorkTable bench && bench.def.defName == "MaidAssemblyBench")
            {
                var comp = bench.GetComp<CompMaidAssembly>();
                if (__instance.defName.StartsWith("UpgradeMaid_"))
                {
                    // Verify if a Mech_Maid is physically contained inside the bench component
                    if (comp == null || !comp.HasMaid)
                    {
                        __result = false;
                    }
                    else
                    {
                        Pawn presentMaid = comp.ContainedMaid;
                        // Map upgrade recipe to Hediff DefName
                        string moduleSuffix = __instance.defName.Substring("UpgradeMaid_".Length);
                        string hediffDefName = "MaidModule_" + moduleSuffix;
                        HediffDef moduleHediff = HediffDef.Named(hediffDefName);

                        if (moduleHediff != null)
                        {
                            int activeModules = 0;
                            bool alreadyHasThis = false;
                            foreach (var hediff in presentMaid.health.hediffSet.hediffs)
                            {
                                if (hediff.def != null && hediff.def.defName.StartsWith("MaidModule_"))
                                {
                                    activeModules++;
                                    if (hediff.def.defName == hediffDefName)
                                    {
                                        alreadyHasThis = true;
                                    }
                                }
                            }

                            if (activeModules >= 3 || alreadyHasThis)
                            {
                                __result = false;
                            }
                        }
                    }
                }
            }
        }
    }

    // ==================== 4. GENRECIPE PRODUCT INTERCEPTOR (DIRECT UPGRADES) ====================
    [HarmonyPatch(typeof(GenRecipe), nameof(GenRecipe.MakeRecipeProducts))]
    public static class Patch_GenRecipe_MakeRecipeProducts
    {
        [HarmonyPostfix]
        public static void Postfix(RecipeDef recipeDef, Pawn worker, List<Thing> ingredients, Thing dominantIngredient, IBillGiver billGiver, ref IEnumerable<Thing> __result)
        {
            if (recipeDef == null || billGiver == null || !recipeDef.defName.StartsWith("UpgradeMaid_")) return;

            if (billGiver is Building_WorkTable bench && bench.def.defName == "MaidAssemblyBench")
            {
                var comp = bench.GetComp<CompMaidAssembly>();
                if (comp != null)
                {
                    // Get the contained target maid
                    Pawn targetMaid = comp.ContainedMaid;

                    if (targetMaid != null)
                    {
                        // Map recipeDefName to HediffDefName
                        string moduleSuffix = recipeDef.defName.Substring("UpgradeMaid_".Length);
                        string hediffDefName = "MaidModule_" + moduleSuffix;
                        HediffDef moduleHediff = HediffDef.Named(hediffDefName);

                        if (moduleHediff != null)
                        {
                            // Add the Hediff directly to the target maid's Health tab!
                            if (!targetMaid.health.hediffSet.HasHediff(moduleHediff))
                            {
                                targetMaid.health.AddHediff(moduleHediff);
                                Messages.Message("[MaidAndroidMod] 성공적으로 " + targetMaid.LabelShort + "에게 " + moduleHediff.label + " 모듈을 성공적으로 설치했습니다!", targetMaid, MessageTypeDefOf.PositiveEvent);
                            }
                        }

                        // Eject the upgraded maid from the bench component!
                        comp.EjectMaid();
                    }
                }

                // Prevent physical product item from spawning on the floor
                __result = new List<Thing>();
            }
        }
    }
}
