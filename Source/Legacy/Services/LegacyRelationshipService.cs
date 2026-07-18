using System.Collections.Generic;
using Legacy.Core;
using Legacy.Domain;
using Legacy.Storage;
using RimWorld;
using Verse;

namespace Legacy.Services
{
    public static class LegacyRelationshipService
    {
        public static void EvaluateRelationships(LegacyWorldComponent component)
        {
            if (component == null || LegacyMod.Settings == null)
            {
                return;
            }

            Dictionary<string, RelationshipAccumulator> scores = BuildScores(component.Repository.AllRecords());
            foreach (RelationshipAccumulator accumulator in scores.Values)
            {
                float stateScore = accumulator.Score;
                LegacyRelationshipKind newKind;
                float karmaScore;
                if (LegacyKarmaCompatibilityService.TryClassifyPawn(accumulator.OtherPawnId, out newKind, out karmaScore))
                {
                    stateScore = karmaScore;
                }
                else
                {
                    newKind = Classify(accumulator.Score);
                }

                ApplyPlayerFactionRecruitmentReset(accumulator, ref stateScore, ref newKind);

                LegacyRelationshipState state = component.GetOrCreateRelationshipState(accumulator.SubjectPawnId, accumulator.OtherPawnId);
                LegacyRelationshipKind oldKind = state.kind;

                state.subjectName = accumulator.SubjectName;
                state.otherName = accumulator.OtherName;
                state.score = stateScore;

                if (newKind != oldKind)
                {
                    state.kind = newKind;
                    state.lastChangedTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
                    SendRelationshipLetter(state, oldKind, newKind);
                }
            }
        }

        private static Dictionary<string, RelationshipAccumulator> BuildScores(IEnumerable<LegacyRecord> records)
        {
            Dictionary<string, RelationshipAccumulator> scores = new Dictionary<string, RelationshipAccumulator>();

            foreach (LegacyRecord record in records)
            {
                if (record == null || record.subject == null || !LegacyPawnEligibilityService.IsKnownAlivePawn(record.subject.thingIdNumber))
                {
                    continue;
                }

                LegacySubjectRef other = GetOtherPawn(record);
                if (other == null || !LegacyPawnEligibilityService.IsKnownAlivePawn(other.thingIdNumber))
                {
                    continue;
                }

                string key = record.subject.thingIdNumber + ":" + other.thingIdNumber;
                RelationshipAccumulator accumulator;
                if (!scores.TryGetValue(key, out accumulator))
                {
                    accumulator = new RelationshipAccumulator
                    {
                        SubjectPawnId = record.subject.thingIdNumber,
                        OtherPawnId = other.thingIdNumber,
                        SubjectName = record.subject.name,
                        OtherName = other.name
                    };
                    scores[key] = accumulator;
                }

                Pawn subjectPawn = LegacyPawnEligibilityService.TryResolveAlivePawn(record.subject.thingIdNumber);
                accumulator.Score += LegacyForgivenessService.AdjustRelationshipImpact(subjectPawn, record);
            }

            return scores;
        }

        private static LegacyRelationshipKind Classify(float score)
        {
            if (score >= LegacyMod.Settings.heroThreshold)
            {
                return LegacyRelationshipKind.Hero;
            }

            if (score <= LegacyMod.Settings.nemesisThreshold)
            {
                return LegacyRelationshipKind.Nemesis;
            }

            return LegacyRelationshipKind.Neutral;
        }

        private static void ApplyPlayerFactionRecruitmentReset(RelationshipAccumulator accumulator, ref float stateScore, ref LegacyRelationshipKind kind)
        {
            if (stateScore >= 0f)
            {
                return;
            }

            Pawn subject = LegacyPawnEligibilityService.TryResolveAlivePawn(accumulator.SubjectPawnId);
            Pawn other = LegacyPawnEligibilityService.TryResolveAlivePawn(accumulator.OtherPawnId);
            if (!IsPlayerFactionPawn(subject) || !IsPlayerFactionPawn(other))
            {
                return;
            }

            stateScore = 0f;
            kind = LegacyRelationshipKind.Neutral;
        }

        private static bool IsPlayerFactionPawn(Pawn pawn)
        {
            return pawn != null && pawn.Faction == Faction.OfPlayer;
        }

        private static void SendRelationshipLetter(LegacyRelationshipState state, LegacyRelationshipKind oldKind, LegacyRelationshipKind newKind)
        {
            if (newKind == LegacyRelationshipKind.Neutral || Find.LetterStack == null)
            {
                return;
            }

            string title;
            string text;
            LetterDef letterDef;

            if (newKind == LegacyRelationshipKind.Hero)
            {
                title = "Legacy hero formed";
                text = state.otherName + " became the hero of " + state.subjectName + ".\n\nLegacy score: " + state.score.ToString("+0;-0;0");
                letterDef = LetterDefOf.PositiveEvent;
            }
            else
            {
                title = "Legacy nemesis formed";
                text = state.otherName + " became the nemesis of " + state.subjectName + ".\n\nLegacy score: " + state.score.ToString("+0;-0;0");
                letterDef = LetterDefOf.ThreatSmall;
            }

            Find.LetterStack.ReceiveLetter(title, text, letterDef);
        }

        private static LegacySubjectRef GetOtherPawn(LegacyRecord record)
        {
            if (record.participants == null)
            {
                return null;
            }

            foreach (LegacyParticipant participant in record.participants)
            {
                if (participant != null && participant.role == LegacyParticipantRole.OtherPawn && participant.pawn != null)
                {
                    return participant.pawn;
                }
            }

            return null;
        }

        private class RelationshipAccumulator
        {
            public int SubjectPawnId;
            public int OtherPawnId;
            public string SubjectName;
            public string OtherName;
            public float Score;
        }
    }
}
