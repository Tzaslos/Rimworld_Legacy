using System;
using System.Reflection;
using Legacy.Domain;
using Legacy.Storage;
using RimWorld;
using Verse;

namespace Legacy.Services
{
    public static class LegacyRimWorldOfMagicService
    {
        private const float WitnessRadius = 24f;

        public static void TryRecordAbilityUse(object abilityVerb)
        {
            Pawn caster = TryGetCaster(abilityVerb);
            if (!LegacyPawnEligibilityService.CanCreateLegacyEvents(caster) || caster.Map == null)
            {
                return;
            }

            MagicEventInfo info = BuildEventInfo(caster, abilityVerb);
            ApplyClassTitle(caster, info);

            LegacyEventDef eventDef = info.IsNecromancy ? LegacyEventDefOf.Legacy_NecromancyWitnessed : LegacyEventDefOf.Legacy_MagicWitnessed;
            if (eventDef == null)
            {
                return;
            }

            foreach (Thing thing in GenRadial.RadialDistinctThingsAround(caster.Position, caster.Map, WitnessRadius, true))
            {
                Pawn witness = thing as Pawn;
                if (!CanWitness(witness, caster))
                {
                    continue;
                }

                RecordWitness(witness, caster, eventDef, info);
            }
        }

        private static void RecordWitness(Pawn witness, Pawn caster, LegacyEventDef eventDef, MagicEventInfo info)
        {
            LegacyRecordCandidate candidate = LegacyRecordCandidateFactory.ForPawnEvent(
                eventDef,
                witness,
                "RimWorld of Magic: " + info.SourceName,
                caster.LabelShort + " used " + info.ReadableAbilityLabel + ".");

            if (candidate == null)
            {
                return;
            }

            candidate.participants.Add(new LegacyParticipantCandidate(caster, LegacyParticipantRole.OtherPawn));
            candidate.moodOffset = ScoreForWitness(witness, info);
            candidate.polarity = candidate.moodOffset >= 0f ? LegacyImpactPolarity.Positive : LegacyImpactPolarity.Negative;
            candidate.sourceType = LegacyImpactSourceType.PersistentSituation;
            candidate.label = info.DisplayLabel;
            candidate.description = WitnessDescription(witness, caster, info, candidate.moodOffset);

            LegacyRecord record;
            LegacyRecordService.TryRecord(candidate, out record);
        }

        private static Pawn TryGetCaster(object abilityVerb)
        {
            object caster = GetObjectMember(abilityVerb, "caster", "Caster");
            Pawn pawn = caster as Pawn;
            if (pawn != null)
            {
                return pawn;
            }

            Verb verb = abilityVerb as Verb;
            return verb != null ? verb.CasterPawn : null;
        }

        private static MagicEventInfo BuildEventInfo(Pawn caster, object abilityVerb)
        {
            string source = SourceText(caster, abilityVerb);
            string lower = source.ToLowerInvariant();
            bool blood = lower.Contains("blood") || HasTraitKeyword(caster, "blood");
            bool necromancy = lower.Contains("necro")
                || lower.Contains("undead")
                || lower.Contains("raise")
                || lower.Contains("corpse")
                || lower.Contains("deathbolt")
                || HasTraitKeyword(caster, "necro")
                || HasHediffKeyword(caster, "undead");

            string classLabel = ClassLabel(caster);
            string abilityLabel = ReadableAbilityLabel(source);

            MagicEventInfo info = new MagicEventInfo();
            info.SourceName = string.IsNullOrEmpty(source) ? "unknown ability" : source;
            info.ReadableAbilityLabel = abilityLabel;
            info.ClassLabel = classLabel;
            info.IsBloodMagic = blood;
            info.IsNecromancy = necromancy;

            if (necromancy)
            {
                info.DisplayLabel = classLabel == "Necromancer" ? "Necromancer" : "Raised the dead";
                info.BaseImpact = -8f;
                return info;
            }

            if (blood)
            {
                info.DisplayLabel = "Blood mage";
                info.BaseImpact = -5f;
                return info;
            }

            info.DisplayLabel = !string.IsNullOrEmpty(classLabel) ? classLabel : "Used magic";
            info.BaseImpact = 2f;
            return info;
        }

        private static bool CanWitness(Pawn witness, Pawn caster)
        {
            return LegacyPawnEligibilityService.CanCreateLegacyEvents(witness)
                && witness != caster
                && witness.Map == caster.Map
                && GenSight.LineOfSight(witness.Position, caster.Position, caster.Map);
        }

        private static float ScoreForWitness(Pawn witness, MagicEventInfo info)
        {
            float score = info.BaseImpact;

            if (info.IsNecromancy)
            {
                if (HasTrait(witness, "Psychopath") || HasTrait(witness, "Bloodlust") || HasTraitKeyword(witness, "sadist"))
                {
                    score *= 0.55f;
                }

                if (HasIdeoKeyword(witness, "Death") || HasIdeoKeyword(witness, "Corpse"))
                {
                    score *= -0.35f;
                }
            }
            else if (info.IsBloodMagic)
            {
                if (HasTrait(witness, "Bloodlust") || HasTraitKeyword(witness, "blood"))
                {
                    score *= -0.45f;
                }
            }
            else if (HasTrait(witness, "Kind"))
            {
                score *= 1.2f;
            }

            if (HasIdeoKeyword(witness, "Darkness") || HasIdeoKeyword(witness, "PainIsVirtue"))
            {
                score *= info.BaseImpact < 0f ? 0.65f : 1.1f;
            }

            return score;
        }

        private static string WitnessDescription(Pawn witness, Pawn caster, MagicEventInfo info, float score)
        {
            string reaction = score >= 0f ? "impressive" : "troubling";
            return witness.LabelShort + " found it " + reaction + " when " + caster.LabelShort + " used "
                + info.ReadableAbilityLabel + ". Legacy interpretation may be further adjusted by traits, Psychology, Ideology, and Karma integrations.";
        }

