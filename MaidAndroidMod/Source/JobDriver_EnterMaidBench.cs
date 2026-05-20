using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace MaidAndroidMod
{
    public class JobDriver_EnterMaidBench : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            // Reserve the target bench so multiple pawns don't try to enter at the same split second
            return pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // 1. Move to the bench's interaction cell
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.InteractionCell);

            // 2. Once arrived, enter the bench!
            yield return new Toil
            {
                initAction = delegate
                {
                    if (TargetA.Thing is Building_WorkTable bench)
                    {
                        var comp = bench.GetComp<CompMaidAssembly>();
                        if (comp != null)
                        {
                            comp.TryAcceptPawn(pawn);
                        }
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}
