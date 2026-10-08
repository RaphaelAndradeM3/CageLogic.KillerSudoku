---
description: "Task list for implementing the Killer Sudoku game session"
---

# Tasks: Sessão de Jogo Killer Sudoku

**Input**: Design documents from `specs/004-game-session/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/game-session.md`, `quickstart.md`

**Tests**: Included because the feature spec defines independent test criteria and the project guardrails require Release build/test gates. Write NUnit tests before the corresponding Application implementation; perform MAUI interaction/accessibility acceptance manually on both targets.

**Organization**: Tasks are grouped by the two P1 user stories. The second story depends on a session created by the first in the host; its Application behavior remains independently testable from a deterministic session fixture.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel with other marked tasks because the files differ and prerequisites are complete.
- **[Story]**: User story served by the task (`US1` or `US2`).
- Every task names the file path(s) to create or change.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add the MAUI host and logging boundary to the existing .NET 10 solution.

- [X] T001 [P] Create the .NET MAUI host project targeting `net10.0-android` and `net10.0-windows10.0.19041.0`, with GraphicsView, MVVM, DI and logging dependencies in `src/CageLogic.Maui/CageLogic.Maui.csproj` and the application entry point in `src/CageLogic.Maui/App.xaml` and `src/CageLogic.Maui/App.xaml.cs`.
- [X] T002 [P] Create the .NET 10 logging Infrastructure project and its Serilog/`Microsoft.Extensions.Logging` dependencies in `src/CageLogic.Infrastructure/CageLogic.Infrastructure.csproj`.
- [X] T003 Create the NUnit 5 logging test project with `Microsoft.NET.Test.Sdk` and `NUnit.Analyzers`, referencing Infrastructure in `tests/CageLogic.Infrastructure.Tests/CageLogic.Infrastructure.Tests.csproj`.
- [X] T004 Add `CageLogic.Maui`, `CageLogic.Infrastructure`, and `CageLogic.Infrastructure.Tests` to `CageLogic.slnx`; reference Application and Infrastructure from MAUI, and reference Infrastructure only from its tests.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Complete safe logging and host exception handling before either story relies on the host.

- [ ] T005 Add NUnit tests for separate daily message/error logs, timestamp/offset and correlation fields, configurable retention, sensitive-data exclusion, and logger fallback in `tests/CageLogic.Infrastructure.Tests/Logging/DailyFileLoggingTests.cs`.
- [ ] T006 Implement configurable daily Serilog sinks for separate messages/errors under app-data `logs` by default, timestamp/offset, structured context/correlation, retention/timezone and safe fallback through `Microsoft.Extensions.Logging` in `src/CageLogic.Infrastructure/Logging/SerilogLoggingConfiguration.cs` and `src/CageLogic.Infrastructure/Logging/LoggingOptions.cs`.
- [ ] T007 Register the logger in the real MAUI composition root and handle unexpected/unobserved host exceptions once, with correlation and a generic player-safe message, in `src/CageLogic.Maui/MauiProgram.cs` and `src/CageLogic.Maui/App.xaml.cs`.

**Checkpoint**: Host and Infrastructure projects are in the solution; unexpected failures have a safe host boundary and local structured logging.

---

## Phase 3: User Story 1 - Jogar em desktop ou dispositivo móvel (Priority: P1) 🎯 MVP

**Goal**: Start a generated puzzle, render the 9×9 board/cages and let the player select and edit cells using the platform controls.

**Independent Test**: On Windows and Android, start a puzzle, inspect values/cages/targets, select fixed and editable cells, enter a response, and observe rule conflicts. Traverse all 81 cells and game controls with keyboard, TalkBack and Narrator; verify the documented semantic state and key mapping.

### Tests for User Story 1

- [ ] T008 [P] [US1] Add NUnit coverage for creating a session from `GeneratedPuzzle`, initial board/cage projection, selecting fixed/editable cells and applying a conflicting answer in `tests/CageLogic.Application.Tests/GameSessions/GameSessionBoardTests.cs` (FR-001–FR-004, FR-007; SC-001, SC-007).

### Implementation for User Story 1

