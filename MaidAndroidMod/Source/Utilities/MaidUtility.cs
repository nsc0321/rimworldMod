using System;
using System.Collections.Generic;
using Verse;
using RimWorld;
using System.Linq;

namespace MaidAndroidMod
{
    public static class MaidUtility
    {
        public static bool IsMaid(Pawn pawn)
        {
            if (pawn == null || pawn.def == null) return false;
            return pawn.def.defName == "Mech_Maid_Basic" ||
                   pawn.def.defName == "Mech_Maid_Standard" ||
                   pawn.def.defName == "Mech_Maid_High" ||
                   pawn.def.defName == "Mech_Maid_Ultra" ||
                   pawn.def.defName == "Mech_Maid"; // Legacy fallback
        }

        public static bool IsSameBaseModule(string hediffDefName, string baseSuffix)
        {
            if (hediffDefName == null) return false;
            if (hediffDefName.StartsWith("MaidModule_" + baseSuffix + "_")) return true;
            
            // Legacy matches
            if (baseSuffix == "Domestic")
            {
                return hediffDefName.StartsWith("MaidModule_Cook_") || hediffDefName.StartsWith("MaidModule_Warden_");
            }
            if (baseSuffix == "Work")
            {
                return hediffDefName.StartsWith("MaidModule_Mine_") || hediffDefName.StartsWith("MaidModule_Construct_");
            }
            if (baseSuffix == "Agriculture")
            {
                return hediffDefName.StartsWith("MaidModule_Grow_");
            }
            if (baseSuffix == "Crafting")
            {
                return hediffDefName.StartsWith("MaidModule_Craft_") || hediffDefName.StartsWith("MaidModule_Art_");
            }
            if (baseSuffix == "Rescue")
            {
                return hediffDefName.StartsWith("MaidModule_Doctor_");
            }
            
            return false;
        }

        public static bool HasMaidModule(Pawn pawn, string suffix)
        {
            if (pawn == null || pawn.health == null || pawn.health.hediffSet == null) return false;
            
            foreach (var hediff in pawn.health.hediffSet.hediffs)
            {
                if (hediff.def != null && IsSameBaseModule(hediff.def.defName, suffix))
                {
                    return true;
                }
            }
            return false;
        }

        public static string GetBaseModuleSuffix(string suffix)
        {
            if (suffix.Contains("_"))
            {
                return suffix.Split('_')[0];
            }
            return suffix;
        }

        public static int GetModuleTier(string defName)
        {
            if (defName.EndsWith("_Ultra")) return 4;
            if (defName.EndsWith("_High")) return 3;
            if (defName.EndsWith("_Standard")) return 2;
            if (defName.EndsWith("_Basic")) return 1;
            // Legacy fallbacks
            if (defName.EndsWith("_Regular")) return 2; 
            return 1;
        }

        public static int GetMaxModules(Pawn pawn)
        {
            if (pawn == null || pawn.def == null) return 3;
            if (pawn.def.defName == "Mech_Maid_Basic") return 2;
            if (pawn.def.defName == "Mech_Maid_Standard") return 3;
            if (pawn.def.defName == "Mech_Maid_High") return 4;
            if (pawn.def.defName == "Mech_Maid_Ultra") return 5;
            return 3; // Mech_Maid legacy fallback is 3
        }

        public static void EnableWorkType(Pawn maid, string moduleSuffix)
        {
            if (maid.workSettings == null)
            {
                maid.workSettings = new Pawn_WorkSettings(maid);
                maid.workSettings.EnableAndInitialize();
            }

            List<string> workDefNames = GetWorkTypesForModule(moduleSuffix);

            foreach (string wName in workDefNames)
            {
                WorkTypeDef w = DefDatabase<WorkTypeDef>.GetNamedSilentFail(wName);
                if (w != null)
                {
                    maid.workSettings.SetPriority(w, 3);
                }
            }
        }

        public static void DisableWorkType(Pawn maid, string moduleSuffix)
        {
            if (maid.workSettings == null) return;

            List<string> workDefNames = GetWorkTypesForModule(moduleSuffix);

            foreach (string wName in workDefNames)
            {
                // Check if any other installed module enables this work type
                bool otherEnables = false;
                foreach (var otherSuffix in new[] { "Combat", "Rescue", "Domestic", "Work", "Research", "Agriculture", "Crafting" })
                {
                    if (otherSuffix != moduleSuffix && HasMaidModule(maid, otherSuffix))
                    {
                        if (GetWorkTypesForModule(otherSuffix).Contains(wName))
                        {
                            otherEnables = true;
                            break;
                        }
                    }
                }

                if (!otherEnables)
                {
                    WorkTypeDef w = DefDatabase<WorkTypeDef>.GetNamedSilentFail(wName);
                    if (w != null)
                    {
                        maid.workSettings.SetPriority(w, 0);
                    }
                }
            }
        }

        public static List<string> GetWorkTypesForModule(string moduleSuffix)
        {
            List<string> list = new List<string>();
            
            // Map legacy module names as well to enable corresponding work settings
            if (moduleSuffix == "Combat")
            {
                list.Add("Hunting");
            }
            else if (moduleSuffix == "Rescue" || moduleSuffix == "Doctor")
            {
                list.Add("Doctor");
                list.Add("Firefighter");
            }
            else if (moduleSuffix == "Domestic" || moduleSuffix == "Cook" || moduleSuffix == "Warden")
            {
                list.Add("Cooking");
                list.Add("Warden");
                list.Add("Tailoring");
                list.Add("Childcare");
            }
            else if (moduleSuffix == "Work" || moduleSuffix == "Mine" || moduleSuffix == "Construct")
            {
                list.Add("Mining");
                list.Add("Construction");
                list.Add("PlantCutting");
            }
            else if (moduleSuffix == "Research")
            {
                list.Add("Research");
                list.Add("DarkStudy");
            }
            else if (moduleSuffix == "Agriculture" || moduleSuffix == "Grow")
            {
                list.Add("Growing");
                list.Add("PlantCutting");
                list.Add("Handling");
            }
            else if (moduleSuffix == "Crafting" || moduleSuffix == "Craft" || moduleSuffix == "Art")
            {
                list.Add("Smithing");
                list.Add("Tailoring");
                list.Add("Crafting");
                list.Add("Art");
            }
            return list;
        }
    }
}
