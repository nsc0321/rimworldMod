using System.Collections.Generic;
using Verse;
using RimWorld;

namespace HiveMod
{
    public class GameComponent_HiveEvolution : GameComponent
    {
        // Tracks which Hive Parts (GeneDefs) have been unlocked
        public HashSet<string> unlockedParts = new HashSet<string>();

        public GameComponent_HiveEvolution(Game game)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            
            // Serialize the set as a list
            List<string> list = new List<string>(unlockedParts);
            Scribe_Collections.Look(ref list, "unlockedParts", LookMode.Value);
            
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (list != null)
                {
                    unlockedParts = new HashSet<string>(list);
                }
                else
                {
                    unlockedParts = new HashSet<string>();
                }
            }
        }

        public bool IsUnlocked(GeneDef def)
        {
            return unlockedParts.Contains(def.defName);
        }

        public void Unlock(GeneDef def)
        {
            if (!unlockedParts.Contains(def.defName))
            {
                unlockedParts.Add(def.defName);
            }
        }
    }
}
