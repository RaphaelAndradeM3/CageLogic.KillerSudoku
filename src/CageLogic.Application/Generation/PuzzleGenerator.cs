using System.Diagnostics;
using System.Security.Cryptography;
using CageLogic.Application.Difficulty;
using CageLogic.Application.Solving;
using CageLogic.Domain.Puzzles;

namespace CageLogic.Application.Generation;

/// <summary>Builds, validates and accepts only unique puzzles whose complete trace matches the request.</summary>
public sealed class PuzzleGenerator
{
    private const int EasyInitialGivenCountPerRowAndBlock = 3;
    private const int InitialGivenSelectionSalt = unchecked((int)0x6E624EB7);

    private readonly ISolvedGridGenerator _solvedGridGenerator;
    private readonly ICagePartitionGenerator _cagePartitionGenerator;
    private readonly PuzzleStructureValidator _structureValidator;
    private readonly SudokuSolver _solver;
    private readonly IDifficultyAnalyzer _difficultyAnalyzer;
    private readonly IPuzzleGenerationObserver? _observer;

    public PuzzleGenerator(
        ISolvedGridGenerator? solvedGridGenerator = null,
        ICagePartitionGenerator? cagePartitionGenerator = null,
        PuzzleStructureValidator? structureValidator = null,
        SudokuSolver? solver = null,
        IDifficultyAnalyzer? difficultyAnalyzer = null,
        IPuzzleGenerationObserver? observer = null)
    {
        _solvedGridGenerator = solvedGridGenerator ?? new SolvedGridGenerator();
        _cagePartitionGenerator = cagePartitionGenerator ?? new CagePartitionGenerator();
        _structureValidator = structureValidator ?? new PuzzleStructureValidator();
        _solver = solver ?? new SudokuSolver();
        _difficultyAnalyzer = difficultyAnalyzer ?? new DifficultyAnalyzer();
        _observer = observer;
    }

    public PuzzleGenerationResult Generate(
        PuzzleGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var stopwatch = Stopwatch.StartNew();
        using var deadlineSource = new CancellationTokenSource();
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            deadlineSource.Token);
        using var deadlineWatcher = new DeadlineWatcher(stopwatch, request.Budget.TimeLimit, deadlineSource);
        var attempts = 0;

        try
        {
            while (attempts < request.Budget.MaxAttempts)
            {
                ThrowIfCallerCancelled(cancellationToken);
                if (stopwatch.Elapsed >= request.Budget.TimeLimit)
                {
                    return PuzzleGenerationResult.Unavailable(
                        UnavailableReason.TimeBudgetExhausted,
                        attempts,
                        stopwatch.Elapsed);
                }

                linkedCancellation.Token.ThrowIfCancellationRequested();
                var attempt = attempts++;
                var seed = request.Seed.HasValue
                    ? DeriveSeed(request.Seed.Value, attempt)
                    : RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue);
                var partitionSeed = unchecked(seed ^ 0x5F3759DF);

                var solutionValues = Measure(
                    attempt + 1,
                    PuzzleGenerationStage.SolvedGrid,
                    () => _solvedGridGenerator.Generate(seed, linkedCancellation.Token));
                if (request.Difficulty == DifficultyLevel.Expert)
                {
                    solutionValues = ExpertPuzzleSymmetry.TransformSolution(solutionValues, partitionSeed);
                }

                var cages = Measure(
                    attempt + 1,
                    PuzzleGenerationStage.CagePartition,
                    () => _cagePartitionGenerator.Generate(
                        solutionValues,
                        request.Difficulty,
                        partitionSeed,
                        linkedCancellation.Token));
                var givens = CreateInitialGivens(solutionValues, request.Difficulty, partitionSeed);
                var definition = new PuzzleDefinition(
                    givens,
                    cages);
                var structure = Measure(
                    attempt + 1,
                    PuzzleGenerationStage.StructureValidation,
                    () => _structureValidator.Validate(definition));
                if (!structure.IsValid || structure.Puzzle is null)
                {
                    _observer?.CandidateRejected(attempt + 1, PuzzleGenerationRejectionReason.InvalidStructure);
                    continue;
                }

                var validatedPuzzle = structure.Puzzle;
                var search = Measure(
                    attempt + 1,
                    PuzzleGenerationStage.UniquenessSearch,
                    () => _solver.FindSolutions(validatedPuzzle, linkedCancellation.Token));
                if (search.Multiplicity != SolutionMultiplicity.Unique || search.FirstSolution is null)
                {
                    _observer?.CandidateRejected(attempt + 1, search.Multiplicity switch
                    {
                        SolutionMultiplicity.NoSolution => PuzzleGenerationRejectionReason.NoSolution,
                        SolutionMultiplicity.Multiple => PuzzleGenerationRejectionReason.MultipleSolutions,
                        _ => throw new InvalidOperationException("Unexpected solution multiplicity.")
                    });
                    continue;
                }

                if (!search.FirstSolution.Values.SequenceEqual(solutionValues))
                {
                    _observer?.CandidateRejected(attempt + 1, PuzzleGenerationRejectionReason.GeneratedSolutionMismatch);
                    continue;
                }

                var difficulty = Measure(
                    attempt + 1,
                    PuzzleGenerationStage.DifficultyAnalysis,
                    () => _difficultyAnalyzer.Analyze(validatedPuzzle, linkedCancellation.Token));
                if (!difficulty.IsClassified)
                {
                    _observer?.CandidateRejected(attempt + 1, PuzzleGenerationRejectionReason.Unclassifiable);
                    continue;
                }

                if (difficulty.Level != request.Difficulty)
                {
                    _observer?.CandidateRejected(attempt + 1, PuzzleGenerationRejectionReason.DifficultyMismatch);
                    continue;
                }

                var generated = new GeneratedPuzzle(
                    validatedPuzzle,
                    search.FirstSolution,
                    difficulty,
                    request.Difficulty,
                    request.Seed,
                    attempts,
                    stopwatch.Elapsed);
                ThrowIfCallerCancelled(cancellationToken);
                linkedCancellation.Token.ThrowIfCancellationRequested();
                return PuzzleGenerationResult.Success(generated);
            }

