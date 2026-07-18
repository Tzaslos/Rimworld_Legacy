using HarmonyLib;
using Legacy.Core;
using Verse;

namespace Legacy.Patches
{
    public static class HarmonyBootstrap
    {
        private static bool patched;
        private static Harmony harmony;

        public static void ApplyPatches()
        {
            if (patched)
            {
                return;
            }

            harmony = new Harmony("ruben.legacy");
            harmony.PatchAll();
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                RjwAfterInteractionAttributionPatch.Apply(harmony);
                RimWorldOfMagicAttributionPatch.Apply(harmony);
            });
            patched = true;
            LegacyLog.Message("Harmony patches applied.");
        }
    }
}
