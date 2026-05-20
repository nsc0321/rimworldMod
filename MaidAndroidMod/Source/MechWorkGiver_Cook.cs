using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace MaidAndroidMod
{
    // Inherit from WorkGiver_DoBill to allow mechanoids to work at cooking/butchering benches
    public class MechWorkGiver_Cook : WorkGiver_DoBill
    {
        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            // 1. Core safety checks
            if (pawn == null || t == null)
                return false;

            // 2. Must belong to the player faction
            if (pawn.Faction != Faction.OfPlayer)
                return false;

            // 3. Verify it is a player-controlled Maid Mechanoid
            if (!pawn.IsColonyMech || pawn.def.defName != "Mech_Maid")
                return false;

            // 4. Must be a culinary workbench (Electric Stove, Fueled Stove, Butcher Table)
            if (!(t is Building_WorkTable workTable))
                return false;

            // 5. Verify standard pathing, bandwidth control, and energy levels
            if (pawn.Destroyed || pawn.Downed || pawn.Dead)
                return false;

            // 6. Run vanilla bill scanner to check if resources are near and a Bill exists
            return base.HasJobOnThing(pawn, t, forced);
        }
    }
}
