using CageLogic.Domain.Board;
using CageLogic.Domain.Candidates;
using CageLogic.Domain.Puzzles;
using CageLogic.Domain.Validation;

namespace CageLogic.Application.Solving;

/// <summary>Counts solutions for a validated Killer Sudoku puzzle, stopping after the second.</summary>
public sealed class SudokuSolver
{
    private readonly ICandidateCalculator _candidateCalculator;
    private readonly SudokuBoardValidator _boardValidator;
    private readonly bool _useIncrementalMasks;

    public SudokuSolver()
        : this(new CandidateCalculator(), new SudokuBoardValidator(), useIncrementalMasks: true)
    {
    }

    public SudokuSolver(CandidateCalculator candidateCalculator, SudokuBoardValidator boardValidator)
        : this((ICandidateCalculator)candidateCalculator, boardValidator, useIncrementalMasks: true)
    {
    }

    public SudokuSolver(ICandidateCalculator candidateCalculator, SudokuBoardValidator boardValidator)
        : this(candidateCalculator, boardValidator, useIncrementalMasks: candidateCalculator is CandidateCalculator)
    {
    }

    private SudokuSolver(
        ICandidateCalculator candidateCalculator,
        SudokuBoardValidator boardValidator,
        bool useIncrementalMasks)
    {
        ArgumentNullException.ThrowIfNull(candidateCalculator);
        ArgumentNullException.ThrowIfNull(boardValidator);
        _candidateCalculator = candidateCalculator;
        _boardValidator = boardValidator;
        _useIncrementalMasks = useIncrementalMasks;
    }

    public SolutionSearchResult FindSolutions(ValidatedPuzzle puzzle, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(puzzle);
        cancellationToken.ThrowIfCancellationRequested();

        return _useIncrementalMasks
            ? FindSolutionsUsingIncrementalMasks(puzzle, cancellationToken)
            : FindSolutionsUsingCandidateCalculator(puzzle, cancellationToken);
    }

