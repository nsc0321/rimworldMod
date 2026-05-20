using System;
using System.Collections.Generic;
using Verse;

namespace GeneControlMod
{
    public class GeneControlSettings : ModSettings
    {
        // Endogene (Germline) Settings
        public bool addEndogenesEnabled = false;
        public float endogeneChance = 0.3f;
        public int minEndogenesToAdd = 0; // Configurable from 0
        public int maxEndogenesToAdd = 1;

        // Xenogene (Artificial) Settings
        public bool addXenogenesEnabled = true;
        public float xenogeneChance = 0.5f;
        public int minXenogenesToAdd = 0; // Configurable from 0
        public int maxXenogenesToAdd = 1;

        // Names of blacklisted/whitelisted genes (serializable)
        public List<string> blacklistedGeneNames = new List<string>();
        public List<string> whitelistedGeneNames = new List<string>();

        // Custom individual gene chance overrides (defName -> chance)
        public Dictionary<string, float> customGeneChances = new Dictionary<string, float>();

        // Optimized lookup hashes during runtime
        [Unsaved]
        public HashSet<string> blacklistHash = new HashSet<string>();
        [Unsaved]
        public HashSet<string> whitelistHash = new HashSet<string>();

        public override void ExposeData()
        {
            base.ExposeData();
            
            // Endogene Serialization
            Scribe_Values.Look(ref addEndogenesEnabled, "addEndogenesEnabled", false);
            Scribe_Values.Look(ref endogeneChance, "endogeneChance", 0.3f);
            Scribe_Values.Look(ref minEndogenesToAdd, "minEndogenesToAdd", 0);
            Scribe_Values.Look(ref maxEndogenesToAdd, "maxEndogenesToAdd", 1);

            // Xenogene Serialization
            Scribe_Values.Look(ref addXenogenesEnabled, "addXenogenesEnabled", true);
            Scribe_Values.Look(ref xenogeneChance, "xenogeneChance", 0.5f);
            Scribe_Values.Look(ref minXenogenesToAdd, "minXenogenesToAdd", 0);
            Scribe_Values.Look(ref maxXenogenesToAdd, "maxXenogenesToAdd", 1);

            // Blacklist / Whitelist Lists
            Scribe_Collections.Look(ref blacklistedGeneNames, "blacklistedGeneNames", LookMode.Value);
            Scribe_Collections.Look(ref whitelistedGeneNames, "whitelistedGeneNames", LookMode.Value);

            // Custom Gene Chances Dictionary
            Scribe_Collections.Look(ref customGeneChances, "customGeneChances", LookMode.Value, LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (blacklistedGeneNames == null) blacklistedGeneNames = new List<string>();
                if (whitelistedGeneNames == null) whitelistedGeneNames = new List<string>();
                if (customGeneChances == null) customGeneChances = new Dictionary<string, float>();

                InitializeHashes();
            }
        }

        public void InitializeHashes()
        {
            blacklistHash = new HashSet<string>(blacklistedGeneNames);
            whitelistHash = new HashSet<string>(whitelistedGeneNames);
        }
    }
}
