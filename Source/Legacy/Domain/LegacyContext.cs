using Verse;

namespace Legacy.Domain
{
    public class LegacyContext : IExposable
    {
        public int tile = -1;
        public string mapId;
        public string settlementName;
        public string factionDefName;
        public string factionName;
        public string cause;
        public string extraDescription;

        public void ExposeData()
        {
            Scribe_Values.Look(ref tile, "tile", -1);
            Scribe_Values.Look(ref mapId, "mapId");
            Scribe_Values.Look(ref settlementName, "settlementName");
            Scribe_Values.Look(ref factionDefName, "factionDefName");
            Scribe_Values.Look(ref factionName, "factionName");
            Scribe_Values.Look(ref cause, "cause");
            Scribe_Values.Look(ref extraDescription, "extraDescription");
        }
    }
}
