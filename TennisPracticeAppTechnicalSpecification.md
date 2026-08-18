# Tennis Practice Planner Technical Specification

## 1. Overview

Tennis Practice Planner is a personal-use web application for creating, saving, and reusing tennis practice sessions.

The application will allow a user to build reusable instructions, group those instructions into practice templates, and view the total duration of each practice session.

The application will be built with .NET 8 using Blazor WebAssembly and will store all user data in browser local storage.

## 2. Goals

- Provide a fast and simple way to create tennis practice plans.
- Allow reusable instructions to be saved once and used across many practice sessions.
- Allow one-off instructions to be added directly to a practice session without first saving them to the reusable library.
- Show the total practice time for each session automatically.
- Support mobile-friendly usage on iPhone 13 Pro in Safari.
- Be deployable on Vercel.

## 3. Non-Goals

- No user accounts or authentication.
- No cloud database or server-side persistence.
- No multi-user collaboration.
- No offline sync across devices.
- No native mobile app in the first version.

## 4. Platform and Technology

- Framework: .NET 8
- UI Technology: Blazor WebAssembly
- Hosting Target: Vercel
- Data Storage: Browser local storage only
- Browser Target: Safari on iPhone 13 Pro, plus modern desktop browsers

## 5. Core Concepts

### 5.1 Reusable Instruction

A reusable instruction is a saved drill or activity that can be selected and added to a practice session later.

Examples:

- Hit cross-court backhands
- Serve wide from deuce court
- Forehand approach and volley pattern

### 5.2 Practice Session Template

A practice session template is a named collection of instructions arranged in a specific order.

Each template represents a full tennis practice plan.

### 5.3 One-Off Instruction

A one-off instruction is an instruction created directly inside a practice session template without adding it to the reusable instruction library.

## 6. Functional Requirements

### 6.1 Instruction Library

The application must allow the user to:

- Create a reusable instruction.
- Edit a reusable instruction.
- Delete a reusable instruction.
- View all saved reusable instructions.
- Select a saved reusable instruction from a list when building a practice template.

Each reusable instruction must include:

- Short description text, required.
- Duration in minutes, required.
- Ball count, optional.
- YouTube URL, optional.
- YouTube start time, optional.

### 6.2 Practice Templates

The application must allow the user to:

- Create a practice template.
- Edit a practice template title.
- Delete a practice template.
- View a list of all saved practice templates.
- Support creating more than 5 templates with no hard limit in the application.

Each practice template must include:

- Template title, required.
- Ordered list of instructions.
- Computed total duration.

### 6.3 Adding Instructions to a Template

When editing a template, the application must allow the user to:

- Add a saved reusable instruction from a selectable list.
- Add a new one-off instruction directly to the template.
- Remove an instruction from the template.
- Edit an instruction already inside the template.
- Reorder instructions inside the template.
- When a reusable instruction is added to a template, the template stores its own editable copy of that instruction.
- Editing an instruction inside a template affects only that template entry unless the user is explicitly editing the reusable library version.
- Editing an instruction inside a template does not change the reusable instruction in the instruction library.

### 6.4 Duration Calculation

The application must:

- Sum the duration in minutes for all instructions in a template.
- Display the total duration clearly on the template editor screen.
- Display the total duration when viewing saved templates.

Ball count is informational only and must not affect duration totals.

### 6.5 YouTube Link Behavior

If a YouTube URL is supplied for an instruction, the application must:

- Display a link or button to open the video.
- Open the video in a new browser tab.
- Support an optional start time value.

The start time field should:

- Accept the `mm:ss` format. For example `01:30`. Single Digit numbers are allowed for minutes.
- Be converted into a valid YouTube URL timestamp when the link is opened.

### 6.6 Local Storage Persistence

The application must save all user-created data in browser local storage, including:

- Reusable instructions
- Practice templates
- Template instruction ordering

Saved data must remain available after the browser tab is closed and reopened on the same device and browser.

## 7. Data Model

### 7.1 ReusableInstruction

Suggested fields:

