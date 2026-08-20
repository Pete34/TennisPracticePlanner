# Tennis Practice Planner Handoff

## Current Status

- Project is scaffolded as .NET 8 Blazor WebAssembly.
- Core app features are implemented and building successfully.
- Build verified with: dotnet build (from TennisPracticePlanner folder).
- Planning is complete for a Version 2 feature (Firebase Auth + cloud data). Implementation has not started yet. See "Planned Feature: Firebase Auth and Cloud Sync" section below before starting that work.

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

## Planned Feature: Firebase Auth and Cloud Sync

Full requirements are documented in TennisPracticeAppTechnicalSpecification.md, section 17. Summary:

- Optional Google Sign-In (Firebase Auth). Not signed in = Guest Mode, unchanged local-storage behavior.
- Access restricted to an allow-list of emails stored in a Firestore doc (`config/allowlist`), editable from the Firebase console. Starts with just the app owner's email.
- Signed-in data model: private per-user Firestore collections `users/{uid}/instructions`, `users/{uid}/templates`, `users/{uid}/sessions`.
- Templates and Sessions are always private, never shared.
- Reusable Instructions can optionally be shared (`IsShared` flag + fixed `Tag`: Serve, Return, Volley, Footwork, Forehand, One-Handed Backhand, Doubles, Fitness). Shared instructions are browsable/filterable by other allow-listed users, with a "Copy to my library" action (no live cross-user references).
- No auto-migration of existing local-storage/guest data into the cloud.

### Blocking prerequisite before implementation starts

The user must complete these steps in the Firebase Console (cannot be done by the assistant):

1. Enable Google as a Sign-in provider under Authentication.
2. Create a Firestore Database.
3. Add a Web App under Project Settings and obtain the `firebaseConfig` values (apiKey, authDomain, projectId, storageBucket, messagingSenderId, appId).
4. Manually create a Firestore document at `config/allowlist` with an `emails` array containing the owner's email.

### Implementation steps once config is provided

1. Add Firebase JS SDK + Blazor JS interop wiring.
2. Build `IAuthService`/`FirebaseAuthService` (Google sign-in/out, current user state).
3. Extend the data service layer with a Firestore-backed implementation alongside the existing local-storage one, switching based on auth state.
4. Add sign-in UI.
5. Add shared-instruction browsing/filtering UI (tag, owner, search, pagination) and "Copy to my library".
6. Write Firestore security rules enforcing the allow-list and private/shared model (see spec section 17.4).
