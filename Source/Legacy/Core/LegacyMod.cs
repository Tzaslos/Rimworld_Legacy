using Legacy.Patches;
using Legacy.Services;
using UnityEngine;
using Verse;

namespace Legacy.Core
{
    public class LegacyMod : Mod
    {
        private const float TicksPerSecond = 60f;
        private const float TicksPerMinute = 3600f;
        public static LegacySettings Settings;
        private Vector2 settingsScroll;

        public LegacyMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<LegacySettings>();
            HarmonyBootstrap.ApplyPatches();
        }

        public override string SettingsCategory()
        {
            return "Legacy";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, 560f);
            Widgets.BeginScrollView(inRect, ref settingsScroll, viewRect);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);

            float updateIntervalSeconds = Settings.updateIntervalTicks / TicksPerSecond;
            listing.Label("Update interval: " + updateIntervalSeconds.ToString("0.0") + " seconds");
            updateIntervalSeconds = listing.Slider(updateIntervalSeconds, 250f / TicksPerSecond, 10000f / TicksPerSecond);
            Settings.updateIntervalTicks = SecondsToTicks(updateIntervalSeconds);
            listing.Gap();

            listing.Label("Minimum impact to record: " + Settings.minimumAbsoluteImpact.ToString("0"));
            Settings.minimumAbsoluteImpact = listing.Slider(Settings.minimumAbsoluteImpact, 1f, 20f);
            listing.Gap();

            listing.Label("Hero threshold: +" + Settings.heroThreshold.ToString("0"));
            Settings.heroThreshold = listing.Slider(Settings.heroThreshold, 5f, 100f);
            listing.Gap();

            listing.Label("Nemesis threshold: " + Settings.nemesisThreshold.ToString("0"));
            Settings.nemesisThreshold = listing.Slider(Settings.nemesisThreshold, -100f, -5f);
            listing.Gap();

            listing.CheckboxLabeled("Enable consequence scaling", ref Settings.enableConsequences);
            listing.Label("Consequence scale: " + Settings.consequenceScale.ToString("0.0") + "x");
            Settings.consequenceScale = listing.Slider(Settings.consequenceScale, 0.1f, 5f);
            float consequenceIntervalMinutes = Settings.consequenceIntervalTicks / TicksPerMinute;
            listing.Label("Consequence interval: " + consequenceIntervalMinutes.ToString("0.0") + " minutes");
            consequenceIntervalMinutes = listing.Slider(consequenceIntervalMinutes, 10000f / TicksPerMinute, 240000f / TicksPerMinute);
            Settings.consequenceIntervalTicks = MinutesToTicks(consequenceIntervalMinutes);
            listing.Gap();
            listing.Label("High negative Legacy scores can later scale hostile consequences. High positive scores can later scale gifts, aid, or other benefits.");
            listing.Gap();

            listing.CheckboxLabeled("Show debug details in pawn Legacy window", ref Settings.showDebugDetails);
            listing.Gap();

            bool karmaActive = LegacyKarmaCompatibilityService.IsKarmaModActive();
            listing.CheckboxLabeled("Use Karma & Reputation for Legacy relationship states", ref Settings.useKarmaModForRelationshipStates, karmaActive ? null : "Karma & Reputation is not active. This setting will do nothing until package astryl.KarmaReputation is loaded.");
            if (Settings.useKarmaModForRelationshipStates)
            {
                listing.Label("Karma hero threshold: +" + Settings.karmaHeroThreshold.ToString("0"));
                Settings.karmaHeroThreshold = listing.Slider(Settings.karmaHeroThreshold, 25f, 1000f);
                listing.Label("Karma nemesis threshold: " + Settings.karmaNemesisThreshold.ToString("0"));
                Settings.karmaNemesisThreshold = listing.Slider(Settings.karmaNemesisThreshold, -1000f, -25f);
            }
            listing.Label(karmaActive ? "Karma & Reputation detected." : "Karma & Reputation not detected.");
            listing.Gap();

            if (listing.ButtonText("Reset Legacy settings to defaults"))
            {
                Settings.ResetToDefaults();
            }

            listing.End();
            Widgets.EndScrollView();
            Settings.Write();
        }

        private static int SecondsToTicks(float seconds)
        {
            return (int)(seconds * TicksPerSecond);
        }

        private static int MinutesToTicks(float minutes)
        {
            return (int)(minutes * TicksPerMinute);
        }
    }
}
