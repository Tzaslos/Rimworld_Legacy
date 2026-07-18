using System;
using System.Reflection;
using Legacy.Domain;
using RimWorld;
using Verse;

namespace Legacy.Services
{
    public static class LegacyForgivenessService
    {
        private const string PsychologyPackageId = "Community.Psychology.UnofficialUpdate";
        private static Type psychologyNodeDefType;
        private static Def compassionateNodeDef;
        private static bool reflected;

        public static float AdjustRelationshipImpact(Pawn subject, LegacyRecord record)
        {
            if (record == null || !LegacyPawnEligibilityService.CanCreateLegacyEvents(subject))
            {
                return record != null ? record.moodOffset : 0f;
            }

            return record.moodOffset * InterpretationMultiplier(subject, record);
        }

        private static float InterpretationMultiplier(Pawn pawn, LegacyRecord record)
        {
            string source = SourceText(record);
            bool negative = record.moodOffset < 0f;
            bool positive = record.moodOffset > 0f;
            float multiplier = 1f;

            float compassion;
            if (TryGetPsychologyRating(pawn, "Compassionate", out compassion))
            {
                if (negative && compassion >= 0.75f)
                {
                    multiplier *= 0.45f;
                }
                else if (negative && compassion >= 0.6f)
                {
                    multiplier *= 0.65f;
                }
                else if (negative && compassion <= 0.25f)
                {
                    multiplier *= 1.2f;
                }
            }

            float aggressive;
            if (TryGetPsychologyRating(pawn, "Aggressive", out aggressive) && aggressive >= 0.7f)
            {
                multiplier *= positive ? 0.55f : 1.15f;
            }

            if (negative && HasTrait(pawn, "Kind"))
            {
                multiplier *= 0.55f;
            }

            if (HasTrait(pawn, "Psychopath") || HasTrait(pawn, "Bloodlust") || HasTraitKeyword(pawn, "sadist"))
            {
                multiplier *= positive ? 0.45f : 1.25f;
            }

            if (HasTrait(pawn, "Abrasive"))
            {
                multiplier *= positive ? 0.75f : 1.15f;
            }

            if (IsPainRelated(source) && (HasTrait(pawn, "Masochist") || HasTraitKeyword(pawn, "masoch") || HasIdeoKeyword(pawn, "PainIsVirtue")))
            {
                multiplier *= negative ? -0.35f : 1.2f;
            }

            if ((IsHumanMeatRelated(source) || HasTrait(pawn, "Cannibal") || HasIdeoKeyword(pawn, "Cannibal"))
                && (HasTrait(pawn, "Cannibal") || HasIdeoKeyword(pawn, "Cannibal")))
            {
                multiplier *= positive ? 0.35f : 0.85f;
            }

            return multiplier;
        }

        private static bool TryGetPsychologyRating(Pawn pawn, string nodeDefName, out float rating)
        {
            rating = 0f;
            if (!ModsConfig.IsActive(PsychologyPackageId) || !EnsurePsychologyReflected())
            {
                return false;
            }

            object psyche = TryGetPsyche(pawn);
            Def nodeDef = GetPsychologyNodeDef(nodeDefName);
            if (psyche == null || nodeDef == null)
            {
                return false;
            }

            object value = InvokeRatingMethod(psyche, nodeDef);
            if (value == null)
            {
                value = InvokeRatingMethod(psyche, nodeDefName);
            }

            if (value == null)
            {
                return false;
            }

            try
            {
                rating = Convert.ToSingle(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool EnsurePsychologyReflected()
        {
            if (reflected)
            {
                return psychologyNodeDefType != null;
            }

            reflected = true;
            psychologyNodeDefType = GenTypes.GetTypeInAnyAssembly("Psychology.PersonalityNodeDef");
            if (psychologyNodeDefType == null)
            {
                return false;
            }

            MethodInfo getNamed = null;
            foreach (MethodInfo method in typeof(DefDatabase<>).MakeGenericType(psychologyNodeDefType).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (method.Name == "GetNamedSilentFail" && parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
                {
                    getNamed = method;
                    break;
                }
            }

            if (getNamed != null)
            {
                compassionateNodeDef = getNamed.Invoke(null, new object[] { "Compassionate" }) as Def;
            }

            return psychologyNodeDefType != null;
        }

        private static Def GetPsychologyNodeDef(string defName)
        {
            if (defName == "Compassionate" && compassionateNodeDef != null)
            {
                return compassionateNodeDef;
            }

            if (psychologyNodeDefType == null)
            {
                return null;
            }

            foreach (MethodInfo method in typeof(DefDatabase<>).MakeGenericType(psychologyNodeDefType).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (method.Name == "GetNamedSilentFail" && parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
                {
                    return method.Invoke(null, new object[] { defName }) as Def;
                }
            }

            return null;
        }

        private static object TryGetPsyche(Pawn pawn)
        {
            if (pawn.AllComps == null)
            {
                return null;
            }

            foreach (ThingComp comp in pawn.AllComps)
            {
                if (comp == null || comp.GetType().FullName == null || !comp.GetType().FullName.Contains("CompPsychology"))
                {
                    continue;
                }

                PropertyInfo property = comp.GetType().GetProperty("Psyche", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null)
                {
                    return property.GetValue(comp, null);
                }
            }

            return null;
        }

        private static object InvokeRatingMethod(object psyche, object argument)
        {
            foreach (MethodInfo method in psyche.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (method.Name != "GetPersonalityRating")
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length != 1 || argument == null || !parameters[0].ParameterType.IsInstanceOfType(argument))
                {
                    continue;
                }

                return method.Invoke(psyche, new object[] { argument });
            }

            return null;
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

            foreach (Trait trait in pawn.story.traits.allTraits)
            {
                if (trait == null || trait.def == null || string.IsNullOrEmpty(trait.def.defName))
                {
                    continue;
                }

                if (trait.def.defName.ToLowerInvariant().Contains(keyword))
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

            foreach (object precept in enumerable)
            {
                object def = GetObjectMember(precept, "def");
                string defName = GetObjectMember(def, "defName") as string;
                if (!string.IsNullOrEmpty(defName) && defName.ToLowerInvariant().Contains(keyword.ToLowerInvariant()))
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

        private static string SourceText(LegacyRecord record)
        {
            string result = string.Empty;
            result += record.sourceThoughtDefName ?? string.Empty;
            result += " ";
            result += record.label ?? string.Empty;
            result += " ";
            result += record.description ?? string.Empty;
            result += " ";
            result += record.context != null ? record.context.cause ?? string.Empty : string.Empty;
            return result.ToLowerInvariant();
        }

        private static bool IsPainRelated(string source)
        {
            return source.Contains("pain")
                || source.Contains("hurt")
                || source.Contains("wound")
                || source.Contains("injur")
                || source.Contains("shot")
                || source.Contains("stab")
                || source.Contains("combatdamage")
                || source.Contains("rape")
                || source.Contains("non-consensual")
                || source.Contains("nonconsensual")
                || source.Contains("forced")
                || source.Contains("abuse");
        }

        private static bool IsHumanMeatRelated(string source)
        {
            return source.Contains("cannibal")
                || source.Contains("human meat")
                || source.Contains("ate human")
                || source.Contains("organ")
                || source.Contains("harvest");
        }
    }
}
