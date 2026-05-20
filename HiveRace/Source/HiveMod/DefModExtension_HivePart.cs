using Verse;

namespace HiveMod
{
    // Used on HediffDef to specify it as an evolvable part
    public class DefModExtension_HivePart : DefModExtension
    {
        public float researchCostEnergy = 500f;   // Energy cost to unlock this part
        public float assemblyCostBiomass = 10f;   // Biomass cost to assemble this part onto a unit
        public string category = "Misc";          // e.g. "Jaw", "Carapace", "Legs"
    }
}
