using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace HiveMod
{
    public class CompProperties_HiveUnit : CompProperties
    {
        public CompProperties_HiveUnit()
        {
            this.compClass = typeof(CompHiveUnit);
        }
    }

    public class CompHiveUnit : ThingComp
    {
        private Pawn Pawn => (Pawn)this.parent;

        public override void CompTick()
        {
            base.CompTick();
            
            // Do checks every 30 ticks to save performance
            if (Pawn.IsHashIntervalTick(30))
            {
                CheckCreepBuff();
                CheckOvermindConnection();
            }
        }

        private void CheckCreepBuff()
        {
            if (!Pawn.Spawned || Pawn.Dead) return;

            TerrainDef terrain = Pawn.Position.GetTerrain(Pawn.Map);
            if (terrain != null && terrain.defName == "Hive_Creep")
            {
                Hediff hediff = Pawn.health.hediffSet.GetFirstHediffOfDef(DefDatabase<HediffDef>.GetNamed("Hediff_CreepBuff"));
                if (hediff == null)
                {
                    Pawn.health.AddHediff(DefDatabase<HediffDef>.GetNamed("Hediff_CreepBuff"));
                }
                else
                {
                    // Refresh the duration since it's set to disappear automatically
                    var comp = hediff.TryGetComp<HediffComp_Disappears>();
                    if (comp != null)
                    {
                        comp.ticksToDisappear = 60; // Refresh to 1 second
                    }
                }
            }
        }

        private void CheckOvermindConnection()
        {
            if (!Pawn.Spawned || Pawn.Dead || Pawn.MentalStateDef == MentalStateDefOf.Berserk) return;

            // Simple check: Is there any player Overmind on the map?
            bool overmindExists = false;
            foreach (Thing building in Pawn.Map.listerBuildings.allBuildingsColonist)
            {
                if (building is Building_Overmind)
                {
                    overmindExists = true;
                    break;
                }
            }

            if (!overmindExists)
            {
                // Overmind is dead or missing! Hive units go feral.
                Pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, "Overmind destroyed!", true, false, null, false);
                Messages.Message($"{Pawn.Label} has gone berserk due to losing connection with the Overmind!", Pawn, MessageTypeDefOf.NegativeEvent);
            }
        }
    }
}
