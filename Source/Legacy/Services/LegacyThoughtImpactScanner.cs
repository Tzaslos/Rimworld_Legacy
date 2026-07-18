using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Legacy.Services
{
    public static class LegacyThoughtImpactScanner
    {
        private static readonly List<Thought> Thoughts = new List<Thought>();

        public static void ScanVisiblePawnThoughts()
        {
            if (Find.Maps == null)
            {
                return;
            }

            foreach (Map map in Find.Maps)
            {
                if (map == null || map.mapPawns == null)
                {
                    continue;
                }

                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                {
                    ScanPawn(pawn);
                }
            }
        }

        private static void ScanPawn(Pawn pawn)
        {
            if (!LegacyPawnEligibilityService.CanCreateLegacyEvents(pawn) || pawn.needs == null || pawn.needs.mood == null || pawn.needs.mood.thoughts == null)
            {
                return;
            }

            Thoughts.Clear();
            pawn.needs.mood.thoughts.GetAllMoodThoughts(Thoughts);

            foreach (Thought thought in Thoughts)
            {
                LegacyThoughtImpactRecorder.TryRecord(pawn, thought);
            }

            Thoughts.Clear();
        }
    }
}
