using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace HiveMod
{
    public class Window_SpawnHiveUnit : Window
    {
        private Building_Overmind overmind;
        private GameComponent_HiveEvolution evolutionComponent;
        private const float BaseBiomassCost = 10f; // Base cost for the naked unit
        
        // Dictionary to track which part is selected per category (e.g., "Jaw" -> "HivePart_ToxicJaw")
        private Dictionary<string, HediffDef> selectedParts = new Dictionary<string, HediffDef>();

        public override Vector2 InitialSize => new Vector2(500f, 600f);

        public Window_SpawnHiveUnit(Building_Overmind overmind)
        {
            this.overmind = overmind;
            this.forcePause = true;
            this.doCloseX = true;
            this.doCloseButton = true;
            this.absorbInputAroundWindow = true;
            this.evolutionComponent = Current.Game.GetComponent<GameComponent_HiveEvolution>();
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 35f), "Assemble Custom Hive Unit");
            Text.Font = GameFont.Small;

            float currentY = 40f;
            int totalBiomass = GetTotalBiomass(overmind.Map);
            float totalCost = CalculateTotalCost();

            Widgets.Label(new Rect(0f, currentY, inRect.width, 24f), $"Available Biomass on Map: {totalBiomass}");
            currentY += 25f;
            Widgets.Label(new Rect(0f, currentY, inRect.width, 24f), $"Total Assembly Cost: {totalCost}");
            currentY += 30f;

            // Get all unlocked parts grouped by category
            var unlockedParts = DefDatabase<HediffDef>.AllDefs
                .Where(d => d.HasModExtension<DefModExtension_HivePart>() && evolutionComponent.IsUnlocked(d))
                .GroupBy(d => d.GetModExtension<DefModExtension_HivePart>().category)
                .ToList();

            foreach (var group in unlockedParts)
            {
                Widgets.Label(new Rect(0f, currentY, inRect.width, 24f), $"--- {group.Key} ---");
                currentY += 25f;

                // Option for "None"
                bool noneSelected = !selectedParts.ContainsKey(group.Key);
                if (Widgets.RadioButtonLabeled(new Rect(10f, currentY, 200f, 24f), "None", noneSelected))
                {
                    if (selectedParts.ContainsKey(group.Key))
                        selectedParts.Remove(group.Key);
                }
                currentY += 25f;

                // Options for unlocked parts
                foreach (var partDef in group)
                {
                    var ext = partDef.GetModExtension<DefModExtension_HivePart>();
                    bool isSelected = selectedParts.ContainsKey(group.Key) && selectedParts[group.Key] == partDef;
                    
                    if (Widgets.RadioButtonLabeled(new Rect(10f, currentY, 400f, 24f), $"{partDef.label.CapitalizeFirst()} (+{ext.assemblyCostBiomass} Cost)", isSelected))
                    {
                        selectedParts[group.Key] = partDef;
                    }
                    currentY += 25f;
                }
                currentY += 10f; // Spacing between categories
            }

            // Spawn Button
            Rect spawnRect = new Rect(0f, inRect.height - 80f, inRect.width, 40f);
            if (Widgets.ButtonText(spawnRect, "Assemble & Spawn Unit"))
            {
                if (totalBiomass >= totalCost)
                {
                    ConsumeBiomass(totalCost);
                    SpawnUnitWithParts();
                    this.Close();
                }
                else
                {
                    Messages.Message("Not enough Biomass.", MessageTypeDefOf.RejectInput);
                }
            }
        }

        private float CalculateTotalCost()
        {
            float cost = BaseBiomassCost;
            foreach (var partDef in selectedParts.Values)
            {
                var ext = partDef.GetModExtension<DefModExtension_HivePart>();
                cost += ext.assemblyCostBiomass;
            }
            return cost;
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

        private void SpawnUnitWithParts()
        {
            PawnKindDef kindDef = DefDatabase<PawnKindDef>.GetNamed("HiveKind_Unit");
            PawnGenerationRequest request = new PawnGenerationRequest(kindDef, Faction.OfPlayer, PawnGenerationContext.NonPlayer, -1, true, false, false, false, true, 1f, false, true, false, true, false, false);
            Pawn pawn = PawnGenerator.GeneratePawn(request);
            
            GenSpawn.Spawn(pawn, overmind.InteractionCell, overmind.Map);

            // Apply selected parts (Hediffs)
            foreach (var partDef in selectedParts.Values)
            {
                // In a real mod, we would try to find the specific body part record (e.g. "Jaw") to apply the Hediff to.
                // For simplicity, we apply it to the whole body if no specific part is found, or just rely on the Hediff to affect global stats.
                BodyPartRecord targetPart = null;
                var ext = partDef.GetModExtension<DefModExtension_HivePart>();
                if (ext.category == "Jaw") targetPart = pawn.RaceProps.body.GetPartsWithDef(BodyPartDefOf.Jaw).FirstOrDefault();
                else if (ext.category == "Legs") targetPart = pawn.RaceProps.body.GetPartsWithDef(BodyPartDefOf.Leg).FirstOrDefault();
                
                pawn.health.AddHediff(partDef, targetPart);
            }

            Messages.Message($"Spawned a mutated {kindDef.label}!", pawn, MessageTypeDefOf.PositiveEvent);
        }
    }
}
