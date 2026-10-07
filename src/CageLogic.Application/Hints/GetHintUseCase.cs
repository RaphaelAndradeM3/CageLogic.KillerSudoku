using CageLogic.Application.Solving;
using CageLogic.Domain.Board;
using CageLogic.Domain.Candidates;
using CageLogic.Domain.Cages;
using CageLogic.Domain.LogicalSteps;
using CageLogic.Domain.Puzzles;
using CageLogic.Domain.Validation;

namespace CageLogic.Application.Hints;

/// <summary>Analyzes one immutable board snapshot and returns its first supported logical hint.</summary>
public sealed class GetHintUseCase
{
    private readonly ILogicalStepAnalyzer _analyzer;
    private readonly PuzzleStructureValidator _puzzleValidator;
    private readonly SudokuBoardValidator _boardValidator;
    private readonly SudokuSolver _solver;
    private readonly LogicalHintExplanationCatalog _catalog;

    public GetHintUseCase(
        ILogicalStepAnalyzer? analyzer = null,
        PuzzleStructureValidator? puzzleValidator = null,
        SudokuBoardValidator? boardValidator = null,
        SudokuSolver? solver = null,
        LogicalHintExplanationCatalog? catalog = null)
    {
        _analyzer = analyzer ?? new LogicalStepAnalyzer();
        _puzzleValidator = puzzleValidator ?? new PuzzleStructureValidator();
        _boardValidator = boardValidator ?? new SudokuBoardValidator();
        _solver = solver ?? new SudokuSolver();
        _catalog = catalog ?? new LogicalHintExplanationCatalog();
    }

    public Task<HintResult> ExecuteAsync(HintRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Task.Run(() => Analyze(request, cancellationToken), cancellationToken);
    }

    private HintResult Analyze(HintRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var context = request.PuzzleContext;
        var board = request.CurrentBoard;
        var revision = request.BoardRevision;

        var boardValidation = _boardValidator.Validate(board);
        if (!boardValidation.IsValid || context.Multiplicity == SolutionMultiplicity.NoSolution)
        {
            return Terminal(revision, HintStatus.InconsistentState);
        }

        if (context.Multiplicity == SolutionMultiplicity.Unique && !MatchesUniqueSolution(board, context.UniqueSolution!))
        {
            return Terminal(revision, HintStatus.InconsistentState);
        }

        if (boardValidation.IsSolved)
        {
            return Terminal(revision, HintStatus.PuzzleSolved);
        }

        if (context.Multiplicity == SolutionMultiplicity.Multiple &&
            !HasCompatibleSolution(context.Puzzle, board, cancellationToken))
        {
            return Terminal(revision, HintStatus.InconsistentState);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var state = LogicalState.Create(board);
        var step = _analyzer.FindNextStep(state, cancellationToken: cancellationToken);
        if (step is null)
        {
            return Terminal(revision, HintStatus.NoSafeHint);
        }

        if (step.Evidence is null)
        {
            throw new InvalidOperationException($"Logical technique {step.TechniqueId} returned no explanation evidence.");
        }

        var explanation = _catalog.Get(step.TechniqueId);
        return new HintResult(
            revision,
            HintStatus.Available,
            step.TechniqueId,
            explanation.Name,
            explanation.Explanation);
    }

    private bool HasCompatibleSolution(
        ValidatedPuzzle originalPuzzle,
        SudokuBoard currentBoard,
        CancellationToken cancellationToken)
    {
        var givens = new Dictionary<PuzzleDefinitionPosition, int>();
        foreach (var (position, value) in originalPuzzle.Givens)
        {
            givens.Add(new PuzzleDefinitionPosition(position.Row, position.Column), value);
        }

        foreach (var cell in currentBoard.Cells)
        {
            if (cell.PlayerValue is { } playerValue)
            {
                givens.Add(new PuzzleDefinitionPosition(cell.Position.Row, cell.Position.Column), playerValue);
            }
        }

        var cages = originalPuzzle.Cages.Select(cage => new CageDefinition(
            cage.TargetSum,
            cage.Positions.Select(position => new PuzzleDefinitionPosition(position.Row, position.Column))));
        var structure = _puzzleValidator.Validate(new PuzzleDefinition(givens, cages));
        if (!structure.IsValid)
        {
            return false;
        }

        return _solver.FindSolutions(structure.Puzzle!, cancellationToken).Multiplicity != SolutionMultiplicity.NoSolution;
    }

    private static bool MatchesUniqueSolution(SudokuBoard board, SolutionGrid solution)
    {
        return board.Cells
            .Where(cell => cell.CurrentValue.HasValue)
            .All(cell => cell.CurrentValue == solution.GetValue(cell.Position));
    }

    private static HintResult Terminal(long revision, HintStatus status) => new(revision, status);
}
