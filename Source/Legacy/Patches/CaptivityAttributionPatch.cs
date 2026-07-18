using HarmonyLib;
using RimWorld;
using Verse;

namespace Legacy.Patches
{
    [HarmonyPatch(typeof(Pawn_GuestTracker), "CapturedBy")]
    public static class CapturedByAttributionPatch
    {
        public static void Postfix(Pawn_GuestTracker __instance, object[] __args)
        {
            Pawn subject = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
            Pawn actor = FirstPawnArg(__args);
            Legacy.Services.LegacyRecentPawnAttributionService.Register(subject, actor, "CapturedBy");
        }

        private static Pawn FirstPawnArg(object[] args)
        {
            if (args == null)
            {
                return null;
            }

            foreach (object arg in args)
            {
                Pawn pawn = arg as Pawn;
                if (pawn != null)
                {
                    return pawn;
                }
            }

            return null;
        }
    }

    [HarmonyPatch(typeof(InteractionWorker_EnslaveAttempt), "Interacted")]
    public static class EnslaveAttemptAttributionPatch
    {
        public static void Postfix(object[] __args)
        {
            RegisterInteraction(__args, "EnslaveAttempt");
        }

        private static void RegisterInteraction(object[] args, string source)
        {
            Pawn actor;
            Pawn subject;
            if (TryGetFirstTwoPawns(args, out actor, out subject))
            {
                Legacy.Services.LegacyRecentPawnAttributionService.Register(subject, actor, source);
            }
        }

        private static bool TryGetFirstTwoPawns(object[] args, out Pawn first, out Pawn second)
        {
            first = null;
            second = null;

            if (args == null)
            {
                return false;
            }

            foreach (object arg in args)
            {
                Pawn pawn = arg as Pawn;
                if (pawn == null)
                {
                    continue;
                }

                if (first == null)
                {
                    first = pawn;
                    continue;
                }

                second = pawn;
                return true;
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(InteractionWorker_RecruitAttempt), "Interacted")]
    public static class RecruitAttemptAttributionPatch
    {
        public static void Postfix(object[] __args)
        {
            Pawn actor;
            Pawn subject;
            if (TryGetFirstTwoPawns(__args, out actor, out subject))
            {
                Legacy.Services.LegacyRecentPawnAttributionService.Register(subject, actor, "RecruitAttempt");
            }
        }

        private static bool TryGetFirstTwoPawns(object[] args, out Pawn first, out Pawn second)
        {
            first = null;
            second = null;

            if (args == null)
            {
                return false;
            }

            foreach (object arg in args)
            {
                Pawn pawn = arg as Pawn;
                if (pawn == null)
                {
                    continue;
                }

                if (first == null)
                {
                    first = pawn;
                    continue;
                }

                second = pawn;
                return true;
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(InteractionWorker_ConvertIdeoAttempt), "Interacted")]
    public static class ConvertAttemptAttributionPatch
    {
        public static void Postfix(object[] __args)
        {
            Pawn actor;
            Pawn subject;
            if (TryGetFirstTwoPawns(__args, out actor, out subject))
            {
                Legacy.Services.LegacyRecentPawnAttributionService.Register(subject, actor, "ConvertAttempt");
            }
        }

        private static bool TryGetFirstTwoPawns(object[] args, out Pawn first, out Pawn second)
        {
            first = null;
            second = null;

            if (args == null)
            {
                return false;
            }

            foreach (object arg in args)
            {
                Pawn pawn = arg as Pawn;
                if (pawn == null)
                {
                    continue;
                }

                if (first == null)
                {
                    first = pawn;
                    continue;
                }

                second = pawn;
                return true;
            }

            return false;
        }
    }
}