- `Id: string`
- `TitleOrDescription: string`
- `DurationMinutes: int`
- `BallCount: int?`
- `YoutubeUrl: string?`
- `YoutubeStartTime: string?`
- `CreatedUtc: DateTime`
- `UpdatedUtc: DateTime`

### 7.2 PracticeTemplate

Suggested fields:

- `Id: string`
- `Title: string`
- `InstructionItems: List<TemplateInstructionItem>`
- `CreatedUtc: DateTime`
- `UpdatedUtc: DateTime`

### 7.3 TemplateInstructionItem

Suggested fields:

- `Id: string`
- `SourceInstructionId: string?`
- `Description: string`
- `DurationMinutes: int`
- `BallCount: int?`
- `YoutubeUrl: string?`
- `YoutubeStartTime: string?`
- `SortOrder: int`

`SourceInstructionId` is optional so that both reusable instructions and one-off instructions can be supported.
It is reference only and not a live link.
The template may optionally keep the original reusable instruction ID for reference, but it must not depend on it for live updates.

## 8. User Experience Requirements

### 8.1 Main Views

The first version should include these views:

- Dashboard or home page
- Reusable instruction management page
- Practice template list page
- Practice template editor page

### 8.2 Mobile Layout

The interface must be optimized for small screens, especially iPhone 13 Pro width.

The UI should:

- Use touch-friendly buttons and form controls.
- Avoid horizontal scrolling.
- Keep primary actions visible and easy to tap.
- Use responsive layouts that work on both mobile and desktop.

### 8.3 Template Editing Flow

The recommended flow is:

1. User creates a template and enters a title.
2. User adds instructions from the reusable library or creates one-off instructions.
3. User reorders or edits instructions within the template by way of arrows.
4. User sees the total session duration update automatically.
5. User saves the template.

## 9. Validation Rules

The application must enforce the following validation:

- Template title is required.
- Instruction description is required and no title is needed.
- Duration minutes is required and must be greater than 0.
- Ball count, if provided, must be 0 or greater.
- YouTube URL, if provided, must be a valid URL format.
- YouTube start time, if provided, must use the `mm:ss` format.

## 10. Error Handling

The application should:

- Show validation messages near the affected fields.
- Prevent invalid forms from being saved.
- Handle invalid or corrupted local storage data gracefully.
- Fall back to empty default data if stored data cannot be read safely.

## 11. Performance Requirements

Because this is a personal-use local-storage app, performance requirements are lightweight, but the app should:

- Load quickly on mobile Safari.
- Save changes without requiring page refreshes.
- Recalculate template duration immediately after instruction changes.

## 12. Accessibility Requirements

The application should:

- Use semantic HTML where possible.
- Support keyboard navigation on desktop.
- Use clear labels for all form inputs.
- Provide sufficient color contrast.

## 13. Deployment Requirements

The application must:

- Build as a Blazor WebAssembly app.
- Be deployable to Vercel.
- Avoid any dependency on server-only runtime behavior for core features.

## 14. Recommended Nice-to-Have Features

These are not required for the first version but would be useful:

- Export all local data to JSON.
- Import data from JSON.
- Duplicate an existing template.
- Clear all saved data with confirmation.
- Create default sample templates for first-time use.

## 15. Acceptance Criteria for Version 1

Version 1 is complete when:

- The user can create, edit, and delete reusable instructions.
- The user can create, edit, and delete practice templates.
- A template can contain both saved reusable instructions and one-off instructions.
- Each instruction supports required minutes, optional ball count, required description, optional YouTube URL, and optional start time.
- The total practice duration is calculated correctly and shown to the user.
- Practice instructions can be reordered via arrows.
- All data persists in browser local storage.
- The app works well on iPhone 13 Pro Safari.
- The app can be deployed to Vercel.

## 16. Suggested Build Direction

The first implementation should prioritize:

1. Data models
2. Local storage service
3. Instruction library CRUD screens
4. Template CRUD screens
5. Template editor with duration calculation
6. Mobile styling and responsive layout
7. Vercel deployment configuration.
