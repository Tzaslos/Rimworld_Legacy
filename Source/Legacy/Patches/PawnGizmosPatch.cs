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
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            __result = AppendLegacyGizmo(__result, __instance);
        }

        private static IEnumerable<Gizmo> AppendLegacyGizmo(IEnumerable<Gizmo> values, Pawn pawn)
        {
            if (values != null)
            {
                foreach (Gizmo gizmo in values)
                {
                    yield return gizmo;
                }
            }

            if (pawn == null || pawn.RaceProps == null || !pawn.RaceProps.Humanlike)
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
                    Find.WindowStack.Add(new Dialog_LegacyDebug(pawn));
                }
            };
        }
    }
}
