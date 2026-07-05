using System.Collections.Generic;
using HarmonyLib;
using Verse;
using RimWorld;
using System.Linq;

namespace MaidAndroidMod
{
    [HarmonyPatch(typeof(ScenPart_StartingMech), "PlayerStartingThings")]
    public static class Patch_ScenPart_StartingMech_PlayerStartingThings
    {
        [HarmonyPostfix]
        public static void Postfix(ref IEnumerable<Thing> __result)
        {
            if (__result == null) return;
            
            var list = __result.ToList();
            
            foreach (var thing in list)
            {
                if (thing is Pawn pawn && pawn.def != null && MaidUtility.IsMaid(pawn))
                {
                    if (pawn.health != null && pawn.health.hediffSet != null)
                    {
                        BodyPartRecord brainPart = pawn.health.hediffSet.GetBrain();
                        if (brainPart == null)
                        {
                            brainPart = pawn.RaceProps.body.AllParts.FirstOrDefault(p => p.def.defName == "Brain" || p.def.defName == "ArtificialBrain" || p.def.defName == "MechanicalHead");
                        }

                        // 1. Install work module (Basic)
                        var workHediff = HediffDef.Named("MaidModule_Work_Basic");
                        if (workHediff != null && !pawn.health.hediffSet.HasHediff(workHediff))
                        {
                            pawn.health.AddHediff(workHediff, brainPart);
                            MaidUtility.EnableWorkType(pawn, "Work");
                        }
                        
                        // 2. Install combat module (Basic)
                        var combatHediff = HediffDef.Named("MaidModule_Combat_Basic");
                        if (combatHediff != null && !pawn.health.hediffSet.HasHediff(combatHediff))
                        {
                            pawn.health.AddHediff(combatHediff, brainPart);
                            MaidUtility.EnableWorkType(pawn, "Combat");
                        }
                    }
                }
            }
            
            __result = list;
        }
    }
}
