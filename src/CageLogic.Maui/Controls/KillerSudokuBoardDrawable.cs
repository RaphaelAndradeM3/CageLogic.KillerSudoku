using CageLogic.Application.GameSessions;
using CageLogic.Domain.Board;
using Microsoft.Maui.Graphics;

namespace CageLogic.Maui.Controls;

public sealed class KillerSudokuBoardDrawable : IDrawable
{
	private static readonly Color BoardBackground = Color.FromArgb("#FFFCF6");
	private static readonly Color GridColor = Color.FromArgb("#C7D0DA");
	private static readonly Color BlockColor = Color.FromArgb("#41536A");
	private static readonly Color CageColor = Color.FromArgb("#70849A");
	private GameSessionViewState? _viewState;

	public void Update(GameSessionViewState viewState)
	{
		ArgumentNullException.ThrowIfNull(viewState);
		_viewState = viewState;
	}

	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		ArgumentNullException.ThrowIfNull(canvas);
		var geometry = BoardGeometry.Fit(dirtyRect.Width, dirtyRect.Height);
		if (geometry.CellSize <= 0)
			return;

		canvas.SaveState();
		canvas.FillColor = BoardBackground;
		canvas.FillRectangle(geometry.Bounds);

		if (_viewState is not null)
			DrawCellValuesAndSelection(canvas, geometry, _viewState);

		DrawGrid(canvas, geometry);
		if (_viewState is not null)
			DrawCageBoundariesAndTargets(canvas, geometry, _viewState);

