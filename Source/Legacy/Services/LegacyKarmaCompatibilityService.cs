using System;
using System.Reflection;
using Legacy.Core;
using Legacy.Domain;
using Verse;

namespace Legacy.Services
{
    public static class LegacyKarmaCompatibilityService
    {
        private const string KarmaPackageId = "astryl.KarmaReputation";
        private const string KarmaApiTypeName = "AstrylMods.KarmaReputation.KarmaAPI";

        private static Type karmaApiType;
        private static MethodInfo getPawnKarmaMethod;
        private static MethodInfo getRecentEventsForPawnMethod;
        private static bool reflected;

        public static bool CanUseKarmaStates()
        {
            return LegacyMod.Settings != null
                && LegacyMod.Settings.useKarmaModForRelationshipStates
                && IsKarmaModActive()
                && EnsureReflected();
        }

        public static bool IsKarmaModActive()
        {
            return LegacyModIntegrationService.IsKarmaActive();
        }

        public static bool TryClassifyPawn(int pawnId, out LegacyRelationshipKind kind, out float karmaScore)
        {
            kind = LegacyRelationshipKind.Neutral;
            karmaScore = 0f;

            if (!CanUseKarmaStates())
            {
                return false;
            }

            Pawn pawn = LegacyPawnEligibilityService.TryResolveAlivePawn(pawnId);
            if (pawn == null)
            {
                return false;
            }

            try
            {
                object value = getPawnKarmaMethod.Invoke(null, new object[] { pawn });
                karmaScore = Convert.ToSingle(value);
            }
            catch (Exception exception)
            {
                LegacyLog.Warning("Could not read Karma & Reputation score: " + exception.Message);
                return false;
            }

            if (karmaScore >= LegacyMod.Settings.karmaHeroThreshold)
            {
                kind = LegacyRelationshipKind.Hero;
            }
            else if (karmaScore <= LegacyMod.Settings.karmaNemesisThreshold)
            {
                kind = LegacyRelationshipKind.Nemesis;
            }

            return true;
        }

        public static bool TryGetRecentPawnCondition(Pawn subject, Pawn actor, out string label, out string description, out float impact)
        {
            label = null;
            description = null;
            impact = 0f;

            if (!CanUseKarmaStates()
                || !LegacyPawnEligibilityService.CanCreateLegacyEvents(subject)
                || !LegacyPawnEligibilityService.CanCreateLegacyEvents(actor)
                || getRecentEventsForPawnMethod == null)
            {
                return false;
            }

            object events;
            try
            {
                events = getRecentEventsForPawnMethod.Invoke(null, new object[] { actor });
            }
            catch
            {
                return false;
            }

            System.Collections.IEnumerable enumerable = events as System.Collections.IEnumerable;
            if (enumerable == null)
            {
                return false;
            }

            object bestEvent = null;
            int bestTick = int.MinValue;
            foreach (object karmaEvent in enumerable)
            {
                if (!EventTargetsPawn(karmaEvent, subject))
                {
                    continue;
                }

                int tick = GetIntMember(karmaEvent, "tick", "Tick", "ticksGame", "TicksGame");
                if (bestEvent == null || tick > bestTick)
                {
                    bestEvent = karmaEvent;
                    bestTick = tick;
                }
            }

            if (bestEvent == null)
            {
                return false;
            }

            string reason = GetStringMember(bestEvent, "reason", "Reason", "eventType", "EventType", "historyEventDefName", "HistoryEventDefName");
            string readableReason = HumanizeReason(reason);
            impact = GetFloatMember(bestEvent, "value", "Value", "amount", "Amount", "delta", "Delta", "karma", "Karma", "change", "Change");
            if (impact == 0f)
            {
                impact = GetFloatMember(bestEvent, "pawnKarma", "PawnKarma");
            }

            label = actor.LabelShort + " " + readableReason + " " + subject.LabelShort;
            description = "Karma & Reputation recorded this pawn-specific deed"
                + (impact != 0f ? " (" + impact.ToString("+0;-0") + " karma)" : string.Empty)
                + ".";
            return true;
        }

        private static bool EnsureReflected()
        {
            if (reflected)
            {
                return getPawnKarmaMethod != null;
            }

            reflected = true;
            karmaApiType = GenTypes.GetTypeInAnyAssembly(KarmaApiTypeName);
            if (karmaApiType == null)
            {
                return false;
            }

            getPawnKarmaMethod = karmaApiType.GetMethod("GetPawnKarma", BindingFlags.Public | BindingFlags.Static);
            if (getPawnKarmaMethod == null)
            {
                getPawnKarmaMethod = karmaApiType.GetMethod("GetKarma", BindingFlags.Public | BindingFlags.Static);
            }

            getRecentEventsForPawnMethod = karmaApiType.GetMethod("GetRecentEventsForPawn", BindingFlags.Public | BindingFlags.Static);

            return getPawnKarmaMethod != null && getPawnKarmaMethod.GetParameters().Length == 1;
        }

        private static bool EventTargetsPawn(object karmaEvent, Pawn pawn)
        {
            object victim = GetObjectMember(karmaEvent, "victim", "Victim", "target", "Target", "otherPawn", "OtherPawn");
            Pawn victimPawn = victim as Pawn;
            if (victimPawn != null)
            {
                return victimPawn.thingIDNumber == pawn.thingIDNumber;
            }

            int pawnId = GetIntMember(karmaEvent, "victimId", "VictimId", "targetPawnId", "TargetPawnId", "otherPawnId", "OtherPawnId");
            return pawnId >= 0 && pawnId == pawn.thingIDNumber;
        }

        private static object GetObjectMember(object instance, params string[] names)
        {
            if (instance == null)
            {
                return null;
            }

            Type type = instance.GetType();
            foreach (string name in names)
            {
                FieldInfo field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    return field.GetValue(instance);
                }

                PropertyInfo property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null && property.GetIndexParameters().Length == 0)
                {
                    return property.GetValue(instance, null);
                }
            }

            return null;
        }

        private static string GetStringMember(object instance, params string[] names)
        {
            object value = GetObjectMember(instance, names);
            return value != null ? value.ToString() : string.Empty;
        }

        private static int GetIntMember(object instance, params string[] names)
        {
            object value = GetObjectMember(instance, names);
            if (value == null)
            {
                return -1;
            }

            try
            {
                return Convert.ToInt32(value);
            }
            catch
            {
                return -1;
            }
        }

        private static float GetFloatMember(object instance, params string[] names)
        {
            object value = GetObjectMember(instance, names);
            if (value == null)
            {
                return 0f;
            }

            try
            {
                return Convert.ToSingle(value);
            }
            catch
            {
                return 0f;
            }
        }

        private static string HumanizeReason(string reason)
        {
            if (string.IsNullOrEmpty(reason))
            {
                return "affected";
            }

            string lower = reason.ToLowerInvariant();
            if (lower.Contains("rjw") || lower.Contains("rape") || lower.Contains("nonconsensual") || lower.Contains("non_consensual") || lower.Contains("forced"))
            {
                return "performed a non-consensual act on";
            }

            if (lower.Contains("rescu"))
            {
                return "rescued";
            }

            if (lower.Contains("captur") || lower.Contains("imprison"))
            {
                return "captured";
            }

            if (lower.Contains("harvest") || lower.Contains("organ"))
            {
                return "harvested from";
            }

            if (lower.Contains("tend") || lower.Contains("heal") || lower.Contains("charity"))
            {
                return "helped";
            }

            return "affected";
        }
    }
}