        private static void ApplyClassTitle(Pawn caster, MagicEventInfo info)
        {
            if (Find.World == null || string.IsNullOrEmpty(info.DisplayLabel))
            {
                return;
            }

            LegacyWorldComponent component = Find.World.GetComponent<LegacyWorldComponent>();
            if (component != null)
            {
                component.SetPawnTitle(caster.thingIDNumber, info.DisplayLabel);
            }
        }

        private static string SourceText(Pawn caster, object abilityVerb)
        {
            string result = string.Empty;
            result += " " + GetDefName(GetObjectMember(abilityVerb, "Ability", "ability", "Power", "power"));
            result += " " + GetDefName(GetObjectMember(abilityVerb, "verbProps"));
            result += " " + GetDefName(GetObjectMember(abilityVerb, "EquipmentSource", "equipmentSource"));
            result += " " + TraitText(caster);
            return result.Trim();
        }

        private static string TraitText(Pawn pawn)
        {
            if (pawn.story == null || pawn.story.traits == null)
            {
                return string.Empty;
            }

            string result = string.Empty;
            foreach (Trait trait in pawn.story.traits.allTraits)
            {
                if (trait != null && trait.def != null)
                {
                    result += " " + trait.def.defName + " " + trait.Label;
                }
            }

            return result;
        }

        private static string ClassLabel(Pawn pawn)
        {
            if (HasTraitKeyword(pawn, "blood"))
            {
                return "Blood mage";
            }

            if (HasTraitKeyword(pawn, "necro"))
            {
                return "Necromancer";
            }

            if (pawn.story == null || pawn.story.traits == null)
            {
                return null;
            }

            foreach (Trait trait in pawn.story.traits.allTraits)
            {
                if (trait == null || trait.def == null)
                {
                    continue;
                }

                string defName = trait.def.defName;
                if (!string.IsNullOrEmpty(defName) && (defName.StartsWith("TM_") || defName.StartsWith("TorannMagic_")))
                {
                    return trait.Label;
                }
            }

            return null;
        }

        private static string ReadableAbilityLabel(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return "magic";
            }

            string[] parts = source.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return "magic";
            }

            string raw = parts[0];
            int dot = raw.LastIndexOf('.');
            if (dot >= 0)
            {
                raw = raw.Substring(dot + 1);
            }

            raw = raw.Replace("TM_", string.Empty).Replace("_", " ");
            return raw.ToLowerInvariant();
        }

        private static string GetDefName(object instance)
        {
            if (instance == null)
            {
                return string.Empty;
            }

            Def def = instance as Def;
            if (def != null)
            {
                return def.defName + " " + def.label;
            }

            object nestedDef = GetObjectMember(instance, "def", "Def");
            def = nestedDef as Def;
            if (def != null)
            {
                return def.defName + " " + def.label;
            }

            object label = GetObjectMember(instance, "label", "Label", "LabelCap");
            return label != null ? label.ToString() : instance.GetType().FullName;
        }

        private static bool HasTrait(Pawn pawn, string defName)
        {
            if (pawn.story == null || pawn.story.traits == null)
            {
                return false;
            }

            foreach (Trait trait in pawn.story.traits.allTraits)
            {
                if (trait != null && trait.def != null && trait.def.defName == defName)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasTraitKeyword(Pawn pawn, string keyword)
        {
            if (pawn.story == null || pawn.story.traits == null)
            {
                return false;
            }

            string lowerKeyword = keyword.ToLowerInvariant();
            foreach (Trait trait in pawn.story.traits.allTraits)
            {
                if (trait == null || trait.def == null)
                {
                    continue;
                }

                string text = (trait.def.defName + " " + trait.Label).ToLowerInvariant();
                if (text.Contains(lowerKeyword))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasHediffKeyword(Pawn pawn, string keyword)
        {
            if (pawn.health == null || pawn.health.hediffSet == null)
            {
                return false;
            }

            string lowerKeyword = keyword.ToLowerInvariant();
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {
                if (hediff != null && hediff.def != null && hediff.def.defName.ToLowerInvariant().Contains(lowerKeyword))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasIdeoKeyword(Pawn pawn, string keyword)
        {
            object ideo = GetObjectMember(pawn, "Ideo");
            if (ideo == null)
            {
                object ideoTracker = GetObjectMember(pawn, "ideo");
                ideo = GetObjectMember(ideoTracker, "Ideo");
            }

            if (ideo == null)
            {
                return false;
            }

            object precepts = GetObjectMember(ideo, "PreceptsListForReading", "precepts");
            System.Collections.IEnumerable enumerable = precepts as System.Collections.IEnumerable;
            if (enumerable == null)
            {
                return false;
            }

            string lowerKeyword = keyword.ToLowerInvariant();
            foreach (object precept in enumerable)
            {
                object def = GetObjectMember(precept, "def");
                string defName = GetObjectMember(def, "defName") as string;
                if (!string.IsNullOrEmpty(defName) && defName.ToLowerInvariant().Contains(lowerKeyword))
                {
                    return true;
                }
            }

            return false;
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
                PropertyInfo property = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null && property.GetIndexParameters().Length == 0)
                {
                    return property.GetValue(instance, null);
                }

                FieldInfo field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    return field.GetValue(instance);
                }
            }

            return null;
        }

        private class MagicEventInfo
        {
            public string SourceName;
            public string ReadableAbilityLabel;
            public string ClassLabel;
            public string DisplayLabel;
            public bool IsBloodMagic;
            public bool IsNecromancy;
            public float BaseImpact;
        }
    }
}
