# Legacy

Legacy is a RimWorld storytelling framework for pawn life events with lasting consequences.

Instead of replacing RimWorld's mood, memory, opinion, or relationship systems, Legacy watches meaningful events and preserves them as long-term history. Those records can later shape reputation, relationship states, debug-visible pawn histories, and optional consequence events.

## Features

- Persistent pawn legacy records for defining moments.
- Relationship history screens for recorded pawn interactions.
- Hero and nemesis states based on accumulated Legacy impact.
- Optional consequence events for major legacy relationships.
- Debug tools for reviewing records and assigning reputational titles.
- Deduplication and pawn eligibility checks to avoid repeated records and dead-pawn noise.
- A public C# API for other modders to record and query Legacy events.

## Recorded Event Areas

Legacy currently tracks or supports records for events such as:

- Thought impacts
- Captivity and prisoner release
- Violence and combat attribution
- Surgery and medical harm/help
- Slavery and rescue events
- Relationship-changing moments
- Colony-level story events
- Optional modded magic events

## Optional Integrations

Legacy is designed to work without hard dependencies on optional mods.

- **Karma & Reputation** can inform Legacy relationship states when enabled.
- **Psychology**, vanilla traits, Ideology, and compatible trait mods can shape how pawns interpret events.
- **A RimWorld of Magic** is detected automatically when loaded. Pawns can remember witnessed magic, blood magic, and necromancy, including labels such as `Blood mage` and `Necromancer`.

These integrations are optional. Legacy should load and run with Harmony alone.

## Requirements

- RimWorld `1.6`
- Harmony

## Installation

For players:

1. Install Harmony.
2. Place this mod folder in your RimWorld `Mods` directory, or subscribe through the distribution channel where you found it.
3. Enable `Legacy` in the RimWorld mod list.
4. Load it after optional integration mods when possible.

For local development, the compiled assembly is expected at:

```text
Assemblies/Legacy.dll
```

## Developer Notes

The source project lives in:

```text
Source/Legacy/Legacy.csproj
```

The project targets .NET Framework `4.7.2`, matching RimWorld modding conventions.

A typical local build command is:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe' Source\Legacy\Legacy.csproj /p:Configuration=Debug
```

You will need Visual Studio or Build Tools with:

- MSBuild
- C# and Visual Basic Roslyn compilers
- .NET Framework 4.7.2 SDK
- .NET Framework 4.7.2 targeting pack

## Modder API

Other mods can integrate with Legacy through the public C# API in the `Legacy.API` namespace.

Start here:

- [Modder API documentation](Docs/ModderAPI.md)
- [Example integration](Examples/LegacyApiExample.cs)

The short version:

```csharp
using Legacy.API;
using Legacy.Domain;

LegacyRecord record;
bool recorded = LegacyApi.TryRecordEvent(
    "YourMod_SavedPawnFromFire",
    rescuedPawn,
    rescuerPawn,
    LegacyParticipantRole.Rescuer,
    "Your mod rescue logic",
    rescuedPawn.LabelShort + " was carried out of danger.",
    out record);
```

Prefer defining your own `Legacy.Domain.LegacyEventDef` XML records so Legacy and other integrations can categorize your events cleanly.

## Repository Layout

```text
About/              RimWorld mod metadata
Assemblies/         Compiled mod assembly
Defs/               RimWorld XML defs
Docs/               Developer and API documentation
Examples/           Integration examples
Languages/          Translation/keyed text files
Source/Legacy/      C# source project
```

## Compatibility Philosophy

Legacy favors optional, reflection-based integrations for external mods. Optional patches should:

- Never require another mod's assembly at compile time unless that dependency is intentional.
- Check whether the target mod is active before patching.
- Fail quietly or log a single useful warning when an expected integration point is missing.
- Record normal Legacy events rather than bypassing the repository and scoring pipeline.

## License

Legacy is licensed under the MIT License.

See `LICENSE` if this repository includes the full license text.
