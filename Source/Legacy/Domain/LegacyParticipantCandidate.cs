using Verse;

namespace Legacy.Domain
{
    public class LegacyParticipantCandidate
    {
        public Pawn pawn;
        public LegacyParticipantRole role;

        public LegacyParticipantCandidate(Pawn pawn, LegacyParticipantRole role)
        {
            this.pawn = pawn;
            this.role = role;
        }
    }
}
