using Verse;

namespace Legacy.Domain
{
    public class LegacyParticipant : IExposable
    {
        public LegacySubjectRef pawn;
        public LegacyParticipantRole role;

        public LegacyParticipant()
        {
        }

        public LegacyParticipant(LegacySubjectRef pawn, LegacyParticipantRole role)
        {
            this.pawn = pawn;
            this.role = role;
        }

        public void ExposeData()
        {
            Scribe_Deep.Look(ref pawn, "pawn");
            Scribe_Values.Look(ref role, "role");
        }
    }
}
