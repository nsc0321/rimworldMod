using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;
using RimWorld;

namespace GeneControlMod
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("com.developer.genecontrolmod");
            harmony.PatchAll();
            Log.Message("[GeneControlMod] Harmony Patches applied successfully!");
        }
    }

    [HarmonyPatch(typeof(PawnGenerator), "GeneratePawn", new Type[] { typeof(PawnGenerationRequest) })]
    public static class Patch_PawnGenerator_GeneratePawn
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn __result)
        {
            // 1. Check settings availability
            if (GeneControlMod.settings == null)
                return;

            // 2. Check if the pawn is valid, humanlike, and has genes capability (Biotech DLC check)
            if (__result == null || !__result.RaceProps.Humanlike || __result.genes == null)
                return;

            // 3. Process Endogenes (Germline / Inheritable) Addition
            if (GeneControlMod.settings.addEndogenesEnabled)
            {
                TryAddRandomGenes(__result, 
                    asXenogene: false, 
                    chance: GeneControlMod.settings.endogeneChance, 
                    minGenes: GeneControlMod.settings.minEndogenesToAdd, 
                    maxGenes: GeneControlMod.settings.maxEndogenesToAdd);
            }

            // 4. Process Xenogenes (Artificial / Non-inheritable) Addition
            if (GeneControlMod.settings.addXenogenesEnabled)
            {
                TryAddRandomGenes(__result, 
                    asXenogene: true, 
                    chance: GeneControlMod.settings.xenogeneChance, 
                    minGenes: GeneControlMod.settings.minXenogenesToAdd, 
                    maxGenes: GeneControlMod.settings.maxXenogenesToAdd);
            }
        }

        private static void TryAddRandomGenes(Pawn pawn, bool asXenogene, float chance, int minGenes, int maxGenes)
        {
            // A. Gather all eligible candidate genes
            List<GeneDef> allGenes = DefDatabase<GeneDef>.AllDefsListForReading;
            if (allGenes == null || allGenes.Count == 0)
                return;

            List<GeneDef> candidates = new List<GeneDef>();
            bool useWhitelist = GeneControlMod.settings.whitelistHash.Count > 0;

            foreach (var gene in allGenes)
            {
                if (gene == null) continue;

                // Rule 1: Exclude blacklisted genes
                if (GeneControlMod.settings.blacklistHash.Contains(gene.defName))
                    continue;

                // Rule 2: If whitelist is active, restrict candidates to whitelisted genes
                if (useWhitelist && !GeneControlMod.settings.whitelistHash.Contains(gene.defName))
                    continue;

                // Rule 3: Skip if the pawn already possesses this active gene to avoid duplication
                if (pawn.genes.HasActiveGene(gene))
                    continue;

                candidates.Add(gene);
            }

            if (candidates.Count == 0)
                return;

            // B. Realign and sanity-check the Min/Max bounds
            int minCount = Math.Max(0, minGenes);
            int maxCount = Math.Max(minCount, maxGenes);
            
            if (maxCount == 0)
                return;

            int currentAdded = 0;

            // C. Evaluate each gene sequentially one-by-one
            while (currentAdded < maxCount && candidates.Count > 0)
            {
                bool shouldAdd = false;

                if (currentAdded < minCount)
                {
                    // 1. Under Minimum limit: 100% guaranteed spawning!
                    shouldAdd = true;
                }
                else
                {
                    // 2. Between Min and Max limits: Spawns strictly at the configured Global Chance %
                    if (Rand.Value <= chance)
                    {
                        shouldAdd = true;
                    }
                    else
                    {
                        // Roll failed! Immediately stop further sequential evaluations (halls further additions)
                        break;
                    }
                }

                if (shouldAdd)
                {
                    // D. Calculate active weights for weighted random selection from remaining pool
                    float totalWeight = 0f;
                    List<float> weights = new List<float>();

                    foreach (var candidate in candidates)
                    {
                        float weight = 1.0f; // Default baseline spawning weight
                        if (GeneControlMod.settings.customGeneChances.TryGetValue(candidate.defName, out float customChance))
                        {
                            weight = customChance;
                        }

                        // Treat extremely low/0% probabilities as inactive
                        if (weight < 0.001f)
                        {
                            weight = 0f;
                        }

                        weights.Add(weight);
                        totalWeight += weight;
                    }

                    // Fallback to uniform selection if all remaining options have 0 weight
                    GeneDef selectedGene = null;
                    if (totalWeight <= 0f)
                    {
                        selectedGene = candidates.RandomElement();
                    }
                    else
                    {
                        // Weighted roll
                        float roll = Rand.Range(0f, totalWeight);
                        float cumulative = 0f;

                        for (int i = 0; i < candidates.Count; i++)
                        {
                            cumulative += weights[i];
                            if (roll <= cumulative)
                            {
                                selectedGene = candidates[i];
                                break;
                            }
                        }
                    }

                    // Strict boundary fallback
                    if (selectedGene == null)
                    {
                        selectedGene = candidates.RandomElement();
                    }

                    // Inject the gene onto the pawn
                    pawn.genes.AddGene(selectedGene, xenogene: asXenogene);
                    
                    // Remove selected gene from candidates to ensure no duplicate additions in subsequent rolls
                    candidates.Remove(selectedGene);
                    currentAdded++;
                }
            }
        }
    }
}
