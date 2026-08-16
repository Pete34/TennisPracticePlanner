# Tennis Practice Planner Handoff

## Current Status

- Project is scaffolded as .NET 8 Blazor WebAssembly.
- Core app features are implemented and building successfully.
- Build verified with: dotnet build (from TennisPracticePlanner folder).

## Workspace Structure

- Root spec and planning docs:
  - TennisPracticeAppInstructions.txt
  - TennisPracticeAppTechnicalSpecification.md
- App project:
  - TennisPracticePlanner/

## Implemented Features

### 1) Reusable Instruction Library

- Create, edit, delete reusable instructions.
- Fields supported:
  - Description (required)
  - DurationMinutes (required)
  - BallCount (optional)
  - YoutubeUrl (optional)
  - YoutubeStartTime in mm:ss (optional)

Main file:
- TennisPracticePlanner/Pages/Instructions.razor

### 2) Practice Templates

- Create, list, and delete templates.
- Each template can be opened in editor.
- Template card shows instruction count and total duration.

Main file:
- TennisPracticePlanner/Pages/Templates.razor

### 3) Template Editor

- Route: /templates/{TemplateId}
- Update template title.
- Add instruction copy from reusable library.
- Add one-off instruction.
- Edit template instructions in place.
- Delete instructions.
- Reorder instructions with Move Up / Move Down arrows.
- Live total duration display.

Main file:
- TennisPracticePlanner/Pages/TemplateEditor.razor

### 4) Session Library and Clean Session Detail

- Session library route: /sessions
- Session detail route: /sessions/{SessionId}
- Session detail is rendered with a dedicated no-sidebar layout.
- Detail view shows:
  - Session title
  - Instruction description list
  - Duration in minutes per instruction
  - Ball count per instruction when provided
  - YouTube link when provided
  - Total session duration

Main files:
- TennisPracticePlanner/Pages/Sessions.razor
- TennisPracticePlanner/Pages/SessionDetail.razor
- TennisPracticePlanner/Layout/SessionDetailLayout.razor
- TennisPracticePlanner/Layout/SessionDetailLayout.razor.css

## Data and Persistence

- All app data is stored in browser local storage.
- Storage key:
  - tennis-practice-planner-data-v1
- Includes starter sample data on first run.
- Corrupt local storage recovery falls back safely.

Main files:
- TennisPracticePlanner/Services/ITennisPracticeDataService.cs
- TennisPracticePlanner/Services/TennisPracticeDataService.cs
- TennisPracticePlanner/wwwroot/js/storage.js

## Data Model Files

- TennisPracticePlanner/Models/ReusableInstruction.cs
- TennisPracticePlanner/Models/TemplateInstructionItem.cs
- TennisPracticePlanner/Models/PracticeTemplate.cs
- TennisPracticePlanner/Models/AppDataStore.cs

## Navigation and Layout

- Sidebar nav includes:
  - Home
  - Instructions
  - Templates
  - Sessions

Main files:
- TennisPracticePlanner/Layout/NavMenu.razor
- TennisPracticePlanner/Layout/MainLayout.razor

## Styling

- Custom responsive app styling and clean session styling in:
  - TennisPracticePlanner/wwwroot/css/app.css

## Deployment

- Vercel config exists in:
  - TennisPracticePlanner/vercel.json

## Commands

From TennisPracticePlanner folder:

- Build:
  - dotnet build
- Run locally:
  - dotnet run

## Notes For Next Session

- If dotnet run fails while build succeeds, check:
  - Port conflicts
  - Existing running process
  - First error line in terminal output
- The repository was initialized locally at root, so .git exists in workspace root.
- Specification source of truth is TennisPracticeAppTechnicalSpecification.md.

## Good Next Enhancements

1. Add JSON export/import UI for backups.
2. Add explicit confirmations for destructive deletes.
3. Add optional print-friendly session detail page layout.
4. Add lightweight integration tests for local storage service behavior.
