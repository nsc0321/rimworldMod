using HarmonyLib;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace MaidAndroidMod
{
    [HarmonyPatch(typeof(SkillRecord), "Level", MethodType.Getter)]
    public static class Patch_SkillRecord_Level
    {
        [HarmonyPostfix]
        public static void Postfix(SkillRecord __instance, ref int __result)
        {
            Pawn pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            if (pawn != null && MaidUtility.IsMaid(pawn))
            {
                SkillDef skill = __instance.def;
                if (skill != null)
                {
                    List<string> affectingModules = new List<string>();

                    if (skill.defName == "Cooking")
                    {
                        affectingModules.Add("Domestic");
                    }
                    else if (skill.defName == "Plants")
                    {
                        affectingModules.Add("Agriculture");
                        affectingModules.Add("Work");
                    }
                    else if (skill.defName == "Crafting")
                    {
                        affectingModules.Add("Crafting");
                        affectingModules.Add("Domestic");
                    }
                    else if (skill.defName == "Mining")
                    {
                        affectingModules.Add("Work");
                    }
                    else if (skill.defName == "Construction")
                    {
                        affectingModules.Add("Work");
                    }
                    else if (skill.defName == "Artistic")
                    {
                        affectingModules.Add("Crafting");
                    }
                    else if (skill.defName == "Social")
                    {
                        affectingModules.Add("Domestic");
                    }
                    else if (skill.defName == "Intellectual")
                    {
                        affectingModules.Add("Research");
                    }
                    else if (skill.defName == "Medicine")
                    {
                        affectingModules.Add("Rescue");
                    }
                    else if (skill.defName == "Shooting" || skill.defName == "Melee")
                    {
                        affectingModules.Add("Combat");
                    }
                    else if (skill.defName == "Animals")
                    {
                        affectingModules.Add("Agriculture");
                    }

                    if (affectingModules.Count > 0)
                    {
                        int highestLevel = 0;
                        bool hasAnyModule = false;

                        if (pawn.health?.hediffSet?.hediffs != null)
                        {
                            foreach (string moduleType in affectingModules)
                            {
                                List<string> prefixes = new List<string>();
                                prefixes.Add("MaidModule_" + moduleType + "_");
                                
                                // Legacy fallbacks for skill matching
                                if (moduleType == "Domestic")
                                {
                                    prefixes.Add("MaidModule_Cook_");
                                    prefixes.Add("MaidModule_Warden_");
                                }
                                else if (moduleType == "Work")
                                {
                                    prefixes.Add("MaidModule_Mine_");
                                    prefixes.Add("MaidModule_Construct_");
                                }
                                else if (moduleType == "Agriculture")
                                {
                                    prefixes.Add("MaidModule_Grow_");
                                }
                                else if (moduleType == "Crafting")
                                {
                                    prefixes.Add("MaidModule_Craft_");
                                    prefixes.Add("MaidModule_Art_");
                                }
                                else if (moduleType == "Rescue")
                                {
                                    prefixes.Add("MaidModule_Doctor_");
                                }

                                foreach (var hediff in pawn.health.hediffSet.hediffs)
                                {
                                    if (hediff.def != null)
                                    {
                                        foreach (var prefix in prefixes)
                                        {
                                            if (hediff.def.defName.StartsWith(prefix))
                                            {
                                                hasAnyModule = true;
                                                int level = 5;
                                                if (hediff.def.defName.EndsWith("_Ultra")) level = 15;
                                                else if (hediff.def.defName.EndsWith("_High")) level = 12;
                                                else if (hediff.def.defName.EndsWith("_Standard")) level = 8;
                                                else if (hediff.def.defName.EndsWith("_Basic")) level = 5;
                                                else if (hediff.def.defName.EndsWith("_Regular")) level = 8; // Legacy regular = 8 (standard)

                                                if (level > highestLevel)
                                                {
                                                    highestLevel = level;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        Log.Message($"[MaidAndroidMod] Postfix SkillRecord.Level: Pawn={pawn.LabelShort}, DefName={pawn.def.defName}, Skill={skill.defName}, Original={__result}, HasModule={hasAnyModule}, ComputedLevel={highestLevel}");

                        if (hasAnyModule)
                        {
                            __result = highestLevel;
                        }
                        else
                        {
                            __result = 0;
                        }
                    }
                }
            }
        }
    }
}
