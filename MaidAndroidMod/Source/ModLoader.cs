using System;
using HarmonyLib;
using Verse;

namespace MaidAndroidMod
{
    [StaticConstructorOnStartup]
    public static class ModLoader
    {
        static ModLoader()
        {
            try
            {
                var harmony = new Harmony("nsc.MaidAndroidMod");
                harmony.PatchAll();
                Log.Message("[MaidAndroidMod] Custom Maid Mechanoid Mod successfully loaded! Dynamic 올라운더 Work-locks, 4-tier Maid support & Consolidated 7 Modules active.");
            }
            catch (Exception ex)
            {
                Log.Error("[MaidAndroidMod] Failed to initialize Harmony Patches: " + ex);
            }
        }
    }
}
