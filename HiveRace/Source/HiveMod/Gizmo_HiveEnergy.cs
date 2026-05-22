using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace HiveMod
{
    [StaticConstructorOnStartup]
    public class Gizmo_HiveEnergy : Gizmo
    {
        public Gene_Deploy gene;
        
        private static readonly Texture2D EnergyBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.3f, 0.1f, 0.4f));
        private static readonly Texture2D EmptyBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.03f, 0.035f, 0.05f));
        private static readonly Texture2D EnergyBarHighlightTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.4f, 0.2f, 0.5f));

        public Gizmo_HiveEnergy(Gene_Deploy gene)
        {
            this.gene = gene;
            this.Order = -100f;
        }

        public override float GetWidth(float maxWidth)
        {
            return 140f;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Rect rect2 = rect.ContractedBy(6f);
            Widgets.DrawWindowBackground(rect);
            
            Text.Font = GameFont.Small;
            Rect labelRect = new Rect(rect2.x, rect2.y, rect2.width, 24f);
            Widgets.Label(labelRect, "Hive Energy");
            
            Rect barRect = new Rect(rect2.x, rect2.y + 24f, rect2.width, rect2.height - 24f);
            float fillPercent = Mathf.Clamp01(gene.CurrentEnergy / gene.MaxEnergy);
            
            Widgets.FillableBar(barRect, fillPercent, EnergyBarTex, EmptyBarTex, true);
            
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(barRect, gene.CurrentEnergy.ToString("F0") + " / " + gene.MaxEnergy.ToString("F0"));
            Text.Anchor = TextAnchor.UpperLeft;
            
            if (Mouse.IsOver(rect))
            {
                Widgets.DrawHighlight(rect);
                TooltipHandler.TipRegion(rect, "Hive energy generated from nearby creep.\n\nEnergy is used to spawn new units, generate biomass, and research new evolutions for the swarm.");
            }
            
            return new GizmoResult(GizmoState.Clear);
        }
    }
}
