using System.Collections.Generic;
using HarmonyLib;
using Legacy.UI.Debug;
using RimWorld;
using Verse;

namespace Legacy.Patches
{
    [HarmonyPatch(typeof(Pawn), "GetGizmos")]
    public static class PawnGizmosPatch
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> values, Pawn __instance)
        {
            foreach (Gizmo gizmo in values)
            {
                yield return gizmo;
            }

            if (__instance == null || __instance.RaceProps == null || !__instance.RaceProps.Humanlike)
            {
                yield break;
            }

            yield return new Command_Action
            {
                defaultLabel = "Legacy",
                defaultDesc = "View this pawn's relational Legacy memories.",
                icon = TexCommand.DesirePower,
                action = delegate
                {
                    Find.WindowStack.Add(new Dialog_LegacyDebug(__instance));
                }
            };
        }
    }
}
