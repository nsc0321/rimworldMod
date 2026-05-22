using RimWorld;
using Verse;

namespace HiveMod
{
    public interface IHiveCore
    {
        float CurrentEnergy { get; set; }
        float MaxEnergy { get; }
        Map Map { get; }
        IntVec3 Position { get; }
        Thing ThingContext { get; }
    }
}
