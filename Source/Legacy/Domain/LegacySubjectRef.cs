using RimWorld;
using Verse;

namespace Legacy.Domain
{
    public class LegacySubjectRef : IExposable
    {
        public int thingIdNumber = -1;
        public string name;
        public string factionDefName;
        public string factionName;
        public Gender gender;
        public bool wasColonist;
        public bool wasPrisoner;
        public bool wasSlave;

        public void ExposeData()
        {
            Scribe_Values.Look(ref thingIdNumber, "thingIdNumber", -1);
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref factionDefName, "factionDefName");
            Scribe_Values.Look(ref factionName, "factionName");
            Scribe_Values.Look(ref gender, "gender");
            Scribe_Values.Look(ref wasColonist, "wasColonist");
            Scribe_Values.Look(ref wasPrisoner, "wasPrisoner");
            Scribe_Values.Look(ref wasSlave, "wasSlave");
        }

        public static LegacySubjectRef FromPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                return null;
            }

            return new LegacySubjectRef
            {
                thingIdNumber = pawn.thingIDNumber,
                name = pawn.Name != null ? pawn.Name.ToStringFull : pawn.LabelShort,
                factionDefName = pawn.Faction != null && pawn.Faction.def != null ? pawn.Faction.def.defName : null,
                factionName = pawn.Faction != null ? pawn.Faction.Name : null,
                gender = pawn.gender,
                wasColonist = pawn.IsColonist,
                wasPrisoner = pawn.guest != null && pawn.guest.IsPrisoner,
                wasSlave = pawn.IsSlave
            };
        }
    }
}
