using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Legacy.Core;
using Legacy.Services;
using Verse;

namespace Legacy.Patches
{
    public static class RjwAfterInteractionAttributionPatch
    {
        private static bool patched;
        private static bool reportedMissing;

        public static void Apply(Harmony harmony)
        {
            if (patched || harmony == null)
            {
                return;
            }

            foreach (MethodBase method in TargetMethods())
            {
                harmony.Patch(method, prefix: new HarmonyMethod(typeof(RjwAfterInteractionAttributionPatch), "Prefix"));
                patched = true;
                LegacyLog.Message("RJW compatibility patch applied.");
                return;
            }
        }

        private static IEnumerable<MethodBase> TargetMethods()
        {
            if (!LegacyModIntegrationService.IsRjwActive())
            {
                yield break;
            }

            Type afterSexUtility = AccessTools.TypeByName("rjw.AfterSexUtility");
            if (afterSexUtility == null)
            {
                yield break;
            }

            MethodInfo method = AccessTools.Method(afterSexUtility, "think_about_sex", new[]
            {
                typeof(Pawn),
                typeof(Pawn),
                typeof(bool),
                AccessTools.TypeByName("rjw.SexProps"),
                typeof(bool)
            });

            if (method == null)
            {
                if (!reportedMissing)
                {
                    reportedMissing = true;
                    LegacyLog.Warning("RJW detected, but Legacy could not find the expected RJW thought attribution method.");
                }

                yield break;
            }

            yield return method;
        }

        public static void Prefix(object[] __args)
        {
            if (LegacyMod.Settings == null || !LegacyMod.Settings.useRjwIntegration)
            {
                return;
            }

            if (__args == null || __args.Length < 2)
            {
                return;
            }

            Pawn subject = __args[0] as Pawn;
            Pawn actor = __args[1] as Pawn;
            LegacyRecentPawnAttributionService.Register(subject, actor, "RJW interaction");
        }
    }
}
