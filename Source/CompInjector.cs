using System.Linq;
using Verse;

namespace MeleePractice
{
    [StaticConstructorOnStartup]
    public static class CompInjector
    {
        static CompInjector()
        {
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
            {
                if (def.race == null || def.comps == null)
                    continue;

                bool alreadyHas = def.comps.Any(c => c.compClass == typeof(CompBullyFlags));
                if (!alreadyHas)
                {
                    def.comps.Add(new CompProperties
                    {
                        compClass = typeof(CompBullyFlags)
                    });
                }
            }
        }
    }
}
