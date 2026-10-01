# Data Model: Regras e Candidatos de Killer Sudoku

## Overview

Represent one 9×9 board state, its 81 positions, a partition into Killer Sudoku cages, fixed puzzle values, player-entered values, validation results, conflicts, and local candidate sets. The model is owned by Domain. Application coordinates use cases and returns these results to future hosts.

## Entities and value objects

| Type | Fields and invariants | Relationships |
|---|---|---|
| PuzzleDefinitionPosition | Unvalidated integer Row and Column supplied to structural validation. Coordinates are not CellPosition values until they pass range checks. | Used only while validating a puzzle definition. |
| CageDefinition | Target sum plus a list of raw PuzzleDefinitionPosition values. It can be malformed before validation. | Part of PuzzleDefinition. |
| PuzzleDefinition | Raw givens and cage definitions received by the domain validator. It is not a playable board. | Validated into a ValidatedPuzzle or returned with structure issues. |
| CellPosition | Immutable Row and Column, each zero-based in the range 0–8. Constructed only from an in-range coordinate. | Identifies exactly one board cell and can occur in one cage. |
| Cage | A validated non-empty set of distinct CellPosition values and a positive target sum. Every position is on the board; all cells are connected by shared sides; its target is achievable by assigning one distinct digit from 1–9 per cell. | Cages partition all 81 board positions. |
| ValidatedPuzzle | A complete 81-position board definition with validated cages and fixed values. | Source for creating SudokuBoard states. |
| SudokuCell | Position, optional GivenValue, optional PlayerValue. Each value is 1–9. CurrentValue is the given value when present, otherwise the player value, otherwise empty. A given value cannot be overwritten by a player move. | Belongs to exactly one cage, one row, one column, and one 3×3 block. |
| SudokuBoard | The 81 cells plus the cage partition and current player entries. Values do not encode a solution or generator metadata. | Contains cells and references the puzzle's cages. |
| Move | Position plus either a value to enter or an explicit clear operation. It does not mutate a given cell. | Applied to a board state by an Application use case. |
| PuzzleStructureResult | Either a ValidatedPuzzle or invalid structure with issues. Issues use raw coordinates/cage identity so out-of-range input remains reportable without constructing an invalid CellPosition. Covers out-of-range or uncovered positions, cage overlap/duplication, empty or disconnected cages, and unreachable targets. | Produced before board value validation; kept separate from player value conflicts. |
| ValueConflict | Rule identifier and the involved positions. Rule kinds include duplicate row, column, block, cage, and cage target unreachable under the current partial values. | One validation result may contain multiple conflicts. |
| BoardValidationResult | IsValid, IsComplete, IsSolved, and value conflicts. IsSolved is true only when the structure is valid, the board is complete, and no value conflicts remain. A partial conflict-free board is valid and incomplete. | Returned after a move or validation request. |
| CandidateSet | Position of an empty cell and a set of allowed digits from 1–9; the set may be empty in an inconsistent state. | Derived from a board state and its cage. Filled cells are not candidate targets. |

## Structural rules

- PuzzleDefinition is validated before converting raw coordinates into CellPosition values.
- There are exactly 81 CellPosition values in the validated board, with rows and columns 0–8.
- Every position belongs to exactly one cage.
- A cage cannot be empty or repeat a position.
- Cage connectivity uses orthogonal side adjacency; diagonal contact alone does not connect cells.
- For a cage with N cells, its target must equal the sum of N distinct digits selected from 1 through 9. Reachability is exact, not just a comparison to a minimum and maximum.
- An invalid cage definition or out-of-range position is reported as invalid puzzle structure, separate from conflicts in entered values.

## Board value and state rules

- GivenValue and PlayerValue are stored separately so a player cannot replace a fixed puzzle value.
- Row, column, and 3×3 block conflicts arise from repeated current values.
- Cage conflicts arise from repeated values or partial/final values that cannot reach the cage target.
- Validity and completeness are independent facts. A conflict-free partial board is valid and incomplete; a complete conflict-free board is solved.
- Expected invalid moves return explicit results. Exceptions are reserved for unexpected failures.

## Candidate calculation rules

- Candidate results are returned only for empty positions.
- Begin with digits 1–9 and remove values already present in that position's row, column, block, or cage.
- Retain a candidate only when placing it preserves at least one distinct-value assignment for the remaining empty positions in the same cage that reaches the target sum.
- The candidate calculator does not search for a completion of the entire Sudoku board. An empty candidate set is a result and is not itself an exception.
- Candidate results are recomputed from the current board state after moves; no mutable global cache is part of the initial design.

## State transitions

1. Receive a raw PuzzleDefinition and validate its coordinate bounds, cage partition, connectivity, and target reachability.
2. On success, create a ValidatedPuzzle; on failure, return structure issues containing the original raw coordinates as needed.
3. Create a board state from fixed puzzle values and an initially empty set of player entries.
4. Apply a player move or clear operation, preserving fixed values and returning explicit invalid-move outcomes.
5. Recompute validation and candidate information from the resulting state.
6. Mark the board solved only when all 81 positions have values and no value conflict remains.