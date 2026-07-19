using System.Collections.Generic;
using Legacy.Core;
using Legacy.Services;
using Legacy.Domain;
using RimWorld.Planet;
using Verse;

namespace Legacy.Storage
{
    public class LegacyWorldComponent : WorldComponent
    {
        private int dataVersion = 1;
        private List<LegacyRecord> records = new List<LegacyRecord>();
        private Dictionary<int, string> pawnTitles = new Dictionary<int, string>();
        private List<LegacyRelationshipState> relationshipStates = new List<LegacyRelationshipState>();
        private LegacyRecordRepository repository;

        public LegacyWorldComponent(World world) : base(world)
        {
        }

        public LegacyRecordRepository Repository
        {
            get
            {
                if (repository == null)
                {
                    repository = new LegacyRecordRepository(records);
                }

                return repository;
            }
        }

        public string GetPawnTitle(int pawnId)
        {
            string title;
            return pawnTitles != null && pawnTitles.TryGetValue(pawnId, out title) ? title : string.Empty;
        }

        public void SetPawnTitle(int pawnId, string title)
        {
            if (pawnTitles == null)
            {
                pawnTitles = new Dictionary<int, string>();
            }

            if (string.IsNullOrEmpty(title))
            {
                pawnTitles.Remove(pawnId);
                return;
            }

            pawnTitles[pawnId] = title;
        }

        public List<LegacyRelationshipState> RelationshipStates
        {
            get { return relationshipStates; }
        }

        public LegacyRelationshipState GetRelationshipState(int subjectPawnId, int otherPawnId)
        {
            if (relationshipStates == null)
            {
                relationshipStates = new List<LegacyRelationshipState>();
            }

            foreach (LegacyRelationshipState state in relationshipStates)
            {
                if (state.subjectPawnId == subjectPawnId && state.otherPawnId == otherPawnId)
                {
                    return state;
                }
            }

            return null;
        }

        public LegacyRelationshipState GetOrCreateRelationshipState(int subjectPawnId, int otherPawnId)
        {
            LegacyRelationshipState state = GetRelationshipState(subjectPawnId, otherPawnId);
            if (state != null)
            {
                return state;
            }

            state = new LegacyRelationshipState
            {
                subjectPawnId = subjectPawnId,
                otherPawnId = otherPawnId
            };
            relationshipStates.Add(state);
            return state;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref dataVersion, "dataVersion", 1);
            Scribe_Collections.Look(ref records, "records", LookMode.Deep);
            Scribe_Collections.Look(ref pawnTitles, "pawnTitles", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref relationshipStates, "relationshipStates", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (records == null)
                {
                    records = new List<LegacyRecord>();
                }

                if (pawnTitles == null)
                {
                    pawnTitles = new Dictionary<int, string>();
                }

                if (relationshipStates == null)
                {
                    relationshipStates = new List<LegacyRelationshipState>();
                }

                RebuildRuntimeState();
            }
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();

            int interval = LegacyMod.Settings != null ? LegacyMod.Settings.updateIntervalTicks : 2500;
            if (interval < 250)
            {
                interval = 250;
            }

            if (Find.TickManager == null || Find.TickManager.TicksGame % interval != 0)
            {
                return;
            }

            LegacyThoughtImpactScanner.ScanVisiblePawnThoughts();
            LegacyRelationshipService.EvaluateRelationships(this);

            if (LegacyMod.Settings != null && LegacyMod.Settings.debugMode && LegacyMod.Settings.debugUseHardTimerConsequences)
            {
                LegacyConsequenceService.EvaluateConsequences(this);
            }
        }

        private void RebuildRuntimeState()
        {
            repository = new LegacyRecordRepository(records);
        }
    }
}
