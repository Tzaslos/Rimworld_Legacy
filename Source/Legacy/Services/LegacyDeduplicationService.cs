using System.Collections.Generic;
using Legacy.Domain;
using Verse;

namespace Legacy.Services
{
    public static class LegacyDeduplicationService
    {
        private const int DuplicateTickWindow = 60;
        private const int TriggeredThoughtDuplicateTickWindow = 60000;
        private static readonly Dictionary<string, int> RecentTriggerTicks = new Dictionary<string, int>();

        public static bool IsDuplicate(LegacyRecordCandidate candidate, IEnumerable<LegacyRecord> existingRecords)
        {
            if (IsRecentTriggerDuplicate(candidate))
            {
                return true;
            }

            foreach (LegacyRecord existing in existingRecords)
            {
                if (existing == null || existing.eventDef == null || existing.subject == null)
                {
                    continue;
                }

                if (existing.eventDef != candidate.eventDef)
                {
                    continue;
                }

                if (existing.subject.thingIdNumber != candidate.subject.thingIDNumber)
                {
                    continue;
                }

                int candidateOtherPawnId = OtherPawnId(candidate);
                int existingOtherPawnId = OtherPawnId(existing);
                if (candidateOtherPawnId != existingOtherPawnId)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(candidate.sourceThoughtDefName))
                {
                    if (existing.sourceThoughtDefName != candidate.sourceThoughtDefName)
                    {
                        continue;
                    }

                    if (SameStableCause(existing, candidate))
                    {
                        return true;
                    }

                    if (candidate.sourceType == LegacyImpactSourceType.PersistentSituation)
                    {
                        return true;
                    }

                    if (System.Math.Abs(existing.tick - candidate.tick) <= TriggeredThoughtDuplicateTickWindow)
                    {
                        return true;
                    }
                }

                if (System.Math.Abs(existing.tick - candidate.tick) <= DuplicateTickWindow)
                {
                    return true;
                }
            }

            RememberRecentTrigger(candidate);
            return false;
        }

        private static bool IsRecentTriggerDuplicate(LegacyRecordCandidate candidate)
        {
            string key = StableTriggerKey(candidate);
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            int lastTick;
            if (!RecentTriggerTicks.TryGetValue(key, out lastTick))
            {
                return false;
            }

            return System.Math.Abs(CurrentTick() - lastTick) <= TriggeredThoughtDuplicateTickWindow;
        }

        private static void RememberRecentTrigger(LegacyRecordCandidate candidate)
        {
            string key = StableTriggerKey(candidate);
            if (!string.IsNullOrEmpty(key))
            {
                RecentTriggerTicks[key] = CurrentTick();
            }
        }

        private static string StableTriggerKey(LegacyRecordCandidate candidate)
        {
            if (candidate == null || candidate.subject == null || string.IsNullOrEmpty(candidate.sourceThoughtDefName))
            {
                return null;
            }

            return candidate.eventDef.defName
                + "|" + candidate.subject.thingIDNumber
                + "|" + OtherPawnId(candidate)
                + "|" + candidate.sourceThoughtDefName
                + "|" + StableCause(candidate);
        }

        private static string StableCause(LegacyRecordCandidate candidate)
        {
            if (candidate.context == null || string.IsNullOrEmpty(candidate.context.cause))
            {
                return string.Empty;
            }

            return candidate.context.cause;
        }

        private static int CurrentTick()
        {
            return Find.TickManager != null ? Find.TickManager.TicksGame : 0;
        }

        private static bool SameStableCause(LegacyRecord existing, LegacyRecordCandidate candidate)
        {
            string existingCause = existing.context != null ? existing.context.cause : null;
            string candidateCause = candidate.context != null ? candidate.context.cause : null;
            if (!string.IsNullOrEmpty(existingCause) && !string.IsNullOrEmpty(candidateCause) && existingCause == candidateCause)
            {
                return true;
            }

            if (!string.IsNullOrEmpty(existing.label) && !string.IsNullOrEmpty(candidate.label) && existing.label == candidate.label)
            {
                return true;
            }

            return false;
        }

        private static int OtherPawnId(LegacyRecordCandidate candidate)
        {
            if (candidate.participants == null)
            {
                return -1;
            }

            foreach (LegacyParticipantCandidate participant in candidate.participants)
            {
                if (participant != null && participant.role == LegacyParticipantRole.OtherPawn && participant.pawn != null)
                {
                    return participant.pawn.thingIDNumber;
                }
            }

            return -1;
        }

        private static int OtherPawnId(LegacyRecord record)
        {
            if (record.participants == null)
            {
                return -1;
            }

            foreach (LegacyParticipant participant in record.participants)
            {
                if (participant != null && participant.role == LegacyParticipantRole.OtherPawn && participant.pawn != null)
                {
                    return participant.pawn.thingIdNumber;
                }
            }

            return -1;
        }
    }
}
