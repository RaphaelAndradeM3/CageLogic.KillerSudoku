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

    private sealed class InvalidPartitionGenerator : ICagePartitionGenerator
    {
        public IReadOnlyList<CageDefinition> Generate(
            IReadOnlyList<int> solution,
            DifficultyLevel difficulty,
            int seed,
            CancellationToken cancellationToken = default) => Array.Empty<CageDefinition>();
    }

}
