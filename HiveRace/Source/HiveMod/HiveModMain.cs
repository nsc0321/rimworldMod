using HarmonyLib;
using Verse;

namespace HiveMod
{
    [StaticConstructorOnStartup]
    public static class HiveModMain
    {
        static HiveModMain()
        {
            var harmony = new Harmony("Player.HiveRace");
            harmony.PatchAll();
            Log.Message("[HiveMod] 로드 완료.");
        }
    }
}
