using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Legacy.Core;
using Legacy.Services;
using Verse;

namespace Legacy.Patches
{
    public static class RimWorldOfMagicAttributionPatch
    {
        private static bool patched;
        private static bool reportedMissing;

        public static void Apply(Harmony harmony)
        {
            if (patched || harmony == null || !LegacyModIntegrationService.IsRimWorldOfMagicActive())
            {
                return;
            }

            int patchCount = 0;
            foreach (MethodBase method in TargetMethods())
            {
                harmony.Patch(method, postfix: new HarmonyMethod(typeof(RimWorldOfMagicAttributionPatch), "Postfix"));
                patchCount++;
            }

            if (patchCount > 0)
            {
                patched = true;
                LegacyLog.Message("RimWorld of Magic compatibility patch applied to " + patchCount + " ability method(s).");
            }
            else if (!reportedMissing)
            {
                reportedMissing = true;
                LegacyLog.Warning("RimWorld of Magic detected, but Legacy could not find expected ability cast methods.");
            }
        }

        private static IEnumerable<MethodBase> TargetMethods()
        {
            HashSet<MethodBase> methods = new HashSet<MethodBase>();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException exception)
                {
                    types = exception.Types;
                }

                foreach (Type type in types)
                {
                    if (type == null || type.FullName == null)
                    {
                        continue;
                    }

                    string fullName = type.FullName;
                    bool likelyAbilityVerb = fullName == "AbilityUser.Verb_UseAbility"
                        || fullName == "TorannMagic.Verb_UseAbility"
                        || (fullName.Contains("TorannMagic") && fullName.Contains("Verb") && fullName.Contains("Ability"));

                    if (!likelyAbilityVerb)
                    {
                        continue;
                    }

                    AddMethod(methods, type, "TryCastShot");
                    AddMethod(methods, type, "TryStartCastOn");
                    AddMethod(methods, type, "Cast");
                }
            }

            foreach (MethodBase method in methods)
            {
                yield return method;
            }
        }

        private static void AddMethod(HashSet<MethodBase> methods, Type type, string methodName)
        {
            MethodInfo method = AccessTools.Method(type, methodName);
            if (method != null && !method.IsAbstract)
            {
                methods.Add(method);
            }
        }

        public static void Postfix(object __instance)
        {
            LegacyRimWorldOfMagicService.TryRecordAbilityUse(__instance);
        }
    }
}
