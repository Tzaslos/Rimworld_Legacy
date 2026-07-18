using HarmonyLib;
using Verse;

namespace Legacy.Patches
{
    [HarmonyPatch(typeof(Thing), "PreApplyDamage")]
    public static class CombatAttributionPatch
    {
        public static void Prefix(Thing __instance, ref DamageInfo dinfo)
        {
            Pawn victim = __instance as Pawn;
            Pawn attacker = dinfo.Instigator as Pawn;
            string damageDefName = dinfo.Def != null ? dinfo.Def.defName : "UnknownDamage";
            Legacy.Services.LegacyRecentPawnAttributionService.Register(victim, attacker, "CombatDamage " + damageDefName);
        }
    }
}
