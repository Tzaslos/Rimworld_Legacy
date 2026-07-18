using System;
using Legacy.Domain;
using Legacy.Storage;
using Verse;

namespace Legacy.Services
{
    public static class LegacyRecordService
    {
        public static bool TryRecord(LegacyRecordCandidate candidate, out LegacyRecord record)
        {
            record = null;

            if (candidate == null || !LegacyPawnEligibilityService.CanCreateLegacyEvents(candidate.subject) || candidate.eventDef == null || Find.World == null)
            {
                return false;
            }

            LegacyWorldComponent component = Find.World.GetComponent<LegacyWorldComponent>();
            if (component == null)
            {
                return false;
            }

            if (LegacyDeduplicationService.IsDuplicate(candidate, component.Repository.AllRecords()))
            {
                return false;
            }

            record = new LegacyRecord
            {
                id = Guid.NewGuid().ToString("N"),
                eventDef = candidate.eventDef,
                subject = LegacySubjectRef.FromPawn(candidate.subject),
                context = candidate.context,
                tick = candidate.tick,
                moodOffset = candidate.moodOffset,
                polarity = candidate.polarity,
                sourceType = candidate.sourceType,
                sourceThoughtDefName = candidate.sourceThoughtDefName,
                sourceStageIndex = candidate.sourceStageIndex,
                label = candidate.label,
                description = candidate.description
            };

            foreach (LegacyParticipantCandidate participant in candidate.participants)
            {
                if (participant != null && LegacyPawnEligibilityService.CanCreateLegacyEvents(participant.pawn))
                {
                    record.participants.Add(new LegacyParticipant(LegacySubjectRef.FromPawn(participant.pawn), participant.role));
                }
            }

            if (record.participants.Count == 0)
            {
                return false;
            }

            component.Repository.Add(record);
            return true;
        }
    }
}
