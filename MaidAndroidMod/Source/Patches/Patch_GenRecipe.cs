using System.Collections.Generic;
using HarmonyLib;
using Verse;
using RimWorld;
using System.Linq;

namespace MaidAndroidMod
{
    [HarmonyPatch(typeof(GenRecipe), nameof(GenRecipe.MakeRecipeProducts))]
    public static class Patch_GenRecipe_MakeRecipeProducts
    {
        [HarmonyPostfix]
        public static void Postfix(RecipeDef recipeDef, Pawn worker, List<Thing> ingredients, Thing dominantIngredient, IBillGiver billGiver, ref IEnumerable<Thing> __result)
        {
            if (recipeDef == null || billGiver == null) return;

            if (billGiver is Building_WorkTable bench && bench.def.defName == "MaidModuleInstallationBench")
            {
                if (recipeDef.defName.StartsWith("UpgradeMaid_"))
                {
                    var comp = bench.GetComp<CompMaidAssembly>();
                    if (comp != null)
                    {
                        Pawn targetMaid = comp.ContainedMaid;
                        if (targetMaid != null)
                        {
                            string moduleSuffix = recipeDef.defName.Substring("UpgradeMaid_".Length);
                            string baseSuffix = MaidUtility.GetBaseModuleSuffix(moduleSuffix);
                            string hediffDefName = "MaidModule_" + moduleSuffix;
                            HediffDef moduleHediff = HediffDef.Named(hediffDefName);

                            if (moduleHediff != null)
                            {
                                targetMaid.health.hediffSet.hediffs.RemoveAll(h => h.def != null && MaidUtility.IsSameBaseModule(h.def.defName, baseSuffix));

                                BodyPartRecord brainPart = targetMaid.health.hediffSet.GetBrain();
                                if (brainPart == null)
                                {
                                    brainPart = targetMaid.RaceProps.body.AllParts.FirstOrDefault(p => p.def.defName == "Brain" || p.def.defName == "ArtificialBrain" || p.def.defName == "MechanicalHead");
                                }
                                targetMaid.health.AddHediff(moduleHediff, brainPart);
                                Messages.Message("[MaidAndroidMod] 성공적으로 " + targetMaid.LabelShort + "에게 " + moduleHediff.label + " 모듈을 설치했습니다!", targetMaid, MessageTypeDefOf.PositiveEvent);

                                MaidUtility.EnableWorkType(targetMaid, baseSuffix);
                            }
                        }
                    }
                    __result = new List<Thing>();
                }
                else if (recipeDef.defName.StartsWith("RemoveMaid_"))
                {
                    var comp = bench.GetComp<CompMaidAssembly>();
                    var products = new List<Thing>();
                    if (comp != null)
                    {
                        Pawn targetMaid = comp.ContainedMaid;
                        if (targetMaid != null)
                        {
                            string moduleSuffix = recipeDef.defName.Substring("RemoveMaid_".Length);
                            string baseSuffix = MaidUtility.GetBaseModuleSuffix(moduleSuffix);
                             // Find the installed hediff of this base type
                             Hediff targetHediff = targetMaid.health.hediffSet.hediffs.FirstOrDefault(h => h.def != null && MaidUtility.IsSameBaseModule(h.def.defName, baseSuffix));
                            if (targetHediff != null)
                            {
                                string installedDefName = targetHediff.def.defName;
                                string installedSuffix = installedDefName.Substring("MaidModule_".Length);

                                targetMaid.health.RemoveHediff(targetHediff);
                                Messages.Message("[MaidAndroidMod] 성공적으로 " + targetMaid.LabelShort + "의 " + targetHediff.def.label + " 모듈을 분리했습니다.", targetMaid, MessageTypeDefOf.NeutralEvent);

                                MaidUtility.DisableWorkType(targetMaid, baseSuffix);

                                 string recipeLookupSuffix = installedSuffix;
                                 if (installedSuffix.StartsWith("Mine_")) recipeLookupSuffix = installedSuffix.Replace("Mine_", "Work_");
                                 else if (installedSuffix.StartsWith("Construct_")) recipeLookupSuffix = installedSuffix.Replace("Construct_", "Work_");
                                 else if (installedSuffix.StartsWith("Cook_")) recipeLookupSuffix = installedSuffix.Replace("Cook_", "Domestic_");
                                 else if (installedSuffix.StartsWith("Warden_")) recipeLookupSuffix = installedSuffix.Replace("Warden_", "Domestic_");
                                 else if (installedSuffix.StartsWith("Grow_")) recipeLookupSuffix = installedSuffix.Replace("Grow_", "Agriculture_");
                                 else if (installedSuffix.StartsWith("Craft_")) recipeLookupSuffix = installedSuffix.Replace("Craft_", "Crafting_");
                                 else if (installedSuffix.StartsWith("Art_")) recipeLookupSuffix = installedSuffix.Replace("Art_", "Crafting_");
                                 else if (installedSuffix.StartsWith("Doctor_")) recipeLookupSuffix = installedSuffix.Replace("Doctor_", "Rescue_");
                                 else if (installedSuffix.EndsWith("_Regular")) recipeLookupSuffix = recipeLookupSuffix.Replace("_Regular", "_Standard");

                                 RecipeDef upgradeRecipe = DefDatabase<RecipeDef>.GetNamedSilentFail("UpgradeMaid_" + recipeLookupSuffix);
                                 if (upgradeRecipe != null)
                                {
                                    foreach (var ing in upgradeRecipe.ingredients)
                                    {
                                        ThingDef thingDef = ing.filter?.AllowedThingDefs?.FirstOrDefault();
                                        if (thingDef != null)
                                        {
                                            int count = (int)ing.GetBaseCount();
                                            if (count > 0)
                                            {
                                                Thing thing = ThingMaker.MakeThing(thingDef);
                                                thing.stackCount = count;
                                                products.Add(thing);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    __result = products;
                }
            }
        }
    }
}
