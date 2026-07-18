using System.Collections.Generic;
using HarmonyLib;
using Legacy.Services;
using RimWorld;
using Verse;

namespace Legacy.Patches
{
    [HarmonyPatch(typeof(Recipe_RemoveBodyPart), "ApplyOnPawn")]
    public static class RemoveBodyPartAttributionPatch
    {
        public static void Postfix(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            LegacyRecentPawnAttributionService.Register(pawn, billDoer, "RemoveBodyPart");
        }
    }

    [HarmonyPatch(typeof(Recipe_InstallArtificialBodyPart), "ApplyOnPawn")]
    public static class InstallArtificialBodyPartAttributionPatch
    {
        public static void Postfix(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            LegacyRecentPawnAttributionService.Register(pawn, billDoer, "InstallArtificialBodyPart");
        }
    }

    [HarmonyPatch(typeof(Recipe_InstallNaturalBodyPart), "ApplyOnPawn")]
    public static class InstallNaturalBodyPartAttributionPatch
    {
        public static void Postfix(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            LegacyRecentPawnAttributionService.Register(pawn, billDoer, "InstallNaturalBodyPart");
        }
    }

    [HarmonyPatch(typeof(Recipe_RemoveHediff), "ApplyOnPawn")]
    public static class RemoveHediffAttributionPatch
    {
        public static void Postfix(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            LegacyRecentPawnAttributionService.Register(pawn, billDoer, "RemoveHediff");
        }
    }
}
