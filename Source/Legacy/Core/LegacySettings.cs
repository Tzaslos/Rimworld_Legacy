using Verse;

namespace Legacy.Core
{
    public class LegacySettings : ModSettings
    {
        public const int DefaultUpdateIntervalTicks = 2500;
        public const float DefaultMinimumAbsoluteImpact = 4f;
        public const float DefaultConsequenceScale = 1f;
        public const float DefaultHeroThreshold = 30f;
        public const float DefaultNemesisThreshold = -30f;
        public const float DefaultKarmaHeroThreshold = 250f;
        public const float DefaultKarmaNemesisThreshold = -250f;
        public const int DefaultConsequenceIntervalTicks = 60000;

        public int updateIntervalTicks = DefaultUpdateIntervalTicks;
        public float minimumAbsoluteImpact = DefaultMinimumAbsoluteImpact;
        public float consequenceScale = DefaultConsequenceScale;
        public bool enableConsequences = false;
        public bool debugMode = false;
        public bool showDebugDetails = false;
        public bool debugUseHardTimerConsequences = false;
        public bool useKarmaModForRelationshipStates = false;
        public bool useRimWorldOfMagicIntegration = true;
        public bool useRjwIntegration = false;
        public float heroThreshold = DefaultHeroThreshold;
        public float nemesisThreshold = DefaultNemesisThreshold;
        public float karmaHeroThreshold = DefaultKarmaHeroThreshold;
        public float karmaNemesisThreshold = DefaultKarmaNemesisThreshold;
        public int consequenceIntervalTicks = DefaultConsequenceIntervalTicks;

        public void ResetToDefaults()
        {
            updateIntervalTicks = DefaultUpdateIntervalTicks;
            minimumAbsoluteImpact = DefaultMinimumAbsoluteImpact;
            consequenceScale = DefaultConsequenceScale;
            enableConsequences = false;
            debugMode = false;
            showDebugDetails = false;
            debugUseHardTimerConsequences = false;
            useKarmaModForRelationshipStates = false;
            useRimWorldOfMagicIntegration = true;
            useRjwIntegration = false;
            heroThreshold = DefaultHeroThreshold;
            nemesisThreshold = DefaultNemesisThreshold;
            karmaHeroThreshold = DefaultKarmaHeroThreshold;
            karmaNemesisThreshold = DefaultKarmaNemesisThreshold;
            consequenceIntervalTicks = DefaultConsequenceIntervalTicks;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref updateIntervalTicks, "updateIntervalTicks", DefaultUpdateIntervalTicks);
            Scribe_Values.Look(ref minimumAbsoluteImpact, "minimumAbsoluteImpact", DefaultMinimumAbsoluteImpact);
            Scribe_Values.Look(ref consequenceScale, "consequenceScale", DefaultConsequenceScale);
            Scribe_Values.Look(ref enableConsequences, "enableConsequences", false);
            Scribe_Values.Look(ref debugMode, "debugMode", false);
            Scribe_Values.Look(ref showDebugDetails, "showDebugDetails", false);
            Scribe_Values.Look(ref debugUseHardTimerConsequences, "debugUseHardTimerConsequences", false);
            Scribe_Values.Look(ref useKarmaModForRelationshipStates, "useKarmaModForRelationshipStates", false);
            Scribe_Values.Look(ref useRimWorldOfMagicIntegration, "useRimWorldOfMagicIntegration", true);
            Scribe_Values.Look(ref useRjwIntegration, "useRjwIntegration", false);
            Scribe_Values.Look(ref heroThreshold, "heroThreshold", DefaultHeroThreshold);
            Scribe_Values.Look(ref nemesisThreshold, "nemesisThreshold", DefaultNemesisThreshold);
            Scribe_Values.Look(ref karmaHeroThreshold, "karmaHeroThreshold", DefaultKarmaHeroThreshold);
            Scribe_Values.Look(ref karmaNemesisThreshold, "karmaNemesisThreshold", DefaultKarmaNemesisThreshold);
            Scribe_Values.Look(ref consequenceIntervalTicks, "consequenceIntervalTicks", DefaultConsequenceIntervalTicks);
        }
    }
}
