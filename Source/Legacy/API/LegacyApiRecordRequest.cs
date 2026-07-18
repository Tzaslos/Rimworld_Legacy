using System.Collections.Generic;
using Legacy.Domain;
using Verse;

namespace Legacy.API
{
    public class LegacyApiRecordRequest
    {
        public LegacyEventDef eventDef;
        public string eventDefName;
        public Pawn subject;
        public List<LegacyParticipantCandidate> participants = new List<LegacyParticipantCandidate>();
        public string cause;
        public string extraDescription;
        public float moodOffset;
        public LegacyImpactPolarity polarity;
        public LegacyImpactSourceType sourceType;
        public string sourceThoughtDefName;
        public int sourceStageIndex = -1;
        public string label;
        public string description;
        public int? tick;

        public LegacyApiRecordRequest WithParticipant(Pawn pawn, LegacyParticipantRole role)
        {
            participants.Add(new LegacyParticipantCandidate(pawn, role));
            return this;
        }
    }
}
