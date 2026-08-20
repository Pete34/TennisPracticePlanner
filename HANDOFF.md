# Tennis Practice Planner Handoff

## Current Status

- Project is scaffolded as .NET 8 Blazor WebAssembly.
- Core app features are implemented and building successfully.
- Build verified with: dotnet build (from TennisPracticePlanner folder).
- Planning is complete for a Version 2 feature (Firebase Auth + cloud data). Implementation is done and manually tested end-to-end (sign-in, cloud sync, sharing, tag filter, search) as of 2026-08-20. See "Planned Feature: Firebase Auth and Cloud Sync" section below for details and remaining follow-ups.

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

Status: Implemented and manually tested end-to-end (2026-08-20). Firebase project: `tennis-practice-planner`.

Full requirements are documented in TennisPracticeAppTechnicalSpecification.md, section 17. Summary:

- Optional Google Sign-In (Firebase Auth). Not signed in = Guest Mode, unchanged local-storage behavior.
- Access restricted to an allow-list of emails stored in a Firestore doc (`config/allowlist`), editable from the Firebase console. Starts with just the app owner's email.
- Signed-in data model: private per-user Firestore collections `users/{uid}/instructions`, `users/{uid}/templates`. (Sessions Library/Detail pages are a read-only view over templates; no separate sessions collection.)
- Templates are always private, never shared.
- Reusable Instructions can optionally be shared (`IsShared` flag + fixed `Tag`: Serve, Return, Volley, Footwork, Forehand, One-Handed Backhand, Doubles, Fitness). Shared instructions are browsable/filterable by other allow-listed users, with a "Copy to my library" action (no live cross-user references).
- No auto-migration of existing local-storage/guest data into the cloud.

### Key files

- TennisPracticePlanner/wwwroot/js/firebase-interop.js - ES module (dynamic import via IJSRuntime). Wraps Firebase Auth (Google) + Firestore CRUD + collectionGroup shared-instruction query. Firebase SDK v12.18.0 via CDN.
- TennisPracticePlanner/Services/IAuthService.cs, AuthService.cs - sign-in/out, CurrentUser, IsAllowed (checked against config/allowlist), AuthStateChanged event, LastErrorMessage for graceful failure display.
- TennisPracticePlanner/Services/CloudTennisPracticeDataService.cs - Firestore-backed data service (one doc per instruction/template under users/{uid}/...). Also exposes GetSharedInstructionsAsync/CopySharedInstructionToLibraryAsync (cloud-only, not on ITennisPracticeDataService).
- TennisPracticePlanner/Services/CompositeTennisPracticeDataService.cs - switches between guest (local storage) and cloud based on IsCloudActive = IsSignedIn && IsAllowed==true.
- TennisPracticePlanner/Pages/SharedInstructions.razor - route /shared-instructions, tag+search filter, copy-to-library.
- TennisPracticePlanner/Layout/NavMenu.razor - sign-in/out button, auth status, Shared Instructions nav link.
- TennisPracticePlanner/Pages/Instructions.razor - Tag dropdown + IsShared checkbox, shown only when cloud-active.
- firestore.rules (repo root) - deployed. Enforces allow-list, private per-user access, and a `match /{path=**}/instructions/{id}` collection-group rule for shared reads.
- Models/AppUser.cs, Models/InstructionTag.cs, ReusableInstruction.cs extended with IsShared/Tag/OwnerUid/OwnerDisplayName/SharedAtUtc.

### Bugs found and fixed during manual testing

1. Auth-state race condition: pages checked `IsCloudActive` once in `OnInitializedAsync`, before the async Firebase session restore completed, so signed-in users briefly saw "Guest Mode"/"sign in" UI. Fixed by subscribing to `AuthService.AuthStateChanged` in `Instructions.razor` and `SharedInstructions.razor` and reloading/re-rendering on change.
2. `AddReusableInstructionAsync` (create path) didn't set `OwnerUid`/`OwnerDisplayName`/`SharedAtUtc` when creating a new shared instruction - only the update path did. Fixed by extracting a shared `ApplySharingMetadata` helper used by both Add and Update.
3. The Tag `<select>` visually defaults to its first option ("Serve") but Blazor's bound nullable enum value stays `null` unless the user changes the dropdown, silently saving no tag. Fixed by defaulting `InstructionForm.Tag = InstructionTag.Serve` on form init/reset.
4. Tag filter on Shared Instructions always returned 0 results: `InstructionTag` was serialized as a number by default `System.Text.Json` behavior when written to Firestore, but the query compared against `tag.ToString()` (a string). Fixed by adding `JsonStringEnumConverter()` to `CloudTennisPracticeDataService`'s `JsonSerializerOptions`. Any instruction docs written before this fix have a numeric `tag` field and must be re-saved (edit + save) to pick up the corrected string value.
5. Unhandled sign-in/out JS exceptions (e.g. Google provider not yet enabled, popup closed by user) crashed the whole Blazor render tree with the full-page "unhandled error" banner. Fixed by catching `JSException` in `AuthService` and surfacing a friendly `LastErrorMessage` in the NavMenu instead.

### Firestore setup notes for future reference

- Firestore database created in production mode (not test mode) since `firestore.rules` already existed.
- Default database (`(default)`), no need for named databases.
- The `collectionGroup` query for shared instructions required two composite/collection-group indexes to be created manually in the Firebase Console (Firestore > Indexes > Composite tab, Collection group = `instructions`): one on `isShared` alone, one on `isShared` + `tag`. New index creation can occasionally fail with a transient "unknown error" - delete and recreate if that happens.

### Remaining/optional follow-ups

- Deploying firestore.rules changes in the future should go through Firebase Console > Firestore > Rules (already done once for the current ruleset).
- Consider applying the same AuthStateChanged-subscription pattern to Templates/Sessions pages if stale guest/cloud data on sign-in transition is ever reported there (not yet observed/tested).
