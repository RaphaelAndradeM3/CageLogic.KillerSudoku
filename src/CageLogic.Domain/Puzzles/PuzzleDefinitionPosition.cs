namespace CageLogic.Domain.Puzzles;

/// <summary>A raw coordinate that has not yet been checked against the 9 by 9 board.</summary>
public readonly record struct PuzzleDefinitionPosition(int Row, int Column);
