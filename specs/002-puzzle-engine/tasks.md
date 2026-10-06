---
description: "Task list for the Killer Sudoku puzzle engine feature"
---

# Tasks: Motor de puzzles Killer Sudoku

**Input**: Design documents from `/specs/002-puzzle-engine/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `quickstart.md`. There are no external contracts; services remain internal to the existing .NET projects.

**Tests**: Included because `spec.md` explicitly defines independent test scenarios and local validation for each story. Add these tests before their story implementation and establish the failing baseline; no test was run while generating this plan.

**Organization**: Tasks are grouped by the two P1 user stories. User Story 2 depends on the solver and uniqueness contract delivered by User Story 1.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Align the feature documents with the repository and finalize the normative requirements before implementation. The solution, Domain/Application projects, test projects, .NET 10 targets, and NUnit packages already exist; no new project or package is needed.

- [X] T001 Reconcile `README.md`, `specs/README.md`, the feature specs/checklists and `specs/002-puzzle-engine/spec.md` with the existing `CageLogic.slnx`, Domain/Application projects, NUnit projects, and available .NET 10 Release build/test gates.
- [X] T002 Record the normative cumulative Easy/Medium/Hard/Expert technique sets, catalog version 1, deterministic tie-break, and “lowest profile that completes a full logical trace” rule in `specs/002-puzzle-engine/spec.md` and align `specs/002-puzzle-engine/plan.md`.
- [X] T003 Define generated MVP puzzles as having zero given digits in `specs/002-puzzle-engine/spec.md` and `specs/002-puzzle-engine/data-model.md`; keep the full solution separate from the player-visible cage clues.
- [X] T004 Align `specs/002-puzzle-engine/spec.md` and `specs/002-puzzle-engine/data-model.md` on positive, explicit attempt/time budgets, `Unavailable` versus caller cancellation, and production defaults only after target-device measurements.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Use the existing validated-puzzle and candidate APIs from feature 001. No new project, package, database, external contract, MAUI host, or DI composition root is required for these library stories. Complete Phase 1 document alignment before beginning story work.

**Checkpoint**: Existing `CageLogic.Domain`, `CageLogic.Application`, `CageLogic.Domain.Tests`, and `CageLogic.Application.Tests` form the shared foundation; there are no additional foundational code tasks.

---

## Phase 3: User Story 1 - Verificar se um puzzle pode ser resolvido (Priority: P1) 🎯 MVP técnico

**Goal**: Resolve a validated Killer Sudoku puzzle and distinguish zero, one, and at least two solutions, retaining a complete first solution when one exists.

**Independent Test**: Application fixtures built from structurally valid puzzles distinguish `NoSolution`, `Unique`, and `Multiple`; the first solution, when present, satisfies all Sudoku and cage constraints, and cancellation is not reported as a solution count.

### Tests for User Story 1

- [X] T005 [P] [US1] Add NUnit coverage for a structurally valid puzzle with contradictory givens returning `NoSolution` in `tests/CageLogic.Application.Tests/Solving/SudokuSolverNoSolutionTests.cs`; run it to establish the failing baseline.
- [X] T006 [P] [US1] Add known unique- and multiple-solution fixtures, checking a complete valid first solution and the `Multiple` cutoff at two, in `tests/CageLogic.Application.Tests/Solving/SudokuSolverMultiplicityTests.cs`; run them to establish the failing baseline.
- [X] T007 [P] [US1] Add pre-cancelled and during-search cancellation cases without wall-clock assertions in `tests/CageLogic.Application.Tests/Solving/SudokuSolverCancellationTests.cs`; run them to establish the failing baseline.

### Implementation for User Story 1

- [X] T008 [P] [US1] Add immutable `SolutionGrid` in `src/CageLogic.Application/Solving/SolutionGrid.cs` with exactly 81 values in the range 1–9 and validation that each row, column, and 3×3 block contains 1–9 once and every associated cage satisfies its sum and no-repeat rule.
- [X] T009 [P] [US1] Add `SolutionMultiplicity` with `NoSolution`, `Unique`, and `Multiple` in `src/CageLogic.Application/Solving/SolutionMultiplicity.cs`; define `Multiple` as at least two solutions.
- [X] T010 [US1] Add immutable `SolutionSearchResult` in `src/CageLogic.Application/Solving/SolutionSearchResult.cs` with multiplicity, optional first solution, and a solution count capped at 2; require count 0 for `NoSolution`, 1 for `Unique`, and 2 for `Multiple`.
- [X] T011 [US1] Implement synchronous CPU-bound `SudokuSolver` in `src/CageLogic.Application/Solving/SudokuSolver.cs` over `ValidatedPuzzle`, reusing `CandidateCalculator`, branching with `SudokuBoard.WithPlayerValue`, selecting the empty cell with the fewest candidates (MRV), validating every complete leaf, checking cancellation during search, and stopping at the second distinct solution.
- [X] T012 [US1] Add `FindSolutionsUseCase` in `src/CageLogic.Application/Solving/FindSolutionsUseCase.cs` that runs the solver off the UI thread with `Task.Run`, passes the same `CancellationToken` to the task and search, and propagates `OperationCanceledException` separately from `SolutionSearchResult`.

**Checkpoint**: User Story 1 is independently usable by a caller that supplies `ValidatedPuzzle`; all three multiplicity outcomes and cancellation have focused tests.

---

## Phase 4: User Story 2 - Iniciar puzzle na dificuldade escolhida (Priority: P1)

**Goal**: Generate and publish only a structurally valid, unique Killer Sudoku puzzle whose complete logical trace matches the requested difficulty; return explicit unavailability on budget exhaustion and publish nothing on cancellation.

**Independent Test**: For each of Easy, Medium, Hard, and Expert, seeded requests produce a validated puzzle with exactly one solution and a complete logical trace in the requested profile. Same seed/options/catalog version replay deterministically. Invalid or ineligible candidates are discarded, exhaustion returns `Unavailable` without fallback, and cancellation publishes no result.

### Tests for User Story 2

- [X] T013 [P] [US2] Add Domain fixtures for Naked Single, Hidden Single, Cage Single, Cage Combination, Cage/Region Intersection, Rule of 45, Naked Pair, Hidden Pair, and Naked Triple, asserting each stable technique ID and logical deduction in `tests/CageLogic.Domain.Tests/LogicalSteps/LogicalTechniqueTests.cs`; run them to establish the failing baseline.
- [X] T014 [P] [US2] Add Application fixtures for all four cumulative profiles, tier boundaries, deterministic technique tie-breaking, candidate-elimination persistence across placements, complete-trace requirement, and `Unclassifiable` in `tests/CageLogic.Application.Tests/Difficulty/DifficultyAnalyzerTests.cs`; run them to establish the failing baseline.
- [X] T015 [P] [US2] Add seeded generation fixtures for valid unique puzzles with zero givens at each requested level, replay determinism, rejected candidates, attempt/time exhaustion without fallback, and cancellation without partial publication in `tests/CageLogic.Application.Tests/Generation/PuzzleGeneratorTests.cs`; run them to establish the failing baseline.

### Implementation for User Story 2

- [X] T016 [US2] Add stable, presentation-neutral technique identifiers for the nine catalog entries in `src/CageLogic.Domain/LogicalSteps/LogicalTechniqueId.cs`; keep IDs independent of localized display text.
- [X] T017 [US2] Add immutable `LogicalStep` and `LogicalState` in `src/CageLogic.Domain/LogicalSteps/LogicalStep.cs` and `src/CageLogic.Domain/LogicalSteps/LogicalState.cs`; carry a technique ID, placement/elimination effects, related cells, current board and candidate sets, with no hint prose/UI model and with eliminations preserved across placements.
- [X] T018 [US2] Add one shared strategy contract and deterministic orchestrator in `src/CageLogic.Domain/LogicalSteps/ILogicalTechnique.cs` and `src/CageLogic.Domain/LogicalSteps/LogicalStepAnalyzer.cs`; consume `LogicalState`, use catalog v1 technique order then row-major related positions, ascending digit and placement-before-elimination tie-break, and return no step when no supported logical technique applies.
- [X] T019 [P] [US2] Implement the Naked Single, Hidden Single, and Cage Single strategies in `src/CageLogic.Domain/LogicalSteps/Techniques/NakedSingleTechnique.cs`, `src/CageLogic.Domain/LogicalSteps/Techniques/HiddenSingleTechnique.cs`, and `src/CageLogic.Domain/LogicalSteps/Techniques/CageSingleTechnique.cs`.
- [X] T020 [P] [US2] Implement Killer cage-combination deductions in `src/CageLogic.Domain/LogicalSteps/Techniques/CageCombinationTechnique.cs`.
- [X] T021 [P] [US2] Implement cage/region interaction deductions in `src/CageLogic.Domain/LogicalSteps/Techniques/CageRegionIntersectionTechnique.cs`.
- [X] T022 [P] [US2] Implement Rule of 45 deductions in `src/CageLogic.Domain/LogicalSteps/Techniques/RuleOf45Technique.cs`.
- [X] T023 [P] [US2] Implement Naked Pair deductions in `src/CageLogic.Domain/LogicalSteps/Techniques/NakedPairTechnique.cs`.
- [X] T024 [P] [US2] Implement Hidden Pair deductions in `src/CageLogic.Domain/LogicalSteps/Techniques/HiddenPairTechnique.cs`.
- [X] T025 [P] [US2] Implement Naked Triple deductions in `src/CageLogic.Domain/LogicalSteps/Techniques/NakedTripleTechnique.cs`.
- [X] T026 [US2] Add version-1 cumulative profiles in `src/CageLogic.Application/Difficulty/DifficultyProfileCatalog.cs`: Easy = Naked Single, Hidden Single, Cage Single; Medium = Easy + Cage Combination; Hard = Medium + Cage/Region Intersection + Rule of 45; Expert = Hard + Naked Pair + Hidden Pair + Naked Triple.
- [X] T027 [US2] Add `DifficultyAnalysisResult` in `src/CageLogic.Application/Difficulty/DifficultyAnalysisResult.cs` with `Classified`/`Unclassifiable`, level only when classified, catalog version, and the ordered technique-ID trace.
- [X] T028 [US2] Implement `DifficultyAnalyzer` in `src/CageLogic.Application/Difficulty/DifficultyAnalyzer.cs` to restart a deterministic logical trace at each cumulative profile and classify at the least advanced profile that solves fully; return `Unclassifiable` if every profile stalls and never use backtracking to complete the logical trace.
- [X] T029 [P] [US2] Add `GenerationBudget` and `PuzzleGenerationRequest` in `src/CageLogic.Application/Generation/GenerationBudget.cs` and `src/CageLogic.Application/Generation/PuzzleGenerationRequest.cs`, requiring requested difficulty and a positive explicit attempt/time budget, accepting an optional seed, and providing no unmeasured production default.
- [X] T030 [P] [US2] Add immutable `GeneratedPuzzle` in `src/CageLogic.Application/Generation/GeneratedPuzzle.cs` containing the `ValidatedPuzzle`, its `SolutionGrid`, matching `DifficultyAnalysisResult`, seed when supplied, and attempts/elapsed metrics.
- [X] T031 [US2] Add `PuzzleGenerationResult` and `UnavailableReason` in `src/CageLogic.Application/Generation/PuzzleGenerationResult.cs` after T030; model only success with a complete `GeneratedPuzzle` or unavailable with reason/attempts/elapsed, never a partial puzzle.
- [X] T032 [US2] Implement seeded full-grid creation in `src/CageLogic.Application/Generation/SolvedGridGenerator.cs` using Sudoku constraints, deterministic cell/value ordering for equal seed and runtime version, and cooperative cancellation.
- [X] T033 [US2] Implement cage partition creation in `src/CageLogic.Application/Generation/CagePartitionGenerator.cs` that covers all 81 cells once with orthogonally connected cages, never repeats a solution digit within a cage, derives each target from the known solution, and observes the seed and cancellation token.
- [X] T034 [US2] Implement the retry/accept pipeline in `src/CageLogic.Application/Generation/PuzzleGenerator.cs`: create `PuzzleDefinition` with zero given values, validate through `PuzzleStructureValidator`, require solver multiplicity `Unique`, require the requested difficulty profile, discard every rejected candidate, and return `Unavailable` with reason/attempts/elapsed at either budget limit without fallback; use internal deadline cancellation distinct from caller cancellation, with caller cancellation taking precedence.
- [X] T035 [US2] Add `GeneratePuzzleUseCase` in `src/CageLogic.Application/Generation/GeneratePuzzleUseCase.cs` that runs generation away from the UI thread with `Task.Run`, passes cancellation through every stage, propagates caller cancellation separately from an internal time-budget deadline, and publishes only a complete success result.
- [ ] T040 [US2] Tune the production cage partitioner and add a seeded end-to-end Expert generation fixture using the real `PuzzleSolver` and `DifficultyAnalyzer`; the current Expert test injects the analyzer and does not prove that the default pipeline can produce an Expert puzzle.

**Checkpoint**: User Story 2 can produce all requested profiles from seeded generation and has explicit, separately modeled rejection, exhaustion, and cancellation outcomes.

**Status note**: Easy, Medium, and Hard have real seeded end-to-end fixtures. Expert still requires T040; the User Story 2 checkpoint is not complete until that task passes.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Measure production budgets, keep validation guidance current, and run repository gates after both stories are complete.

- [X] T036 Add an opt-in fixed-seed performance fixture in `tests/CageLogic.Application.Tests/Generation/PuzzleGeneratorPerformanceTests.cs` that records per-stage duration, attempts/rejection reasons, p50/p95/maximum request latency, and cancellation response without running in the ordinary unit-test suite.
- [ ] T037 Select and record the minimum supported Android benchmark device/runtime, then run the Release measurement fixture on Windows and that Android target when its host/workloads exist; set measured configurable production defaults in `src/CageLogic.Application/Generation/GenerationBudget.cs` and record device, corpus, and results in `specs/002-puzzle-engine/research.md` and `specs/002-puzzle-engine/quickstart.md`. This is a release/configuration gate, not a blocker for implementing explicit per-request budgets.
- [X] T038 Update `specs/002-puzzle-engine/quickstart.md` with the actual focused fixture names, current prerequisites, measured budget rationale, and applicable Windows/Android build commands.
- [X] T039 Run `dotnet build CageLogic.slnx --configuration Release --warnaserror` and, after a successful build, `dotnet test CageLogic.slnx --no-build --configuration Release`; build Windows/Android targets when the host and workloads are configured, and record outcomes in `specs/002-puzzle-engine/quickstart.md`.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No code dependency; reconcile the requirements and stale repository/toolchain statements first.
- **Foundational (Phase 2)**: Uses existing solution/projects and feature 001 APIs; no additional code setup is needed.
- **User Story 1 (Phase 3)**: Starts after Phase 2; does not depend on another story.
- **User Story 2 (Phase 4)**: Starts after User Story 1 solver/result contract is complete; generation calls the solver for uniqueness. Difficulty and generation acceptance must both pass before publication.
- **Polish (Phase 5)**: Depends on both stories; measured defaults follow the implemented generator and representative target measurements.

### User Story Dependencies

- **US1 (P1)**: Depends on existing `ValidatedPuzzle`, `CandidateCalculator`, and board validation from feature 001; otherwise independently testable.
- **US2 (P1)**: Depends on US1's `SolutionGrid`, solution multiplicity, and cancellation-aware search. Its logic-step analyzer, difficulty classifier, and cage construction can be implemented in parallel after their shared Domain contracts exist.

### Within Each User Story

- Write and run that story's new tests to establish the failing baseline before implementation.
- In US1, immutable result types precede solver search; the async application use case follows the synchronous solver.
- In US2, technique IDs/step/strategy contract precede technique implementations; those implementations precede profiles and difficulty analysis; request/result models and generators precede the orchestration/use case.
- US2 generation orchestration follows US1's uniqueness result and US2's classifier.
- Release performance measurements and final build/test gates happen after both stories.

### Parallel Opportunities

- US1 test files T005–T007 are separate; `SolutionGrid` and `SolutionMultiplicity` (T008–T009) are separate source files.
- US2 test files T013–T015 are separate. Once the shared step contract exists, technique files T019–T025 can be implemented independently. Generation request/budget and result model files T029–T031 are also independent.
- US2 cannot be delivered before US1 because the generation acceptance pipeline depends on the uniqueness solver.

---

## Parallel Example: User Story 1

```text
Task: T005 Add zero-solution fixture in tests/CageLogic.Application.Tests/Solving/SudokuSolverNoSolutionTests.cs
Task: T006 Add unique/multiple fixtures in tests/CageLogic.Application.Tests/Solving/SudokuSolverMultiplicityTests.cs
Task: T007 Add cancellation fixtures in tests/CageLogic.Application.Tests/Solving/SudokuSolverCancellationTests.cs
```

## Parallel Example: User Story 2

```text
Task: T013 Add logical technique fixtures in tests/CageLogic.Domain.Tests/LogicalSteps/LogicalTechniqueTests.cs
Task: T014 Add profile fixtures in tests/CageLogic.Application.Tests/Difficulty/DifficultyAnalyzerTests.cs
Task: T015 Add generation fixtures in tests/CageLogic.Application.Tests/Generation/PuzzleGeneratorTests.cs