    private SolutionSearchResult FindSolutionsUsingIncrementalMasks(
        ValidatedPuzzle puzzle,
        CancellationToken cancellationToken)
    {
        var state = new SearchState(puzzle.CreateBoard());
        if (!state.IsConsistent)
        {
            return new SolutionSearchResult(SolutionMultiplicity.NoSolution, null, 0);
        }

        SolutionGrid? firstSolution = null;
        var solutionCount = 0;
        var foundSolutions = new HashSet<string>(StringComparer.Ordinal);
        Search();
        return CreateResult(solutionCount, firstSolution);

        void Search()
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (solutionCount >= 2)
            {
                return;
            }

            var selectedIndex = -1;
            List<int>? selectedCandidates = null;
            for (var index = 0; index < state.Values.Length; index++)
            {
                if (state.Values[index] != 0)
                {
                    continue;
                }

                var candidates = state.GetCandidates(index);
                if (candidates.Count == 0)
                {
                    return;
                }

                if (selectedCandidates is null || candidates.Count < selectedCandidates.Count)
                {
                    selectedIndex = index;
                    selectedCandidates = candidates;
                    if (candidates.Count == 1)
                    {
                        break;
                    }
                }
            }

            if (selectedIndex < 0)
            {
                var board = state.CreateBoard();
                if (!_boardValidator.Validate(board).IsSolved)
                {
                    return;
                }

                var values = (int[])state.Values.Clone();
                var key = string.Concat(values.Select(value => (char)('0' + value)));
                if (foundSolutions.Add(key))
                {
                    solutionCount++;
                    firstSolution ??= new SolutionGrid(values, puzzle);
                }

                return;
            }

            foreach (var value in selectedCandidates!)
            {
                cancellationToken.ThrowIfCancellationRequested();
                state.Place(selectedIndex, value);
                try
                {
                    Search();
                }
                finally
                {
                    state.Remove(selectedIndex, value);
                }

                if (solutionCount >= 2)
                {
                    return;
                }
            }
        }
    }

    private SolutionSearchResult FindSolutionsUsingCandidateCalculator(
        ValidatedPuzzle puzzle,
        CancellationToken cancellationToken)
    {
        SolutionGrid? firstSolution = null;
        var solutionCount = 0;
        var foundSolutions = new HashSet<string>(StringComparer.Ordinal);
        Search(puzzle.CreateBoard());
        return CreateResult(solutionCount, firstSolution);

        void Search(SudokuBoard board)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (solutionCount >= 2)
            {
                return;
            }

            var candidateSets = _candidateCalculator.Calculate(board);
            cancellationToken.ThrowIfCancellationRequested();
            if (candidateSets.Count == 0)
            {
                if (!_boardValidator.Validate(board).IsSolved)
                {
                    return;
                }

                var values = board.Cells.Select(cell => cell.CurrentValue!.Value).ToArray();
                var key = string.Concat(values.Select(value => (char)('0' + value)));
                if (foundSolutions.Add(key))
                {
                    solutionCount++;
                    firstSolution ??= new SolutionGrid(values, puzzle);
                }

                return;
            }

            var selected = candidateSets
                .OrderBy(candidates => candidates.Values.Count)
                .ThenBy(candidates => candidates.Position.Row * 9 + candidates.Position.Column)
                .First();
            if (selected.Values.Count == 0)
            {
                return;
            }

            foreach (var value in selected.Values.Order())
            {
                cancellationToken.ThrowIfCancellationRequested();
                Search(board.WithPlayerValue(selected.Position, value));
                if (solutionCount >= 2)
                {
                    return;
                }
            }
        }
    }

    private static SolutionSearchResult CreateResult(int solutionCount, SolutionGrid? firstSolution)
    {
        return solutionCount switch
        {
            0 => new SolutionSearchResult(SolutionMultiplicity.NoSolution, null, 0),
            1 => new SolutionSearchResult(SolutionMultiplicity.Unique, firstSolution, 1),
            _ => new SolutionSearchResult(SolutionMultiplicity.Multiple, firstSolution, 2)
        };
    }

    private sealed class SearchState
    {
        private readonly SudokuBoard _initialBoard;
        private readonly int[] _rowMasks = new int[9];
        private readonly int[] _columnMasks = new int[9];
        private readonly int[] _blockMasks = new int[9];
        private readonly int[] _cageIndexes = new int[81];
        private readonly int[] _cageTargets;
        private readonly int[] _cageMasks;
        private readonly int[] _cageSums;
        private readonly int[] _cageEmptyCounts;

        public SearchState(SudokuBoard board)
        {
            _initialBoard = board;
            Values = new int[81];
            _cageTargets = board.Cages.Select(cage => cage.TargetSum).ToArray();
            _cageMasks = new int[board.Cages.Count];
            _cageSums = new int[board.Cages.Count];
            _cageEmptyCounts = board.Cages.Select(cage => cage.Positions.Count).ToArray();

            for (var cageIndex = 0; cageIndex < board.Cages.Count; cageIndex++)
            {
                foreach (var position in board.Cages[cageIndex].Positions)
                {
                    _cageIndexes[position.Row * 9 + position.Column] = cageIndex;
                }
            }

            var consistent = true;
            foreach (var cell in board.Cells)
            {
                var value = cell.CurrentValue;
                if (!value.HasValue)
                {
                    continue;
                }

                var index = cell.Position.Row * 9 + cell.Position.Column;
                var digitMask = 1 << value.Value;
                var block = GetBlockIndex(index);
                var cage = _cageIndexes[index];
                Values[index] = value.Value;
                consistent &= AddDigit(_rowMasks, cell.Position.Row, digitMask);
                consistent &= AddDigit(_columnMasks, cell.Position.Column, digitMask);
                consistent &= AddDigit(_blockMasks, block, digitMask);
                consistent &= AddDigit(_cageMasks, cage, digitMask);
                _cageSums[cage] += value.Value;
                _cageEmptyCounts[cage]--;
            }

            for (var cage = 0; cage < _cageTargets.Length; cage++)
            {
                consistent &= CanReachTarget(
                    _cageTargets[cage],
                    _cageEmptyCounts[cage],
                    _cageSums[cage],
                    _cageMasks[cage]);
            }

            IsConsistent = consistent;
        }

        public int[] Values { get; }

        public bool IsConsistent { get; }

        public List<int> GetCandidates(int index)
        {
            var position = new CellPosition(index / 9, index % 9);
            var cage = _cageIndexes[index];
            var forbidden = _rowMasks[position.Row] |
                            _columnMasks[position.Column] |
                            _blockMasks[GetBlockIndex(index)] |
                            _cageMasks[cage];
            var candidates = new List<int>(9);
            for (var digit = 1; digit <= 9; digit++)
            {
                var digitMask = 1 << digit;
                if ((forbidden & digitMask) != 0)
                {
                    continue;
                }

                if (CanReachTarget(
                        _cageTargets[cage],
                        _cageEmptyCounts[cage] - 1,
                        _cageSums[cage] + digit,
                        _cageMasks[cage] | digitMask))
                {
                    candidates.Add(digit);
                }
            }

            return candidates;
        }

        public void Place(int index, int digit)
        {
            var position = new CellPosition(index / 9, index % 9);
            var digitMask = 1 << digit;
            var cage = _cageIndexes[index];
            Values[index] = digit;
            _rowMasks[position.Row] |= digitMask;
            _columnMasks[position.Column] |= digitMask;
            _blockMasks[GetBlockIndex(index)] |= digitMask;
            _cageMasks[cage] |= digitMask;
            _cageSums[cage] += digit;
            _cageEmptyCounts[cage]--;
        }

        public void Remove(int index, int digit)
        {
            var position = new CellPosition(index / 9, index % 9);
            var digitMask = ~(1 << digit);
            var cage = _cageIndexes[index];
            Values[index] = 0;
            _rowMasks[position.Row] &= digitMask;
            _columnMasks[position.Column] &= digitMask;
            _blockMasks[GetBlockIndex(index)] &= digitMask;
            _cageMasks[cage] &= digitMask;
            _cageSums[cage] -= digit;
            _cageEmptyCounts[cage]++;
        }

        public SudokuBoard CreateBoard()
        {
            var board = _initialBoard;
            foreach (var cell in _initialBoard.Cells)
            {
                if (!cell.GivenValue.HasValue)
                {
                    var value = Values[cell.Position.Row * 9 + cell.Position.Column];
                    board = board.WithPlayerValue(cell.Position, value);
                }
            }

            return board;
        }

        private static bool AddDigit(int[] masks, int index, int digitMask)
        {
            var hasDuplicate = (masks[index] & digitMask) != 0;
            masks[index] |= digitMask;
            return !hasDuplicate;
        }

        private static int GetBlockIndex(int index)
        {
            var row = index / 9;
            var column = index % 9;
            return (row / 3 * 3) + column / 3;
        }

        private static bool CanReachTarget(int targetSum, int remainingCells, int currentSum, int usedDigitMask)
        {
            return CanChooseDigits(1, remainingCells, targetSum - currentSum, usedDigitMask);
        }

        private static bool CanChooseDigits(int firstDigit, int count, int remainingSum, int usedDigitMask)
        {
            if (count == 0)
            {
                return remainingSum == 0;
            }

            for (var digit = firstDigit; digit <= 9; digit++)
            {
                var digitMask = 1 << digit;
                if ((usedDigitMask & digitMask) != 0)
                {
                    continue;
                }

                if (CanChooseDigits(digit + 1, count - 1, remainingSum - digit, usedDigitMask | digitMask))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
