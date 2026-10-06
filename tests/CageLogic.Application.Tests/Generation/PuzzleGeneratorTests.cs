using CageLogic.Application.Difficulty;
using CageLogic.Application.Generation;
using CageLogic.Application.Solving;
using CageLogic.Domain.Cages;
using CageLogic.Domain.LogicalSteps;
using CageLogic.Domain.Puzzles;
using CageLogic.Domain.Validation;

namespace CageLogic.Application.Tests.Generation;

public sealed class PuzzleGeneratorTests
{
    [Test]
    public void Generate_EasySeed_ReturnsUniquePuzzleWithoutGivens()
    {
        var result = new PuzzleGenerator().Generate(Request(DifficultyLevel.Easy, seed: 20261005));

        Assert.That(result.IsSuccess, Is.True);
        var generated = result.GeneratedPuzzle!;
        Assert.That(generated.Puzzle.Givens, Is.Empty);
        Assert.That(generated.Difficulty.Level, Is.EqualTo(DifficultyLevel.Easy));
        Assert.That(generated.Attempts, Is.EqualTo(1));
        Assert.That(new SudokuBoardValidator().Validate(generated.Solution.ToBoard(generated.Puzzle)).IsSolved, Is.True);
        Assert.That(new SudokuSolver().FindSolutions(generated.Puzzle).Multiplicity, Is.EqualTo(SolutionMultiplicity.Unique));
    }

    [Test]
    public void Generate_SeededMediumFixture_UsesTheRealSolverAndDifficultyAnalyzer()
    {
        var result = new PuzzleGenerator().Generate(new PuzzleGenerationRequest(
            DifficultyLevel.Medium,
            new GenerationBudget(25, TimeSpan.FromSeconds(30)),
            seed: 4100));

        Assert.That(result.IsSuccess, Is.True, $"{result.UnavailableReason}, attempts={result.Attempts}");
        Assert.That(result.GeneratedPuzzle!.Difficulty.Level, Is.EqualTo(DifficultyLevel.Medium));
        Assert.That(result.GeneratedPuzzle.Puzzle.Givens, Is.Empty);
        Assert.That(new SudokuSolver().FindSolutions(result.GeneratedPuzzle.Puzzle).Multiplicity,
            Is.EqualTo(SolutionMultiplicity.Unique));
    }

    [Test]
    public void Generate_SeededHardFixture_UsesTheRealSolverAndDifficultyAnalyzer()
    {
        var result = new PuzzleGenerator().Generate(new PuzzleGenerationRequest(
            DifficultyLevel.Hard,
            new GenerationBudget(1, TimeSpan.FromSeconds(30)),
            seed: 408863218));

        Assert.That(result.IsSuccess, Is.True, $"{result.UnavailableReason}, attempts={result.Attempts}");
        Assert.That(result.GeneratedPuzzle!.Difficulty.Level, Is.EqualTo(DifficultyLevel.Hard));
        Assert.That(result.GeneratedPuzzle.Puzzle.Givens, Is.Empty);
        Assert.That(new SudokuSolver().FindSolutions(result.GeneratedPuzzle.Puzzle).Multiplicity,
            Is.EqualTo(SolutionMultiplicity.Unique));
    }

    [Test]
    public void Generate_SeededExpertFixture_UsesProductionPartitionerSolverAndAnalyzer()
    {
        int[] solvedGrid =
        [
            7, 8, 4, 2, 3, 6, 1, 5, 9,
            9, 6, 1, 7, 5, 4, 2, 8, 3,
            3, 2, 5, 8, 9, 1, 4, 7, 6,
            4, 1, 3, 5, 2, 9, 7, 6, 8,
            8, 9, 7, 6, 4, 3, 5, 1, 2,
            2, 5, 6, 1, 7, 8, 3, 9, 4,
            1, 4, 8, 3, 6, 5, 9, 2, 7,
            5, 7, 9, 4, 8, 2, 6, 3, 1,
            6, 3, 2, 9, 1, 7, 8, 4, 5
        ];
        var request = new PuzzleGenerationRequest(
            DifficultyLevel.Expert,
            new GenerationBudget(32, TimeSpan.FromSeconds(30)),
            seed: unchecked(2 ^ 0x5F3759DF));
        var generator = new PuzzleGenerator(new FixedSolvedGridGenerator(solvedGrid));

        var result = generator.Generate(request);

        Assert.That(result.IsSuccess, Is.True,
            $"{result.UnavailableReason}, attempts={result.Attempts}");
        var generated = result.GeneratedPuzzle!;
        Assert.That(generated.Puzzle.Givens, Is.Empty);
        Assert.That(generated.Difficulty.Level, Is.EqualTo(DifficultyLevel.Expert));
        Assert.That(generated.Solution.Values, Is.EqualTo(solvedGrid));
        Assert.That(new SudokuSolver().FindSolutions(generated.Puzzle).Multiplicity,
            Is.EqualTo(SolutionMultiplicity.Unique));

        var replay = generator.Generate(request);

        Assert.That(replay.IsSuccess, Is.True);
        Assert.That(CageSignature(replay.GeneratedPuzzle!.Puzzle), Is.EqualTo(CageSignature(generated.Puzzle)));
        Assert.That(replay.GeneratedPuzzle.Solution.Values, Is.EqualTo(generated.Solution.Values));
    }

