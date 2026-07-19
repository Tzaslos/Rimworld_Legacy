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
            EnsureSelectedPawnIsSelectable();

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

        private void EnsureSelectedPawnIsSelectable()
        {
            if (selectedPawnId >= 0)
            {
                Pawn selectedPawn = LegacyPawnEligibilityService.TryResolveKnownPawn(selectedPawnId);
                if (IsSelectableColonyPawn(selectedPawn))
                {
                    return;
                }
            }

            List<Pawn> pawns = GetSelectableColonyPawns();
            if (pawns.Count > 0)
            {
                SelectPawn(pawns[0]);
                return;
            }

            selectedPawnId = -1;
            selectedPawnName = "No colony pawn";
            titleBuffer = string.Empty;
        }

        private void DrawPawnSelector(Rect rect)
        {
            if (Widgets.ButtonText(rect, selectedPawnName))
            {
                SelectNextColonyPawn();
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

            float headerHeight = 24f;
            float rowHeight = 30f;
            float nameWidth = outRect.width * 0.34f;
            float factionWidth = outRect.width * 0.26f;
            float relationWidth = outRect.width * 0.16f;
            float statusWidth = outRect.width - nameWidth - factionWidth - relationWidth - 26f;

            DrawRelationHeader(new Rect(outRect.x, outRect.y, outRect.width - 16f, headerHeight), nameWidth, factionWidth, relationWidth, statusWidth);

            Rect scrollOutRect = new Rect(outRect.x, outRect.y + headerHeight, outRect.width, outRect.height - headerHeight);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, summaries.Count * rowHeight);
            Widgets.BeginScrollView(scrollOutRect, ref relationScroll, viewRect);

            float y = 0f;
            foreach (RelationSummary summary in summaries)
            {
                Rect row = new Rect(0f, y, viewRect.width, rowHeight - 4f);
                Widgets.DrawBoxSolid(row, new Color(0.16f, 0.16f, 0.16f, 0.35f));
                if (Mouse.IsOver(row))
                {
                    Widgets.DrawHighlight(row);
                }

                if (Widgets.ButtonInvisible(row))
                {
                    Find.WindowStack.Add(new Dialog_LegacyHistory(selectedPawnId, selectedPawnName, summary.OtherPawnId, summary.Name));
                }

                DrawRelationRow(row, summary, nameWidth, factionWidth, relationWidth, statusWidth);
                y += rowHeight;
            }

            Widgets.EndScrollView();
        }

        private static void DrawRelationHeader(Rect rect, float nameWidth, float factionWidth, float relationWidth, float statusWidth)
        {
            Color previousColor = GUI.color;
            GUI.color = new Color(0.72f, 0.72f, 0.72f);
            float x = rect.x + 8f;
            Widgets.Label(new Rect(x, rect.y + 2f, nameWidth - 8f, rect.height), "Pawn");
            x += nameWidth;
            Widgets.Label(new Rect(x, rect.y + 2f, factionWidth - 8f, rect.height), "Faction");
            x += factionWidth;
            Widgets.Label(new Rect(x, rect.y + 2f, relationWidth - 8f, rect.height), "Relation");
            x += relationWidth;
            Widgets.Label(new Rect(x, rect.y + 2f, statusWidth - 8f, rect.height), "Status");
            GUI.color = previousColor;
        }

        private static void DrawRelationRow(Rect row, RelationSummary summary, float nameWidth, float factionWidth, float relationWidth, float statusWidth)
        {
            float x = row.x + 8f;
            Widgets.Label(new Rect(x, row.y + 5f, nameWidth - 8f, 22f), summary.Name);
            x += nameWidth;
            Widgets.Label(new Rect(x, row.y + 5f, factionWidth - 8f, 22f), summary.FactionText);
            x += factionWidth;

            Color previousColor = GUI.color;
            GUI.color = RelationColor(summary.Kind);
            Widgets.Label(new Rect(x, row.y + 5f, relationWidth - 8f, 22f), summary.Kind.ToString());
            GUI.color = previousColor;

            x += relationWidth;
            Widgets.Label(new Rect(x, row.y + 5f, statusWidth - 8f, 22f), summary.StatusText);
        }

        private static Color RelationColor(LegacyRelationshipKind kind)
        {
            if (kind == LegacyRelationshipKind.Hero)
            {
                return Color.green;
            }

            if (kind == LegacyRelationshipKind.Nemesis)
            {
                return Color.red;
            }

            return Color.yellow;
        }

        private static void DrawRecordList(Rect outRect, List<LegacyRecord> records, ref Vector2 scroll)
        {
            float rowHeight = 44f;
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, records.Count * rowHeight);
            Widgets.BeginScrollView(outRect, ref scroll, viewRect);

            float y = 0f;
            foreach (LegacyRecord record in records)
            {
                DrawRecord(new Rect(0f, y, viewRect.width, 36f), record);
                y += rowHeight;
            }

            Widgets.EndScrollView();
        }

        private static void DrawPanel(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.08f, 0.09f, 0.1f, 0.72f));
            Widgets.DrawBox(rect);
        }

        private void SelectNextColonyPawn()
        {
            List<Pawn> pawns = GetSelectableColonyPawns();
            if (pawns.Count == 0)
            {
                Messages.Message("No colony pawns available for Legacy.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            int nextIndex = 0;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i] != null && pawns[i].thingIDNumber == selectedPawnId)
                {
                    nextIndex = (i + 1) % pawns.Count;
                    break;
                }
            }

            SelectPawn(pawns[nextIndex]);
        }

        private void SelectPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            selectedPawnId = pawn.thingIDNumber;
            selectedPawnName = pawn.LabelShort;
            LegacyWorldComponent component = GetComponent();
            titleBuffer = component != null ? component.GetPawnTitle(selectedPawnId) : string.Empty;
            relationScroll = Vector2.zero;
        }

        private static List<Pawn> GetSelectableColonyPawns()
        {
            List<Pawn> pawns = new List<Pawn>();
            if (Find.Maps == null)
            {
                return pawns;
            }

            foreach (Map map in Find.Maps)
            {
                if (map == null || !map.IsPlayerHome || map.mapPawns == null)
                {
                    continue;
                }

                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                {
                    if (IsSelectableColonyPawn(pawn))
                    {
                        pawns.Add(pawn);
                    }
                }
            }

            return pawns
                .OrderBy(pawn => pawn.LabelShort)
                .ToList();
        }

        private static bool IsSelectableColonyPawn(Pawn pawn)
        {
            if (!LegacyPawnEligibilityService.CanCreateLegacyEvents(pawn) || pawn.Map == null || !pawn.Map.IsPlayerHome)
            {
                return false;
            }

            return pawn.IsColonist
                || pawn.IsSlave
                || (pawn.guest != null && pawn.guest.IsPrisoner);
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
            string headline = BuildHeadline(record, subjectName, otherPawnName);
            string impact = record.moodOffset != 0f ? " (" + record.moodOffset.ToString("+0;-0") + ")" : string.Empty;
            float impactWidth = !string.IsNullOrEmpty(impact) ? Text.CalcSize(impact).x + 4f : 0f;

            Widgets.Label(new Rect(inner.x, inner.y + 2f, inner.width - impactWidth, 22f), headline);
            if (!string.IsNullOrEmpty(impact))
            {
                Color previousColor = GUI.color;
                GUI.color = record.moodOffset > 0f ? Color.green : Color.red;
                Widgets.Label(new Rect(inner.x + inner.width - impactWidth, inner.y + 2f, impactWidth, 22f), impact);
                GUI.color = previousColor;
            }
        }

        internal static bool ShowDebugDetails()
        {
            return LegacyMod.Settings != null && LegacyMod.Settings.debugMode && LegacyMod.Settings.showDebugDetails;
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

            if (source.Contains("rjw") || source.Contains("rape") || source.Contains("non-consensual") || source.Contains("nonconsensual") || source.Contains("forced") || source.Contains("sexual"))
            {
                return "sexually assaulted";
            }

            if (source.Contains("stab") || source.Contains("cut") || source.Contains("melee") || source.Contains("combatdamage"))
            {
                return "attacked";
            }

            if (source.Contains("captur") || source.Contains("imprison"))
            {
                return "imprisoned";
            }

            if (source.Contains("slave") || source.Contains("enslav"))
            {
                return "enslaved";
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
                    summary = new RelationSummary
                    {
                        Name = other.name,
                        OtherPawnId = other.thingIdNumber,
                        FactionText = BuildFactionText(other),
                        StatusText = BuildStatusText(other.thingIdNumber, other)
                    };
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

            return summaries.Values
                .OrderBy(summary => summary.Kind == LegacyRelationshipKind.Nemesis ? 0 : summary.Kind == LegacyRelationshipKind.Hero ? 1 : 2)
                .ThenBy(summary => summary.Name)
                .ToList();
        }

        private static string BuildFactionText(LegacySubjectRef pawnRef)
        {
            Pawn pawn = pawnRef != null ? LegacyPawnEligibilityService.TryResolveKnownPawn(pawnRef.thingIdNumber) : null;
            Faction faction = pawn != null ? pawn.Faction : null;
            string factionName = faction != null ? faction.Name : pawnRef != null ? pawnRef.factionName : null;
            if (string.IsNullOrEmpty(factionName))
            {
                factionName = "Independent";
            }

            return factionName;
        }

        private static string BuildStatusText(int pawnId, LegacySubjectRef pawnRef)
        {
            Pawn pawn = LegacyPawnEligibilityService.TryResolveKnownPawn(pawnId);
            if (pawn == null)
            {
                if (pawnRef != null && pawnRef.wasSlave)
                {
                    return "formerly enslaved";
                }

                if (pawnRef != null && pawnRef.wasPrisoner)
                {
                    return "formerly imprisoned";
                }

                return "whereabouts unknown";
            }

            if (pawn.Dead)
            {
                return "dead";
            }

            string captivity = CurrentCaptivityText(pawn);
            if (!string.IsNullOrEmpty(captivity))
            {
                return captivity;
            }

            if (pawn.Spawned)
            {
                if (pawn.Map != null && pawn.Map.IsPlayerHome)
                {
                    if (pawn.HostileTo(Faction.OfPlayer))
                    {
                        return "raiding on player map";
                    }

                    return "roaming on player map";
                }

                if (pawn.HostileTo(Faction.OfPlayer))
                {
                    return "hostile on nearby map";
                }

                return "active on nearby map";
            }

            if (pawn.HostileTo(Faction.OfPlayer))
            {
                return "hostile, roaming the world";
            }

            return "roaming the world";
        }

        private static string CurrentCaptivityText(Pawn pawn)
        {
            if (pawn == null)
            {
                return null;
            }

            if (pawn.guest != null && pawn.guest.IsPrisoner)
            {
                return "imprisoned";
            }

            if (pawn.IsSlave)
            {
                return "enslaved";
            }

            return null;
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
            public string FactionText;
            public string StatusText;
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
            float rowHeight = 44f;
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, records.Count * rowHeight);
            Widgets.BeginScrollView(outRect, ref scroll, viewRect);

            float y = 0f;
            foreach (LegacyRecord record in records)
            {
                Dialog_LegacyDebug.DrawRecord(new Rect(0f, y, viewRect.width, 36f), record);
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
