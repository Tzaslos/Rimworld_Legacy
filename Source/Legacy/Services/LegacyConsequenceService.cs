using System.Collections.Generic;
using Legacy.Core;
using Legacy.Domain;
using Legacy.Storage;
using RimWorld;
using Verse;

namespace Legacy.Services
{
    public static class LegacyConsequenceService
    {
        public static void EvaluateConsequences(LegacyWorldComponent component)
        {
            if (component == null || LegacyMod.Settings == null || !LegacyMod.Settings.enableConsequences)
            {
                return;
            }

            int currentTick = Find.TickManager != null ? Find.TickManager.TicksGame : 0;
            int interval = LegacyMod.Settings.consequenceIntervalTicks;
            if (interval < 10000)
            {
                interval = 10000;
            }

            foreach (LegacyRelationshipState state in component.RelationshipStates)
            {
                if (state == null)
                {
                    continue;
                }

                if (currentTick - state.lastConsequenceTick < interval)
                {
                    continue;
                }

                if (!IsEligibleForConsequence(state, state != null ? state.kind : LegacyRelationshipKind.Neutral, Find.AnyPlayerHomeMap, useChance: true))
                {
                    continue;
                }

                if (TryFireConsequence(state))
                {
                    state.lastConsequenceTick = currentTick;
                }
            }
        }

        public static bool CanFireNow(LegacyRelationshipKind kind, Map map)
        {
            LegacyRelationshipState state;
            return TryFindEligibleState(kind, map, out state);
        }

        public static bool TryFireStorytellerConsequence(LegacyRelationshipKind kind, Map map)
        {
            LegacyRelationshipState state;
            if (!TryFindEligibleState(kind, map, out state))
            {
                return false;
            }

            bool fired = TryFireConsequence(state);
            if (fired)
            {
                state.lastConsequenceTick = CurrentTick();
            }

            return fired;
        }

