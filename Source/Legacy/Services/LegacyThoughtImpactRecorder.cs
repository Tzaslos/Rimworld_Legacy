using Legacy.Core;
using Legacy.Domain;
using RimWorld;
using Verse;

namespace Legacy.Services
{
    public static class LegacyThoughtImpactRecorder
    {
        public static void TryRecord(Pawn pawn, Thought thought)
        {
            if (!LegacyPawnEligibilityService.CanCreateLegacyEvents(pawn) || thought == null || thought.def == null || !thought.VisibleInNeedsTab)
            {
                return;
            }

            string attributionSource;
            Pawn otherPawn = GetOtherPawn(pawn, thought, out attributionSource);
            if (!LegacyPawnEligibilityService.CanCreateLegacyEvents(otherPawn))
            {
                return;
            }

            float impact = GetSignedImpact(thought);
            float minimumImpact = LegacyMod.Settings != null ? LegacyMod.Settings.minimumAbsoluteImpact : 4f;
            if (impact == 0f || System.Math.Abs(impact) < minimumImpact)
            {
                return;
            }

            LegacyEventDef eventDef = LegacyEventDefOf.Legacy_ThoughtImpact;
            if (eventDef == null)
            {
                LegacyLog.Warning("Could not record thought impact because Legacy_ThoughtImpact is missing.");
                return;
            }

            LegacyRecordCandidate candidate = LegacyRecordCandidateFactory.ForPawnEvent(
                eventDef,
                pawn,
                !string.IsNullOrEmpty(attributionSource) ? attributionSource : thought.def.defName,
                thought.LabelCap.ToString());

            if (candidate == null)
            {
                return;
            }

            candidate.participants.Add(new LegacyParticipantCandidate(otherPawn, LegacyParticipantRole.OtherPawn));
            candidate.moodOffset = impact;
            candidate.polarity = impact > 0f ? LegacyImpactPolarity.Positive : LegacyImpactPolarity.Negative;
            candidate.sourceType = thought is Thought_Memory ? LegacyImpactSourceType.Memory : LegacyImpactSourceType.PersistentSituation;
            candidate.sourceThoughtDefName = thought.def.defName;
            candidate.sourceStageIndex = thought.CurStageIndex;
            candidate.label = thought.LabelCap.ToString();
            candidate.description = thought.Description;

            string karmaLabel;
            string karmaDescription;
            float karmaImpact;
            if (LegacyKarmaCompatibilityService.TryGetRecentPawnCondition(pawn, otherPawn, out karmaLabel, out karmaDescription, out karmaImpact))
            {
                candidate.label = karmaLabel;
                candidate.description = karmaDescription;
                if (karmaImpact != 0f)
                {
                    candidate.moodOffset = karmaImpact;
                    candidate.polarity = karmaImpact > 0f ? LegacyImpactPolarity.Positive : LegacyImpactPolarity.Negative;
                }
            }

            LegacyRecord record;
            LegacyRecordService.TryRecord(candidate, out record);
        }

        private static Pawn GetOtherPawn(Pawn pawn, Thought thought, out string attributionSource)
        {
            attributionSource = null;
            ISocialThought socialThought = thought as ISocialThought;
            if (socialThought != null)
            {
                return socialThought.OtherPawn();
            }

            return LegacyRecentPawnAttributionService.TryGetRecentActor(pawn, thought, out attributionSource);
        }

        private static float GetSignedImpact(Thought thought)
        {
            float moodOffset = thought.MoodOffset();
            if (moodOffset != 0f)
            {
                return moodOffset;
            }

            ISocialThought socialThought = thought as ISocialThought;
            return socialThought != null ? socialThought.OpinionOffset() : 0f;
        }
    }
}
