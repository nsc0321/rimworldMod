using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace GeneControlMod
{
    public class GeneControlMod : Mod
    {
        public static GeneControlSettings settings;
        private Vector2 scrollPosition = Vector2.zero;
        private string searchFilter = "";
        
        // Cache of gene definitions for performance
        private List<GeneDef> cachedGeneDefs = null;

        public GeneControlMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<GeneControlSettings>();
            
            if (settings.blacklistedGeneNames == null) settings.blacklistedGeneNames = new List<string>();
            if (settings.whitelistedGeneNames == null) settings.whitelistedGeneNames = new List<string>();
            if (settings.customGeneChances == null) settings.customGeneChances = new Dictionary<string, float>();
            settings.InitializeHashes();
        }

        public override string SettingsCategory()
        {
            return "GC_Title".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            // Rich Header Design with Translation support
            Text.Font = GameFont.Medium;
            listingStandard.Label("GC_Title".Translate());
            Text.Font = GameFont.Small;
            listingStandard.Gap(8f);

            // -------------------------------------------------------------
            // Endogene (Germline) Settings Section
            // -------------------------------------------------------------
            listingStandard.CheckboxLabeled("GC_EndoEnable".Translate(), ref settings.addEndogenesEnabled, "GC_EndoEnableDesc".Translate());
            listingStandard.Gap(4f);

            if (settings.addEndogenesEnabled)
            {
                // Chance Slider
                float endogenePercent = settings.endogeneChance * 100f;
                listingStandard.Label($"{"GC_EndoChance".Translate()}: {endogenePercent:F0}%");
                settings.endogeneChance = listingStandard.Slider(settings.endogeneChance, 0f, 1f);
                
                // Min/Max Genes count (Allowed from 0)
                Rect rangeRect = listingStandard.GetRect(24f);
                Rect minRect = new Rect(rangeRect.x, rangeRect.y, 160f, 24f);
                Rect maxRect = new Rect(rangeRect.x + 180f, rangeRect.y, 160f, 24f);

                string minStr = settings.minEndogenesToAdd.ToString();
                string maxStr = settings.maxEndogenesToAdd.ToString();

                Widgets.TextFieldNumericLabeled(minRect, "GC_MinGenes".Translate(), ref settings.minEndogenesToAdd, ref minStr, 0, 100);
                Widgets.TextFieldNumericLabeled(maxRect, "GC_MaxGenes".Translate(), ref settings.maxEndogenesToAdd, ref maxStr, 0, 100);
                
                if (settings.minEndogenesToAdd > settings.maxEndogenesToAdd)
                {
                    settings.maxEndogenesToAdd = settings.minEndogenesToAdd;
                }
            }

            listingStandard.Gap(12f);

            // -------------------------------------------------------------
            // Xenogene (Artificial) Settings Section
            // -------------------------------------------------------------
            listingStandard.CheckboxLabeled("GC_XenoEnable".Translate(), ref settings.addXenogenesEnabled, "GC_XenoEnableDesc".Translate());
            listingStandard.Gap(4f);

            if (settings.addXenogenesEnabled)
            {
                // Chance Slider
                float xenogenePercent = settings.xenogeneChance * 100f;
                listingStandard.Label($"{"GC_XenoChance".Translate()}: {xenogenePercent:F0}%");
                settings.xenogeneChance = listingStandard.Slider(settings.xenogeneChance, 0f, 1f);
                
                // Min/Max Genes count (Allowed from 0)
                Rect rangeRect = listingStandard.GetRect(24f);
                Rect minRect = new Rect(rangeRect.x, rangeRect.y, 160f, 24f);
                Rect maxRect = new Rect(rangeRect.x + 180f, rangeRect.y, 160f, 24f);

                string minStr = settings.minXenogenesToAdd.ToString();
                string maxStr = settings.maxXenogenesToAdd.ToString();

                Widgets.TextFieldNumericLabeled(minRect, "GC_MinGenes".Translate(), ref settings.minXenogenesToAdd, ref minStr, 0, 100);
                Widgets.TextFieldNumericLabeled(maxRect, "GC_MaxGenes".Translate(), ref settings.maxXenogenesToAdd, ref maxStr, 0, 100);
                
                if (settings.minXenogenesToAdd > settings.maxXenogenesToAdd)
                {
                    settings.maxXenogenesToAdd = settings.minXenogenesToAdd;
                }
            }

            listingStandard.Gap(15f);

            // Fetch and Cache All Genes
            if (cachedGeneDefs == null)
            {
                cachedGeneDefs = DefDatabase<GeneDef>.AllDefsListForReading
                    .OrderBy(g => g.label ?? g.defName)
                    .ToList();
            }

            // Filter lists based on search filter
            List<GeneDef> filteredGenes = cachedGeneDefs;
            if (!searchFilter.NullOrEmpty())
            {
                filteredGenes = cachedGeneDefs
                    .Where(g => (g.label != null && g.label.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                 g.defName.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }

            // Search Bar & Utility Buttons with Select All capabilities
            Rect utilRect = listingStandard.GetRect(30f);
            Rect searchLabelRect = new Rect(utilRect.x, utilRect.y + 4f, 80f, 24f);
            Rect searchFieldRect = new Rect(utilRect.x + 85f, utilRect.y, 180f, 24f);
            
            // Layout buttons beautifully
            Rect selectWlRect = new Rect(utilRect.x + 275f, utilRect.y, 110f, 24f);
            Rect selectBlRect = new Rect(utilRect.x + 390f, utilRect.y, 110f, 24f);
            Rect resetBtnRect = new Rect(utilRect.xMax - 110f, utilRect.y, 110f, 24f);

            Widgets.Label(searchLabelRect, "GC_Search".Translate());
            searchFilter = Widgets.TextField(searchFieldRect, searchFilter);

            // 1. Select All Whitelist (Acts on filtered subset)
            if (Widgets.ButtonText(selectWlRect, "GC_SelectAllWL".Translate(), true, true, true))
            {
                foreach (var g in filteredGenes)
                {
                    if (!settings.whitelistedGeneNames.Contains(g.defName))
                    {
                        settings.whitelistedGeneNames.Add(g.defName);
                    }
                    settings.blacklistedGeneNames.Remove(g.defName);
                }
                settings.InitializeHashes();
                Messages.Message("Whitelisted all currently shown genes.", MessageTypeDefOf.TaskCompletion, false);
            }

            // 2. Select All Blacklist (Acts on filtered subset)
            if (Widgets.ButtonText(selectBlRect, "GC_SelectAllBL".Translate(), true, true, true))
            {
                foreach (var g in filteredGenes)
                {
                    if (!settings.blacklistedGeneNames.Contains(g.defName))
                    {
                        settings.blacklistedGeneNames.Add(g.defName);
                    }
                    settings.whitelistedGeneNames.Remove(g.defName);
                }
                settings.InitializeHashes();
                Messages.Message("Blacklisted all currently shown genes.", MessageTypeDefOf.TaskCompletion, false);
            }

            // 3. Reset All Settings
            if (Widgets.ButtonText(resetBtnRect, "GC_Reset".Translate()))
            {
                settings.blacklistedGeneNames.Clear();
                settings.whitelistedGeneNames.Clear();
                settings.customGeneChances.Clear();
                settings.InitializeHashes();
                Messages.Message("GC_ResetSuccess".Translate(), MessageTypeDefOf.TaskCompletion, false);
            }

            listingStandard.Gap(10f);

            // Table Header with smooth shading
            Rect headerRect = listingStandard.GetRect(26f);
            Widgets.DrawBoxSolid(headerRect, new Color(0.2f, 0.2f, 0.2f, 0.5f));
            
            Rect hIconRect = new Rect(headerRect.x + 5f, headerRect.y + 3f, 30f, 20f);
            Rect hNameRect = new Rect(headerRect.x + 45f, headerRect.y + 3f, 200f, 20f);
            Rect hButtonsRect = new Rect(headerRect.xMax - 230f, headerRect.y + 3f, 220f, 20f);

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(hIconRect, "GC_Icon".Translate());
            Widgets.Label(hNameRect, "GC_GeneName".Translate());
            
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(hButtonsRect.x, hButtonsRect.y, 105f, 20f), "GC_Whitelist".Translate());
            Widgets.Label(new Rect(hButtonsRect.x + 115f, hButtonsRect.y, 105f, 20f), "GC_Blacklist".Translate());
            Text.Anchor = TextAnchor.UpperLeft; // Reset anchor

            listingStandard.Gap(5f);

            // Scroll view area for Genes list
            Rect viewRect = listingStandard.GetRect(inRect.height - listingStandard.CurHeight - 30f);
            
            // Expanded row height to accommodate Custom Chance settings underneath
            float rowHeight = 54f;
            Rect scrollContentRect = new Rect(0f, 0f, viewRect.width - 16f, filteredGenes.Count * rowHeight);

            Widgets.BeginScrollView(viewRect, ref scrollPosition, scrollContentRect);

            float curY = 0f;
            for (int i = 0; i < filteredGenes.Count; i++)
            {
                GeneDef gene = filteredGenes[i];
                Rect rowRect = new Rect(0f, curY, scrollContentRect.width, rowHeight - 2f);
                
                // Zebra striping for better readability
                if (i % 2 == 0)
                {
                    Widgets.DrawBoxSolid(rowRect, new Color(0.15f, 0.15f, 0.15f, 0.3f));
                }

                // Smooth hover highlights
                Widgets.DrawHighlightIfMouseover(rowRect);

                // --- LINE 1: Info and Lists Buttons ---
                Rect line1Rect = new Rect(rowRect.x, rowRect.y, rowRect.width, 28f);

                // Icon drawing
                Rect iconRect = new Rect(line1Rect.x + 5f, line1Rect.y + 2f, 24f, 24f);
                if (gene.Icon != null)
                {
                    GUI.color = gene.IconColor;
                    Widgets.DrawTextureFitted(iconRect, gene.Icon, 1f);
                    GUI.color = Color.white;
                }
                else
                {
                    Widgets.DrawBox(iconRect);
                }

                // Gene Label & description tooltip
                Rect nameRect = new Rect(line1Rect.x + 45f, line1Rect.y + 4f, 250f, 24f);
                string labelText = gene.label.CapitalizeFirst() ?? gene.defName;
                Widgets.Label(nameRect, labelText);
                TooltipHandler.TipRegion(rowRect, $"{labelText}\n\n{gene.description ?? "No description available."}\n\nDefName: {gene.defName}");

                // Whitelist / Blacklist Buttons container
                Rect btnAreaRect = new Rect(line1Rect.xMax - 230f, line1Rect.y + 2f, 220f, 24f);
                Rect wlBtnRect = new Rect(btnAreaRect.x, btnAreaRect.y, 105f, 22f);
                Rect blBtnRect = new Rect(btnAreaRect.x + 115f, btnAreaRect.y, 105f, 22f);

                bool isWhitelisted = settings.whitelistHash.Contains(gene.defName);
                bool isBlacklisted = settings.blacklistHash.Contains(gene.defName);

                // Whitelist Button Styling (Green when active)
                if (isWhitelisted)
                {
                    GUI.color = new Color(0.4f, 0.8f, 0.4f, 1f);
                }
                if (Widgets.ButtonText(wlBtnRect, isWhitelisted ? "GC_Whitelisted".Translate() : "GC_Whitelist".Translate()))
                {
                    if (isWhitelisted)
                    {
                        settings.whitelistedGeneNames.Remove(gene.defName);
                    }
                    else
                    {
                        settings.whitelistedGeneNames.Add(gene.defName);
                        // Mutual exclusivity with Blacklist
                        if (isBlacklisted)
                        {
                            settings.blacklistedGeneNames.Remove(gene.defName);
                        }
                    }
                    settings.InitializeHashes();
                }
                GUI.color = Color.white;

                // Blacklist Button Styling (Red when active)
                if (isBlacklisted)
                {
                    GUI.color = new Color(0.8f, 0.4f, 0.4f, 1f);
                }
                if (Widgets.ButtonText(blBtnRect, isBlacklisted ? "GC_Blacklisted".Translate() : "GC_Blacklist".Translate()))
                {
                    if (isBlacklisted)
                    {
                        settings.blacklistedGeneNames.Remove(gene.defName);
                    }
                    else
                    {
                        settings.blacklistedGeneNames.Add(gene.defName);
                        // Mutual exclusivity with Whitelist
                        if (isWhitelisted)
                        {
                            settings.whitelistedGeneNames.Remove(gene.defName);
                        }
                    }
                    settings.InitializeHashes();
                }
                GUI.color = Color.white;

                // --- LINE 2: Custom Probability Configurations ---
                Rect line2Rect = new Rect(rowRect.x + 45f, rowRect.y + 28f, rowRect.width - 50f, 22f);
                
                bool hasCustomChance = settings.customGeneChances.ContainsKey(gene.defName);
                bool newCustomChance = hasCustomChance;

                // Custom Chance checkbox
                Widgets.CheckboxLabeled(new Rect(line2Rect.x, line2Rect.y, 140f, 20f), "GC_CustomChance".Translate(), ref newCustomChance);

                if (newCustomChance != hasCustomChance)
                {
                    if (newCustomChance)
                    {
                        settings.customGeneChances[gene.defName] = 0.5f;
                    }
                    else
                    {
                        settings.customGeneChances.Remove(gene.defName);
                    }
                }

                if (newCustomChance)
                {
                    float currentChance = settings.customGeneChances[gene.defName];
                    Rect sliderRect = new Rect(line2Rect.x + 150f, line2Rect.y + 2f, 150f, 18f);
                    Rect percentRect = new Rect(sliderRect.xMax + 10f, line2Rect.y, 50f, 20f);

                    float newChance = Widgets.HorizontalSlider(sliderRect, currentChance, 0f, 1f, true);
                    settings.customGeneChances[gene.defName] = newChance;

                    Widgets.Label(percentRect, $"{newChance * 100f:F0}%");
                }
                else
                {
                    Rect defaultMsgRect = new Rect(line2Rect.x + 150f, line2Rect.y, 250f, 20f);
                    GUI.color = Color.gray;
                    Widgets.Label(defaultMsgRect, "GC_DefaultChance".Translate());
                    GUI.color = Color.white;
                }

                curY += rowHeight;
            }

            Widgets.EndScrollView();
            listingStandard.End();

            // Auto-save changes
            settings.Write();
        }
    }
}
