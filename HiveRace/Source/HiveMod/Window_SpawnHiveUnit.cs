using System;
using UnityEngine;
using Verse;
using RimWorld;

namespace HiveMod
{
    public class Window_SpawnHiveUnit : Window
    {
        private Building_Overmind overmind;
        private const float BiomassCost_Worker = 20f;
        private const float BiomassCost_Soldier = 50f;

        public override Vector2 InitialSize => new Vector2(400f, 300f);

        public Window_SpawnHiveUnit(Building_Overmind overmind)
        {
            this.overmind = overmind;
            this.forcePause = true;
            this.doCloseX = true;
            this.doCloseButton = true;
            this.absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 35f), "Assemble Hive Unit");
            Text.Font = GameFont.Small;

            float currentY = 50f;
            
            // Check available biomass. (For simplicity, we check map for Hive_Biomass items, or overmind's internal if it was stored inside. 
            // In Phase 2 we spawned them on the ground. We need to count them on the map, or just deduct them.)
            int totalBiomass = GetTotalBiomass(overmind.Map);

            Widgets.Label(new Rect(0f, currentY, inRect.width, 24f), $"Available Biomass on Map: {totalBiomass}");
            currentY += 40f;

            // Worker Button
            Rect workerRect = new Rect(0f, currentY, inRect.width, 40f);
            if (Widgets.ButtonText(workerRect, $"Assemble Worker (Cost: {BiomassCost_Worker} Biomass)"))
            {
                if (totalBiomass >= BiomassCost_Worker)
                {
                    ConsumeBiomass(BiomassCost_Worker);
                    SpawnUnit("HiveKind_Worker");
                    this.Close();
                }
                else
                {
                    Messages.Message("Not enough Biomass.", MessageTypeDefOf.RejectInput);
                }
            }
            currentY += 50f;

            // Soldier Button
            Rect soldierRect = new Rect(0f, currentY, inRect.width, 40f);
            if (Widgets.ButtonText(soldierRect, $"Assemble Soldier (Cost: {BiomassCost_Soldier} Biomass)"))
            {
                if (totalBiomass >= BiomassCost_Soldier)
                {
                    ConsumeBiomass(BiomassCost_Soldier);
                    SpawnUnit("HiveKind_Soldier");
                    this.Close();
                }
                else
                {
                    Messages.Message("Not enough Biomass.", MessageTypeDefOf.RejectInput);
                }
            }
        }

        private int GetTotalBiomass(Map map)
        {
            int count = 0;
            foreach (Thing t in map.listerThings.ThingsOfDef(ThingDef.Named("Hive_Biomass")))
            {
                count += t.stackCount;
            }
            return count;
        }

        private void ConsumeBiomass(float amount)
        {
            int remainingToConsume = (int)amount;
            var biomasses = overmind.Map.listerThings.ThingsOfDef(ThingDef.Named("Hive_Biomass"));
            for (int i = biomasses.Count - 1; i >= 0; i--)
            {
                Thing b = biomasses[i];
                if (b.stackCount <= remainingToConsume)
                {
                    remainingToConsume -= b.stackCount;
                    b.Destroy();
                }
                else
                {
                    b.SplitOff(remainingToConsume).Destroy();
                    remainingToConsume = 0;
                }

                if (remainingToConsume <= 0) break;
            }
        }

        private void SpawnUnit(string pawnKindDefName)
        {
            PawnKindDef kindDef = DefDatabase<PawnKindDef>.GetNamed(pawnKindDefName);
            PawnGenerationRequest request = new PawnGenerationRequest(kindDef, Faction.OfPlayer, PawnGenerationContext.NonPlayer, -1, true, false, false, false, true, 1f, false, true, false, true, false, false);
            Pawn pawn = PawnGenerator.GeneratePawn(request);
            
            GenSpawn.Spawn(pawn, overmind.InteractionCell, overmind.Map);
            Messages.Message($"Spawned a new {kindDef.label}!", pawn, MessageTypeDefOf.PositiveEvent);
        }
    }
}
