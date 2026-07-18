using Legacy.Domain;
using Verse;

namespace Legacy.Services
{
    public static class LegacyRecordCandidateFactory
    {
        public static LegacyRecordCandidate ForPawnEvent(
            LegacyEventDef eventDef,
            Pawn subject,
            string cause,
            string extraDescription)
        {
            if (subject == null)
            {
                return null;
            }

            return new LegacyRecordCandidate
            {
                eventDef = eventDef,
                subject = subject,
                context = CreateContext(subject, cause, extraDescription),
                tick = Find.TickManager != null ? Find.TickManager.TicksGame : 0
            };
        }

        private static LegacyContext CreateContext(Pawn pawn, string cause, string extraDescription)
        {
            return new LegacyContext
            {
                tile = pawn.Map != null ? (int)pawn.Map.Tile : -1,
                mapId = pawn.Map != null ? pawn.Map.uniqueID.ToString() : null,
                factionDefName = pawn.Faction != null && pawn.Faction.def != null ? pawn.Faction.def.defName : null,
                factionName = pawn.Faction != null ? pawn.Faction.Name : null,
                cause = cause,
                extraDescription = extraDescription
            };
        }
    }
}