            ThrowIfCallerCancelled(cancellationToken);
            if (stopwatch.Elapsed >= request.Budget.TimeLimit || deadlineSource.IsCancellationRequested)
            {
                return PuzzleGenerationResult.Unavailable(
                    UnavailableReason.TimeBudgetExhausted,
                    attempts,
                    stopwatch.Elapsed);
            }

            return PuzzleGenerationResult.Unavailable(
                UnavailableReason.AttemptBudgetExhausted,
                attempts,
                stopwatch.Elapsed);
        }
        catch (OperationCanceledException) when (deadlineSource.IsCancellationRequested)
        {
            ThrowIfCallerCancelled(cancellationToken);
            return PuzzleGenerationResult.Unavailable(
                UnavailableReason.TimeBudgetExhausted,
                attempts,
                stopwatch.Elapsed);
        }
    }

    private T Measure<T>(int attempt, PuzzleGenerationStage stage, Func<T> action)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            return action();
        }
        finally
        {
            _observer?.StageCompleted(new PuzzleGenerationStageMeasurement(attempt, stage, stopwatch.Elapsed));
        }
    }

    private static int DeriveSeed(int seed, int attempt)
    {
        return unchecked(seed + (attempt * (int)0x9E3779B9));
    }

    private static Dictionary<PuzzleDefinitionPosition, int> CreateInitialGivens(
        IReadOnlyList<int> solutionValues,
        DifficultyLevel difficulty,
        int seed)
    {
        var givens = new Dictionary<PuzzleDefinitionPosition, int>();
        if (difficulty != DifficultyLevel.Easy)
        {
            return givens;
        }

        var random = new Random(unchecked(seed ^ InitialGivenSelectionSalt));
        for (var blockRow = 0; blockRow < 3; blockRow++)
        {
            for (var blockColumn = 0; blockColumn < 3; blockColumn++)
            {
                var localColumns = new[] { 0, 1, 2 };
                for (var index = localColumns.Length - 1; index > 0; index--)
                {
                    var other = random.Next(index + 1);
                    (localColumns[index], localColumns[other]) = (localColumns[other], localColumns[index]);
                }

                for (var localRow = 0; localRow < EasyInitialGivenCountPerRowAndBlock; localRow++)
                {
                    var row = blockRow * 3 + localRow;
                    var column = blockColumn * 3 + localColumns[localRow];
                    var position = new PuzzleDefinitionPosition(row, column);
                    givens.Add(position, solutionValues[row * 9 + column]);
                }
            }
        }

        return givens;
    }

    private static void ThrowIfCallerCancelled(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }
    }

    private sealed class DeadlineWatcher : IDisposable
    {
        private static readonly TimeSpan MaximumTimerInterval = TimeSpan.FromDays(1);
        private readonly Stopwatch _stopwatch;
        private readonly TimeSpan _timeLimit;
        private readonly CancellationTokenSource _deadlineSource;
        private readonly Timer _timer;

        public DeadlineWatcher(Stopwatch stopwatch, TimeSpan timeLimit, CancellationTokenSource deadlineSource)
        {
            _stopwatch = stopwatch;
            _timeLimit = timeLimit;
            _deadlineSource = deadlineSource;
            _timer = new Timer(OnTimer, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            ScheduleNextCheck();
        }

        public void Dispose()
        {
            _timer.Dispose();
        }

        private void OnTimer(object? state)
        {
            var remaining = _timeLimit - _stopwatch.Elapsed;
            if (remaining <= TimeSpan.Zero)
            {
                _deadlineSource.Cancel();
                return;
            }

            ScheduleNextCheck(remaining);
        }

        private void ScheduleNextCheck(TimeSpan? remaining = null)
        {
            var delay = remaining ?? _timeLimit;
            _timer.Change(delay < MaximumTimerInterval ? delay : MaximumTimerInterval, Timeout.InfiniteTimeSpan);
        }
    }
}
