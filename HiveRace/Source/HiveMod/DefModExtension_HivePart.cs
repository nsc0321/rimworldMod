using Verse;

namespace HiveMod
{
    // Used on GeneDef to specify it as an evolvable part
    public class DefModExtension_HivePart : DefModExtension
    {
        public float researchCostEnergy = 500f;   // Energy cost to unlock this part
        public float assemblyCostBiomass = 10f;   // Biomass cost to assemble this part onto a unit
        public float assemblyCostEnergy = 20f;    // Energy cost to assemble this part
        public string category = "Misc";          // e.g. "Jaw", "Carapace", "Legs"
    }
}