- [ ] T009 [US1] Implement the Application session initialized from `GeneratedPuzzle`, immutable board projection, selected position and answer entry through existing move/validation use cases in `src/CageLogic.Application/GameSessions/GameSession.cs` and `src/CageLogic.Application/GameSessions/GameSessionViewState.cs` (FR-001, FR-003–FR-004, FR-007).
- [ ] T010 [P] [US1] Implement difficulty selection, asynchronous single-flight puzzle generation, visible loading, player cancellation, explicit unavailable/cancelled state, retry, session creation and navigation in `src/CageLogic.Maui/ViewModels/HomeViewModel.cs`, `src/CageLogic.Maui/Views/HomePage.xaml`, and `src/CageLogic.Maui/AppShell.xaml`; register `GeneratePuzzleUseCase`, `ApplyMoveUseCase`, `GetCandidatesUseCase`, and `ValidateBoardUseCase` in `src/CageLogic.Maui/MauiProgram.cs` (FR-001, FR-011; SC-001, SC-012).
- [ ] T011 [P] [US1] Implement shared board geometry, resize-aware hit testing and `GraphicsView`/`IDrawable` rendering for the 9×9 grid, cage boundaries and targets in `src/CageLogic.Maui/Controls/BoardGeometry.cs` and `src/CageLogic.Maui/Controls/KillerSudokuBoardDrawable.cs` (FR-002; SC-001).
- [ ] T012 [US1] Bind the read-only session projection to the game page, including givens, player values, cages, targets, selection and rule-conflict highlights in `src/CageLogic.Maui/ViewModels/GamePageViewModel.cs`, `src/CageLogic.Maui/Views/GamePage.xaml`, and `src/CageLogic.Maui/Controls/KillerSudokuBoardView.cs` (FR-002–FR-004, FR-007).
- [ ] T013 [US1] Map touch, mouse, arrow navigation and 1–9 answer input to the same session intents; keep fixed cells selectable but read-only and keep arrow navigation on the board at its edges in `src/CageLogic.Maui/Controls/BoardInputBehavior.cs` and `src/CageLogic.Maui/ViewModels/GamePageViewModel.cs` (FR-003–FR-004, FR-012; SC-004).
- [ ] T014 [US1] Expose every board cell and game control semantically to TalkBack and Narrator, announcing row/column, value or empty, fixed/editable state, notes, selection, conflict and hint highlight without relying only on color in `src/CageLogic.Maui/Controls/KillerSudokuBoardView.cs` and `src/CageLogic.Maui/Controls/AccessibleCellPeer.cs` (FR-012; SC-011).

**Checkpoint**: The player can start and enter a basic answer on both targets; visual and accessible cell state agrees with the Application projection.

---

## Phase 4: User Story 2 - Controlar e concluir uma partida (Priority: P1)

**Goal**: Edit candidate notes, undo/redo, pause and resume active time, receive revision-safe hints, and finish with the defined summary counts.

**Independent Test**: From a deterministic session fixture, edit answers/notes, exercise conflict and note-clearing rules, undo/redo manual and automatic candidates, pause across lifecycle transitions, change the board while a hint is pending, and complete both incorrect and correct boards. On Windows and Android, also confirm navigation/input remain responsive during move validation and full-board automatic-candidate calculation. Confirm summary counts match FR-010.

### Tests for User Story 2

- [ ] T015 [P] [US2] Add NUnit tests for manual candidates, note clearing on answer entry, Undo restoring entry notes, later answer deletion leaving notes empty, and conflict count semantics in `tests/CageLogic.Application.Tests/GameSessions/GameSessionContentHistoryTests.cs` (FR-004–FR-008; SC-002, SC-007, SC-010).
- [ ] T016 [P] [US2] Add NUnit tests for replacing all empty-cell notes with automatic candidates and undo/redo as one transaction in `tests/CageLogic.Application.Tests/GameSessions/GameSessionAutoFillTests.cs` (FR-006, FR-008; SC-002, SC-008).
- [ ] T017 [P] [US2] Add deterministic `TimeProvider` tests for manual/background pause, idempotent lifecycle events, explicit resume and excluded background duration in `tests/CageLogic.Application.Tests/GameSessions/GameSessionTimerTests.cs` (FR-009; SC-006).
- [ ] T018 [P] [US2] Add delayed-result NUnit tests for stale hint discard after a board-value edit, no invalidation after candidate-only edits, cancellation, restart at `Explanation`, current-revision `NoSafeHint` and hint-count semantics in `tests/CageLogic.Application.Tests/GameSessions/GameSessionHintTests.cs` (FR-013; SC-005, SC-010).
- [ ] T019 [P] [US2] Add NUnit tests for incomplete/conflicting/wrong-solution completion rejection, successful completion, and exact error/hint totals in `tests/CageLogic.Application.Tests/GameSessions/GameSessionCompletionTests.cs` (FR-007, FR-010; SC-003, SC-009–SC-010).

