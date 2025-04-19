using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace MeleePractice
{
    public class CompBullyFlags : ThingComp
    {
        private bool isVictim;
        private bool isBully;

        public bool IsVictim => isVictim;
        public bool IsBully => isBully;

        public void ToggleVictim()
        {
            isVictim = !isVictim;
        }

        public void ToggleBully()
        {
            isBully = !isBully;
        }

        // Determines when to show a Gizmo for victim toggle
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent is not Pawn pawn)
                yield break;

            // Allow colonists, prisoners, slaves, tamed animals
            // Allow if the pawn is a colonist
            bool isColonist = pawn.IsColonist;

            // Allow if the pawn is a prisoner of the colony
            bool isColonyPrisoner = pawn.IsPrisonerOfColony;

            // Allow if the pawn is a slave of the colony
            bool isColonySlave = pawn.IsSlaveOfColony;

            // Allow if the pawn is a tamed animal belonging to the colony
            bool isColonyAnimal = pawn.RaceProps.Animal && pawn.Faction == Faction.OfPlayer;

            if (!(isColonist || isColonyPrisoner || isColonySlave || isColonyAnimal))
                yield break;

            yield return new Command_Action
            {
                defaultLabel = isVictim ? "Cancel Victim" : "Mark as Victim",
                defaultDesc = "Toggle whether this pawn or animal should be used for melee practice.",
                icon = ContentFinder<Texture2D>.Get(isVictim ? "UI/BullyVictimCancel" : "UI/BullyVictim"),
                action = () =>
                {
                    ToggleVictim();
                    SoundStarter.PlayOneShotOnCamera(
                        isVictim ? SoundDefOf.Checkbox_TurnedOn : SoundDefOf.Checkbox_TurnedOff
                    );
                }
            };

            // Determines when to show a Gizmo for bully toggle

            bool isNonViolent = pawn.WorkTagIsDisabled(WorkTags.Violent);

            // Only colonists and slaves can bully
            if ((isColonist || isColonySlave) && !isNonViolent)
            {
                yield return new Command_Action
                {
                    defaultLabel = isBully ? "Cancel Bullying" : "Start Bullying",
                    defaultDesc = "Toggle whether this pawn is allowed to train melee by bullying others.",
                    icon = ContentFinder<Texture2D>.Get(isBully ? "UI/BullyOthersCancel" : "UI/BullyOthers"),
                    action = () =>
                    {
                        ToggleBully();
                        SoundStarter.PlayOneShotOnCamera(
                            isBully ? SoundDefOf.Checkbox_TurnedOn : SoundDefOf.Checkbox_TurnedOff
                        );
                    }
                };
            }
        }

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref isVictim, "isVictim", false);
            Scribe_Values.Look(ref isBully, "isBully", false);
        }
    }
}
