using System.Collections.Generic;
using System.Linq;
using Legacy.Core;
using Legacy.Domain;
using Legacy.Services;
using Legacy.Storage;
using RimWorld;
using UnityEngine;
using Verse;

namespace Legacy.UI.Debug
{
    public class Dialog_LegacyDebug : Window
    {
        private static readonly string[] PositiveTitleWords = { "Beloved", "Bright", "Kind", "Loyal", "Steadfast", "Sheltering" };
        private static readonly string[] NegativeTitleWords = { "Feared", "Bitter", "Grim", "Haunted", "Vengeful", "Cold" };
        private static readonly string[] NeutralTitleWords = { "Witness", "Survivor", "Keeper", "Wanderer", "Namebearer", "Archivist" };

        private Vector2 relationScroll;
        private int selectedPawnId = -1;
        private string selectedPawnName = "Select pawn";
        private string titleBuffer = string.Empty;

        public Dialog_LegacyDebug()
        {
            doCloseButton = true;
            doCloseX = true;
            absorbInputAroundWindow = true;
        }

        public Dialog_LegacyDebug(Pawn pawn) : this()
        {
            if (pawn != null)
            {
                selectedPawnId = pawn.thingIDNumber;
                selectedPawnName = pawn.LabelShort;
                LegacyWorldComponent component = GetComponent();
                titleBuffer = component != null ? component.GetPawnTitle(selectedPawnId) : string.Empty;
            }
        }

        public override Vector2 InitialSize
        {
            get { return new Vector2(900f, 620f); }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 34f), "Legacy Records");
            Text.Font = GameFont.Small;

            DrawPawnSelector(new Rect(inRect.x, inRect.y + 38f, 260f, 32f));

            Rect bodyRect = new Rect(inRect.x, inRect.y + 84f, inRect.width, inRect.height - 124f);
            float leftWidth = 150f;
            float gap = 14f;
            float relationWidth = bodyRect.width - leftWidth - gap;

            Rect pawnRect = new Rect(bodyRect.x, bodyRect.y, leftWidth, 184f);
            Rect titleRect = new Rect(bodyRect.x, pawnRect.yMax + 10f, leftWidth, 112f);
            Rect relationRect = new Rect(pawnRect.xMax + gap, bodyRect.y, relationWidth, bodyRect.height);

