using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;

namespace HiveMod
{
    public class Gene_Deploy : Gene, IHiveCore
    {
        public bool isDeployed = false;
        private float currentEnergy = 0f;
        private float maxEnergy = 1000f;
        private int tickCounter = 0;

        public float CurrentEnergy { get { return currentEnergy; } set { currentEnergy = value; } }
        public float MaxEnergy { get { return maxEnergy; } }
        public Map Map { get { return pawn?.Map; } }
        public IntVec3 Position { get { return pawn?.Position ?? IntVec3.Invalid; } }
        public Thing ThingContext { get { return pawn; } }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref isDeployed, "isDeployed", false);
            Scribe_Values.Look(ref currentEnergy, "currentEnergy", 0f);
        }

        public override void Tick()
        {
            base.Tick();
            
            if (!isDeployed || pawn == null || !pawn.Spawned || pawn.Dead) return;

            tickCounter++;
            if (tickCounter >= 60) // Every 1 second at normal speed
            {
                tickCounter = 0;
                GenerateEnergyFromCreep();
                SpreadCreep();
            }
        }

        private void SpreadCreep()
        {
            TerrainDef creepDef = DefDatabase<TerrainDef>.GetNamedSilentFail("Hive_Creep");
            if (creepDef == null) return;

            int numCells = GenRadial.NumCellsInRadius(15.9f);
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

        private void GenerateEnergyFromCreep()
        {
            TerrainDef creepDef = DefDatabase<TerrainDef>.GetNamedSilentFail("Hive_Creep");
            if (creepDef == null) return;

            int creepCount = 0;
            int numCells = GenRadial.NumCellsInRadius(15.9f);
            for (int i = 0; i < numCells; i++)
            {
                IntVec3 cell = pawn.Position + GenRadial.RadialPattern[i];
                if (cell.InBounds(pawn.Map) && cell.GetTerrain(pawn.Map) == creepDef)
                {
                    creepCount++;
                }
            }

            // Generate energy based on creep (every 60 ticks now, so scaled down slightly)
            float energyGain = 0.25f + (creepCount / 20f);
            currentEnergy = Mathf.Min(currentEnergy + energyGain, maxEnergy);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            if (this.pawn == null || this.pawn.Faction != Faction.OfPlayer)
            {
                yield break;
            }

            if (!isDeployed)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Deploy",
                    defaultDesc = "Deploys the unit into a stationary Hive Core. WARNING: This is irreversible!",
                    icon = ContentFinder<Texture2D>.Get("Dummy", true),
                    action = () =>
                    {
                        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                            "Deploying will permanently immobilize this unit. Are you sure?",
                            () => DeployUnit()
                        ));
                    }
                };
            }
            else
            {
                yield return new Gizmo_HiveEnergy(this);

                // Nest Gizmos (Available only when deployed)
                yield return new Command_Action
                {
                    defaultLabel = "Generate Biomass",
                    defaultDesc = "Convert 100 Energy into 10 Biomass.",
                    icon = ContentFinder<Texture2D>.Get("Dummy", true),
                    action = () =>
                    {
                        if (currentEnergy >= 100f)
                        {
                            currentEnergy -= 100f;
                            Thing biomass = ThingMaker.MakeThing(ThingDef.Named("Hive_Biomass"));
                            biomass.stackCount = 10;
                            GenPlace.TryPlaceThing(biomass, pawn.Position, pawn.Map, ThingPlaceMode.Near);
                            Messages.Message("Generated 10 Biomass.", pawn, MessageTypeDefOf.PositiveEvent);
                        }
                        else
                        {
                            Messages.Message("Not enough energy.", pawn, MessageTypeDefOf.RejectInput);
                        }
                    }
                };

                yield return new Command_Action
                {
                    defaultLabel = "Assemble Unit",
                    defaultDesc = "Open the assembly menu to spend Biomass and create Hive units.",
                    icon = ContentFinder<Texture2D>.Get("Dummy", true),
                    action = () =>
                    {
                        Find.WindowStack.Add(new Window_SpawnHiveUnit(this));
                    }
                };

                yield return new Command_Action
                {
                    defaultLabel = "Evolution",
                    defaultDesc = "Open the evolution menu to spend Energy and research new parts.",
                    icon = ContentFinder<Texture2D>.Get("Dummy", true),
                    action = () =>
                    {
                        Find.WindowStack.Add(new Window_HiveEvolution(this));
                    }
                };
            }
        }

        private void DeployUnit()
        {
            isDeployed = true;

            HediffDef hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("Hediff_HiveDeploy");
            if (hediffDef != null)
            {
                pawn.health.AddHediff(hediffDef);
                Messages.Message(pawn.LabelShort + " has permanently deployed into a Hive Core.", pawn, MessageTypeDefOf.PositiveEvent);
            }
            else
            {
                Log.Error("Hediff_HiveDeploy not found!");
            }
        }
    }
}