		canvas.RestoreState();
	}

	private static void DrawCellValuesAndSelection(ICanvas canvas, BoardGeometry geometry, GameSessionViewState viewState)
	{
		foreach (var cell in viewState.Cells)
		{
			var bounds = geometry.GetCellBounds(cell.Position);
			if (cell.HasConflict)
			{
				canvas.FillColor = Color.FromArgb("#FFE0DE");
				canvas.FillRectangle(bounds);
			}
			else if (cell.IsSelected)
			{
				canvas.FillColor = Color.FromArgb("#DFECFA");
				canvas.FillRectangle(bounds);
			}
			DrawHintRoles(canvas, bounds, geometry.CellSize, cell);

			if (cell.Value is int value)
			{
				canvas.Font = Microsoft.Maui.Graphics.Font.Default;
				canvas.FontSize = geometry.CellSize * 0.52f;
				canvas.FontColor = cell.IsGiven ? Color.FromArgb("#24364B") : Color.FromArgb("#245DA0");
				canvas.DrawString(value.ToString(), bounds, HorizontalAlignment.Center, VerticalAlignment.Center);
				continue;
			}

			DrawNotes(canvas, bounds, geometry.CellSize, cell.Notes);
		}
	}

	private static void DrawNotes(ICanvas canvas, RectF bounds, float cellSize, IReadOnlyList<int> notes)
	{
		if (notes.Count == 0)
			return;
		canvas.Font = Microsoft.Maui.Graphics.Font.Default;
		canvas.FontSize = Math.Max(6f, cellSize * 0.17f);
		canvas.FontColor = Color.FromArgb("#526174");
		var slotSize = cellSize / 3f;
		foreach (var digit in notes)
		{
			var index = digit - 1;
			var noteBounds = new RectF(
				bounds.Left + (index % 3) * slotSize,
				bounds.Top + (index / 3) * slotSize,
				slotSize,
				slotSize);
			canvas.DrawString(digit.ToString(), noteBounds, HorizontalAlignment.Center, VerticalAlignment.Center);
		}
	}

	private static void DrawHintRoles(ICanvas canvas, RectF bounds, float cellSize, GameSessionCellViewState cell)
	{
		if (cell.HintRoles.Count == 0)
			return;

		canvas.StrokeSize = Math.Max(1.2f, cellSize * 0.045f);
		canvas.StrokeDashPattern = null;
		canvas.StrokeColor = Color.FromArgb("#7A4D9C");
		if (cell.HintRoles.Contains(CageLogic.Application.Hints.HintHighlightRole.Pattern))
			canvas.DrawRectangle(bounds.Inflate(-cellSize * 0.11f, -cellSize * 0.11f));
		if (cell.HintRoles.Contains(CageLogic.Application.Hints.HintHighlightRole.Scope))
		{
			canvas.StrokeDashPattern = [2f, 2f];
			canvas.StrokeColor = Color.FromArgb("#2D7191");
			canvas.DrawRectangle(bounds.Inflate(-cellSize * 0.17f, -cellSize * 0.17f));
		}
		if (cell.HintRoles.Contains(CageLogic.Application.Hints.HintHighlightRole.Target))
		{
			canvas.StrokeDashPattern = null;
			canvas.StrokeSize = Math.Max(2f, cellSize * 0.07f);
			canvas.StrokeColor = Color.FromArgb("#35834C");
			canvas.DrawRectangle(bounds.Inflate(-cellSize * 0.06f, -cellSize * 0.06f));
		}
		if (cell.HintRoles.Contains(CageLogic.Application.Hints.HintHighlightRole.Affected))
		{
			canvas.StrokeDashPattern = [3f, 2f];
			canvas.StrokeSize = Math.Max(1.2f, cellSize * 0.045f);
			canvas.StrokeColor = Color.FromArgb("#B66728");
			canvas.DrawRectangle(bounds.Inflate(-cellSize * 0.23f, -cellSize * 0.23f));
		}
		canvas.StrokeDashPattern = null;
	}

	private static void DrawGrid(ICanvas canvas, BoardGeometry geometry)
	{
		var bounds = geometry.Bounds;
		canvas.StrokeDashPattern = null;
		for (var index = 0; index <= BoardGeometry.BoardOrder; index++)
		{
			var majorLine = index % 3 == 0;
			canvas.StrokeColor = majorLine ? BlockColor : GridColor;
			canvas.StrokeSize = majorLine ? 2.4f : 0.8f;
			var offset = index * geometry.CellSize;
			canvas.DrawLine(bounds.Left + offset, bounds.Top, bounds.Left + offset, bounds.Bottom);
			canvas.DrawLine(bounds.Left, bounds.Top + offset, bounds.Right, bounds.Top + offset);
		}
	}

	private static void DrawCageBoundariesAndTargets(ICanvas canvas, BoardGeometry geometry, GameSessionViewState viewState)
	{
		canvas.StrokeColor = CageColor;
		canvas.StrokeSize = Math.Max(1f, geometry.CellSize * 0.035f);
		canvas.StrokeDashPattern = [4f, 2.5f];
		foreach (var cage in viewState.Cages)
		{
			var positions = cage.Positions.ToHashSet();
			foreach (var position in cage.Positions)
			{
				var cell = geometry.GetCellBounds(position);
				if (!Contains(positions, position.Row - 1, position.Column))
					canvas.DrawLine(cell.Left, cell.Top, cell.Right, cell.Top);
				if (!Contains(positions, position.Row + 1, position.Column))
					canvas.DrawLine(cell.Left, cell.Bottom, cell.Right, cell.Bottom);
				if (!Contains(positions, position.Row, position.Column - 1))
					canvas.DrawLine(cell.Left, cell.Top, cell.Left, cell.Bottom);
				if (!Contains(positions, position.Row, position.Column + 1))
					canvas.DrawLine(cell.Right, cell.Top, cell.Right, cell.Bottom);
			}

			var labelCell = cage.Positions.MinBy(position => (position.Row * 9) + position.Column);
			var labelBounds = geometry.GetCellBounds(labelCell);
			canvas.Font = Microsoft.Maui.Graphics.Font.Default;
			canvas.FontSize = Math.Max(7f, geometry.CellSize * 0.19f);
			canvas.FontColor = Color.FromArgb("#344A60");
			canvas.DrawString(
				cage.TargetSum.ToString(),
				new RectF(labelBounds.Left + 2, labelBounds.Top + 1, geometry.CellSize * 0.48f, geometry.CellSize * 0.25f),
				HorizontalAlignment.Left,
				VerticalAlignment.Top);
		}
		canvas.StrokeDashPattern = null;
	}

	private static bool Contains(IReadOnlySet<CellPosition> positions, int row, int column)
	{
		return row is >= 0 and < 9 && column is >= 0 and < 9 && positions.Contains(new CellPosition(row, column));
	}
}
