using System.Diagnostics;
using CageLogic.Application.Difficulty;
using CageLogic.Application.Generation;
using CageLogic.Domain.Cages;

namespace CageLogic.Application.Tests.Generation;

public sealed class PuzzleGeneratorPerformanceTests
{
    [Test, Explicit("Opt-in fixed-seed Release benchmark; run on each supported device before selecting production budgets.")]
    public void FixedSeedCorpus_RecordsStageLatencyRejectionsAndCancellationResponse()
    {
        var requestLatencies = new List<TimeSpan>();
        var measurements = new List<PuzzleGenerationStageMeasurement>();
        var rejections = new Dictionary<PuzzleGenerationRejectionReason, int>();
        var attemptCounts = new List<int>();

        foreach (var seed in Enumerable.Range(20261001, 10))
        {
            var observer = new RecordingObserver(measurements, rejections);
            var request = new PuzzleGenerationRequest(
                DifficultyLevel.Easy,
                new GenerationBudget(1, TimeSpan.FromSeconds(30)),
                seed);
            var stopwatch = Stopwatch.StartNew();
            var result = new PuzzleGenerator(observer: observer).Generate(request);
            stopwatch.Stop();

            Assert.That(result.IsSuccess, Is.True, $"Seed {seed}: {result.UnavailableReason}");
            requestLatencies.Add(stopwatch.Elapsed);
            attemptCounts.Add(result.Attempts);
        }

        var rejectionObserver = new RecordingObserver(measurements, rejections);
        var rejected = new PuzzleGenerator(
                cagePartitionGenerator: new InvalidPartitionGenerator(),
                observer: rejectionObserver)
            .Generate(new PuzzleGenerationRequest(
                DifficultyLevel.Easy,
                new GenerationBudget(3, TimeSpan.FromSeconds(30)),
                seed: 42));
        Assert.That(rejected.UnavailableReason, Is.EqualTo(UnavailableReason.AttemptBudgetExhausted));
        attemptCounts.Add(rejected.Attempts);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var cancellationWatch = Stopwatch.StartNew();
        Assert.That(
            () => new PuzzleGenerator().Generate(
                new PuzzleGenerationRequest(DifficultyLevel.Easy,
                    new GenerationBudget(1, TimeSpan.FromSeconds(30)), seed: 7),
                cancellation.Token),
            Throws.InstanceOf<OperationCanceledException>());
        cancellationWatch.Stop();

        TestContext.Progress.WriteLine($"Request latency ms p50={Percentile(requestLatencies, 0.50):F2}, p95={Percentile(requestLatencies, 0.95):F2}, max={requestLatencies.Max().TotalMilliseconds:F2}");
        TestContext.Progress.WriteLine($"Attempt counts: {string.Join(',', attemptCounts)}");
        foreach (var stage in Enum.GetValues<PuzzleGenerationStage>())
        {
            var durations = measurements.Where(measurement => measurement.Stage == stage)
                .Select(measurement => measurement.Elapsed).ToArray();
            if (durations.Length > 0)
            {
                TestContext.Progress.WriteLine(
                    $"{stage} ms p50={Percentile(durations, 0.50):F2}, p95={Percentile(durations, 0.95):F2}, max={durations.Max().TotalMilliseconds:F2}");
            }
        }

        TestContext.Progress.WriteLine($"Rejection reasons: {string.Join(", ", rejections.Select(pair => $"{pair.Key}={pair.Value}"))}");
        TestContext.Progress.WriteLine($"Pre-cancel response ms: {cancellationWatch.Elapsed.TotalMilliseconds:F2}");

        var hardMeasurements = new List<PuzzleGenerationStageMeasurement>();
        var hardObserver = new RecordingObserver(hardMeasurements, new Dictionary<PuzzleGenerationRejectionReason, int>());
        var hardWatch = Stopwatch.StartNew();
        var hardResult = new PuzzleGenerator(observer: hardObserver).Generate(new PuzzleGenerationRequest(
            DifficultyLevel.Hard,
            new GenerationBudget(1, TimeSpan.FromSeconds(30)),
            seed: 408863218));
        hardWatch.Stop();
        Assert.That(hardResult.IsSuccess, Is.True, $"Hard fixture: {hardResult.UnavailableReason}");
        TestContext.Progress.WriteLine(
            $"Hard fixture request ms={hardWatch.Elapsed.TotalMilliseconds:F2}, attempts={hardResult.Attempts}");
        WriteStageMeasurements("Hard", hardMeasurements);

        var expertMeasurements = new List<PuzzleGenerationStageMeasurement>();
        var expertObserver = new RecordingObserver(expertMeasurements, new Dictionary<PuzzleGenerationRejectionReason, int>());
        var expertWatch = Stopwatch.StartNew();
        var expertRequest = new PuzzleGenerationRequest(
            DifficultyLevel.Expert,
            new GenerationBudget(32, TimeSpan.FromSeconds(30)),
            seed: unchecked(2 ^ 0x5F3759DF));
        var expertResult = new PuzzleGenerator(
                new FixedSolvedGridGenerator(ExpertSolvedGrid),
                observer: expertObserver)
            .Generate(expertRequest);
        expertWatch.Stop();
        Assert.That(expertResult.IsSuccess, Is.True, $"Expert fixture: {expertResult.UnavailableReason}");
        TestContext.Progress.WriteLine(
            $"Expert fixture request ms={expertWatch.Elapsed.TotalMilliseconds:F2}, attempts={expertResult.Attempts}");
        WriteStageMeasurements("Expert", expertMeasurements);
    }

    private static void WriteStageMeasurements(
        string difficulty,
        IEnumerable<PuzzleGenerationStageMeasurement> measurements)
    {
        foreach (var measurement in measurements)
        {
            TestContext.Progress.WriteLine(
                $"{difficulty} {measurement.Stage} ms={measurement.Elapsed.TotalMilliseconds:F2}");
        }
    }

    private static double Percentile(IReadOnlyCollection<TimeSpan> values, double percentile)
    {
        var ordered = values.Select(value => value.TotalMilliseconds).Order().ToArray();
        var index = Math.Clamp((int)Math.Ceiling(percentile * ordered.Length) - 1, 0, ordered.Length - 1);
        return ordered[index];
    }

    private sealed class RecordingObserver(
        ICollection<PuzzleGenerationStageMeasurement> measurements,
        IDictionary<PuzzleGenerationRejectionReason, int> rejections) : IPuzzleGenerationObserver
    {
        public void StageCompleted(PuzzleGenerationStageMeasurement measurement) => measurements.Add(measurement);

        public void CandidateRejected(int attempt, PuzzleGenerationRejectionReason reason)
        {
            _ = attempt;
            rejections[reason] = rejections.TryGetValue(reason, out var count) ? count + 1 : 1;
        }
    }

    private static int[] ExpertSolvedGrid =>
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

    private sealed class FixedSolvedGridGenerator(IReadOnlyList<int> values) : ISolvedGridGenerator
    {
        public IReadOnlyList<int> Generate(int seed, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return values.ToArray();
        }
    }

    private sealed class InvalidPartitionGenerator : ICagePartitionGenerator
    {
        public IReadOnlyList<CageDefinition> Generate(
            IReadOnlyList<int> solution,
            DifficultyLevel difficulty,
            int seed,
            CancellationToken cancellationToken = default) => Array.Empty<CageDefinition>();
    }

}
