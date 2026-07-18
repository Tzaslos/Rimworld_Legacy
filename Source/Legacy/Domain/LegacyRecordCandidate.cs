using System.Collections.Generic;
using Verse;

namespace Legacy.Domain
{
    public class LegacyRecordCandidate
    {
        public LegacyEventDef eventDef;
        public Pawn subject;
        public List<LegacyParticipantCandidate> participants = new List<LegacyParticipantCandidate>();
        public LegacyContext context;
        public int tick;
        public float moodOffset;
        public LegacyImpactPolarity polarity;
        public LegacyImpactSourceType sourceType;
        public string sourceThoughtDefName;
        public int sourceStageIndex = -1;
        public string label;
        public string description;
    }
}
