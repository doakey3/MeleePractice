using RimWorld;   //  <-- new
using Verse;

namespace MeleePractice
{
    public class MeleePracticeSettings : ModSettings
    {
        public bool  stopWhenSaturated = true;

        public float painColonist  = 0.15f; // hit colonists 1-2 times
        public float painGuest     = 0.15f; // hit guests 1-2 times
        public float painSlave     = 0.30f; // beat slaves just shy of the intense pain mood debuff
        public float painPrisoner  = 0.60f; // Severely beat prisoners
        public float painAnimal    = 1.00f; // Down animals

        /* -----------------------------------------------------------------
         *  Return the pain‑threshold that should stop the beating for
         *  whatever kind of pawn ‘p’ is.
         * ----------------------------------------------------------------*/
        public float PainLimitFor(Pawn p)
        {
            if (p.RaceProps.Animal)                     return painAnimal;
            if (p.IsSlaveOfColony)                      return painSlave;
            if (p.IsPrisonerOfColony)                   return painPrisoner;
            if (p.IsColonistPlayerControlled)           return painColonist;

            /* guests = any humanlike hosted by the player
             * that is NOT a prisoner / slave / colonist                               */
            if (p.HostFaction == Faction.OfPlayer)       return painGuest;

            /* fallback – shouldn’t really happen                                    */
            return 0.50f;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref stopWhenSaturated, "stopWhenSaturated", true);
            Scribe_Values.Look(ref painColonist ,  "painColonist" ,  0.15f);
            Scribe_Values.Look(ref painGuest    ,  "painGuest"    ,  0.15f);
            Scribe_Values.Look(ref painSlave    ,  "painSlave"    ,  0.30f);
            Scribe_Values.Look(ref painPrisoner ,  "painPrisoner" ,  0.60f);
            Scribe_Values.Look(ref painAnimal   ,  "painAnimal"   ,  1.00f);
        }

        public void Reset()
        {
            stopWhenSaturated = true;

            painColonist  = 0.15f;
            painGuest     = 0.15f;
            painSlave     = 0.30f;
            painPrisoner  = 0.60f;
            painAnimal    = 1.00f;
        }
    }
}
