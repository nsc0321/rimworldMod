using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace HiveMod
{
    public class Window_HiveEvolution : Window
    {
        private IHiveCore overmind;
        private GameComponent_HiveEvolution evolutionComponent;
        private Vector2 scrollPosition = Vector2.zero;

        public override Vector2 InitialSize => new Vector2(500f, 600f);

        public Window_HiveEvolution(IHiveCore overmind)
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
            Widgets.Label(new Rect(0f, 0f, inRect.width, 35f), "Hive Evolution (Research)");
            Text.Font = GameFont.Small;

            float currentY = 40f;
            Widgets.Label(new Rect(0f, currentY, inRect.width, 24f), $"Available Energy: {overmind.CurrentEnergy:F0}");
            currentY += 30f;

            // Get all parts
            var allParts = DefDatabase<GeneDef>.AllDefs
                .Where(d => d.HasModExtension<DefModExtension_HivePart>())
                .ToList();

            Rect viewRect = new Rect(0, 0, inRect.width - 20f, allParts.Count * 80f);
            Rect scrollRect = new Rect(0, currentY, inRect.width, inRect.height - currentY - 50f);

            Widgets.BeginScrollView(scrollRect, ref scrollPosition, viewRect);
            
            float listY = 0f;
            foreach (var partDef in allParts)
            {
                var ext = partDef.GetModExtension<DefModExtension_HivePart>();
                bool isUnlocked = evolutionComponent.IsUnlocked(partDef);

                Rect rowRect = new Rect(0f, listY, viewRect.width, 70f);
                Widgets.DrawBoxSolid(rowRect, new Color(0.2f, 0.2f, 0.2f, 0.5f));

                Rect labelRect = new Rect(5f, listY + 5f, 250f, 25f);
                Text.Font = GameFont.Small;
                Widgets.Label(labelRect, partDef.label.CapitalizeFirst() + $" ({ext.category})");
                
                Rect descRect = new Rect(5f, listY + 30f, 300f, 35f);
                Text.Font = GameFont.Tiny;
                Widgets.Label(descRect, partDef.description);
                Text.Font = GameFont.Small;

                Rect buttonRect = new Rect(viewRect.width - 130f, listY + 15f, 120f, 40f);
                if (isUnlocked)
                {
                    GUI.color = Color.gray;
                    Widgets.ButtonText(buttonRect, "Unlocked");
                    GUI.color = Color.white;
                }
                else
                {
                    if (Widgets.ButtonText(buttonRect, $"Unlock\n({ext.researchCostEnergy} Energy)"))
                    {
                        if (overmind.CurrentEnergy >= ext.researchCostEnergy)
                        {
                            overmind.CurrentEnergy -= ext.researchCostEnergy;
                            evolutionComponent.Unlock(partDef);
                            Messages.Message($"Unlocked: {partDef.label}", MessageTypeDefOf.PositiveEvent);
                        }
                        else
                        {
                            Messages.Message("Not enough Energy to research this part.", MessageTypeDefOf.RejectInput);
                        }
                    }
                }
                listY += 80f;
            }

            Widgets.EndScrollView();
        }
    }
}
