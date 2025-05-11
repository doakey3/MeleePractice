// Source/WorkGiver_BullyVictims.cs
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
            if (t is not Pawn vic)                       return false;
            if (!vic.Spawned || vic.Dead || vic.Downed) return false;
            if (!pawn.CanReserveAndReach(vic, PathEndMode.Touch, Danger.Some)) return false;
            if (pawn == vic)                            return false;
            if (pawn.WorkTagIsDisabled(WorkTags.Violent)) return false;
            if (!IsTargetOwnedByPlayer(vic))            return false;
            if (!IsSafeToBully(vic))                    return false;

            var bc = pawn.GetComp<CompBullyFlags>();
            var vc = vic.GetComp<CompBullyFlags>();
            if (bc?.IsBully != true || vc?.IsVictim != true) return false;

            // optionally break when XP saturated
            if (MeleePracticeMod.Settings.stopWhenSaturated &&
                pawn.skills?.GetSkill(SkillDefOf.Melee)?.LearningSaturatedToday == true)
                return false;

            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) =>
            JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("MeleePractice_Bully"), t);

        private static bool IsTargetOwnedByPlayer(Pawn p) =>
            p.Faction == Faction.OfPlayer || p.HostFaction == Faction.OfPlayer;

        private static bool IsSafeToBully(Pawn p) =>
            !p.health.hediffSet.hediffs.Any(h =>
                    h is Hediff_Injury inj && inj.CanHealNaturally() && !inj.IsPermanent());
    }
}
