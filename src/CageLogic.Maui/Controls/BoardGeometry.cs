using CageLogic.Domain.Board;
using Microsoft.Maui.Graphics;

namespace CageLogic.Maui.Controls;

/// <summary>Shared square board geometry used by both drawing and pointer hit testing.</summary>
public readonly record struct BoardGeometry(float Left, float Top, float CellSize)
{
	public const int BoardOrder = 9;

	public RectF Bounds => new(Left, Top, CellSize * BoardOrder, CellSize * BoardOrder);

	public static BoardGeometry Fit(float width, float height, float inset = 2f)
	{
		if (!float.IsFinite(width) || !float.IsFinite(height) || !float.IsFinite(inset) || inset < 0)
			return default;

		var side = Math.Max(0, Math.Min(width, height) - (2 * inset));
		var cellSize = side / BoardOrder;
		return new BoardGeometry((width - side) / 2, (height - side) / 2, cellSize);
	}

	public RectF GetCellBounds(CellPosition position)
	{
		return new RectF(
			Left + (position.Column * CellSize),
			Top + (position.Row * CellSize),
			CellSize,
			CellSize);
	}

	public bool TryHitTest(float x, float y, out CellPosition position)
	{
		position = default;
		var bounds = Bounds;
		if (CellSize <= 0 || x < bounds.Left || y < bounds.Top || x >= bounds.Right || y >= bounds.Bottom)
			return false;

		var row = (int)((y - Top) / CellSize);
		var column = (int)((x - Left) / CellSize);
		if (row is < 0 or >= BoardOrder || column is < 0 or >= BoardOrder)
			return false;

		position = new CellPosition(row, column);
		return true;
	}
}
