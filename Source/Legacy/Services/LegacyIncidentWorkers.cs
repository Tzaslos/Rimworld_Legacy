using Legacy.Domain;
using RimWorld;
using Verse;

namespace Legacy.Services
{
    public abstract class IncidentWorker_LegacyConsequence : IncidentWorker
    {
        protected abstract LegacyRelationshipKind RelationshipKind { get; }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = parms != null ? parms.target as Map : null;
            return map != null && LegacyConsequenceService.CanFireNow(RelationshipKind, map);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms != null ? parms.target as Map : null;
            return map != null && LegacyConsequenceService.TryFireStorytellerConsequence(RelationshipKind, map);
        }
    }

    public class IncidentWorker_LegacyHeroGift : IncidentWorker_LegacyConsequence
    {
        protected override LegacyRelationshipKind RelationshipKind
        {
            get { return LegacyRelationshipKind.Hero; }
        }
    }

    public class IncidentWorker_LegacyNemesisThreat : IncidentWorker_LegacyConsequence
    {
        protected override LegacyRelationshipKind RelationshipKind
        {
            get { return LegacyRelationshipKind.Nemesis; }
        }
    }
}
