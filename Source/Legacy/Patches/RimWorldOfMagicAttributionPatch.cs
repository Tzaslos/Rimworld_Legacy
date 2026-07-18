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
        private const string RimWorldOfMagicPackageId = "torann.arimworldofmagic";
        private static bool patched;
        private static bool reportedMissing;

        public static void Apply(Harmony harmony)
        {
            if (patched || harmony == null || !IsRimWorldOfMagicActive())
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

        private static bool IsRimWorldOfMagicActive()
        {
            if (ModsConfig.IsActive(RimWorldOfMagicPackageId))
            {
                return true;
            }

            foreach (ModMetaData mod in ModsConfig.ActiveModsInLoadOrder)
            {
                if (mod == null)
                {
                    continue;
                }

                string packageId = mod.PackageId;
                string workshopId = GetModMetadataString(mod, "WorkshopId", "PublishedFileId", "SteamWorkshopId");
                if ((!string.IsNullOrEmpty(packageId) && packageId.ToLowerInvariant() == RimWorldOfMagicPackageId)
                    || workshopId == "1201382956")
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetModMetadataString(ModMetaData mod, params string[] names)
        {
            Type type = mod.GetType();
            foreach (string name in names)
            {
                PropertyInfo property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null && property.GetIndexParameters().Length == 0)
                {
                    object value = property.GetValue(mod, null);
                    if (value != null)
                    {
                        return value.ToString();
                    }
                }

                FieldInfo field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    object value = field.GetValue(mod);
                    if (value != null)
                    {
                        return value.ToString();
                    }
                }
            }

            return null;
        }

        public static void Postfix(object __instance)
        {
            LegacyRimWorldOfMagicService.TryRecordAbilityUse(__instance);
        }
    }
}
