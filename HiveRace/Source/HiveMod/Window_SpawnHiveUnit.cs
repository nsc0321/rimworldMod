using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace HiveMod
{
    public class Window_SpawnHiveUnit : Window
    {
        private IHiveCore overmind;
        private GameComponent_HiveEvolution evolutionComponent;
        private const float BaseBiomassCost = 10f; // Base cost for the naked unit
        private const float BaseEnergyCost = 50f;  // Base energy cost for the unit
        
        // Dictionary to track which part is selected per category (e.g., "Jaw" -> "HivePart_ToxicJaw")
        private Dictionary<string, GeneDef> selectedParts = new Dictionary<string, GeneDef>();

        public override Vector2 InitialSize => new Vector2(500f, 600f);

        public Window_SpawnHiveUnit(IHiveCore overmind)
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
            (float totalBiomassCost, float totalEnergyCost) = CalculateTotalCost();

            Widgets.Label(new Rect(0f, currentY, inRect.width, 24f), $"Available Biomass: {totalBiomass}  |  Available Energy: {overmind.CurrentEnergy:F0}");
            currentY += 25f;

            if (Widgets.ButtonText(new Rect(0f, currentY, 250f, 24f), "Convert 100 Energy -> 10 Biomass"))
            {
                if (overmind.CurrentEnergy >= 100f)
                {
                    overmind.CurrentEnergy -= 100f;
                    Thing biomass = ThingMaker.MakeThing(ThingDef.Named("Hive_Biomass"));
                    biomass.stackCount = 10;
                    GenPlace.TryPlaceThing(biomass, overmind.Position, overmind.Map, ThingPlaceMode.Near);
                    Messages.Message("Generated 10 Biomass.", MessageTypeDefOf.PositiveEvent);
                }
                else
                {
                    Messages.Message("Not enough Energy.", MessageTypeDefOf.RejectInput);
                }
            }
            currentY += 30f;

            Widgets.Label(new Rect(0f, currentY, inRect.width, 24f), $"Assembly Cost: {totalBiomassCost} Biomass, {totalEnergyCost} Energy");
            currentY += 30f;

            // Get all unlocked parts grouped by category
            var unlockedParts = DefDatabase<GeneDef>.AllDefs
                .Where(d => d.HasModExtension<DefModExtension_HivePart>() && evolutionComponent.IsUnlocked(d))
                .GroupBy(d => d.GetModExtension<DefModExtension_HivePart>().category)
                .ToList();

            float boxWidth = 140f;
            float boxHeight = 100f;
            int columns = Mathf.FloorToInt(inRect.width / (boxWidth + 15f));
            float gap = 15f;
            int i = 0;

            foreach (var group in unlockedParts)
            {
                int col = i % columns;
                int row = i / columns;
                Rect boxRect = new Rect(col * (boxWidth + gap), currentY + row * (boxHeight + gap), boxWidth, boxHeight);

                Widgets.DrawMenuSection(boxRect);
                Widgets.DrawHighlightIfMouseover(boxRect);

                // Category Name
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperCenter;
                Widgets.Label(new Rect(boxRect.x, boxRect.y + 5f, boxRect.width, 24f), group.Key);

                // Selected Part Name
                Text.Anchor = TextAnchor.MiddleCenter;
                bool hasSelection = selectedParts.ContainsKey(group.Key);
                string partName = hasSelection ? selectedParts[group.Key].label.CapitalizeFirst() : "None";
                Widgets.Label(boxRect, partName);

                // Selected Part Cost
                Text.Anchor = TextAnchor.LowerCenter;
                if (hasSelection)
                {
                    var ext = selectedParts[group.Key].GetModExtension<DefModExtension_HivePart>();
                    Widgets.Label(new Rect(boxRect.x, boxRect.yMax - 25f, boxRect.width, 24f), $"(+{ext.assemblyCostBiomass}B, +{ext.assemblyCostEnergy}E)");
                }

                Text.Anchor = TextAnchor.UpperLeft;

                // Handle Click
                if (Widgets.ButtonInvisible(boxRect))
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>();
                    
                    options.Add(new FloatMenuOption("None", () => {
                        if (selectedParts.ContainsKey(group.Key))
                            selectedParts.Remove(group.Key);
                    }));

                    foreach (var partDef in group)
                    {
                        var ext = partDef.GetModExtension<DefModExtension_HivePart>();
                        string label = $"{partDef.label.CapitalizeFirst()} (+{ext.assemblyCostBiomass} Biomass, +{ext.assemblyCostEnergy} Energy)";
                        // Need to copy the loop variable
                        GeneDef localDef = partDef; 
                        string localKey = group.Key;
                        options.Add(new FloatMenuOption(label, () => {
                            selectedParts[localKey] = localDef;
                        }));
                    }

                    Find.WindowStack.Add(new FloatMenu(options));
                }
                
                i++;
            }

            // Spawn Button
            Rect spawnRect = new Rect(0f, inRect.height - 80f, inRect.width, 40f);
            if (Widgets.ButtonText(spawnRect, "Assemble & Spawn Unit"))
            {
                if (totalBiomass >= totalBiomassCost && overmind.CurrentEnergy >= totalEnergyCost)
                {
                    ConsumeBiomass(totalBiomassCost);
                    overmind.CurrentEnergy -= totalEnergyCost;
                    SpawnUnitWithParts();
                    this.Close();
                }
                else
                {
                    Messages.Message("Not enough Biomass or Energy.", MessageTypeDefOf.RejectInput);
                }
            }
        }

        private (float, float) CalculateTotalCost()
        {
            float biomassCost = BaseBiomassCost;
            float energyCost = BaseEnergyCost;
            foreach (var partDef in selectedParts.Values)
            {
                var ext = partDef.GetModExtension<DefModExtension_HivePart>();
                biomassCost += ext.assemblyCostBiomass;
                energyCost += ext.assemblyCostEnergy;
            }
            return (biomassCost, energyCost);
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
            
            GenSpawn.Spawn(pawn, overmind.Position, overmind.Map);

            // Apply selected parts (Genes)
            if (pawn.genes == null)
            {
                pawn.genes = new Pawn_GeneTracker(pawn);
            }

            foreach (var partDef in selectedParts.Values)
            {
                pawn.genes.AddGene(partDef, true);
            }

            Messages.Message($"Spawned a mutated {kindDef.label}!", pawn, MessageTypeDefOf.PositiveEvent);
        }
    }
}
