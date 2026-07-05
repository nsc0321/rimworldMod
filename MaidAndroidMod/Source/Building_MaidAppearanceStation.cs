using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace MaidAndroidMod
{
    // ==================== 1. MODIFICATION UTILITY ====================
    public static class MaidModificationUtility
    {
        public class ModuleDef
        {
            public string defName;
            public string label;
            public string description;
            public string part; // "Head", "Body", "Arm", "Leg"

            public ModuleDef(string defName, string label, string description, string part)
            {
                this.defName = defName;
                this.label = label;
                this.description = description;
                this.part = part;
            }
        }

        public class ResourceCost
        {
            public int steel;
            public int plasteel;
            public int gold;
            public int components;
            public int advancedComponents;

            public ResourceCost(int steel, int plasteel, int gold, int components, int advancedComponents)
            {
                this.steel = steel;
                this.plasteel = plasteel;
                this.gold = gold;
                this.components = components;
                this.advancedComponents = advancedComponents;
            }

            public static ResourceCost GetCost(int tier)
            {
                switch (tier)
                {
                    case 1: return new ResourceCost(20, 0, 0, 1, 0); // Basic
                    case 2: return new ResourceCost(30, 10, 0, 2, 0); // Standard
                    case 3: return new ResourceCost(40, 20, 0, 2, 1); // High
                    case 4: return new ResourceCost(50, 30, 5, 2, 2); // Ultra
                    default: return new ResourceCost(0, 0, 0, 0, 0);
                }
            }
        }

        public static readonly List<ModuleDef> AllModules = new List<ModuleDef>
        {
            // Head
            new ModuleDef("MaidMod_OpticalLens", "광학 렌즈", "사격 정확도를 향상시킵니다.", "Head"),
            new ModuleDef("MaidMod_TasteSensor", "미각 센서", "조리 속도를 가속시킵니다.", "Head"),
            new ModuleDef("MaidMod_SmellSensor", "후각 센서", "조리 시 식중독 확률을 대폭 감소시킵니다.", "Head"),
            new ModuleDef("MaidMod_Armor_Head", "헤드 장갑판", "머리 부분 장갑을 보강하여 방어도를 높입니다.", "Head"),

            // Body
            new ModuleDef("MaidMod_StorageModule", "저장 모듈", "운반 시 최대 수송 용량을 확장합니다.", "Body"),
            new ModuleDef("MaidMod_ShockAbsorber", "충격 흡수 장치", "피해 흡수 판을 내장하여 둔탁함 방어력을 높입니다.", "Body"),
            new ModuleDef("MaidMod_Battery", "배터리 최적화", "연산 소모 대기 전력을 절약하여 배터리 지속시간을 늘립니다.", "Body"),
            new ModuleDef("MaidMod_FastCharger", "고속 충전기", "충전 도킹 시 에너지 충전 속도를 가속합니다.", "Body"),
            new ModuleDef("MaidMod_Armor_Body", "몸통 장갑판", "흉부 전면 장갑을 보강하여 방어도를 높입니다.", "Body"),

            // Arm
            new ModuleDef("MaidMod_PrecisionSensor", "정밀 센서", "정밀 팔 제어 서보를 강화해 전체적인 작업 속도를 가속합니다.", "Arm"),
            new ModuleDef("MaidMod_PowerActuator", "동력 장치", "근접 공격 명중률과 물리 관통을 대폭 증가시킵니다.", "Arm"),
            new ModuleDef("MaidMod_Armor_Arm", "양팔 장갑판", "양팔 장갑을 보강하여 방어도를 높입니다.", "Arm"),

            // Leg
            new ModuleDef("MaidMod_MobilityEquipment", "기동 장비", "다리 유압 구동계를 강화해 이동력을 향상시킵니다.", "Leg"),
            new ModuleDef("MaidMod_Armor_Leg", "다리 장갑판", "다리 장갑을 보강하여 방어도를 높입니다.", "Leg")
        };

        public static int GetTierFromSeverity(float severity)
        {
            if (severity >= 3.9f) return 4;
            if (severity >= 2.9f) return 3;
            if (severity >= 1.9f) return 2;
            if (severity >= 0.9f) return 1;
            return 0;
        }

        public static (string defName, int tier) GetInstalledModuleInSlot(Pawn pawn, string slot)
        {
            if (pawn?.health?.hediffSet == null) return (null, 0);

            foreach (var hediff in pawn.health.hediffSet.hediffs)
            {
                if (hediff.def == null) continue;

                var module = AllModules.FirstOrDefault(m => m.defName == hediff.def.defName);
                if (module != null && module.part == slot)
                {
                    int tier = GetTierFromSeverity(hediff.Severity);
                    return (module.defName, tier);
                }
            }

            return (null, 0);
        }

        public static bool IsResearchFinished(string researchDefName)
        {
            var proj = DefDatabase<ResearchProjectDef>.GetNamed(researchDefName, false);
            return proj != null && proj.IsFinished;
        }

        public static int GetAvailableResourceCount(Map map, ThingDef def)
        {
            if (map == null || def == null) return 0;
            return map.resourceCounter.GetCount(def);
        }

        public static bool HasMaterials(Map map, ResourceCost cost)
        {
            if (map == null) return false;
            if (GetAvailableResourceCount(map, ThingDefOf.Steel) < cost.steel) return false;
            if (GetAvailableResourceCount(map, ThingDefOf.Plasteel) < cost.plasteel) return false;
            if (GetAvailableResourceCount(map, ThingDefOf.Gold) < cost.gold) return false;
            if (GetAvailableResourceCount(map, ThingDefOf.ComponentIndustrial) < cost.components) return false;
            if (GetAvailableResourceCount(map, ThingDefOf.ComponentSpacer) < cost.advancedComponents) return false;
            return true;
        }

        public static ResourceCost CalculateNetCost(Pawn pawn, Dictionary<string, (string defName, int tier)> selectedMods)
        {
            int totalSteel = 0;
            int totalPlasteel = 0;
            int totalGold = 0;
            int totalComponents = 0;
            int totalAdvanced = 0;

            foreach (var kv in selectedMods)
            {
                string slot = kv.Key;
                string selDefName = kv.Value.defName;
                int selTier = kv.Value.tier;

                if (selDefName == null || selTier == 0) continue;

                var installed = GetInstalledModuleInSlot(pawn, slot);
                ResourceCost neededCost = ResourceCost.GetCost(selTier);

                if (installed.defName == selDefName)
                {
                    if (selTier > installed.tier)
                    {
                        ResourceCost prevCost = ResourceCost.GetCost(installed.tier);
                        totalSteel += Math.Max(0, neededCost.steel - prevCost.steel);
                        totalPlasteel += Math.Max(0, neededCost.plasteel - prevCost.plasteel);
                        totalGold += Math.Max(0, neededCost.gold - prevCost.gold);
                        totalComponents += Math.Max(0, neededCost.components - prevCost.components);
                        totalAdvanced += Math.Max(0, neededCost.advancedComponents - prevCost.advancedComponents);
                    }
                }
                else
                {
                    totalSteel += neededCost.steel;
                    totalPlasteel += neededCost.plasteel;
                    totalGold += neededCost.gold;
                    totalComponents += neededCost.components;
                    totalAdvanced += neededCost.advancedComponents;
                }
            }

            return new ResourceCost(totalSteel, totalPlasteel, totalGold, totalComponents, totalAdvanced);
        }

        public static void ConsumeResources(Map map, IntVec3 pos, ResourceCost cost)
        {
            if (map == null) return;

            ConsumeSpecificResource(map, pos, ThingDefOf.Steel, cost.steel);
            if (cost.plasteel > 0) ConsumeSpecificResource(map, pos, ThingDefOf.Plasteel, cost.plasteel);
            if (cost.gold > 0) ConsumeSpecificResource(map, pos, ThingDefOf.Gold, cost.gold);
            if (cost.components > 0) ConsumeSpecificResource(map, pos, ThingDefOf.ComponentIndustrial, cost.components);
            if (cost.advancedComponents > 0) ConsumeSpecificResource(map, pos, ThingDefOf.ComponentSpacer, cost.advancedComponents);
        }

        private static void ConsumeSpecificResource(Map map, IntVec3 pos, ThingDef def, int amountNeeded)
        {
            if (amountNeeded <= 0) return;

            var candidates = new List<Thing>();
            foreach (var t in map.listerThings.ThingsOfDef(def))
            {
                if (!t.Position.Fogged(map) && !t.IsForbidden(Faction.OfPlayer))
                {
                    candidates.Add(t);
                }
            }

            candidates.Sort((t1, t2) => t1.Position.DistanceToSquared(pos).CompareTo(t2.Position.DistanceToSquared(pos)));

            int remaining = amountNeeded;
            foreach (var thing in candidates)
            {
                if (remaining <= 0) break;

                int toTake = Math.Min(thing.stackCount, remaining);
                thing.SplitOff(toTake).Destroy();
                remaining -= toTake;
            }
        }

        public static void ClearModifications(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null) return;

            var toRemove = new List<Hediff>();
            foreach (var hediff in pawn.health.hediffSet.hediffs)
            {
                if (hediff.def != null && hediff.def.defName.StartsWith("MaidMod_"))
                {
                    toRemove.Add(hediff);
                }
            }

            foreach (var hediff in toRemove)
            {
                pawn.health.RemoveHediff(hediff);
            }
        }

        public static void ApplyModifications(Pawn pawn, Dictionary<string, (string defName, int tier)> selectedMods)
        {
            ClearModifications(pawn);

            if (pawn.health == null) return;

            foreach (var kv in selectedMods)
            {
                string defName = kv.Value.defName;
                int tier = kv.Value.tier;

                if (defName == null || tier == 0) continue;

                var hdef = DefDatabase<HediffDef>.GetNamed(defName, false);
                if (hdef != null)
                {
                    Hediff hediff = HediffMaker.MakeHediff(hdef, pawn);
                    hediff.Severity = (float)tier;
                    pawn.health.AddHediff(hediff);
                }
            }

            pawn.Drawer.renderer.SetAllGraphicsDirty();
        }
    }

    // ==================== 2. CUSTOMIZATION DIALOG WINDOW ====================
    public class Dialog_MaidCustomization : Window
    {
        private Pawn maid;
        private int selectedSkin;
        private Dictionary<string, (string defName, int tier)> selectedMods = new Dictionary<string, (string defName, int tier)>();

        public override Vector2 InitialSize => new Vector2(780f, 680f);

        public Dialog_MaidCustomization(Pawn maid)
        {
            this.maid = maid;
            this.closeOnAccept = false;
            this.closeOnCancel = true;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;

            // Load skin state
            var skinComp = maid.GetComp<CompMaidSkin>();
            if (skinComp != null)
            {
                selectedSkin = skinComp.skinIndex;
            }

            // Load modification state
            selectedMods["Head"] = MaidModificationUtility.GetInstalledModuleInSlot(maid, "Head");
            selectedMods["Body"] = MaidModificationUtility.GetInstalledModuleInSlot(maid, "Body");
            selectedMods["Arm"] = MaidModificationUtility.GetInstalledModuleInSlot(maid, "Arm");
            selectedMods["Leg"] = MaidModificationUtility.GetInstalledModuleInSlot(maid, "Leg");
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Title
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(0f, 0f, inRect.width, 35f);
            Widgets.Label(titleRect, "메이드 외장 튜닝 및 성능 개조 시스템");
            Text.Font = GameFont.Small;

            float y = 45f;

            // Divide the space into Left and Right Columns
            float totalHeight = inRect.height - 110f; // leave space for bottom buttons
            float leftWidth = 380f;
            float rightWidth = inRect.width - leftWidth - 15f;

            Rect leftRect = new Rect(0f, y, leftWidth, totalHeight);
            Rect rightRect = new Rect(leftWidth + 15f, y, rightWidth, totalHeight);

            // RENDER LEFT SIDE: SCHEMATIC
            DrawSchematic(leftRect);

            // RENDER RIGHT SIDE: SKIN & COST SUMMARY
            DrawConfigPanel(rightRect);

            // BOTTOM BUTTONS
            DrawBottomButtons(inRect, leftWidth);
        }

        private void DrawSchematic(Rect rect)
        {
            // Background box for the schematic
            Widgets.DrawBoxSolidWithOutline(rect, new Color(0.1f, 0.1f, 0.1f, 0.8f), new Color(0.3f, 0.3f, 0.3f));

            // Section Title
            Rect sectionTitleRect = new Rect(rect.x + 10f, rect.y + 10f, rect.width - 20f, 25f);
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            Widgets.Label(sectionTitleRect, "● 외장 개조 설계도 (Exterior Schematic)");
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            // Draw Pawn Preview in the center
            Rect pawnRect = new Rect(rect.x + 110f, rect.y + 130f, 160f, 240f);
            Widgets.ThingIcon(pawnRect, maid);

            float parentX = rect.x;
            float parentY = rect.y;

            // Target connection points on the pawn
            Vector2 headTarget = new Vector2(parentX + 190f, parentY + 160f);
            Vector2 bodyTarget = new Vector2(parentX + 190f, parentY + 240f);
            Vector2 armTarget = new Vector2(parentX + 145f, parentY + 240f);
            Vector2 legTarget = new Vector2(parentX + 190f, parentY + 320f);

            // Head Slot (Top-Right): x = 275f, y = 95f
            Rect headSlot = new Rect(parentX + 275f, parentY + 95f, 90f, 55f);
            DrawSlotButton("Head", headSlot, new Vector2(headSlot.x, headSlot.y + 27f), headTarget);

            // Body Slot (Middle-Right): x = 275f, y = 220f
            Rect bodySlot = new Rect(parentX + 275f, parentY + 220f, 90f, 55f);
            DrawSlotButton("Body", bodySlot, new Vector2(bodySlot.x, bodySlot.y + 27f), bodyTarget);

            // Leg Slot (Bottom-Right): x = 275f, y = 345f
            Rect legSlot = new Rect(parentX + 275f, parentY + 345f, 90f, 55f);
            DrawSlotButton("Leg", legSlot, new Vector2(legSlot.x, legSlot.y + 27f), legTarget);

            // Arm Slot (Middle-Left): x = 15f, y = 220f
            Rect armSlot = new Rect(parentX + 15f, parentY + 220f, 90f, 55f);
            DrawSlotButton("Arm", armSlot, new Vector2(armSlot.xMax, armSlot.y + 27f), armTarget);
        }

        private void DrawSlotButton(string slot, Rect slotRect, Vector2 lineStart, Vector2 lineEnd)
        {
            // Draw connection line
            Widgets.DrawLine(lineStart, lineEnd, new Color(0.4f, 0.4f, 0.4f), 1.5f);

            var installed = selectedMods[slot];
            bool hasMod = installed.defName != null && installed.tier > 0;

            Color bg = new Color(0.15f, 0.15f, 0.15f);
            Color border = new Color(0.3f, 0.3f, 0.3f);

            if (hasMod)
            {
                switch (installed.tier)
                {
                    case 1: // Basic
                        bg = new Color(0.18f, 0.15f, 0.12f);
                        border = new Color(0.5f, 0.35f, 0.2f);
                        break;
                    case 2: // Standard
                        bg = new Color(0.15f, 0.18f, 0.18f);
                        border = new Color(0.4f, 0.5f, 0.5f);
                        break;
                    case 3: // High
                        bg = new Color(0.2f, 0.18f, 0.12f);
                        border = new Color(0.6f, 0.5f, 0.2f);
                        break;
                    case 4: // Ultra
                        bg = new Color(0.12f, 0.2f, 0.2f);
                        border = new Color(0.2f, 0.6f, 0.6f);
                        break;
                }
            }

            Widgets.DrawBoxSolidWithOutline(slotRect, bg, border);

            if (Mouse.IsOver(slotRect))
            {
                Widgets.DrawHighlight(slotRect);
            }

            if (Widgets.ButtonInvisible(slotRect))
            {
                OpenModuleSelectionMenu(slot);
            }

            string label;
            string tierStr = "";
            if (hasMod)
            {
                var modDef = MaidModificationUtility.AllModules.FirstOrDefault(m => m.defName == installed.defName);
                label = modDef != null ? modDef.label : "개조";
                switch (installed.tier)
                {
                    case 1: tierStr = "기초"; break;
                    case 2: tierStr = "표준"; break;
                    case 3: tierStr = "고급"; break;
                    case 4: tierStr = "초고급"; break;
                }
            }
            else
            {
                label = "+ 장착";
            }

            Text.Font = GameFont.Tiny;
            Rect labelRect = new Rect(slotRect.x + 4f, slotRect.y + 8f, slotRect.width - 8f, 20f);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(labelRect, label);

            if (tierStr != "")
            {
                Rect tierRect = new Rect(slotRect.x + 4f, slotRect.y + 28f, slotRect.width - 8f, 20f);
                GUI.color = new Color(0.8f, 0.8f, 0.8f);
                Widgets.Label(tierRect, $"({tierStr})");
                GUI.color = Color.white;
            }
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            string tip = GetTooltipForSlot(slot, installed.defName, installed.tier);
            TooltipHandler.TipRegion(slotRect, tip);
        }

        private string GetTooltipForSlot(string slot, string defName, int tier)
        {
            string slotLabel = "";
            switch (slot)
            {
                case "Head": slotLabel = "머리 (Head)"; break;
                case "Body": slotLabel = "몸통 (Body)"; break;
                case "Arm": slotLabel = "양팔 (Arm)"; break;
                case "Leg": slotLabel = "양다리 (Leg)"; break;
            }

            if (defName == null || tier == 0)
            {
                return $"슬롯: {slotLabel}\n상태: 비어 있음\n\n클릭하여 새로운 외장 모듈을 장착하세요.";
            }

            var modDef = MaidModificationUtility.AllModules.FirstOrDefault(m => m.defName == defName);
            string modName = modDef != null ? modDef.label : "알 수 없음";
            string desc = modDef != null ? modDef.description : "";
            string tierStr = "";
            switch (tier)
            {
                case 1: tierStr = "기초 (Basic)"; break;
                case 2: tierStr = "표준 (Standard)"; break;
                case 3: tierStr = "고급 (High)"; break;
                case 4: tierStr = "초고급 (Ultra)"; break;
            }

            string statsDesc = GetStatsDescription(defName, tier);
            return $"슬롯: {slotLabel}\n모듈: {modName}\n등급: {tierStr}\n설명: {desc}\n\n효과:\n{statsDesc}\n\n클릭하여 변경 또는 장착을 해제합니다.";
        }

        private string GetStatsDescription(string defName, int tier)
        {
            switch (defName)
            {
                case "MaidMod_OpticalLens":
                    float acc = tier == 1 ? 1.0f : (tier == 2 ? 2.0f : (tier == 3 ? 3.0f : 4.5f));
                    return $" - 사격 정확도: +{acc:F1}";
                case "MaidMod_TasteSensor":
                    float cook = tier == 1 ? 0.15f : (tier == 2 ? 0.30f : (tier == 3 ? 0.45f : 0.60f));
                    return $" - 조리 속도: +{cook * 100:F0}%";
                case "MaidMod_SmellSensor":
                    float poison = tier == 1 ? -0.10f : (tier == 2 ? -0.25f : (tier == 3 ? -0.50f : -0.75f));
                    return $" - 식중독 확률: {poison * 100:F0}%";
                case "MaidMod_StorageModule":
                    int cap = tier == 1 ? 15 : (tier == 2 ? 30 : (tier == 3 ? 50 : 75));
                    return $" - 운반 수량: +{cap}";
                case "MaidMod_ShockAbsorber":
                    float blunt = tier == 1 ? 0.10f : (tier == 2 ? 0.20f : (tier == 3 ? 0.30f : 0.45f));
                    return $" - 둔탁함 방어도: +{blunt * 100:F0}%";
                case "MaidMod_Battery":
                    float drain = tier == 1 ? -0.10f : (tier == 2 ? -0.20f : (tier == 3 ? -0.30f : -0.40f));
                    return $" - 전력 소모량: {drain * 100:F0}%";
                case "MaidMod_FastCharger":
                    int chg = tier == 1 ? 15 : (tier == 2 ? 40 : (tier == 3 ? 70 : 100));
                    return $" - 대전 충전 속도: +{chg}%";
                case "MaidMod_PrecisionSensor":
                    float global = tier == 1 ? 0.10f : (tier == 2 ? 0.20f : (tier == 3 ? 0.30f : 0.45f));
                    return $" - 전역 작업 속도: +{global * 100:F0}%";
                case "MaidMod_PowerActuator":
                    float hit = tier == 1 ? 1.0f : (tier == 2 ? 2.0f : (tier == 3 ? 3.0f : 4.5f));
                    float ap = tier == 1 ? 0.10f : (tier == 2 ? 0.20f : (tier == 3 ? 0.30f : 0.45f));
                    return $" - 근접 명중률: +{hit:F1}\n - 근접 장갑 관통: +{ap * 100:F0}%";
                case "MaidMod_MobilityEquipment":
                    float speed = tier == 1 ? 0.4f : (tier == 2 ? 0.8f : (tier == 3 ? 1.2f : 1.6f));
                    return $" - 이동 속도: +{speed:F2}칸/s";
                case "MaidMod_Armor_Head":
                case "MaidMod_Armor_Body":
                case "MaidMod_Armor_Arm":
                case "MaidMod_Armor_Leg":
                    float armVal = tier == 1 ? 0.08f : (tier == 2 ? 0.15f : (tier == 3 ? 0.25f : 0.40f));
                    return $" - 날카로움 방어도: +{armVal * 100:F0}%\n - 둔탁함 방어도: +{armVal * 100:F0}%\n - 열기 방어도: +{armVal * 100:F0}%";
                default:
                    return "";
            }
        }

        private void OpenModuleSelectionMenu(string slot)
        {
            var options = new List<FloatMenuOption>();

            options.Add(new FloatMenuOption("장착 해제 (None)", delegate
            {
                selectedMods[slot] = (null, 0);
            }));

            foreach (var module in MaidModificationUtility.AllModules)
            {
                if (module.part != slot)
                {
                    // Common modules can be applied to any slot
                    if (!(module.defName.StartsWith("MaidMod_Armor_") && 
                          ((slot == "Head" && module.defName == "MaidMod_Armor_Head") ||
                           (slot == "Body" && module.defName == "MaidMod_Armor_Body") ||
                           (slot == "Arm" && module.defName == "MaidMod_Armor_Arm") ||
                           (slot == "Leg" && module.defName == "MaidMod_Armor_Leg"))))
                    {
                        continue;
                    }
                }

                for (int tier = 1; tier <= 4; tier++)
                {
                    string tierName = "";
                    string researchDef = "";
                    switch (tier)
                    {
                        case 1: tierName = "기초 (Basic)"; researchDef = "BasicMechtech"; break;
                        case 2: tierName = "표준 (Standard)"; researchDef = "StandardMechtech"; break;
                        case 3: tierName = "고급 (High)"; researchDef = "HighMechtech"; break;
                        case 4: tierName = "초고급 (Ultra)"; researchDef = "UltraMechtech"; break;
                    }

                    bool researched = MaidModificationUtility.IsResearchFinished(researchDef);
                    
                    string statsDesc = GetStatsDescription(module.defName, tier)
                        .Replace("\n", ", ")
                        .Replace(" - ", " ")
                        .Replace("  ", " ")
                        .Trim(' ', '-', ',');
                    
                    string optionLabel = $"{module.label} - {tierName} ({statsDesc})";

                    var cost = MaidModificationUtility.ResourceCost.GetCost(tier);
                    string costText = $" [강철:{cost.steel}";
                    if (cost.plasteel > 0) costText += $", 플라스틸:{cost.plasteel}";
                    if (cost.gold > 0) costText += $", 금:{cost.gold}";
                    if (cost.components > 0) costText += $", 부품:{cost.components}";
                    if (cost.advancedComponents > 0) costText += $", 고급부품:{cost.advancedComponents}";
                    costText += "]";

                    optionLabel += costText;

                    string finalLabel = optionLabel;
                    int finalTier = tier;
                    string finalDefName = module.defName;

                    if (!researched)
                    {
                        options.Add(new FloatMenuOption($"{finalLabel} (미연구: {researchDef} 필요)", null)
                        {
                            Disabled = true
                        });
                    }
                    else
                    {
                        options.Add(new FloatMenuOption(finalLabel, delegate
                        {
                            selectedMods[slot] = (finalDefName, finalTier);
                        }));
                    }
                }
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void DrawConfigPanel(Rect rect)
        {
            // --- SECTION 1: SKIN CUSTOMIZATION ---
            Widgets.DrawBoxSolidWithOutline(new Rect(rect.x, rect.y, rect.width, 95f), new Color(0.15f, 0.15f, 0.15f), new Color(0.3f, 0.3f, 0.3f));
            Rect skinLabelRect = new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, 25f);
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            Widgets.Label(skinLabelRect, "● 외장 스킨 선택 (외형 도색 변경)");
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            float skinWidth = (rect.width - 40f) / 2f;
            string[] skins = new string[] { "기본형 (Default)", "고풍형 (Antique)", "현대형 (Modern)", "정장형 (Formal)", "도우미형 (Custom)" };

            for (int i = 0; i < skins.Length; i++)
            {
                float curX = rect.x + 15f + (i % 2) * (skinWidth + 10f);
                float curY = rect.y + 35f + (i / 2) * 22f;
                Rect r = new Rect(curX, curY, skinWidth, 20f);
                if (Widgets.RadioButtonLabeled(r, skins[i], selectedSkin == i))
                {
                    selectedSkin = i;
                }
            }

            // --- SECTION 2: COSTS ---
            float yMat = rect.y + 110f;
            float matHeight = rect.height - 110f;
            Widgets.DrawBoxSolidWithOutline(new Rect(rect.x, yMat, rect.width, matHeight), new Color(0.12f, 0.12f, 0.12f), new Color(0.3f, 0.3f, 0.3f));

            Rect matTitleRect = new Rect(rect.x + 10f, yMat + 8f, rect.width - 20f, 25f);
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            Widgets.Label(matTitleRect, "● 개조 소모 자원 요약 (Material Cost Summary)");
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            var netCost = MaidModificationUtility.CalculateNetCost(maid, selectedMods);
            var map = maid.MapHeld;

            float curItemY = yMat + 35f;
            DrawMaterialRow(rect.x + 15f, ref curItemY, rect.width - 30f, "강철 (Steel)", netCost.steel, ThingDefOf.Steel, map);
            DrawMaterialRow(rect.x + 15f, ref curItemY, rect.width - 30f, "플라스틸 (Plasteel)", netCost.plasteel, ThingDefOf.Plasteel, map);
            DrawMaterialRow(rect.x + 15f, ref curItemY, rect.width - 30f, "금 (Gold)", netCost.gold, ThingDefOf.Gold, map);
            DrawMaterialRow(rect.x + 15f, ref curItemY, rect.width - 30f, "부품 (Component)", netCost.components, ThingDefOf.ComponentIndustrial, map);
            DrawMaterialRow(rect.x + 15f, ref curItemY, rect.width - 30f, "고급 부품 (Adv. Component)", netCost.advancedComponents, ThingDefOf.ComponentSpacer, map);

            float statusY = curItemY + 15f;
            Rect statusRect = new Rect(rect.x + 15f, statusY, rect.width - 30f, matHeight - (statusY - yMat) - 10f);

            bool enough = MaidModificationUtility.HasMaterials(map, netCost);

            Text.Font = GameFont.Tiny;
            if (netCost.steel == 0 && netCost.plasteel == 0 && netCost.gold == 0 && netCost.components == 0 && netCost.advancedComponents == 0)
            {
                GUI.color = new Color(0.6f, 0.8f, 0.6f);
                Widgets.Label(statusRect, "정보: 추가 설치되거나 상위 등급으로 업그레이드되는 모듈이 없어 추가 자원이 소모되지 않습니다.");
            }
            else if (enough)
            {
                GUI.color = Color.green;
                Widgets.Label(statusRect, "준비 완료: 개조에 필요한 자원이 맵의 보관 구역에 모두 준비되어 있습니다. '개조 적용'을 눌러 작업을 개시하세요.");
            }
            else
            {
                GUI.color = Color.red;
                Widgets.Label(statusRect, "오류: 개조를 수행하기 위해 필요한 일부 자원이 부족합니다. 식민지 보관 구역에 자원을 더 비치해야 합니다.");
            }
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }

        private void DrawMaterialRow(float x, ref float y, float width, string label, int needed, ThingDef def, Map map)
        {
            if (needed <= 0) return;

            int available = MaidModificationUtility.GetAvailableResourceCount(map, def);
            bool hasEnough = available >= needed;

            Rect rowRect = new Rect(x, y, width, 22f);
            Widgets.Label(new Rect(rowRect.x, rowRect.y, 160f, rowRect.height), label);

            Text.Anchor = TextAnchor.MiddleRight;
            string countText = $"{needed} / {available}";

            GUI.color = hasEnough ? Color.green : Color.red;
            Widgets.Label(new Rect(rowRect.x + 160f, rowRect.y, width - 160f, rowRect.height), countText);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;

            y += 24f;
        }

        private void DrawBottomButtons(Rect inRect, float leftWidth)
        {
            float y = inRect.height - 55f;

            Rect applyButton = new Rect(inRect.x + leftWidth / 2f - 90f, y, 180f, 40f);

            var netCost = MaidModificationUtility.CalculateNetCost(maid, selectedMods);
            var map = maid.MapHeld;
            bool enough = MaidModificationUtility.HasMaterials(map, netCost);

            if (Widgets.ButtonText(applyButton, "개조 적용", true, true, enough))
            {
                var skinComp = maid.GetComp<CompMaidSkin>();
                if (skinComp != null)
                {
                    skinComp.skinIndex = selectedSkin;
                }

                MaidModificationUtility.ConsumeResources(map, maid.PositionHeld, netCost);
                MaidModificationUtility.ApplyModifications(maid, selectedMods);

                SoundDefOf.GeneAssembler_Complete?.PlayOneShotOnCamera(null);

                maid.Drawer.renderer.SetAllGraphicsDirty();

                Messages.Message($"[MaidAndroidMod] {maid.LabelShort}의 외장 스킨 및 성능 개조가 성공적으로 반영되었습니다.", maid, MessageTypeDefOf.PositiveEvent);
                Close(true);
            }

            Rect cancelButton = new Rect(inRect.x + leftWidth + 15f + (inRect.width - leftWidth - 15f) / 2f - 90f, y, 180f, 40f);
            if (Widgets.ButtonText(cancelButton, "취소"))
            {
                Close(true);
            }
        }
    }

    // ==================== 3. BUILDING APPEARANCE COMPONENT ====================
    public class CompMaidAppearance : ThingComp, IThingHolder
    {
        public ThingOwner innerContainer;
        public Pawn targetMaidToEnter;

        public CompMaidAppearance()
        {
            innerContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
            Scribe_References.Look(ref targetMaidToEnter, "targetMaidToEnter");
        }

        public ThingOwner GetDirectlyHeldThings() => innerContainer;

        public void GetChildHolders(List<IThingHolder> outChildren) {}

        public bool HasMaid => innerContainer != null && innerContainer.Count > 0 && innerContainer[0] is Pawn;
        public Pawn ContainedMaid => HasMaid ? (Pawn)innerContainer[0] : null;

        public bool Accepts(Pawn pawn)
        {
            return pawn != null && MaidUtility.IsMaid(pawn) && pawn.Faction == Faction.OfPlayer && !pawn.Dead;
        }

        public bool TryAcceptPawn(Pawn pawn)
        {
            if (!Accepts(pawn)) return false;

            if (pawn.Spawned)
            {
                pawn.DeSpawn();
            }
            innerContainer.TryAdd(pawn);
            
            if (targetMaidToEnter == pawn)
            {
                targetMaidToEnter = null;
            }

            SoundDefOf.CryptosleepCasket_Accept.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
            return true;
        }

        public void EjectMaid()
        {
            if (innerContainer != null && innerContainer.Count > 0)
            {
                Thing thing = innerContainer[0];
                innerContainer.RemoveAt(0);
                GenPlace.TryPlaceThing(thing, parent.def.hasInteractionCell ? parent.InteractionCell : parent.Position, parent.Map, ThingPlaceMode.Near);
                
                SoundDefOf.CryptosleepCasket_Eject.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
            }
            targetMaidToEnter = null;
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            EjectMaid();
            base.PostDestroy(mode, previousMap);
        }

        public override void PostDraw()
        {
            base.PostDraw();
            if (HasMaid)
            {
                Pawn maid = ContainedMaid;
                if (maid != null)
                {
                    Vector3 drawPos = parent.DrawPos;
                    drawPos.y += 0.05f;
                    maid.Drawer.renderer.RenderPawnAt(drawPos);
                }
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            
            if (targetMaidToEnter != null && !HasMaid)
            {
                if (targetMaidToEnter.Dead || !targetMaidToEnter.Spawned || targetMaidToEnter.Map != parent.Map)
                {
                    targetMaidToEnter = null;
                    return;
                }

                IntVec3 targetCell = parent.def.hasInteractionCell ? parent.InteractionCell : parent.Position;
                if (targetMaidToEnter.Position == targetCell || targetMaidToEnter.Position.AdjacentTo8WayOrInside(parent.Position))
                {
                    TryAcceptPawn(targetMaidToEnter);
                }
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (base.CompGetGizmosExtra() != null)
            {
                foreach (var g in base.CompGetGizmosExtra())
                {
                    yield return g;
                }
            }

            if (!HasMaid)
            {
                yield return new Command_Action
                {
                    defaultLabel = "외장 개조 메카노이드 선택",
                    defaultDesc = "외장 개조 및 성능 튜닝을 진행할 메이드 메카노이드를 선택합니다.",
                    icon = ContentFinder<Texture2D>.Get("Things/Pawn/Mechanoid/Mech_Maid/Mech_Maid_south", false) ?? TexCommand.GatherSpotActive,
                    action = delegate
                    {
                        List<FloatMenuOption> list = new List<FloatMenuOption>();
                        var map = parent.Map;
                        if (map != null)
                        {
                            foreach (var p in map.mapPawns.AllPawnsSpawned)
                            {
                                if (p != null && MaidUtility.IsMaid(p) && p.Faction == Faction.OfPlayer && !p.Dead)
                                {
                                    Pawn maid = p;
                                    string text = maid.LabelShort;
                                    if (targetMaidToEnter == maid)
                                    {
                                        text += " (이동 중...)";
                                    }
                                    list.Add(new FloatMenuOption(text, delegate
                                    {
                                        targetMaidToEnter = maid;
                                        Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("EnterMaidAppearanceStation"), parent);
                                        maid.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                                    }));
                                }
                            }
                        }

                        if (list.Count == 0)
                        {
                            list.Add(new FloatMenuOption("사용 가능한 메이드 메카노이드가 없습니다.", null));
                        }
                        Find.WindowStack.Add(new FloatMenu(list));
                    }
                };
            }

            if (HasMaid)
            {
                yield return new Command_Action
                {
                    defaultLabel = "개조 작업 중단 및 꺼내기",
                    defaultDesc = "작업을 중단하고 수용된 메이드 메카노이드를 즉시 꺼냅니다.",
                    icon = TexCommand.Draft,
                    action = delegate
                    {
                        EjectMaid();
                    }
                };

                yield return new Command_Action
                {
                    defaultLabel = "외장 개조 및 성능 튜닝창 열기",
                    defaultDesc = "메이드의 스킨 도색을 커스텀하고, 증강 장점과 결함 패널티를 튜닝하는 조절창을 엽니다.",
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/UpgradeSpeed", false) ?? TexCommand.GatherSpotActive,
                    action = delegate
                    {
                        Find.WindowStack.Add(new Dialog_MaidCustomization(ContainedMaid));
                    }
                };
            }

            if (targetMaidToEnter != null && !HasMaid)
            {
                yield return new Command_Action
                {
                    defaultLabel = "이동 지시 취소",
                    defaultDesc = "이동 중인 메이드 메카노이드의 개조 지시를 취소합니다.",
                    icon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel", false) ?? TexCommand.ClearPrioritizedWork,
                    action = delegate
                    {
                        if (targetMaidToEnter.CurJobDef?.defName == "EnterMaidAppearanceStation")
                        {
                            targetMaidToEnter.jobs.EndCurrentJob(JobCondition.InterruptForced);
                        }
                        targetMaidToEnter = null;
                    }
                };
            }
        }
    }

    public class CompProperties_MaidAppearance : CompProperties
    {
        public CompProperties_MaidAppearance()
        {
            compClass = typeof(CompMaidAppearance);
        }
    }

    // ==================== 4. PAWN SKIN STATE COMPONENT ====================
    public class CompMaidSkin : ThingComp
    {
        public int skinIndex = 0; // 0: Default, 1: Type A, 2: Type B, 3: Type C, 4: Type D

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref skinIndex, "skinIndex", 0);
        }
    }

    public class CompProperties_MaidSkin : CompProperties
    {
        public CompProperties_MaidSkin()
        {
            compClass = typeof(CompMaidSkin);
        }
    }

    // ==================== 5. JOB DRIVER ====================
    public class JobDriver_EnterMaidAppearanceStation : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.InteractionCell);

            yield return new Toil
            {
                initAction = delegate
                {
                    if (TargetA.Thing is Building bench)
                    {
                        var comp = bench.GetComp<CompMaidAppearance>();
                        if (comp != null)
                        {
                            comp.TryAcceptPawn(pawn);
                        }
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}
