using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace MeleePractice
{
    public class JobDriver_BullyPawn : JobDriver
    {
        private Pawn Victim => (Pawn)job.targetA.Thing;
        private ThingWithComps droppedWeapon;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Victim, job);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnAggroMentalState(TargetIndex.A);
            this.FailOn(() =>
            {
                return Victim.Dead || Victim.Downed || pawn.Downed || MeleeExpCapped(pawn)
                    || !IsVictimStillFlagged(Victim)
                    || !IsBullyStillFlagged(pawn);
            });

            // 1. Walk to victim
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

            // 2. Drop weapon if held
            yield return new Toil
            {
                initAction = () =>
                {
                    var weapon = pawn.equipment?.Primary;
                    if (weapon != null)
                    {
                        pawn.equipment.TryDropEquipment(weapon, out var dropped, pawn.Position, forbid: false);
                        if (dropped.Spawned)
                        {
                            dropped.DeSpawn();
                        }
                        droppedWeapon = dropped;
                        pawn.inventory.innerContainer.TryAdd(dropped);
                        // Log.Message("[MeleePractice] Dropped weapon: " + dropped);
                    }
                    else
                    {
                        // Log.Message("[MeleePractice] No weapon to drop.");
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };

            // 3. Melee attack loop
            Toil attack = new Toil
            {
                tickAction = () =>
                {
                    if (!pawn.Position.AdjacentTo8WayOrInside(Victim.Position))
                    {
                        pawn.pather.StartPath(Victim, PathEndMode.Touch);
                        return;
                    }

                    if (pawn.IsHashIntervalTick(60))
                    {
                        if (pawn.meleeVerbs.TryMeleeAttack(Victim, null, surpriseAttack: false))
                        {
                            // Log.Message("[MeleePractice] Melee attack executed.");
                        }
                    }

                    if (Victim.Dead || Victim.Downed || pawn.Downed || MeleeExpCapped(pawn)
                        || !IsVictimStillFlagged(Victim)
                        || !IsBullyStillFlagged(pawn))
                    {
                        // Log.Message("[MeleePractice] Ending attack — target condition met.");

                        if (droppedWeapon != null) {
                            foreach (Thing thing in pawn.inventory.innerContainer)
                            {
                                if (thing is ThingWithComps weapon && weapon == droppedWeapon)
                                {
                                    if (pawn.inventory.innerContainer.TryDrop(weapon, ThingPlaceMode.Direct, out Thing thingOut) && thingOut is ThingWithComps restoredWeapon)
                                    {
                                        if (restoredWeapon.Spawned)
                                        {
                                            restoredWeapon.DeSpawn();
                                        }

                                        if (restoredWeapon != null && pawn.equipment != null)
                                        {
                                            pawn.equipment.AddEquipment(restoredWeapon);
                                        }
                                    }

                                    break;
                                }
                            }
                        }

                        EndJobWith(JobCondition.Succeeded);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Never,
                handlingFacing = true
            };
            yield return attack;
        }

        private bool IsVictimStillFlagged(Pawn p)
        {
            return p.TryGetComp<CompBullyFlags>()?.IsVictim ?? false;
        }

        private bool IsBullyStillFlagged(Pawn p)
        {
            return p.TryGetComp<CompBullyFlags>()?.IsBully ?? false;
        }

        private bool MeleeExpCapped(Pawn p)
        {
            //var skill = p.skills?.GetSkill(SkillDefOf.Melee);
            //return skill == null || skill.xpSinceMidnight >= 4000f;
            return p.skills?.GetSkill(SkillDefOf.Melee)?.LearningSaturatedToday == true;
        }
    }
}
