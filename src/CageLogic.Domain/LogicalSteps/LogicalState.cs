using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using CageLogic.Domain.Board;
using CageLogic.Domain.Candidates;
using CageLogic.Domain.Cages;

namespace CageLogic.Domain.LogicalSteps;

/// <summary>Immutable board state and remaining candidates for a logical deduction trace.</summary>
public sealed class LogicalState
{
    private readonly ReadOnlyCollection<CandidateSet> _candidateSets;
    private readonly Dictionary<CellPosition, CandidateSet> _candidateByPosition;
    private readonly ConcurrentDictionary<Cage, CageAssignmentSummary> _cageAssignments = new();
    private readonly ConcurrentDictionary<Cage, object> _cageAssignmentGates = new();

    public LogicalState(SudokuBoard board, IEnumerable<CandidateSet> candidates)
        : this(board, candidates, new CandidateCalculator())
    {
    }

    private LogicalState(SudokuBoard board, IEnumerable<CandidateSet> candidates, CandidateCalculator calculator)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(calculator);

        Board = board;
        var candidateArray = candidates
            .OrderBy(candidate => candidate.Position.Row * 9 + candidate.Position.Column)
            .ToArray();
        if (candidateArray.Select(candidate => candidate.Position).Distinct().Count() != candidateArray.Length)
        {
            throw new ArgumentException("There must be exactly one candidate set per empty position.", nameof(candidates));
        }

        var legalCandidates = calculator.Calculate(board).ToDictionary(candidate => candidate.Position);
        if (!candidateArray.Select(candidate => candidate.Position).ToHashSet().SetEquals(legalCandidates.Keys))
        {
            throw new ArgumentException("Candidate sets must cover exactly the empty board positions.", nameof(candidates));
        }

        foreach (var candidate in candidateArray)
        {
            if (!candidate.Values.IsSubsetOf(legalCandidates[candidate.Position].Values))
            {
                throw new ArgumentException("Candidates cannot include values ruled out by the board.", nameof(candidates));
            }
        }

        _candidateSets = Array.AsReadOnly(candidateArray);
        _candidateByPosition = candidateArray.ToDictionary(candidate => candidate.Position);
    }

    public SudokuBoard Board { get; }

    public IReadOnlyList<CandidateSet> Candidates => _candidateSets;

    public CandidateSet GetCandidates(CellPosition position) => _candidateByPosition[position];

    public static LogicalState Create(SudokuBoard board)
    {
        ArgumentNullException.ThrowIfNull(board);
        return new LogicalState(board, new CandidateCalculator().Calculate(board));
    }

    public LogicalState Apply(LogicalStep step)
    {
        ArgumentNullException.ThrowIfNull(step);

        var remaining = _candidateByPosition.ToDictionary(pair => pair.Key, pair => pair.Value.Values.ToHashSet());
        foreach (var elimination in step.Eliminations)
        {
            if (!remaining.TryGetValue(elimination.Position, out var values) || !values.Remove(elimination.Value))
            {
                throw new ArgumentException("A step can eliminate only a candidate currently present in the state.", nameof(step));
            }
        }

        var board = Board;
        if (step.Placement is { } placement)
        {
            if (!remaining.TryGetValue(placement.Position, out var values) || !values.Contains(placement.Value))
            {
                throw new ArgumentException("A step can place only a candidate currently present in the state.", nameof(step));
            }

            board = Board.WithPlayerValue(placement.Position, placement.Value);
        }

        var recalculated = new CandidateCalculator().Calculate(board);
        var candidates = recalculated.Select(candidate => new CandidateSet(
            candidate.Position,
            candidate.Values.Intersect(remaining[candidate.Position]))).ToArray();
        return new LogicalState(board, candidates);
    }

    internal CageAssignmentSummary GetCageAssignments(Cage cage, CancellationToken cancellationToken)
    {
        if (_cageAssignments.TryGetValue(cage, out var cached))
        {
            return cached;
        }

        var gate = _cageAssignmentGates.GetOrAdd(cage, static _ => new object());
        while (!Monitor.TryEnter(gate, millisecondsTimeout: 25))
        {
            cancellationToken.ThrowIfCancellationRequested();
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_cageAssignments.TryGetValue(cage, out cached))
            {
                return cached;
            }

            var summary = CageAssignmentSummary.Create(this, cage, cancellationToken);
            _cageAssignments.TryAdd(cage, summary);
            return summary;
        }
        finally
        {
            Monitor.Exit(gate);
        }
    }
}
