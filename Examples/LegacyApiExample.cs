using Legacy.API;
using Legacy.Domain;
using Verse;

namespace YourMod
{
    public static class LegacyApiExample
    {
        public static void RecordRescue(Pawn rescuedPawn, Pawn rescuerPawn)
        {
            if (!LegacyApi.IsAvailable || rescuedPawn == null)
            {
                return;
            }

            LegacyRecord record;
            LegacyApiRecordRequest request = new LegacyApiRecordRequest
            {
                eventDefName = "YourMod_SavedPawnFromFire",
                subject = rescuedPawn,
                cause = "YourMod rescue system",
                extraDescription = "Rescued during a building fire.",
                label = "Saved from fire",
                description = rescuedPawn.LabelShort + " was rescued from a dangerous fire.",
                polarity = LegacyImpactPolarity.Positive,
                sourceType = LegacyImpactSourceType.PersistentSituation
            };

            request.WithParticipant(rescuerPawn, LegacyParticipantRole.Rescuer);

            LegacyApi.TryRecordEvent(request, out record);
        }
    }
}
