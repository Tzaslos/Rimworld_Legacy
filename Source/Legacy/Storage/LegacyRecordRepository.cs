using System.Collections.Generic;
using System.Linq;
using Legacy.Domain;

namespace Legacy.Storage
{
    public class LegacyRecordRepository
    {
        private readonly List<LegacyRecord> records;
        private readonly Dictionary<int, List<LegacyRecord>> recordsByPawnId = new Dictionary<int, List<LegacyRecord>>();
        private readonly Dictionary<LegacyEventKind, List<LegacyRecord>> recordsByKind = new Dictionary<LegacyEventKind, List<LegacyRecord>>();

        public LegacyRecordRepository(List<LegacyRecord> records)
        {
            this.records = records;
            RebuildIndexes();
        }

        public void Add(LegacyRecord record)
        {
            records.Add(record);
            Index(record);
        }

        public IReadOnlyList<LegacyRecord> AllRecords()
        {
            return records;
        }

        public IReadOnlyList<LegacyRecord> RecordsForPawn(int thingIdNumber)
        {
            List<LegacyRecord> result;
            return recordsByPawnId.TryGetValue(thingIdNumber, out result) ? result : Empty();
        }

        public IReadOnlyList<LegacyRecord> RecordsOfKind(LegacyEventKind kind)
        {
            List<LegacyRecord> result;
            return recordsByKind.TryGetValue(kind, out result) ? result : Empty();
        }

        public void RebuildIndexes()
        {
            recordsByPawnId.Clear();
            recordsByKind.Clear();

            foreach (LegacyRecord record in records.Where(record => record != null))
            {
                Index(record);
            }
        }

        private void Index(LegacyRecord record)
        {
            if (record.subject != null)
            {
                AddToPawnIndex(record.subject.thingIdNumber, record);
            }

            if (record.participants != null)
            {
                foreach (LegacyParticipant participant in record.participants)
                {
                    if (participant != null && participant.pawn != null)
                    {
                        AddToPawnIndex(participant.pawn.thingIdNumber, record);
                    }
                }
            }

            if (record.eventDef != null)
            {
                List<LegacyRecord> byKind;
                if (!recordsByKind.TryGetValue(record.eventDef.kind, out byKind))
                {
                    byKind = new List<LegacyRecord>();
                    recordsByKind[record.eventDef.kind] = byKind;
                }

                byKind.Add(record);
            }
        }

        private void AddToPawnIndex(int thingIdNumber, LegacyRecord record)
        {
            if (thingIdNumber < 0)
            {
                return;
            }

            List<LegacyRecord> byPawn;
            if (!recordsByPawnId.TryGetValue(thingIdNumber, out byPawn))
            {
                byPawn = new List<LegacyRecord>();
                recordsByPawnId[thingIdNumber] = byPawn;
            }

            byPawn.Add(record);
        }

        private static IReadOnlyList<LegacyRecord> Empty()
        {
            return new List<LegacyRecord>();
        }
    }
}
