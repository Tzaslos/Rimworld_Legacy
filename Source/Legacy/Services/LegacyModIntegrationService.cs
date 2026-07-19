using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace Legacy.Services
{
    public static class LegacyModIntegrationService
    {
        private const string KarmaPackageId = "astryl.KarmaReputation";
        private const string RimWorldOfMagicPackageId = "torann.arimworldofmagic";

        public static bool IsKarmaActive()
        {
            return ModsConfig.IsActive(KarmaPackageId);
        }

        public static bool IsRimWorldOfMagicActive()
        {
            return IsPackageOrWorkshopActive(RimWorldOfMagicPackageId, "1201382956")
                || GenTypes.GetTypeInAnyAssembly("TorannMagic.Verb_UseAbility") != null
                || GenTypes.GetTypeInAnyAssembly("AbilityUser.Verb_UseAbility") != null;
        }

        public static bool IsRjwActive()
        {
            return AccessTools.TypeByName("rjw.AfterSexUtility") != null
                || IsPackageWithTextActive("rimjobworld")
                || IsPackageWithTextActive("rjw");
        }

        public static string DetectedText(bool detected)
        {
            return detected ? "Detected" : "Not detected";
        }

        private static bool IsPackageWithTextActive(string text)
        {
            if (string.IsNullOrEmpty(text) || ModsConfig.ActiveModsInLoadOrder == null)
            {
                return false;
            }

            string lowerText = text.ToLowerInvariant();
            foreach (ModMetaData mod in ModsConfig.ActiveModsInLoadOrder)
            {
                if (mod == null)
                {
                    continue;
                }

                string packageId = mod.PackageId;
                string name = mod.Name;
                if ((!string.IsNullOrEmpty(packageId) && packageId.ToLowerInvariant().Contains(lowerText))
                    || (!string.IsNullOrEmpty(name) && name.ToLowerInvariant().Contains(lowerText)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsPackageOrWorkshopActive(string packageId, string workshopId)
        {
            if (!string.IsNullOrEmpty(packageId) && ModsConfig.IsActive(packageId))
            {
                return true;
            }

            if (ModsConfig.ActiveModsInLoadOrder == null)
            {
                return false;
            }

            foreach (ModMetaData mod in ModsConfig.ActiveModsInLoadOrder)
            {
                if (mod == null)
                {
                    continue;
                }

                string activePackageId = mod.PackageId;
                string activeWorkshopId = GetModMetadataString(mod, "WorkshopId", "PublishedFileId", "SteamWorkshopId");
                if ((!string.IsNullOrEmpty(activePackageId) && !string.IsNullOrEmpty(packageId) && activePackageId.ToLowerInvariant() == packageId)
                    || (!string.IsNullOrEmpty(workshopId) && activeWorkshopId == workshopId))
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
    }
}