            DrawPawnView(pawnRect);
            DrawPawnTitle(titleRect);
            DrawRelations(relationRect);
        }

        private void DrawPawnSelector(Rect rect)
        {
            if (Widgets.ButtonText(rect, selectedPawnName))
            {
                OpenPawnSelector();
            }
        }

        private void DrawPawnView(Rect rect)
        {
            DrawPanel(rect);
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(new Rect(rect.x + 6f, rect.y + 8f, rect.width - 12f, 24f), "Pawn View");
            Text.Anchor = TextAnchor.UpperLeft;

            Rect portraitRect = new Rect(rect.x + 16f, rect.y + 38f, rect.width - 32f, 104f);
            Pawn pawn = TryResolveSelectedPawn();
            if (pawn != null)
            {
                Texture portrait = PortraitsCache.Get(pawn, new Vector2(portraitRect.width, portraitRect.height), Rot4.South);
                GUI.DrawTexture(portraitRect, portrait, ScaleMode.ScaleToFit);
            }
            else
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(portraitRect, selectedPawnId >= 0 ? selectedPawnName : "No pawn selected");
                Text.Anchor = TextAnchor.UpperLeft;
            }

            if (!string.IsNullOrEmpty(titleBuffer))
            {
                Text.Anchor = TextAnchor.UpperCenter;
                Widgets.Label(new Rect(rect.x + 8f, portraitRect.yMax + 8f, rect.width - 16f, 28f), titleBuffer);
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }

        private void DrawPawnTitle(Rect rect)
        {
            DrawPanel(rect);
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(new Rect(rect.x + 6f, rect.y + 8f, rect.width - 12f, 24f), "Pawn Title");
            Text.Anchor = TextAnchor.UpperLeft;

            if (selectedPawnId < 0)
            {
                Widgets.Label(new Rect(rect.x + 8f, rect.y + 42f, rect.width - 16f, 24f), "Select a pawn first.");
                return;
            }

            titleBuffer = Widgets.TextField(new Rect(rect.x + 8f, rect.y + 38f, rect.width - 16f, 28f), titleBuffer);

            if (Widgets.ButtonText(new Rect(rect.x + 8f, rect.y + 72f, 62f, 28f), "Save"))
            {
                LegacyWorldComponent component = GetComponent();
                if (component != null)
                {
                    component.SetPawnTitle(selectedPawnId, titleBuffer);
                }
            }

            if (Widgets.ButtonText(new Rect(rect.x + 78f, rect.y + 72f, rect.width - 86f, 28f), "Random"))
            {
                titleBuffer = GenerateTitle(GetRecordsForSelectedPawn());
                LegacyWorldComponent component = GetComponent();
                if (component != null)
                {
                    component.SetPawnTitle(selectedPawnId, titleBuffer);
                }
            }

        }

        private void DrawRelations(Rect rect)
        {
            DrawPanel(rect);
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, 24f), "Pawn Relation + Faction");
            Text.Anchor = TextAnchor.UpperLeft;

            Rect outRect = new Rect(rect.x + 8f, rect.y + 38f, rect.width - 16f, rect.height - 46f);
            List<RelationSummary> summaries = BuildRelationSummaries(GetRecordsForSelectedPawn());

            if (selectedPawnId < 0)
            {
                Widgets.Label(outRect, "Select a pawn first.");
                return;
            }

            if (summaries.Count == 0)
            {
                Widgets.Label(outRect, "No relational impacts recorded yet.");
                return;
            }

            float rowHeight = 54f;
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, summaries.Count * rowHeight);
            Widgets.BeginScrollView(outRect, ref relationScroll, viewRect);

            float y = 0f;
            foreach (RelationSummary summary in summaries)
            {
                Rect row = new Rect(0f, y, viewRect.width, rowHeight - 6f);
                Widgets.DrawBoxSolid(row, new Color(0.16f, 0.16f, 0.16f, 0.35f));
                string score = ShowDebugDetails() ? "  " + summary.Score.ToString("+0;-0;0") : string.Empty;
                string kind = summary.Kind != LegacyRelationshipKind.Neutral ? "  [" + summary.Kind + "]" : string.Empty;
                if (Mouse.IsOver(row))
                {
                    Widgets.DrawHighlight(row);
                }

                if (Widgets.ButtonInvisible(row))
                {
                    Find.WindowStack.Add(new Dialog_LegacyHistory(selectedPawnId, selectedPawnName, summary.OtherPawnId, summary.Name));
                }

                Widgets.Label(new Rect(row.x + 8f, row.y + 6f, row.width - 16f, 22f), summary.Name + score + kind);
                Widgets.Label(new Rect(row.x + 8f, row.y + 26f, row.width - 16f, 18f), summary.Count + " records - click to view history");
                y += rowHeight;
            }

            Widgets.EndScrollView();
        }

        private static void DrawRecordList(Rect outRect, List<LegacyRecord> records, ref Vector2 scroll)
        {
            float rowHeight = 72f;
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, records.Count * rowHeight);
            Widgets.BeginScrollView(outRect, ref scroll, viewRect);

            float y = 0f;
            foreach (LegacyRecord record in records)
            {
                DrawRecord(new Rect(0f, y, viewRect.width, 64f), record);
                y += rowHeight;
            }

            Widgets.EndScrollView();
        }

        private static void DrawPanel(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.08f, 0.09f, 0.1f, 0.72f));
            Widgets.DrawBox(rect);
        }

        private void OpenPawnSelector()
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            foreach (LegacySubjectRef pawnRef in GetKnownPawns())
            {
                LegacySubjectRef localPawnRef = pawnRef;
                options.Add(new FloatMenuOption(localPawnRef.name, delegate
                {
                    selectedPawnId = localPawnRef.thingIdNumber;
                    selectedPawnName = localPawnRef.name;
                    LegacyWorldComponent component = GetComponent();
                    titleBuffer = component != null ? component.GetPawnTitle(selectedPawnId) : string.Empty;
                    relationScroll = Vector2.zero;
                }));
            }

            if (options.Count == 0)
            {
                options.Add(new FloatMenuOption("No Legacy pawns recorded", null));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static List<LegacySubjectRef> GetKnownPawns()
        {
            Dictionary<int, LegacySubjectRef> pawns = new Dictionary<int, LegacySubjectRef>();
            foreach (LegacyRecord record in GetAllRecords())
            {
                AddPawnRef(pawns, record.subject);

                if (record.participants == null)
                {
                    continue;
                }

                foreach (LegacyParticipant participant in record.participants)
                {
                    if (participant != null && participant.role == LegacyParticipantRole.OtherPawn)
                    {
                        AddPawnRef(pawns, participant.pawn);
                    }
                }
            }

            return pawns.Values.OrderBy(pawnRef => pawnRef.name).ToList();
        }

        private static void AddPawnRef(Dictionary<int, LegacySubjectRef> pawns, LegacySubjectRef pawnRef)
        {
            if (pawnRef == null || pawnRef.thingIdNumber < 0 || pawns.ContainsKey(pawnRef.thingIdNumber))
            {
                return;
            }

            pawns.Add(pawnRef.thingIdNumber, pawnRef);
        }

        private List<LegacyRecord> GetRecordsForSelectedPawn()
        {
            if (selectedPawnId < 0)
            {
                return new List<LegacyRecord>();
            }

            return GetAllRecords()
                .Where(InvolvesSelectedPawn)
                .OrderByDescending(record => record.tick)
                .ToList();
        }

        private bool InvolvesSelectedPawn(LegacyRecord record)
        {
            if (record == null)
            {
                return false;
            }

            if (record.subject != null && record.subject.thingIdNumber == selectedPawnId)
            {
                return true;
            }

            if (record.participants == null)
            {
                return false;
            }

            foreach (LegacyParticipant participant in record.participants)
            {
                if (participant != null && participant.pawn != null && participant.pawn.thingIdNumber == selectedPawnId)
                {
                    return true;
                }
            }

            return false;
        }

        private static List<LegacyRecord> GetAllRecords()
        {
            LegacyWorldComponent component = GetComponent();
            return component != null ? new List<LegacyRecord>(component.Repository.AllRecords()) : new List<LegacyRecord>();
        }

        private static LegacyWorldComponent GetComponent()
        {
            return Find.World != null ? Find.World.GetComponent<LegacyWorldComponent>() : null;
        }

        private Pawn TryResolveSelectedPawn()
        {
            if (selectedPawnId < 0 || Find.Maps == null)
            {
                return null;
            }

            foreach (Map map in Find.Maps)
            {
                if (map == null || map.mapPawns == null)
                {
                    continue;
                }

                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                {
                    if (pawn != null && pawn.thingIDNumber == selectedPawnId)
                    {
                        return pawn;
                    }
                }
            }

            return null;
        }

        internal static void DrawRecord(Rect rect, LegacyRecord record)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.16f, 0.16f, 0.16f, 0.35f));
            Rect inner = rect.ContractedBy(8f);

            string subjectName = record.subject != null ? record.subject.name : "Unknown pawn";
            string otherPawnName = GetOtherPawnName(record);
            string label = !string.IsNullOrEmpty(record.label) ? record.label : "Legacy impact";
            string headline = BuildHeadline(record, subjectName, otherPawnName);
            string impact = ShowDebugDetails() && record.moodOffset != 0f ? " (" + record.moodOffset.ToString("+0;-0") + ")" : string.Empty;
            string description = CleanDescription(record.description, otherPawnName);

            Widgets.Label(new Rect(inner.x, inner.y, inner.width, 22f), headline + impact);
            Widgets.Label(new Rect(inner.x, inner.y + 22f, inner.width, 22f), !string.IsNullOrEmpty(description) ? description : label);
            if (ShowDebugDetails())
            {
                Widgets.Label(new Rect(inner.x, inner.y + 44f, inner.width, 20f), "Tick " + record.tick);
            }
        }

        internal static bool ShowDebugDetails()
        {
            return LegacyMod.Settings != null && LegacyMod.Settings.showDebugDetails;
        }

        private static string BuildHeadline(LegacyRecord record, string subjectName, string otherPawnName)
        {
            if (record != null
                && !string.IsNullOrEmpty(record.label)
                && record.label.Contains(subjectName)
                && record.label.Contains(otherPawnName))
            {
                return record.label;
            }

            string action = GetActionPhrase(record);
            if (record != null && record.polarity == LegacyImpactPolarity.Positive)
            {
                return otherPawnName + " helped " + subjectName + ": " + action;
            }

            return subjectName + " was " + action + " by " + otherPawnName;
        }

        private static string GetActionPhrase(LegacyRecord record)
        {
            string source = GetSourceText(record).ToLowerInvariant();

            if (source.Contains("shot") || source.Contains("gun") || source.Contains("bullet"))
            {
                return "shot";
            }

            if (source.Contains("stab") || source.Contains("cut") || source.Contains("melee") || source.Contains("combatdamage"))
            {
                return "attacked";
            }

            if (source.Contains("captur") || source.Contains("imprison"))
            {
                return "captured";
            }

            if (source.Contains("rescu"))
            {
                return "rescued";
            }

            if (source.Contains("harvest") || source.Contains("organ"))
            {
                return "harmed";
            }

            if (source.Contains("install") || source.Contains("prosthetic") || source.Contains("implant"))
            {
                return "treated";
            }

            if (record != null && record.polarity == LegacyImpactPolarity.Positive)
            {
                return "positive impact";
            }

            return "harmed";
        }

        private static string GetSourceText(LegacyRecord record)
        {
            if (record == null)
            {
                return string.Empty;
            }

            string result = string.Empty;
            result += record.sourceThoughtDefName ?? string.Empty;
            result += " ";
            result += record.label ?? string.Empty;
            result += " ";
            result += record.description ?? string.Empty;
            result += " ";
            result += record.context != null ? record.context.cause ?? string.Empty : string.Empty;
            return result;
        }

        private static string CleanDescription(string description, string otherPawnName)
        {
            if (string.IsNullOrEmpty(description))
            {
                return string.Empty;
            }

            string result = description;
            result = result.Replace("The colony harvested", otherPawnName + " harvested");
            result = result.Replace("the colony harvested", otherPawnName + " harvested");
            return result;
        }

        private static string GetOtherPawnName(LegacyRecord record)
        {
            LegacySubjectRef otherPawn = GetOtherPawn(record);
            return otherPawn != null ? otherPawn.name : "Unknown pawn";
        }

        internal static LegacySubjectRef GetOtherPawn(LegacyRecord record)
        {
            if (record.participants == null)
            {
                return null;
            }

            foreach (LegacyParticipant participant in record.participants)
            {
                if (participant != null && participant.role == LegacyParticipantRole.OtherPawn && participant.pawn != null)
                {
                    return participant.pawn;
                }
            }

            return null;
        }

        private List<RelationSummary> BuildRelationSummaries(List<LegacyRecord> records)
        {
            Dictionary<int, RelationSummary> summaries = new Dictionary<int, RelationSummary>();

            foreach (LegacyRecord record in records)
            {
                LegacySubjectRef other = GetRelationCounterpart(record);
                if (other == null)
                {
                    continue;
                }

                RelationSummary summary;
                if (!summaries.TryGetValue(other.thingIdNumber, out summary))
                {
                    summary = new RelationSummary { Name = other.name, OtherPawnId = other.thingIdNumber };
                    summaries[other.thingIdNumber] = summary;
                }

                Pawn subjectPawn = LegacyPawnEligibilityService.TryResolveAlivePawn(selectedPawnId);
                summary.Score += LegacyForgivenessService.AdjustRelationshipImpact(subjectPawn, record);
                summary.Count++;
            }

            foreach (RelationSummary summary in summaries.Values)
            {
                LegacyWorldComponent component = GetComponent();
                LegacyRelationshipState state = component != null ? component.GetRelationshipState(selectedPawnId, summary.OtherPawnId) : null;
                if (state != null)
                {
                    summary.Kind = state.kind;
                }
            }

            return summaries.Values.OrderBy(summary => summary.Score).ToList();
        }

        private LegacySubjectRef GetRelationCounterpart(LegacyRecord record)
        {
            if (record.subject != null && record.subject.thingIdNumber != selectedPawnId)
            {
                return record.subject;
            }

            return GetOtherPawn(record);
        }

        private static string GenerateTitle(List<LegacyRecord> records)
        {
            float score = records.Sum(record => record.moodOffset);
            string[] pool = score > 8f ? PositiveTitleWords : score < -8f ? NegativeTitleWords : NeutralTitleWords;
            string word = pool[Rand.Range(0, pool.Length)];
            return ShowDebugDetails() ? word + " " + Rand.Range(1, 1000).ToString("000") : word;
        }

        private class RelationSummary
        {
            public string Name;
            public int OtherPawnId;
            public float Score;
            public int Count;
            public LegacyRelationshipKind Kind;
        }
    }

    public class Dialog_LegacyHistory : Window
    {
        private readonly int subjectPawnId;
        private readonly int otherPawnId;
        private readonly string subjectName;
        private readonly string otherPawnName;
        private Vector2 scroll;

        public Dialog_LegacyHistory(int subjectPawnId, string subjectName, int otherPawnId, string otherPawnName)
        {
            this.subjectPawnId = subjectPawnId;
            this.subjectName = subjectName;
            this.otherPawnId = otherPawnId;
            this.otherPawnName = otherPawnName;
            doCloseButton = true;
            doCloseX = true;
            absorbInputAroundWindow = true;
        }

        public override Vector2 InitialSize
        {
            get { return new Vector2(700f, 560f); }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 34f), subjectName + " and " + otherPawnName);
            Text.Font = GameFont.Small;

            Rect outRect = new Rect(inRect.x, inRect.y + 44f, inRect.width, inRect.height - 84f);
            List<LegacyRecord> records = GetRecords();
            if (records.Count == 0)
            {
                Widgets.Label(outRect, "No Legacy records for this relationship.");
                return;
            }

            DrawRecordList(outRect, records);
        }

        private void DrawRecordList(Rect outRect, List<LegacyRecord> records)
        {
            float rowHeight = 72f;
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, records.Count * rowHeight);
            Widgets.BeginScrollView(outRect, ref scroll, viewRect);

            float y = 0f;
            foreach (LegacyRecord record in records)
            {
                Dialog_LegacyDebug.DrawRecord(new Rect(0f, y, viewRect.width, 64f), record);
                y += rowHeight;
            }

            Widgets.EndScrollView();
        }

        private List<LegacyRecord> GetRecords()
        {
            LegacyWorldComponent component = Find.World != null ? Find.World.GetComponent<LegacyWorldComponent>() : null;
            if (component == null)
            {
                return new List<LegacyRecord>();
            }

            return component.Repository.AllRecords()
                .Where(InvolvesPair)
                .OrderByDescending(record => record.tick)
                .ToList();
        }

        private bool InvolvesPair(LegacyRecord record)
        {
            if (record == null || record.subject == null)
            {
                return false;
            }

            LegacySubjectRef other = Dialog_LegacyDebug.GetOtherPawn(record);
            if (other == null)
            {
                return false;
            }

            return (record.subject.thingIdNumber == subjectPawnId && other.thingIdNumber == otherPawnId)
                || (record.subject.thingIdNumber == otherPawnId && other.thingIdNumber == subjectPawnId);
        }
    }
}