### Implementation for User Story 2

- [ ] T020 [US2] Implement immutable candidate-note snapshots and before/after history transactions for board plus notes in `src/CageLogic.Application/GameSessions/CandidateNotes.cs` and `src/CageLogic.Application/GameSessions/SessionHistory.cs` (FR-004, FR-006, FR-008; SC-002, SC-008).
- [ ] T021 [US2] Implement answer/candidate editing, rule-conflict projection, one-error-per-conflicting-entry counting unaffected by correction/Undo/Redo, and clearing notes atomically on answer entry in `src/CageLogic.Application/GameSessions/GameSession.cs` (FR-004–FR-005, FR-007–FR-008, FR-010; SC-007, SC-010).
- [ ] T022 [US2] Implement automatic candidate calculation and replacement for every empty editable cell as one reversible history action, using the existing candidate rules, in `src/CageLogic.Application/GameSessions/GameSession.cs` and `src/CageLogic.Application/GameSessions/CandidateNotes.cs` (FR-006, FR-008, FR-011; SC-008).
- [ ] T023 [US2] Connect Undo/Redo to board-and-note snapshots, clear Redo after a new edit, keep selection/mode/pause/time outside history, and issue a new monotonic `BoardRevision` whenever restored values change in `src/CageLogic.Application/GameSessions/SessionHistory.cs` and `src/CageLogic.Application/GameSessions/GameSession.cs` (FR-008, FR-013; SC-002, SC-005).
- [ ] T024 [US2] Implement active-time accumulation with `TimeProvider`, idempotent `Window.Deactivated`/`Window.Stopped` pause handling, and explicit-only resume after `Window.Resumed` in `src/CageLogic.Application/GameSessions/ActiveGameTimer.cs` and `src/CageLogic.Maui/Lifecycle/GameSessionLifecycleBehavior.cs` (FR-009; SC-006).
- [ ] T025 [US2] Implement asynchronous single-flight hint progression through `GetHintUseCase`, cancel/restart only on board-value revision change, suppress hints for inconsistent boards, reject stale results including `NoSafeHint`, and keep editing processable while the hint is pending in `src/CageLogic.Application/GameSessions/GameSessionHintCoordinator.cs` and register the real dependencies in `src/CageLogic.Maui/MauiProgram.cs` (FR-007, FR-011, FR-013; SC-005, SC-007, SC-012).
- [ ] T026 [US2] Implement `TryComplete` using board validation and `SolutionGrid`, and create a summary with the exact error/hint-count rules in `src/CageLogic.Application/GameSessions/GameSessionCompletion.cs` and `src/CageLogic.Application/GameSessions/SessionSummary.cs` (FR-007, FR-010; SC-003, SC-009–SC-010).
- [ ] T027 [US2] Bind candidate mode, digit/delete commands, auto-fill, Undo/Redo, pause/resume, hints and completion to the game page; route move validation and full-board candidate calculation through asynchronous commands off the UI thread, then publish session/view state on the MAUI UI thread so navigation and input remain responsive in `src/CageLogic.Maui/ViewModels/GamePageViewModel.cs` and `src/CageLogic.Maui/Views/GamePage.xaml` (FR-005–FR-013; SC-002–SC-010).
- [ ] T028 [US2] Add the completed-session summary page with difficulty, active time, error count and displayed-hint-level count in `src/CageLogic.Maui/Views/SessionSummaryPage.xaml` and `src/CageLogic.Maui/Views/SessionSummaryPage.xaml.cs` (FR-010; SC-003, SC-009–SC-010).

