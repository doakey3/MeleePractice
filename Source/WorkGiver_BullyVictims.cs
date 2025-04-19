using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace MeleePractice
{
    public class WorkGiver_BullyVictims : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override ThingRequest PotentialWorkThingRequest =>
            ThingRequest.ForGroup(ThingRequestGroup.Pawn);

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            // Check if the target is a pawn
            if (t is not Pawn victim)
            {
                //Log.Message("[MeleePractice] Target is not a pawn.");
                return false;
            }

            // Basic pawn checks
            if (!victim.Spawned || victim.Dead || victim.Downed)
            {
                //Log.Message("[MeleePractice] Victim not in valid state.");
                return false;
            }

            bool isVictimColonist = victim.IsColonist;
            bool isVictimColonyPrisoner = victim.IsPrisonerOfColony;
            bool isVictimColonySlave = victim.IsSlaveOfColony;
            bool isVictimColonyAnimal = victim.RaceProps.Animal && victim.Faction == Faction.OfPlayer;

            if (!(isVictimColonist || isVictimColonyPrisoner || isVictimColonySlave || isVictimColonyAnimal))
            {
                //Log.Message("[MeleePractice] Victim is not a valid type.");
                return false;
            }


            if (!pawn.CanReserveAndReach(victim, PathEndMode.Touch, Danger.Some))
            {
                //Log.Message("[MeleePractice] Cannot reserve or reach victim.");
                return false;
            }

            if (pawn == victim)
            {
                //Log.Message("[MeleePractice] Pawn is trying to bully itself.");
                return false;
            }

            bool isBullyColonist = pawn.IsColonist;
            bool isBullySlave = pawn.IsSlaveOfColony;
            bool isBullyNonViolent = pawn.WorkTagIsDisabled(WorkTags.Violent);

            if (!(isBullyColonist || isBullySlave) || isBullyNonViolent)
            {
                //Log.Message("[MeleePractice] Bully is not valid.");
                return false;
            }

            // Check for bully/victim components
            var bullyComp = pawn.GetComp<CompBullyFlags>();
            var victimComp = victim.GetComp<CompBullyFlags>();

            if (bullyComp == null || victimComp == null)
            {
                //Log.Message("[MeleePractice] One of the pawns lacks bully flags.");
                return false;
            }

            if (!bullyComp.IsBully)
            {
                //Log.Message("[MeleePractice] Pawn is not marked as bully.");
                return false;
            }

            if (!victimComp.IsVictim)
            {
                //Log.Message("[MeleePractice] Victim is not marked as victim.");
                return false;
            }

            if (!IsSafeToBully(victim))
            {
                //Log.Message("[MeleePractice] Victim is not safe to bully.");
                return false;
            }

            if (pawn.Dead || !pawn.Spawned || pawn.Downed || pawn.Drafted)
            {
                //Log.Message("[MeleePractice] Bully is in invalid state.");
                return false;
            }

            // Already saturated skill? skip
            var skill = pawn.skills?.GetSkill(SkillDefOf.Melee);
            if (skill == null || skill.LearningSaturatedToday)
            {
                //Log.Message("[MeleePractice] Pawn has saturated melee XP.");
                return false;
            }

            // Avoid job spam
            var bullyJobDef = DefDatabase<JobDef>.GetNamed("MeleePractice_Bully", true);
            if (pawn.CurJob?.def == bullyJobDef && pawn.CurJob.targetA.Thing == t)
            {
                //Log.Message("[MeleePractice] Already doing this job.");
                return false;
            }

            if (!pawn.CanReserve(victim))
            {
                //Log.Message("[MeleePractice] Pawn cannot reserve victim.");
                return false;
            }

            var heldWeapon = pawn.equipment?.Primary;
            float carriedMass = MassUtility.GearAndInventoryMass(pawn);
            float maxMass = MassUtility.Capacity(pawn);
            if (heldWeapon != null)
            {
                float weaponMass = heldWeapon.GetStatValue(StatDefOf.Mass);
                if (carriedMass + weaponMass > maxMass)
                {
                    //Log.Message("[MeleePractice] Pawn can't carry weapon: too heavy. Unable to swap current weapon into inventory while pummeling victim.");
                    return false;
                }
            }

            //Log.Message("[MeleePractice] All checks passed. Assigning bully job.");
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("MeleePractice_Bully"), t);
        }

        private bool IsSafeToBully(Pawn p)
        {
            // It's OK to start to bully victims who are fully healed.
            foreach (var h in p.health.hediffSet.hediffs)
            {
                if (h is Hediff_Injury injury)
                {
                    if (injury.IsPermanent())
                        continue;

                    return false;
                }
            }

            // No fresh or healing injuries found
            return true;
        }
    }
}