        private static bool TryFindEligibleState(LegacyRelationshipKind kind, Map map, out LegacyRelationshipState state)
        {
            state = null;
            if (LegacyMod.Settings == null || !LegacyMod.Settings.enableConsequences)
            {
                return false;
            }

            LegacyWorldComponent component = Find.World != null ? Find.World.GetComponent<LegacyWorldComponent>() : null;
            if (component == null || component.RelationshipStates == null)
            {
                return false;
            }

            List<LegacyRelationshipState> candidates = new List<LegacyRelationshipState>();
            foreach (LegacyRelationshipState candidate in component.RelationshipStates)
            {
                if (IsEligibleForConsequence(candidate, kind, map, useChance: false))
                {
                    candidates.Add(candidate);
                }
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            state = candidates.RandomElement();
            return true;
        }

        private static bool IsEligibleForConsequence(LegacyRelationshipState state, LegacyRelationshipKind kind, Map map, bool useChance)
        {
            if (state == null || kind == LegacyRelationshipKind.Neutral || state.kind != kind)
            {
                return false;
            }

            if (!LegacyPawnEligibilityService.IsKnownAlivePawn(state.subjectPawnId)
                || !LegacyPawnEligibilityService.IsKnownAlivePawn(state.otherPawnId))
            {
                return false;
            }

            if (IsPawnOnPlayerMap(state.otherPawnId))
            {
                return false;
            }

            int interval = LegacyMod.Settings.consequenceIntervalTicks;
            if (interval < 10000)
            {
                interval = 10000;
            }

            if (CurrentTick() - state.lastConsequenceTick < interval)
            {
                return false;
            }

            if (map == null && Find.AnyPlayerHomeMap == null)
            {
                return false;
            }

            return !useChance || PassesScaledChance(state);
        }

        private static bool TryFireConsequence(LegacyRelationshipState state)
        {
            if (state.kind == LegacyRelationshipKind.Hero)
            {
                SendHeroGift(state);
                return true;
            }

            if (state.kind == LegacyRelationshipKind.Nemesis)
            {
                return SendNemesisThreat(state);
            }

            return false;
        }

        private static bool PassesScaledChance(LegacyRelationshipState state)
        {
            float threshold = state.kind == LegacyRelationshipKind.Hero
                ? LegacyMod.Settings.heroThreshold
                : System.Math.Abs(LegacyMod.Settings.nemesisThreshold);

            if (threshold <= 0f)
            {
                threshold = 1f;
            }

            float chance = (System.Math.Abs(state.score) / threshold) * 0.18f * LegacyMod.Settings.consequenceScale;
            if (chance > 0.75f)
            {
                chance = 0.75f;
            }

            return Rand.Chance(chance);
        }

        private static void SendHeroGift(LegacyRelationshipState state)
        {
            Map map = Find.AnyPlayerHomeMap;
            if (map == null)
            {
                SendLetter("Legacy gift", state.otherName + " tried to send aid to " + state.subjectName + ", but no colony map was available.", LetterDefOf.PositiveEvent);
                return;
            }

            List<Thing> things = new List<Thing>();
            things.Add(MakeRandomGift(state));

            IntVec3 dropSpot = DropCellFinder.TradeDropSpot(map);
            DropPodUtility.DropThingsNear(dropSpot, map, things, leaveSlag: false, canRoofPunch: true);

            SendLetter(
                "Legacy gift",
                state.otherName + " sent a gift in honor of " + state.subjectName + ".",
                LetterDefOf.PositiveEvent);
        }

        private static Thing MakeRandomGift(LegacyRelationshipState state)
        {
            float strength = System.Math.Abs(state.score) * LegacyMod.Settings.consequenceScale;
            int roll = Rand.RangeInclusive(0, 5);
            ThingDef def;
            int amount;

            if (roll == 0)
            {
                def = ThingDefOf.Silver;
                amount = ClampStack(75 + (int)(strength * 3f), 75, 600);
            }
            else if (roll == 1)
            {
                def = ThingDefOf.MedicineIndustrial;
                amount = ClampStack(2 + (int)(strength / 20f), 2, 12);
            }
            else if (roll == 2)
            {
                def = ThingDefOf.ComponentIndustrial;
                amount = ClampStack(2 + (int)(strength / 25f), 2, 10);
            }
            else if (roll == 3)
            {
                def = ThingDefOf.MealSurvivalPack;
                amount = ClampStack(6 + (int)(strength / 8f), 6, 35);
            }
            else if (roll == 4)
            {
                def = ThingDefOf.Plasteel;
                amount = ClampStack(20 + (int)(strength * 0.8f), 20, 140);
            }
            else
            {
                def = ThingDefOf.Jade;
                amount = ClampStack(15 + (int)(strength * 0.6f), 15, 100);
            }

            Thing gift = ThingMaker.MakeThing(def);
            gift.stackCount = amount;
            return gift;
        }

        private static bool SendNemesisThreat(LegacyRelationshipState state)
        {
            Map map = Find.AnyPlayerHomeMap;
            if (map == null)
            {
                SendLetter(
                    "Legacy threat",
                    state.otherName + " has not forgotten " + state.subjectName + ".",
                    LetterDefOf.ThreatSmall);
                return true;
            }

            Pawn nemesis = LegacyPawnEligibilityService.TryResolveAlivePawn(state.otherPawnId);
            if (nemesis == null || nemesis.Spawned)
            {
                return false;
            }

            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, map);
            parms.points = ScaledRaidPoints(state, parms.points);
            parms.forced = true;
            parms.controllerPawn = nemesis;
            if (nemesis.Faction != null && nemesis.Faction.HostileTo(Faction.OfPlayer))
            {
                parms.faction = nemesis.Faction;
            }

            parms.pawnGroups = new Dictionary<Pawn, int> { { nemesis, 0 } };
            parms.customLetterLabel = "Legacy raid";
            parms.customLetterText = state.otherName + " has come for " + state.subjectName + ".";
            parms.customLetterDef = LetterDefOf.ThreatBig;

            bool fired = IncidentDefOf.RaidEnemy.Worker.TryExecute(parms);
            if (!fired)
            {
                SendLetter(
                    "Legacy threat",
                    state.otherName + " has not forgotten " + state.subjectName + ". Their enmity stirs beyond the colony.",
                    LetterDefOf.ThreatSmall);
            }

            return fired;
        }

        private static int CurrentTick()
        {
            return Find.TickManager != null ? Find.TickManager.TicksGame : 0;
        }

        private static float ScaledRaidPoints(LegacyRelationshipState state, float defaultPoints)
        {
            float threshold = System.Math.Abs(LegacyMod.Settings.nemesisThreshold);
            if (threshold <= 0f)
            {
                threshold = 1f;
            }

            float multiplier = 0.35f + (System.Math.Abs(state.score) / threshold) * 0.25f * LegacyMod.Settings.consequenceScale;
            if (multiplier > 1.5f)
            {
                multiplier = 1.5f;
            }

            float points = defaultPoints * multiplier;
            return points < 35f ? 35f : points;
        }

        private static int ClampStack(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }

        private static void SendLetter(string title, string text, LetterDef letterDef)
        {
            if (Find.LetterStack != null)
            {
                Find.LetterStack.ReceiveLetter(title, text, letterDef);
            }
        }

        private static bool IsPawnOnPlayerMap(int pawnId)
        {
            if (pawnId < 0 || Find.Maps == null)
            {
                return false;
            }

            foreach (Map map in Find.Maps)
            {
                if (map == null || !map.IsPlayerHome || map.mapPawns == null)
                {
                    continue;
                }

                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                {
                    if (LegacyPawnEligibilityService.CanCreateLegacyEvents(pawn) && pawn.thingIDNumber == pawnId)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

    }
}