**Checkpoint**: Notes/history, timer, hints and completion work from a deterministic session; the live game and summary expose the specified states.

---

## Phase 5: Polish & Cross-Cutting Validation

**Purpose**: Validate the complete feature on both targets.

- [ ] T029 Run the Release solution build with warnings as errors, then NUnit tests only after build success, and build the Android and Windows MAUI targets using the commands in `specs/004-game-session/quickstart.md` and project `src/CageLogic.Maui/CageLogic.Maui.csproj`.
- [ ] T030 Perform and record the Windows/Android manual acceptance from `specs/004-game-session/quickstart.md`, including TalkBack/Narrator traversal, keyboard mapping, lifecycle pause, stale hints, candidate history, summary counts, offline operation, responsive navigation/input during move validation and full-board candidate calculation, and device/OS plus p95 baseline measurements (FR-001–FR-013; SC-001–SC-012).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; create the host and Infrastructure project boundaries.
- **Foundational (Phase 2)**: Depends on Setup; logging and global safe-error handling block host story work.
- **User Story 1 (Phase 3)**: Depends on Setup and Foundational; delivers the first runnable session and board.
- **User Story 2 (Phase 4)**: Depends on the session and page established by US1 for host integration; Application tests can use a deterministic session fixture.
- **Polish (Phase 5)**: Depends on both stories being complete.

### User Story Dependencies

- **US1 (P1)**: Starts after Phase 2; no dependency on US2.
- **US2 (P1)**: Uses the active-session model and game page from US1. Its Application behavior can be tested independently from a generated-puzzle fixture, but host integration follows US1.

### Parallel Opportunities

- T002 can run in parallel with T001 because it creates a separate project; T003 follows T002 and T004 follows all three project tasks.
- T005 precedes T006; host exception handling T007 follows logger registration.
- After Setup and Foundational, T008 (Application tests) can be authored in parallel with T011 (pure board geometry/drawing files); T009 is still required before T008 passes.
- After T009, T010 (home/generation flow) and T011 (board drawing/geometry) touch separate files and can proceed in parallel.
- After T009, T015–T019 use separate NUnit files and can be authored in parallel before US2 implementation begins.
- Application history, timer, hint and completion implementation share session APIs; keep them sequential in listed order unless file ownership is split deliberately.

## Parallel Example: User Story 1

```text
After Setup and Foundational are complete, these separate files can be worked on in parallel:
Task T008: Specify the initial session behavior in GameSessionBoardTests.cs (the test passes after T009)
Task T011: Build board geometry/drawing in BoardGeometry.cs and KillerSudokuBoardDrawable.cs

After T009 is complete:
Task T010: Build the home/generation flow in HomeViewModel.cs and HomePage.xaml
```

## Parallel Example: User Story 2

```text
After T009 is complete, write the independent tests before implementation:
Task T015: GameSessionContentHistoryTests.cs
Task T016: GameSessionAutoFillTests.cs
Task T017: GameSessionTimerTests.cs
Task T018: GameSessionHintTests.cs
Task T019: GameSessionCompletionTests.cs
```

## Implementation Strategy

### MVP First (User Story 1)

1. Complete Setup and Foundational phases.
2. Complete US1 through T014.
3. Build and exercise puzzle start, board rendering, answer entry and accessibility on Windows/Android.
4. US1 provides the first usable increment; continue to US2 for a complete game session.

### Incremental Delivery

1. Complete host/logging foundation.
2. Deliver US1: start, render, select and enter basic answers.
3. Deliver US2: candidates/history, time/lifecycle, hints and completion summary.
4. Run T029–T030 across the full feature.

## Notes

- `[P]` means the task touches separate files and has no dependency on unfinished work.
- `[US1]` and `[US2]` map tasks to the two P1 stories in `specs/004-game-session/spec.md`.
- NUnit tests are written before their corresponding Application implementation; no MAUI UI automation framework is introduced.
- Do not count local-valid answers that only differ from `SolutionGrid` as errors during play.
- Do not persist active-session state; persistence after process termination remains feature 005.
