using RimWorld;
using Verse;

namespace HiveMod
{
    public class PlaceWorker_OnCreep : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            TerrainDef terrain = loc.GetTerrain(map);
            if (terrain != null && terrain.defName == "Hive_Creep")
            {
                return true;
            }
            return new AcceptanceReport("Must be placed on Creep.");
        }
    }
}
