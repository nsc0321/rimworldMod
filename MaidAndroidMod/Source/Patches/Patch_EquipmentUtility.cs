using System;
using System.Collections.Generic;
using HarmonyLib;
using Verse;
using Verse.AI;
using RimWorld;
using UnityEngine;

namespace MaidAndroidMod
{
    [HarmonyPatch(typeof(EquipmentUtility), nameof(EquipmentUtility.CanEquip), new Type[] { typeof(Thing), typeof(Pawn), typeof(string), typeof(bool) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal })]
    public static class Patch_EquipmentUtility_CanEquip
    {
        [HarmonyPostfix]
        public static void Postfix(Thing thing, Pawn pawn, ref bool __result)
        {
            if (pawn == null || pawn.def == null || pawn.health == null) return;

            // Bypasses the vanilla mechanoid weapon restriction if Maid has Combat Module installed
            if (MaidUtility.IsMaid(pawn))
            {
                if (MaidUtility.HasMaidModule(pawn, "Combat"))
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
                if (pawn == null || pawn.def == null || !MaidUtility.IsMaid(pawn) || pawn.Map == null) continue;

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
                if (MaidUtility.HasMaidModule(pawn, "Combat"))
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
}
