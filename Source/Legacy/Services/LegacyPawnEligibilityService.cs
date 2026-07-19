using Verse;

namespace Legacy.Services
{
    public static class LegacyPawnEligibilityService
    {
        public static bool CanCreateLegacyEvents(Pawn pawn)
        {
            return pawn != null
                && !pawn.Dead
                && !pawn.Destroyed
                && pawn.health != null;
        }

        public static bool IsKnownAlivePawn(int pawnId)
        {
            return TryResolveAlivePawn(pawnId) != null;
        }

        public static Pawn TryResolveAlivePawn(int pawnId)
        {
            if (pawnId < 0)
            {
                return null;
            }

            Pawn spawned = TryResolveSpawnedPawn(pawnId);
            if (spawned != null)
            {
                return spawned;
            }

            if (Find.World == null || Find.World.worldPawns == null)
            {
                return null;
            }

            foreach (Pawn pawn in Find.World.worldPawns.AllPawnsAlive)
            {
                if (CanCreateLegacyEvents(pawn) && pawn.thingIDNumber == pawnId)
                {
                    return pawn;
                }
            }

            return null;
        }

        public static Pawn TryResolveKnownPawn(int pawnId)
        {
            if (pawnId < 0)
            {
                return null;
            }

            Pawn spawned = TryResolveSpawnedPawnIncludingDead(pawnId);
            if (spawned != null)
            {
                return spawned;
            }

            if (Find.World == null || Find.World.worldPawns == null)
            {
                return null;
            }

            foreach (Pawn pawn in Find.World.worldPawns.AllPawnsAliveOrDead)
            {
                if (pawn != null && !pawn.Destroyed && pawn.thingIDNumber == pawnId)
                {
                    return pawn;
                }
            }

            return null;
        }

        private static Pawn TryResolveSpawnedPawn(int pawnId)
        {
            if (Find.Maps == null)
            {
                return null;
            }

            foreach (Map map in Find.Maps)
            {
                if (map == null || map.mapPawns == null)
                {
                    continue;
                }

                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                {
                    if (CanCreateLegacyEvents(pawn) && pawn.thingIDNumber == pawnId)
                    {
                        return pawn;
                    }
                }
            }

            return null;
        }

        private static Pawn TryResolveSpawnedPawnIncludingDead(int pawnId)
        {
            if (Find.Maps == null)
            {
                return null;
            }

            foreach (Map map in Find.Maps)
            {
                if (map == null || map.mapPawns == null)
                {
                    continue;
                }

                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                {
                    if (pawn != null && !pawn.Destroyed && pawn.thingIDNumber == pawnId)
                    {
                        return pawn;
                    }
                }
            }

            return null;
        }
    }
}
