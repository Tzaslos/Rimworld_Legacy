using Verse;

namespace Legacy.Domain
{
    public class LegacyRelationshipState : IExposable
    {
        public int subjectPawnId = -1;
        public int otherPawnId = -1;
        public string subjectName;
        public string otherName;
        public LegacyRelationshipKind kind = LegacyRelationshipKind.Neutral;
        public float score;
        public int lastChangedTick;
        public int lastConsequenceTick;

        public void ExposeData()
        {
            Scribe_Values.Look(ref subjectPawnId, "subjectPawnId", -1);
            Scribe_Values.Look(ref otherPawnId, "otherPawnId", -1);
            Scribe_Values.Look(ref subjectName, "subjectName");
            Scribe_Values.Look(ref otherName, "otherName");
            Scribe_Values.Look(ref kind, "kind", LegacyRelationshipKind.Neutral);
            Scribe_Values.Look(ref score, "score");
            Scribe_Values.Look(ref lastChangedTick, "lastChangedTick");
            Scribe_Values.Look(ref lastConsequenceTick, "lastConsequenceTick");
        }
    }
}
