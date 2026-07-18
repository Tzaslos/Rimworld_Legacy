using System.Collections.Generic;
using Legacy.Domain;
using Legacy.Services;
using Legacy.Storage;
using Verse;

namespace Legacy.API
{
    public static class LegacyApi
    {
        private static readonly IReadOnlyList<LegacyRecord> NoRecords = new List<LegacyRecord>();

        public static bool IsAvailable
        {
            get { return Find.World != null && Find.World.GetComponent<LegacyWorldComponent>() != null; }
        }

        public static bool TryRecordEvent(LegacyApiRecordRequest request, out LegacyRecord record)
        {
            record = null;

            if (request == null || request.subject == null)
            {
                return false;
            }

            LegacyEventDef eventDef = ResolveEventDef(request);
            if (eventDef == null)
            {
                return false;
            }

            LegacyRecordCandidate candidate = LegacyRecordCandidateFactory.ForPawnEvent(
                eventDef,
                request.subject,
                request.cause,
                request.extraDescription);

            if (candidate == null)
            {
                return false;
            }

            candidate.participants.Add(new LegacyParticipantCandidate(request.subject, LegacyParticipantRole.Subject));

            if (request.participants != null)
            {
                foreach (LegacyParticipantCandidate participant in request.participants)
                {
                    if (participant != null && participant.pawn != null)
                    {
                        candidate.participants.Add(participant);
                    }
                }
            }

            candidate.moodOffset = request.moodOffset;
            candidate.polarity = request.polarity;
            candidate.sourceType = request.sourceType;
            candidate.sourceThoughtDefName = request.sourceThoughtDefName;
            candidate.sourceStageIndex = request.sourceStageIndex;
            candidate.label = request.label;
            candidate.description = request.description;

            if (request.tick.HasValue)
            {
                candidate.tick = request.tick.Value;
            }

            return LegacyRecordService.TryRecord(candidate, out record);
        }

        public static bool TryRecordEvent(string eventDefName, Pawn subject, Pawn otherPawn, LegacyParticipantRole otherPawnRole, string cause, string description, out LegacyRecord record)
        {
            LegacyApiRecordRequest request = new LegacyApiRecordRequest
            {
                eventDefName = eventDefName,
                subject = subject,
                cause = cause,
                extraDescription = description,
                description = description
            };

            if (otherPawn != null)
            {
                request.WithParticipant(otherPawn, otherPawnRole);
            }

            return TryRecordEvent(request, out record);
        }

        public static IReadOnlyList<LegacyRecord> AllRecords()
        {
            LegacyWorldComponent component = CurrentComponent();
            return component != null ? component.Repository.AllRecords() : EmptyRecords();
        }

        public static IReadOnlyList<LegacyRecord> RecordsForPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                return EmptyRecords();
            }

            return RecordsForPawn(pawn.thingIDNumber);
        }

        public static IReadOnlyList<LegacyRecord> RecordsForPawn(int thingIdNumber)
        {
            LegacyWorldComponent component = CurrentComponent();
            return component != null ? component.Repository.RecordsForPawn(thingIdNumber) : EmptyRecords();
        }

        public static IReadOnlyList<LegacyRecord> RecordsOfKind(LegacyEventKind kind)
        {
            LegacyWorldComponent component = CurrentComponent();
            return component != null ? component.Repository.RecordsOfKind(kind) : EmptyRecords();
        }

        private static LegacyEventDef ResolveEventDef(LegacyApiRecordRequest request)
        {
            if (request.eventDef != null)
            {
                return request.eventDef;
            }

            return string.IsNullOrEmpty(request.eventDefName)
                ? null
                : DefDatabase<LegacyEventDef>.GetNamedSilentFail(request.eventDefName);
        }

        private static LegacyWorldComponent CurrentComponent()
        {
            return Find.World != null ? Find.World.GetComponent<LegacyWorldComponent>() : null;
        }

        private static IReadOnlyList<LegacyRecord> EmptyRecords()
        {
            return NoRecords;
        }
    }
}
