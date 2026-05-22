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
            MentalStateDef feralDef = DefDatabase<MentalStateDef>.GetNamedSilentFail("WanderingFeral") ?? MentalStateDefOf.Wander_Psychotic;
            if (!Pawn.Spawned || Pawn.Dead || Pawn.MentalStateDef == feralDef) return;

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

            // Check if the character (pawn with HiveSeed gene) is alive and DEPLOYED on the map
            if (!overmindExists)
            {
                GeneDef hiveSeedDef = DefDatabase<GeneDef>.GetNamedSilentFail("Gene_HiveSeed");
                if (hiveSeedDef != null)
                {
                    foreach (Pawn p in Pawn.Map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer))
                    {
                        if (p.genes != null && p.genes.HasActiveGene(hiveSeedDef) && !p.Dead)
                        {
                            var deployGene = p.genes.GetGene(hiveSeedDef) as Gene_Deploy;
                            if (deployGene != null && deployGene.isDeployed)
                            {
                                overmindExists = true;
                                break;
                            }
                        }
                    }
                }
            }

            if (!overmindExists)
            {
                Pawn.mindState.mentalStateHandler.TryStartMentalState(feralDef, "Overmind destroyed!", forced: true);
                Messages.Message($"{Pawn.Label} has lost connection with the Overmind and became uncontrolled!", Pawn, MessageTypeDefOf.NegativeEvent);
            }
        }
    }
}
