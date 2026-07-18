using RimWorld;

namespace Legacy.Domain
{
    [DefOf]
    public static class LegacyEventDefOf
    {
        public static LegacyEventDef Legacy_ThoughtImpact;
        public static LegacyEventDef Legacy_MagicWitnessed;
        public static LegacyEventDef Legacy_NecromancyWitnessed;

        static LegacyEventDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(LegacyEventDefOf));
        }
    }
}
