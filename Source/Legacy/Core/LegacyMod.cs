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
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, Settings.debugMode ? 820f : 620f);
            Widgets.BeginScrollView(inRect, ref settingsScroll, viewRect);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);

            SectionHeader(listing, "Basic settings");
            listing.Label("Minimum impact to record: " + Settings.minimumAbsoluteImpact.ToString("0"));
            Settings.minimumAbsoluteImpact = listing.Slider(Settings.minimumAbsoluteImpact, 1f, 20f);
            listing.Gap();

            listing.Label("Hero relationship threshold: +" + Settings.heroThreshold.ToString("0"));
            Settings.heroThreshold = listing.Slider(Settings.heroThreshold, 5f, 100f);
            listing.Gap();

            listing.Label("Nemesis relationship threshold: " + Settings.nemesisThreshold.ToString("0"));
            Settings.nemesisThreshold = listing.Slider(Settings.nemesisThreshold, -100f, -5f);
            listing.Gap();

            listing.CheckboxLabeled("Enable Legacy consequences", ref Settings.enableConsequences);
            listing.Label("Allows storyteller-paced Legacy gifts and threats when eligible off-map relationships exist.");
            listing.Label("Consequence scale: " + Settings.consequenceScale.ToString("0.0") + "x");
            Settings.consequenceScale = listing.Slider(Settings.consequenceScale, 0.1f, 5f);
            listing.Gap();

            SectionHeader(listing, "Supported mods");
            bool karmaActive = LegacyModIntegrationService.IsKarmaActive();
            bool magicActive = LegacyModIntegrationService.IsRimWorldOfMagicActive();
            bool rjwActive = LegacyModIntegrationService.IsRjwActive();

            listing.Label("Karma & Reputation: " + LegacyModIntegrationService.DetectedText(karmaActive));
            listing.CheckboxLabeled("Use Karma relationship states and recent pawn deeds", ref Settings.useKarmaModForRelationshipStates, karmaActive ? null : "Karma & Reputation is not active.");
            if (Settings.useKarmaModForRelationshipStates && karmaActive)
            {
                listing.Label("Karma hero threshold: +" + Settings.karmaHeroThreshold.ToString("0"));
                Settings.karmaHeroThreshold = listing.Slider(Settings.karmaHeroThreshold, 25f, 1000f);
                listing.Label("Karma nemesis threshold: " + Settings.karmaNemesisThreshold.ToString("0"));
                Settings.karmaNemesisThreshold = listing.Slider(Settings.karmaNemesisThreshold, -1000f, -25f);
            }
            listing.Gap();

            listing.Label("RimWorld of Magic: " + LegacyModIntegrationService.DetectedText(magicActive));
            listing.CheckboxLabeled("Record witnessed magic and class-flavored Legacy events", ref Settings.useRimWorldOfMagicIntegration, magicActive ? null : "RimWorld of Magic is not active.");
            listing.Gap();

            listing.Label("RimJobWorld: " + LegacyModIntegrationService.DetectedText(rjwActive));
            listing.CheckboxLabeled("Use RJW interaction attribution for Legacy records", ref Settings.useRjwIntegration, rjwActive ? null : "RJW is not active.");
            listing.Label("Current support attributes RJW-related thoughts to the interacting pawn. Specific witnessed RJW event categories can be expanded separately.");
            listing.Gap();

            SectionHeader(listing, "Debugging");
            listing.CheckboxLabeled("Debug mode", ref Settings.debugMode);
            if (Settings.debugMode)
            {
                listing.Gap();

                float updateIntervalSeconds = Settings.updateIntervalTicks / TicksPerSecond;
                listing.Label("Update interval: " + updateIntervalSeconds.ToString("0.0") + " seconds");
                updateIntervalSeconds = listing.Slider(updateIntervalSeconds, 250f / TicksPerSecond, 10000f / TicksPerSecond);
                Settings.updateIntervalTicks = SecondsToTicks(updateIntervalSeconds);
                listing.Gap();

                listing.CheckboxLabeled("Use hard-timer consequences for testing", ref Settings.debugUseHardTimerConsequences);
                float consequenceIntervalMinutes = Settings.consequenceIntervalTicks / TicksPerMinute;
                listing.Label("Hard-timer consequence interval: " + consequenceIntervalMinutes.ToString("0.0") + " minutes");
                consequenceIntervalMinutes = listing.Slider(consequenceIntervalMinutes, 10000f / TicksPerMinute, 240000f / TicksPerMinute);
                Settings.consequenceIntervalTicks = MinutesToTicks(consequenceIntervalMinutes);
                listing.Gap();

                listing.CheckboxLabeled("Show debug details in pawn Legacy window", ref Settings.showDebugDetails);
                listing.Gap();
            }

            if (listing.ButtonText("Reset Legacy settings to defaults"))
            {
                Settings.ResetToDefaults();
            }

            listing.End();
            Widgets.EndScrollView();
            Settings.Write();
        }

        private static void SectionHeader(Listing_Standard listing, string label)
        {
            listing.GapLine();
            GameFont previousFont = Text.Font;
            Text.Font = GameFont.Medium;
            listing.Label(label);
            Text.Font = previousFont;
            listing.Gap(4f);
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