# After T018 establishes the shared strategy contract:
Task: T020 Implement CageCombinationTechnique in src/CageLogic.Domain/LogicalSteps/Techniques/CageCombinationTechnique.cs
Task: T021 Implement CageRegionIntersectionTechnique in src/CageLogic.Domain/LogicalSteps/Techniques/CageRegionIntersectionTechnique.cs
Task: T022 Implement RuleOf45Technique in src/CageLogic.Domain/LogicalSteps/Techniques/RuleOf45Technique.cs
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Complete Phase 1 document alignment and confirm the existing Phase 2 foundation.
2. Complete User Story 1: solution search and zero/one/multiple distinction.
3. Stop at its checkpoint and validate the solver fixtures and build gates.
4. Continue immediately to User Story 2; both stories are P1 and the full product goal requires generated puzzles.

### Incremental Delivery

1. Reconcile requirements and retain the existing .NET 10 project structure.
2. Deliver solver and bounded multiplicity as a standalone application capability.
3. Add shared logical deductions and difficulty profiles.
4. Add seeded cage generation, uniqueness/difficulty rejection, and explicit exhaustion/cancellation.
5. Measure target-device budgets, then run the full release gates.

## Notes

- `[P]` means the task touches independent files and has no dependency on an unfinished task.
- `[US1]`/`[US2]` map directly to the P1 stories in `spec.md`.
- Tests are present because the feature specification explicitly requires reference fixtures and validation scenarios; this task-generation run did not execute them.
- No new project, NuGet package, API contract folder, or MAUI host is created by this feature plan.

## Phase 6: Convergence

- [ ] T041 Tune the default cage partitioner and add a bounded, fixed-seed end-to-end Expert generation fixture using the real `SudokuSolver` and `DifficultyAnalyzer`; assert a unique puzzle with zero givens and Expert classification, per US2/AC1, FR-006, and SC-003 (partial; closes T040).
- [ ] T042 Select the minimum supported Android target and run the fixed Release generation corpus on that target and Windows; record stage and request latency, cancellation, device/runtime, and results, then set measured configurable production budgets, per FR-011, SC-006, and the plan performance gate (partial; closes T037).
