using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace MaidAndroidMod
{
    public class CompMaidAssembly : ThingComp, IThingHolder
    {
        // 1. ThingOwner to hold the contained Mech_Maid (Cryptosleep casket style!)
        public ThingOwner innerContainer;
        
        // Track the target maid who is currently pathing to enter
        public Pawn targetMaidToEnter;

        public CompMaidAssembly()
        {
            innerContainer = new ThingOwner<Thing>(this, false, LookMode.Deep);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
            Scribe_References.Look(ref targetMaidToEnter, "targetMaidToEnter");
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
        }

        public bool HasMaid => innerContainer != null && innerContainer.Count > 0 && innerContainer[0] is Pawn;
        public Pawn ContainedMaid => HasMaid ? (Pawn)innerContainer[0] : null;

        // Accepts check
        public bool Accepts(Pawn pawn)
        {
            return pawn != null && pawn.def.defName == "Mech_Maid" && pawn.Faction == Faction.OfPlayer && !pawn.Dead;
        }

        // Load the maid into the container
        public bool TryAcceptPawn(Pawn pawn)
        {
            if (!Accepts(pawn)) return false;

            if (pawn.Spawned)
            {
                pawn.DeSpawn();
            }
            innerContainer.TryAdd(pawn);
            
            if (targetMaidToEnter == pawn)
            {
                targetMaidToEnter = null;
            }

            // Mechanical close sound
            SoundDefOf.CryptosleepCasket_Accept.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
            return true;
        }

        // Eject the maid
        public void EjectMaid()
        {
            if (innerContainer != null && innerContainer.Count > 0)
            {
                Thing thing = innerContainer[0];
                innerContainer.RemoveAt(0);
                GenPlace.TryPlaceThing(thing, parent.def.hasInteractionCell ? parent.InteractionCell : parent.Position, parent.Map, ThingPlaceMode.Near);
                
                SoundDefOf.CryptosleepCasket_Eject.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
            }
            targetMaidToEnter = null;
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            EjectMaid();
            base.PostDestroy(mode, previousMap);
        }

        // Tick controller (check if the pathing maid has arrived at the bench to trigger auto-boarding!)
        public override void CompTick()
        {
            base.CompTick();
            
            if (targetMaidToEnter != null && !HasMaid)
            {
                // Safety checks
                if (targetMaidToEnter.Dead || !targetMaidToEnter.Spawned || targetMaidToEnter.Map != parent.Map)
                {
                    targetMaidToEnter = null;
                    return;
                }

                // If she reached the interaction cell (or is adjacent to the bench)
                IntVec3 targetCell = parent.def.hasInteractionCell ? parent.InteractionCell : parent.Position;
                if (targetMaidToEnter.Position == targetCell || targetMaidToEnter.Position.AdjacentTo8WayOrInside(parent.Position))
                {
                    TryAcceptPawn(targetMaidToEnter);
                }
            }
        }

        // Gizmo button (Select Mechanoid for Maintenance)
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra())
            {
                yield return g;
            }

            // A: Select Mech button if empty
            if (!HasMaid)
            {
                yield return new Command_Action
                {
                    defaultLabel = "정비 메카노이드 선택",
                    defaultDesc = "정비 및 업그레이드를 진행할 메이드 메카노이드를 선택합니다.",
                    icon = ContentFinder<UnityEngine.Texture2D>.Get("Things/Pawn/Mechanoid/Mech_Maid/Mech_Maid_south", false) ?? TexCommand.GatherSpotActive,
                    action = delegate
                    {
                        List<FloatMenuOption> list = new List<FloatMenuOption>();
                        var map = parent.Map;
                        if (map != null)
                        {
                            foreach (var p in map.mapPawns.AllPawnsSpawned)
                            {
                                if (p.def.defName == "Mech_Maid" && p.Faction == Faction.OfPlayer && !p.Dead)
                                {
                                    Pawn maid = p;
                                    string text = maid.LabelShort;
                                    if (targetMaidToEnter == maid)
                                    {
                                        text += " (이동 중...)";
                                    }
                                    list.Add(new FloatMenuOption(text, delegate
                                    {
                                        // Set pathing target
                                        targetMaidToEnter = maid;
                                        
                                        // Give the maid the custom ordered job to walk to the bench
                                        Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("EnterMaidBench"), parent);
                                        maid.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                                    }));
                                }
                            }
                        }

                        if (list.Count == 0)
                        {
                            list.Add(new FloatMenuOption("사용 가능한 메이드 메카노이드가 없습니다.", null));
                        }
                        Find.WindowStack.Add(new FloatMenu(list));
                    }
                };
            }

            // B: Eject Mech button if contained
            if (HasMaid)
            {
                yield return new Command_Action
                {
                    defaultLabel = "정비 완료 및 꺼내기",
                    defaultDesc = "조립대 내부에 안착된 메이드를 즉시 밖으로 방출합니다.",
                    icon = TexCommand.Draft,
                    action = delegate
                    {
                        EjectMaid();
                    }
                };
            }
        }
    }

    public class CompProperties_MaidAssembly : CompProperties
    {
        public CompProperties_MaidAssembly()
        {
            compClass = typeof(CompMaidAssembly);
        }
    }
}
