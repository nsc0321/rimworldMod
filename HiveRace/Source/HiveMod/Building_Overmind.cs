using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;

namespace HiveMod
{
    public class Building_Overmind : Building, IHiveCore
    {
        public float currentEnergy = 0f;
        public float maxEnergy = 1000f;

        public float CurrentEnergy { get { return currentEnergy; } set { currentEnergy = value; } }
        public float MaxEnergy { get { return maxEnergy; } }
        public Thing ThingContext { get { return this; } }

        private int tickCounter = 0;
        private const int UpdateTickInterval = 60; // 1 second in normal speed
        private const float CreepSpreadRadius = 15.9f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref currentEnergy, "currentEnergy", 0f);
        }

        protected override void Tick()
        {
            base.Tick();
            tickCounter++;
            
            if (tickCounter >= UpdateTickInterval)
            {
                tickCounter = 0;
                DoTickInterval();
            }
        }

        private void DoTickInterval()
        {
            // 1. Gather energy
            if (currentEnergy < maxEnergy)
            {
                currentEnergy += 1f; // base energy gain
            }

            // 2. Spread creep (점막 확산)
            SpreadCreep();
        }

        private void SpreadCreep()
        {
            if (!this.Spawned) return;

            TerrainDef creepDef = DefDatabase<TerrainDef>.GetNamed("Hive_Creep", false);
            if (creepDef == null) return;

            // Simple outward spread logic
            int numCells = GenRadial.NumCellsInRadius(CreepSpreadRadius);
            for (int i = 0; i < numCells; i++)
            {
                IntVec3 cell = this.Position + GenRadial.RadialPattern[i];
                if (cell.InBounds(this.Map) && cell.GetTerrain(this.Map) != creepDef)
                {
                    // For performance, only change a few cells per tick interval
                    if (Rand.Value < 0.1f) 
                    {
                        this.Map.terrainGrid.SetTerrain(cell, creepDef);
                        // Stop after spreading once per interval to create gradual growth
                        break;
                    }
                }
            }
        }

        public override string GetInspectString()
        {
            string str = base.GetInspectString();
            if (!str.NullOrEmpty())
            {
                str += "\n";
            }
            str += $"Energy: {currentEnergy:F0} / {maxEnergy:F0}";
            return str;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
            {
                yield return g;
            }

            // Example Button to convert energy to biomass
            yield return new Command_Action
            {
                defaultLabel = "Generate Biomass",
                defaultDesc = "Convert 100 Energy into 10 Biomass.",
                icon = ContentFinder<Texture2D>.Get("Dummy", true), // placeholder icon
                action = () =>
                {
                    if (currentEnergy >= 100f)
                    {
                        currentEnergy -= 100f;
                        Thing biomass = ThingMaker.MakeThing(ThingDef.Named("Hive_Biomass"));
                        biomass.stackCount = 10;
                        GenPlace.TryPlaceThing(biomass, this.InteractionCell, this.Map, ThingPlaceMode.Near);
                        Messages.Message("Generated 10 Biomass.", this, MessageTypeDefOf.PositiveEvent);
                    }
                    else
                    {
                        Messages.Message("Not enough energy.", this, MessageTypeDefOf.RejectInput);
                    }
                }
            };

            // Button to open Unit Assembly UI
            yield return new Command_Action
            {
                defaultLabel = "Assemble Unit",
                defaultDesc = "Open the assembly menu to spend Biomass and create Hive units.",
                icon = ContentFinder<Texture2D>.Get("Dummy", true), // placeholder icon
                action = () =>
                {
                    Find.WindowStack.Add(new Window_SpawnHiveUnit(this));
                }
            };

            // Button to open Evolution UI
            yield return new Command_Action
            {
                defaultLabel = "Evolution",
                defaultDesc = "Open the evolution menu to spend Energy and research new parts.",
                icon = ContentFinder<Texture2D>.Get("Dummy", true), // placeholder icon
                action = () =>
                {
                    Find.WindowStack.Add(new Window_HiveEvolution(this));
                }
            };
        }
    }
}
