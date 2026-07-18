# Legacy Modder API

Legacy exposes a small C# API for other mods that want to record meaningful pawn events or read existing legacy history.

## Reference Legacy

Add `Legacy.dll` as a non-private reference in your mod project:

```xml
<Reference Include="Legacy">
  <HintPath>..\Legacy\Assemblies\Legacy.dll</HintPath>
  <Private>false</Private>
</Reference>
```

Then import:

```csharp
using Legacy.API;
using Legacy.Domain;
```

## Define Your Event

Create a `Legacy.Domain.LegacyEventDef` in your mod's XML:

```xml
<Defs>
  <Legacy.Domain.LegacyEventDef>
    <defName>YourMod_SavedPawnFromFire</defName>
    <label>saved from fire</label>
    <description>A pawn was rescued from a dangerous fire.</description>
    <kind>SavedFromDeath</kind>
    <category>Rescue</category>
    <severity>Major</severity>
    <reputationWeight>1.0</reputationWeight>
    <biographyTags>
      <li>rescue</li>
      <li>fire</li>
    </biographyTags>
  </Legacy.Domain.LegacyEventDef>
</Defs>
```

Use the existing enum values from `Legacy.Domain` for `kind`, `category`, and `severity`.

## Record An Event

Use `LegacyApi.TryRecordEvent` after a world exists. The API returns `false` when Legacy is unavailable, the event def cannot be found, a pawn is not eligible, or Legacy deduplicates the event.

```csharp
LegacyRecord record;
bool recorded = LegacyApi.TryRecordEvent(
    "YourMod_SavedPawnFromFire",
    rescuedPawn,
    rescuerPawn,
    LegacyParticipantRole.Rescuer,
    "Your mod rescue logic",
    rescuedPawn.LabelShort + " was carried out of the fire.",
    out record);
```

For richer integrations, use a request object:

```csharp
LegacyRecord record;
LegacyApiRecordRequest request = new LegacyApiRecordRequest
{
    eventDefName = "YourMod_SavedPawnFromFire",
    subject = rescuedPawn,
    cause = "Your mod rescue logic",
    extraDescription = "Rescued during a building fire.",
    label = "Saved from fire",
    description = rescuedPawn.LabelShort + " survived because someone intervened.",
    polarity = LegacyImpactPolarity.Positive,
    sourceType = LegacyImpactSourceType.PersistentSituation
};

request.WithParticipant(rescuerPawn, LegacyParticipantRole.Rescuer);

bool recorded = LegacyApi.TryRecordEvent(request, out record);
```

## Query Records

```csharp
IReadOnlyList<LegacyRecord> pawnHistory = LegacyApi.RecordsForPawn(pawn);
IReadOnlyList<LegacyRecord> rescues = LegacyApi.RecordsOfKind(LegacyEventKind.SavedFromDeath);
IReadOnlyList<LegacyRecord> allRecords = LegacyApi.AllRecords();
```

These methods return empty lists when Legacy is not available.

## Compatibility Notes

- Check `LegacyApi.IsAvailable` before showing Legacy-specific UI.
- Prefer XML `LegacyEventDef` records over hard-coded labels so other systems can categorize your events.
- Always provide the subject pawn. Add participants for other pawns involved in the event.
- Do not persist `LegacyRecord` references in your own save data; store your own identifiers and query Legacy when needed.