    [TestCase(DifficultyLevel.Easy)]
    [TestCase(DifficultyLevel.Medium)]
    [TestCase(DifficultyLevel.Hard)]
    [TestCase(DifficultyLevel.Expert)]
    public void Generate_WhenInjectedAnalyzerAcceptsRequestedProfile_PublishesCompletePuzzle(DifficultyLevel level)
    {
        var generator = new PuzzleGenerator(
            cagePartitionGenerator: new SingletonCagePartitionGenerator(),
            difficultyAnalyzer: new FixedDifficultyAnalyzer(level));

        var result = generator.Generate(Request(level, seed: 12345));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.GeneratedPuzzle!.Difficulty.Level, Is.EqualTo(level));
        Assert.That(result.GeneratedPuzzle.Seed, Is.EqualTo(12345));
        Assert.That(result.GeneratedPuzzle.Puzzle.Givens, Is.Empty);
    }

    [Test]
    public void Generate_SameSeedAndOptions_ReplaysSolutionAndCages()
    {
        var request = Request(DifficultyLevel.Easy, seed: 9001);
        var first = new PuzzleGenerator().Generate(request).GeneratedPuzzle!;
        var second = new PuzzleGenerator().Generate(request).GeneratedPuzzle!;

        Assert.That(second.Solution.Values, Is.EqualTo(first.Solution.Values));
        Assert.That(CageSignature(second.Puzzle), Is.EqualTo(CageSignature(first.Puzzle)));
    }

    [Test]
    public void Generate_InvalidCandidateIsDiscardedBeforeSuccessfulPublication()
    {
        var generator = new PuzzleGenerator(
            cagePartitionGenerator: new InvalidThenSingletonPartitionGenerator(),
            difficultyAnalyzer: new FixedDifficultyAnalyzer(DifficultyLevel.Easy));

        var result = generator.Generate(Request(DifficultyLevel.Easy, maxAttempts: 2, seed: 77));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.GeneratedPuzzle!.Attempts, Is.EqualTo(2));
    }

    [Test]
    public void Generate_AttemptBudgetExhausted_ReturnsUnavailableWithoutPuzzle()
    {
        var generator = new PuzzleGenerator(
            cagePartitionGenerator: new AlwaysInvalidPartitionGenerator());

        var result = generator.Generate(Request(DifficultyLevel.Easy, maxAttempts: 1, seed: 9));

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.UnavailableReason, Is.EqualTo(UnavailableReason.AttemptBudgetExhausted));
        Assert.That(result.GeneratedPuzzle, Is.Null);
        Assert.That(result.Attempts, Is.EqualTo(1));
    }

    [Test]
    public void Generate_DifficultyMismatchNeverFallsBackToAnotherLevel()
    {
        var generator = new PuzzleGenerator(
            cagePartitionGenerator: new SingletonCagePartitionGenerator(),
            difficultyAnalyzer: new FixedDifficultyAnalyzer(DifficultyLevel.Easy));

        var result = generator.Generate(Request(DifficultyLevel.Expert, maxAttempts: 1, seed: 4));

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.UnavailableReason, Is.EqualTo(UnavailableReason.AttemptBudgetExhausted));
        Assert.That(result.GeneratedPuzzle, Is.Null);
    }

    [Test]
    public void Generate_TimeBudgetExhausted_ReturnsUnavailable()
    {
        var request = new PuzzleGenerationRequest(
            DifficultyLevel.Easy,
            new GenerationBudget(10, TimeSpan.FromTicks(1)),
            seed: 1);

        var result = new PuzzleGenerator().Generate(request);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.UnavailableReason, Is.EqualTo(UnavailableReason.TimeBudgetExhausted));
        Assert.That(result.GeneratedPuzzle, Is.Null);
    }

    [Test]
    public void Generate_PreCancelledRequestThrowsAndPublishesNothing()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.That(
            () => new PuzzleGenerator().Generate(Request(DifficultyLevel.Easy, seed: 1), cancellation.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public void Generate_CancellationDuringGenerationThrowsWithoutPublishingUnavailable()
    {
        using var cancellation = new CancellationTokenSource();
        var generator = new PuzzleGenerator(
            solvedGridGenerator: new CancellingSolvedGridGenerator(cancellation, waitForLinkedCancellation: false));
        PuzzleGenerationResult? result = null;

        Assert.That(
            () => result = generator.Generate(Request(DifficultyLevel.Easy, seed: 12), cancellation.Token),
            Throws.InstanceOf<OperationCanceledException>());
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Generate_CallerCancellationWinsWhenDeadlineAlsoExpires()
    {
        using var cancellation = new CancellationTokenSource();
        var generator = new PuzzleGenerator(
            solvedGridGenerator: new CancellingSolvedGridGenerator(cancellation, waitForLinkedCancellation: true));
        var request = new PuzzleGenerationRequest(
            DifficultyLevel.Easy,
            new GenerationBudget(1, TimeSpan.FromMilliseconds(50)),
            seed: 13);
        PuzzleGenerationResult? result = null;

        Assert.That(
            () => result = generator.Generate(request, cancellation.Token),
            Throws.InstanceOf<OperationCanceledException>());
        Assert.That(cancellation.IsCancellationRequested, Is.True);
        Assert.That(result, Is.Null);
    }

    [TestCase(DifficultyLevel.Easy)]
    [TestCase(DifficultyLevel.Medium)]
    [TestCase(DifficultyLevel.Hard)]
    [TestCase(DifficultyLevel.Expert)]
    public void CagePartitionGenerator_CoversBoardWithConnectedDistinctDigitCages(DifficultyLevel level)
    {
        var solution = new SolvedGridGenerator().Generate(456);

        var cages = new CagePartitionGenerator().Generate(solution, level, seed: 321);
        var definition = new PuzzleDefinition(
            new Dictionary<PuzzleDefinitionPosition, int>(),
            cages);
        var validation = new PuzzleStructureValidator().Validate(definition);

        Assert.That(validation.IsValid, Is.True, string.Join("; ", validation.Issues.Select(issue => issue.Message)));
        Assert.That(cages.SelectMany(cage => cage.Positions).ToArray(), Has.Length.EqualTo(81));
        Assert.That(cages.SelectMany(cage => cage.Positions).Distinct().ToArray(), Has.Length.EqualTo(81));
        Assert.That(cages.All(cage => cage.TargetSum == cage.Positions.Sum(position =>
            solution[position.Row * 9 + position.Column])), Is.True);
        Assert.That(cages.All(cage => cage.Positions.Select(position =>
            solution[position.Row * 9 + position.Column]).Distinct().Count() == cage.Positions.Count), Is.True);
    }

    [Test]
    public void CagePartitionGenerator_ExpertSingletonPositionsVaryBySeedAndReplayDeterministically()
    {
        var solution = new SolvedGridGenerator().Generate(456);
        var generator = new CagePartitionGenerator();

        var first = generator.Generate(solution, DifficultyLevel.Expert, seed: 321);
        var replay = generator.Generate(solution, DifficultyLevel.Expert, seed: 321);
        var otherSeed = generator.Generate(solution, DifficultyLevel.Expert, seed: 322);

        Assert.That(CagePartitionSignature(replay), Is.EqualTo(CagePartitionSignature(first)));
        Assert.That(SingletonIndexes(replay), Is.EqualTo(SingletonIndexes(first)));
        Assert.That(SingletonIndexes(otherSeed), Is.Not.EqualTo(SingletonIndexes(first)));
    }

    [Test]
    public async Task GenerateUseCase_RunsSeededRequestAndReturnsCompleteSuccess()
    {
        var generator = new PuzzleGenerator(
            cagePartitionGenerator: new SingletonCagePartitionGenerator(),
            difficultyAnalyzer: new FixedDifficultyAnalyzer(DifficultyLevel.Easy));

        var result = await new GeneratePuzzleUseCase(generator)
            .ExecuteAsync(Request(DifficultyLevel.Easy, seed: 56));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.GeneratedPuzzle!.Puzzle.Givens, Is.Empty);
        Assert.That(result.GeneratedPuzzle.Solution.Values, Has.Count.EqualTo(81));
    }

    [TestCase(0, 1000)]
    [TestCase(1, 0)]
    public void GenerationBudget_RequiresPositiveAttemptAndTimeLimits(int attempts, int milliseconds)
    {
        Assert.That(() => new GenerationBudget(attempts, TimeSpan.FromMilliseconds(milliseconds)),
            Throws.InstanceOf<ArgumentException>());
    }

    private static PuzzleGenerationRequest Request(
        DifficultyLevel level,
        int maxAttempts = 4,
        int? seed = null)
    {
        return new PuzzleGenerationRequest(level, new GenerationBudget(maxAttempts, TimeSpan.FromSeconds(30)), seed);
    }

    private static string CageSignature(ValidatedPuzzle puzzle)
    {
        return string.Join("|", puzzle.Cages.Select(cage =>
            $"{cage.TargetSum}:{string.Join(',', cage.Positions.Select(position => $"{position.Row}-{position.Column}"))}"));
    }

    private static string CagePartitionSignature(IEnumerable<CageDefinition> cages)
    {
        return string.Join("|", cages.Select(cage =>
            $"{cage.TargetSum}:{string.Join(',', cage.Positions.Select(position => $"{position.Row}-{position.Column}"))}"));
    }

    private static int[] SingletonIndexes(IEnumerable<CageDefinition> cages)
    {
        return cages
            .Where(cage => cage.Positions.Count == 1)
            .Select(cage => cage.Positions[0].Row * 9 + cage.Positions[0].Column)
            .Order()
            .ToArray();
    }

    private sealed class FixedDifficultyAnalyzer(DifficultyLevel level) : IDifficultyAnalyzer
    {
        public DifficultyAnalysisResult Analyze(ValidatedPuzzle puzzle, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new DifficultyAnalysisResult(
                DifficultyAnalysisStatus.Classified,
                level,
                DifficultyProfileCatalog.CurrentVersion,
                Array.Empty<LogicalTechniqueId>());
        }
    }

    private sealed class FixedSolvedGridGenerator(IReadOnlyList<int> values) : ISolvedGridGenerator
    {
        public IReadOnlyList<int> Generate(int seed, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return values.ToArray();
        }
    }

    private sealed class CancellingSolvedGridGenerator(
        CancellationTokenSource callerCancellation,
        bool waitForLinkedCancellation) : ISolvedGridGenerator
    {
        public IReadOnlyList<int> Generate(int seed, CancellationToken cancellationToken = default)
        {
            if (waitForLinkedCancellation)
            {
                cancellationToken.WaitHandle.WaitOne();
            }

            callerCancellation.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
            return new SolvedGridGenerator().Generate(seed, cancellationToken);
        }
    }

    private sealed class SingletonCagePartitionGenerator : ICagePartitionGenerator
    {
        public IReadOnlyList<CageDefinition> Generate(
            IReadOnlyList<int> solution,
            DifficultyLevel difficulty,
            int seed,
            CancellationToken cancellationToken = default)
        {
            return (from row in Enumerable.Range(0, 9)
                    from column in Enumerable.Range(0, 9)
                    let index = row * 9 + column
                    select new CageDefinition(solution[index], [new PuzzleDefinitionPosition(row, column)]))
                .ToArray();
        }
    }

    private sealed class InvalidThenSingletonPartitionGenerator : ICagePartitionGenerator
    {
        private readonly SingletonCagePartitionGenerator _valid = new();
        private int _calls;

        public IReadOnlyList<CageDefinition> Generate(
            IReadOnlyList<int> solution,
            DifficultyLevel difficulty,
            int seed,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _calls) == 1)
            {
                return Array.Empty<CageDefinition>();
            }

            return _valid.Generate(solution, difficulty, seed, cancellationToken);
        }
    }

    private sealed class AlwaysInvalidPartitionGenerator : ICagePartitionGenerator
    {
        public IReadOnlyList<CageDefinition> Generate(
            IReadOnlyList<int> solution,
            DifficultyLevel difficulty,
            int seed,
            CancellationToken cancellationToken = default) => Array.Empty<CageDefinition>();
    }
}
