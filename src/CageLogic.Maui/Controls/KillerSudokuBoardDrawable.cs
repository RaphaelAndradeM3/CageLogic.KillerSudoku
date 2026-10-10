using CageLogic.Application.GameSessions;
using CageLogic.Domain.Board;
using Microsoft.Maui.Graphics;

namespace CageLogic.Maui.Controls;

public sealed class KillerSudokuBoardDrawable : IDrawable
{
	private GameSessionViewState? _viewState;
	private bool _isDarkTheme;

	public void Update(GameSessionViewState viewState, bool isDarkTheme)
	{
		ArgumentNullException.ThrowIfNull(viewState);
		_viewState = viewState;
		_isDarkTheme = isDarkTheme;
	}

	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		ArgumentNullException.ThrowIfNull(canvas);
		var geometry = BoardGeometry.Fit(dirtyRect.Width, dirtyRect.Height);
		if (geometry.CellSize <= 0)
			return;

		canvas.SaveState();
		canvas.FillColor = _isDarkTheme ? Color.FromArgb("#202A38") : Color.FromArgb("#FFFCF6");
		canvas.FillRectangle(geometry.Bounds);

		if (_viewState is not null)
			DrawCellValuesAndSelection(canvas, geometry, _viewState, _isDarkTheme);

		DrawGrid(canvas, geometry, _isDarkTheme);
		if (_viewState is not null)
			DrawCageBoundariesAndTargets(canvas, geometry, _viewState, _isDarkTheme);

		canvas.RestoreState();
	}

	private static void DrawCellValuesAndSelection(ICanvas canvas, BoardGeometry geometry, GameSessionViewState viewState, bool isDarkTheme)
	{
		var selectedPosition = viewState.Cells.FirstOrDefault(static cell => cell.IsSelected)?.Position;
		var satisfiedCagePositions = viewState.Cages
			.Where(static cage => cage.IsSatisfied)
			.SelectMany(static cage => cage.Positions)
			.ToHashSet();

		foreach (var cell in viewState.Cells)
		{
			var bounds = geometry.GetCellBounds(cell.Position);
			if (cell.HasConflict)
			{
				canvas.FillColor = isDarkTheme ? Color.FromArgb("#612F35") : Color.FromArgb("#FFE0DE");
				canvas.FillRectangle(bounds);
			}
			else if (cell.IsSelected)
			{
				canvas.FillColor = isDarkTheme ? Color.FromArgb("#2A4B67") : Color.FromArgb("#DFECFA");
				canvas.FillRectangle(bounds);
			}
			else if (satisfiedCagePositions.Contains(cell.Position))
			{
				canvas.FillColor = isDarkTheme ? Color.FromArgb("#244D73") : Color.FromArgb("#C5DDF4");
				canvas.FillRectangle(bounds);
			}
			else if (selectedPosition is { } selected && SharesSelectedRegion(cell.Position, selected))
			{
				canvas.FillColor = isDarkTheme ? Color.FromArgb("#293747") : Color.FromArgb("#EEF4FA");
				canvas.FillRectangle(bounds);
			}
			DrawHintRoles(canvas, bounds, geometry.CellSize, cell);

			if (cell.Value is int value)
			{
				canvas.Font = Microsoft.Maui.Graphics.Font.Default;
				canvas.FontSize = geometry.CellSize * 0.52f;
				canvas.FontColor = cell.IsGiven
					? (isDarkTheme ? Color.FromArgb("#F1F4F8") : Color.FromArgb("#24364B"))
					: (isDarkTheme ? Color.FromArgb("#83B6FF") : Color.FromArgb("#245DA0"));
				canvas.DrawString(value.ToString(), bounds, HorizontalAlignment.Center, VerticalAlignment.Center);
				continue;
			}

			DrawNotes(canvas, bounds, geometry.CellSize, cell.Notes, isDarkTheme);
		}
	}

	private static bool SharesSelectedRegion(CellPosition position, CellPosition selected)
	{
		var sharesBlock = position.Row / 3 == selected.Row / 3
			&& position.Column / 3 == selected.Column / 3;

		return position.Row == selected.Row
			|| position.Column == selected.Column
			|| sharesBlock;
	}

	private static void DrawNotes(ICanvas canvas, RectF bounds, float cellSize, IReadOnlyList<int> notes, bool isDarkTheme)
	{
		if (notes.Count == 0)
			return;
		canvas.Font = Microsoft.Maui.Graphics.Font.Default;
		canvas.FontSize = Math.Max(6f, cellSize * 0.17f);
		canvas.FontColor = isDarkTheme ? Color.FromArgb("#C4CCD8") : Color.FromArgb("#526174");
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

	private static void DrawGrid(ICanvas canvas, BoardGeometry geometry, bool isDarkTheme)
	{
		var bounds = geometry.Bounds;
		canvas.StrokeDashPattern = null;
		for (var index = 0; index <= BoardGeometry.BoardOrder; index++)
		{
			var majorLine = index % 3 == 0;
			canvas.StrokeColor = majorLine
				? (isDarkTheme ? Color.FromArgb("#B4C0D0") : Color.FromArgb("#41536A"))
				: (isDarkTheme ? Color.FromArgb("#637083") : Color.FromArgb("#C7D0DA"));
			canvas.StrokeSize = majorLine ? 2.4f : 0.8f;
			var offset = index * geometry.CellSize;
			canvas.DrawLine(bounds.Left + offset, bounds.Top, bounds.Left + offset, bounds.Bottom);
			canvas.DrawLine(bounds.Left, bounds.Top + offset, bounds.Right, bounds.Top + offset);
		}
	}

	private static void DrawCageBoundariesAndTargets(ICanvas canvas, BoardGeometry geometry, GameSessionViewState viewState, bool isDarkTheme)
	{
		canvas.StrokeColor = isDarkTheme ? Color.FromArgb("#899AB0") : Color.FromArgb("#70849A");
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
			canvas.FontColor = isDarkTheme ? Color.FromArgb("#F2F4F7") : Color.FromArgb("#344A60");
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
