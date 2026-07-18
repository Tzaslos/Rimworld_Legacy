using System.Collections.Generic;
using Verse;

namespace Legacy.Domain
{
    public class LegacyRecord : IExposable
    {
        public string id;
        public LegacyEventDef eventDef;
        public LegacySubjectRef subject;
        public List<LegacyParticipant> participants = new List<LegacyParticipant>();
        public LegacyContext context;
        public int tick;
        public float moodOffset;
        public LegacyImpactPolarity polarity;
        public LegacyImpactSourceType sourceType;
        public string sourceThoughtDefName;
        public int sourceStageIndex = -1;
        public string label;
        public string description;

        public void ExposeData()
        {
            Scribe_Values.Look(ref id, "id");
            Scribe_Defs.Look(ref eventDef, "eventDef");
            Scribe_Deep.Look(ref subject, "subject");
            Scribe_Collections.Look(ref participants, "participants", LookMode.Deep);
            Scribe_Deep.Look(ref context, "context");
            Scribe_Values.Look(ref tick, "tick");
            Scribe_Values.Look(ref moodOffset, "moodOffset");
            Scribe_Values.Look(ref polarity, "polarity");
            Scribe_Values.Look(ref sourceType, "sourceType");
            Scribe_Values.Look(ref sourceThoughtDefName, "sourceThoughtDefName");
            Scribe_Values.Look(ref sourceStageIndex, "sourceStageIndex", -1);
            Scribe_Values.Look(ref label, "label");
            Scribe_Values.Look(ref description, "description");

            if (Scribe.mode == LoadSaveMode.PostLoadInit && participants == null)
            {
                participants = new List<LegacyParticipant>();
            }
        }
    }
}
