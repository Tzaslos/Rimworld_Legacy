using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Legacy.Services
{
    public static class LegacyRecentPawnAttributionService
    {
        private const int AttributionWindowTicks = 30000;
        private static readonly string[] AttributionKeywords =
        {
            "organ", "harvest", "bodypart", "body part", "prosthetic", "implant", "installed", "removed",
            "scar", "torture", "medical", "operation", "injur", "wound", "harm", "pain", "attack",
            "shot", "stab", "melee", "killed", "died", "death", "murder", "capture", "captured",
            "prisoner", "imprison", "enslave", "slave", "recruit", "convert", "conversion", "released",
            "rescued", "rescue", "kidnap", "banish", "execute", "executed",
            "rjw", "lovin", "partner", "comfort", "bred", "groped", "stole", "virginity", "violat",
            "forced", "abuse", "assault", "raped", "rapist", "broken", "ahegao"
        };

        private static readonly Dictionary<int, Attribution> RecentAttributions = new Dictionary<int, Attribution>();

        public static void Register(Pawn subject, Pawn actor, string source)
        {
            if (!LegacyPawnEligibilityService.CanCreateLegacyEvents(subject)
                || !LegacyPawnEligibilityService.CanCreateLegacyEvents(actor)
                || subject == actor)
            {
                return;
            }

            RecentAttributions[subject.thingIDNumber] = new Attribution
            {
                Actor = actor,
                Source = source,
                Tick = Find.TickManager != null ? Find.TickManager.TicksGame : 0
            };
        }

        public static Pawn TryGetRecentActor(Pawn subject, Thought thought)
        {
            string source;
            return TryGetRecentActor(subject, thought, out source);
        }

        public static Pawn TryGetRecentActor(Pawn subject, Thought thought, out string source)
        {
            source = null;
            if (subject == null || thought == null || !IsAttributableThought(thought))
            {
                return null;
            }

            Attribution attribution;
            if (!RecentAttributions.TryGetValue(subject.thingIDNumber, out attribution))
            {
                return null;
            }

            int currentTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            if (currentTick - attribution.Tick > AttributionWindowTicks)
            {
                RecentAttributions.Remove(subject.thingIDNumber);
                return null;
            }

            if (!LegacyPawnEligibilityService.CanCreateLegacyEvents(attribution.Actor))
            {
                return null;
            }

            source = attribution.Source;
            return attribution.Actor;
        }

        private static bool IsAttributableThought(Thought thought)
        {
            string defName = thought.def != null ? thought.def.defName.ToLowerInvariant() : string.Empty;
            string label = thought.LabelCap.ToString().ToLowerInvariant();

            return ContainsAny(defName) || ContainsAny(label);
        }

        private static bool ContainsAny(string value)
        {
            foreach (string keyword in AttributionKeywords)
            {
                if (value.Contains(keyword))
                {
                    return true;
                }
            }

            return false;
        }

        private class Attribution
        {
            public Pawn Actor;
            public string Source;
            public int Tick;
        }
    }
}
