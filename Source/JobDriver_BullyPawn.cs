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
        private ThingWithComps droppedWeapon;             // non‑spawned copy

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
            /* auto‑fail conditions */
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOn(() => pawn.Downed || !BullyFlagOK || !VictimFlagOK);

            /* 1 ▸ go to victim */
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            /* 2 ▸ drop the weapon (if any) and immediately despawn it */
            yield return new Toil
            {
                initAction = () =>
                {
                    var eq = pawn.equipment?.Primary;

                    if (eq != null)
                    {
                        if (pawn.equipment.TryDropEquipment(eq, out var dropped, pawn.Position, forbid: false))
                        {
                            droppedWeapon = dropped;
                            droppedWeapon.DeSpawn();
                            pawn.inventory.innerContainer.TryAdd(dropped);
                        }
                    }
                    // else Log.Message("[MP] no weapon to drop");
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };

            /* 3 ▸ fight until a stop condition, then re‑equip and end job */
            Toil fight = new Toil { handlingFacing = true };
            fight.tickAction = () =>
            {
                /* stop? */
                bool finished =
                       Victim.Dead
                    || Victim.Downed
                    || StopBecausePain
                    || StopBecauseLearningCapped
                    || !VictimFlagOK
                    || !BullyFlagOK;

                if (finished)
                {
                    /* re‑equip weapon (if one exists and pawn still empty‑handed) */
                    if (droppedWeapon != null && pawn.equipment.Primary == null)
                    {
                        pawn.equipment.AddEquipment(droppedWeapon);
                        // Log.Message("[MP] weapon re‑equipped");
                    }

                    EndJobWith(JobCondition.Succeeded);
                    return;
                }
                if (!pawn.Position.AdjacentTo8WayOrInside(Victim.Position))
                {
                    pawn.pather.StartPath(Victim, PathEndMode.Touch);
                    return;
                }
                /* try a punch every 60 ticks */
                if (pawn.IsHashIntervalTick(60))
                {
                    pawn.meleeVerbs.TryMeleeAttack(Victim);
                }
            };
            fight.defaultCompleteMode = ToilCompleteMode.Never;
            yield return fight;
        }
    }
}
