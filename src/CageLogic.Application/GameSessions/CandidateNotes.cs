using System.Collections.ObjectModel;
using CageLogic.Domain.Board;

namespace CageLogic.Application.GameSessions;

/// <summary>An immutable map of editable empty cells to sorted candidate notes.</summary>
public sealed class CandidateNotes
{
	private static readonly IReadOnlyList<int> NoNotes = Array.Empty<int>();
	private readonly IReadOnlyDictionary<CellPosition, IReadOnlyList<int>> _notes;

	private CandidateNotes(IDictionary<CellPosition, IEnumerable<int>> notes)
	{
		var snapshot = notes
			.Where(pair => pair.Value.Any())
			.ToDictionary(
				pair => pair.Key,
				pair => (IReadOnlyList<int>)Array.AsReadOnly(pair.Value.Distinct().Order().ToArray()));
		_notes = new ReadOnlyDictionary<CellPosition, IReadOnlyList<int>>(snapshot);
	}

	public static CandidateNotes Empty { get; } = new(new Dictionary<CellPosition, IEnumerable<int>>());

	public IReadOnlyDictionary<CellPosition, IReadOnlyList<int>> Notes => _notes;

	public IReadOnlyList<int> For(CellPosition position) => _notes.TryGetValue(position, out var notes) ? notes : NoNotes;

	public bool Toggle(CellPosition position, int digit, out CandidateNotes updated)
	{
		if (digit is < 1 or > 9)
		{
			updated = this;
			return false;
		}

		var existing = For(position).ToHashSet();
		if (!existing.Add(digit))
			existing.Remove(digit);

		return Set(position, existing, out updated);
	}

	public bool Clear(CellPosition position, out CandidateNotes updated)
	{
		return Set(position, Array.Empty<int>(), out updated);
	}

	public bool Set(CellPosition position, IEnumerable<int> digits, out CandidateNotes updated)
	{
		ArgumentNullException.ThrowIfNull(digits);
		var sorted = digits.Distinct().Order().ToArray();
		if (sorted.Any(digit => digit is < 1 or > 9))
			throw new ArgumentOutOfRangeException(nameof(digits), "Candidate notes must use digits from 1 through 9.");
		if (For(position).SequenceEqual(sorted))
		{
			updated = this;
			return false;
		}

		var copy = _notes.ToDictionary(pair => pair.Key, pair => (IEnumerable<int>)pair.Value);
		if (sorted.Length == 0)
			copy.Remove(position);
		else
			copy[position] = sorted;
		updated = new CandidateNotes(copy);
		return true;
	}

	public bool ContentEquals(CandidateNotes other)
	{
		if (ReferenceEquals(this, other))
			return true;
		if (_notes.Count != other._notes.Count)
			return false;
		return _notes.All(pair => other._notes.TryGetValue(pair.Key, out var values) && pair.Value.SequenceEqual(values));
	}

	internal static CandidateNotes Replace(IEnumerable<KeyValuePair<CellPosition, IEnumerable<int>>> notes)
	{
		return new CandidateNotes(notes.ToDictionary(pair => pair.Key, pair => pair.Value));
	}
}
