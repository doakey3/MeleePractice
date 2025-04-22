// Source/MeleePracticeMod.cs
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace MeleePractice
{
    public class MeleePracticeMod : Mod
    {
        public static MeleePracticeSettings Settings;

        public MeleePracticeMod(ModContentPack pack) : base(pack)
        {
            Settings = GetSettings<MeleePracticeSettings>();
        }

        public override string SettingsCategory() => "Melee Practice";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard { ColumnWidth = inRect.width };
            listing.Begin(inRect);

            // master toggle
            listing.CheckboxLabeled("Stop when melee learning saturated", ref Settings.stopWhenSaturated);

            listing.Gap(30f);

            DrawSlider(listing, "Colonists" , ref Settings.painColonist );
            listing.Gap(30f);
            DrawSlider(listing, "Guests"    , ref Settings.painGuest    );
            listing.Gap(30f);
            DrawSlider(listing, "Slaves"    , ref Settings.painSlave    );
            listing.Gap(30f);
            DrawSlider(listing, "Prisoners" , ref Settings.painPrisoner );
            listing.Gap(30f);
            DrawSlider(listing, "Animals"   , ref Settings.painAnimal   );
            listing.Gap(30f);

            Rect resetRect = listing.GetRect(Text.LineHeight);
            if (Widgets.ButtonText(resetRect, "Reset to defaults"))
            {
                Settings.Reset();
                SoundDefOf.Click.PlayOneShotOnCamera();     // optional click sound
            }

            listing.End();
            Settings.Write();
        }

        private static void DrawSlider(Listing_Standard list, string label, ref float value)
        {
            value = Widgets.HorizontalSlider(
                list.GetRect(22f),   // rect
                value,               // current value
                0.05f,               // min
                1f,                  // max
                false,               // middle‑alignment flag
                label + $" – stop when pain ≥ {value.ToStringPercent("F0")}"   // text
            );
        }
    }
}



