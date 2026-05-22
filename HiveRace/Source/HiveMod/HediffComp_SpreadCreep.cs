using System;
using RimWorld;
using Verse;

namespace HiveMod
{
    public class HediffCompProperties_SpreadCreep : HediffCompProperties
    {
        public float radius = 5.9f;
        public int tickInterval = 60;
        
        public HediffCompProperties_SpreadCreep()
        {
            this.compClass = typeof(HediffComp_SpreadCreep);
        }
    }

    public class HediffComp_SpreadCreep : HediffComp
    {
        private int tickCounter = 0;

        public HediffCompProperties_SpreadCreep Props => (HediffCompProperties_SpreadCreep)this.props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);
            
            tickCounter++;
            if (tickCounter >= Props.tickInterval)
            {
                tickCounter = 0;
                SpreadCreep();
            }
        }

        private void SpreadCreep()
        {
            Pawn pawn = this.Pawn;
            if (pawn == null || !pawn.Spawned || pawn.Dead) return;

            TerrainDef creepDef = DefDatabase<TerrainDef>.GetNamedSilentFail("Hive_Creep");
            if (creepDef == null) return;

            int numCells = GenRadial.NumCellsInRadius(Props.radius);
            for (int i = 0; i < numCells; i++)
            {
                IntVec3 cell = pawn.Position + GenRadial.RadialPattern[i];
                if (cell.InBounds(pawn.Map) && cell.GetTerrain(pawn.Map) != creepDef)
                {
                    if (Rand.Value < 0.1f)
                    {
                        pawn.Map.terrainGrid.SetTerrain(cell, creepDef);
                        break;
                    }
                }
            }
        }
    }
}
