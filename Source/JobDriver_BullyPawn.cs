using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace MeleePractice
{
    public class JobDriver_BullyPawn : JobDriver
    {
        /* ------------------------------------------------------------ */
        private Pawn Victim => (Pawn)job.targetA.Thing;

        private bool BullyFlagOK   => pawn.GetComp<CompBullyFlags>()?.IsBully  == true;
        private bool VictimFlagOK  => Victim.GetComp<CompBullyFlags>()?.IsVictim == true;

        private bool StopBecauseLearningCapped =>
            MeleePracticeMod.Settings.stopWhenSaturated &&
            pawn.skills?.GetSkill(SkillDefOf.Melee)?.LearningSaturatedToday == true;

        private bool StopBecausePain =>
            Victim.health.hediffSet.PainTotal >=
            MeleePracticeMod.Settings.PainLimitFor(Victim);

        /* ------------------------------------------------------------ */
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Victim, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOn(() => pawn.Downed || !BullyFlagOK || !VictimFlagOK);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            Toil fight = new Toil { handlingFacing = true };
            fight.tickAction = () =>
            {
                bool finished =
                       Victim.Dead
                    || Victim.Downed
                    || StopBecausePain
                    || StopBecauseLearningCapped
                    || !VictimFlagOK
                    || !BullyFlagOK;

                if (finished)
                {
                    EndJobWith(JobCondition.Succeeded);
                    return;
                }
                if (!pawn.Position.AdjacentTo8WayOrInside(Victim.Position))
                {
                    pawn.pather.StartPath(Victim, PathEndMode.Touch);
                    return;
                }
                if (pawn.IsHashIntervalTick(60) && !(pawn.stances?.FullBodyBusy ?? false))
                {
                    if (InteractionUtility.TryGetRandomVerbForSocialFight(pawn, out var fistVerb))
                    {
                        fistVerb.TryStartCastOn(Victim);
                    }
                }
            };
            fight.defaultCompleteMode = ToilCompleteMode.Never;
            yield return fight;
        }
    }
}
