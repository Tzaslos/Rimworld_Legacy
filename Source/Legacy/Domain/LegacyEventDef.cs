using System.Collections.Generic;
using Verse;

namespace Legacy.Domain
{
    public class LegacyEventDef : Def
    {
        public LegacyEventKind kind;
        public LegacyEventCategory category;
        public LegacyEventSeverity severity;
        public float reputationWeight;
        public List<string> biographyTags;
    }
}
