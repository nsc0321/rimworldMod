using HarmonyLib;
using Verse;
using RimWorld;

namespace MaidAndroidMod
{
    [HarmonyPatch(typeof(PawnRenderNode), nameof(PawnRenderNode.GraphicFor))]
    public static class Patch_PawnRenderNode_GraphicFor
    {
        [HarmonyPostfix]
        public static void Postfix(PawnRenderNode __instance, Pawn pawn, ref Graphic __result)
        {
            if (pawn != null && MaidUtility.IsMaid(pawn))
            {
                if (__instance.GetType().Name.Contains("Body"))
                {
                    var comp = pawn.GetComp<CompMaidSkin>();
                    if (comp != null && comp.skinIndex > 0)
                    {
                        string path = null;
                        bool isMulti = false;
                        if (comp.skinIndex == 1) path = "Things/Pawn/Mechanoid/a1";
                        else if (comp.skinIndex == 2) path = "Things/Pawn/Mechanoid/a2";
                        else if (comp.skinIndex == 3) path = "Things/Pawn/Mechanoid/a3";
                        else if (comp.skinIndex == 4) { path = "Things/Pawn/Mechanoid/Mech_Maid_Custom"; isMulti = true; }

                        if (path != null)
                        {
                            Graphic newGraphic;
                            if (isMulti)
                            {
                                newGraphic = GraphicDatabase.Get<Graphic_Multi>(path, ShaderDatabase.Cutout, pawn.def.graphicData.drawSize, pawn.DrawColor, pawn.DrawColorTwo);
                            }
                            else
                            {
                                newGraphic = GraphicDatabase.Get<Graphic_Single>(path, ShaderDatabase.Cutout, pawn.def.graphicData.drawSize, pawn.DrawColor, pawn.DrawColorTwo);
                            }
                            __result = newGraphic;
                        }
                    }
                }
            }
        }
    }
}
