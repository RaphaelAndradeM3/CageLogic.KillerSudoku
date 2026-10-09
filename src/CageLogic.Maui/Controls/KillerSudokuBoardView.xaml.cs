using CageLogic.Application.GameSessions;
using CageLogic.Domain.Board;

namespace CageLogic.Maui.Controls;

public partial class KillerSudokuBoardView : ContentView
{
	private readonly KillerSudokuBoardDrawable _drawable = new();
	private readonly Button[,] _cellButtons = new Button[BoardGeometry.BoardOrder, BoardGeometry.BoardOrder];

	public KillerSudokuBoardView()
	{
		InitializeComponent();
		BoardCanvas.Drawable = _drawable;
		BoardCanvas.StartInteraction += OnStartInteraction;
		CreateAccessibleCells();
		SizeChanged += (_, _) =>
		{
			var boardSize = Math.Min(Width, 560);
			if (boardSize > 0 && Math.Abs(HeightRequest - boardSize) > 0.5)
				HeightRequest = boardSize;
		};
	}

	public event EventHandler<BoardCellSelectedEventArgs>? CellSelected;

	public void SetViewState(GameSessionViewState viewState)
	{
		ArgumentNullException.ThrowIfNull(viewState);
		_drawable.Update(viewState);
		BoardCanvas.Invalidate();
		foreach (var cell in viewState.Cells)
		{
			var button = _cellButtons[cell.Position.Row, cell.Position.Column];
			SemanticProperties.SetDescription(button, AccessibleCellPeer.Describe(cell));
			SemanticProperties.SetHint(button, "Toque duas vezes para selecionar esta célula.");
		}
	}

	private void CreateAccessibleCells()
	{
		for (var index = 0; index < BoardGeometry.BoardOrder; index++)
		{
			AccessibleCells.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
			AccessibleCells.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
		}

		for (var row = 0; row < BoardGeometry.BoardOrder; row++)
		{
			for (var column = 0; column < BoardGeometry.BoardOrder; column++)
			{
				var position = new CellPosition(row, column);
				var button = new Button
				{
					AutomationId = $"BoardCell_{row + 1}_{column + 1}",
					BackgroundColor = Colors.Transparent,
					BorderColor = Colors.Transparent,
					BorderWidth = 0,
					CornerRadius = 0,
					Padding = 0,
					Margin = 0,
					MinimumHeightRequest = 1,
					MinimumWidthRequest = 1,
					Text = string.Empty,
					TextColor = Colors.Transparent
				};
				SemanticProperties.SetDescription(button, $"Linha {row + 1}, coluna {column + 1}. Vazia. Editável.");
				button.Clicked += (_, _) => CellSelected?.Invoke(this, new BoardCellSelectedEventArgs(position));
				_cellButtons[row, column] = button;
				AccessibleCells.Add(button);
				Grid.SetRow(button, row);
				Grid.SetColumn(button, column);
			}
		}
	}

	private void OnStartInteraction(object? sender, TouchEventArgs eventArgs)
	{
		if (eventArgs.Touches.Length == 0)
			return;

		var touch = eventArgs.Touches[0];
		var geometry = BoardGeometry.Fit((float)BoardCanvas.Width, (float)BoardCanvas.Height);
		if (geometry.TryHitTest(touch.X, touch.Y, out var position))
			CellSelected?.Invoke(this, new BoardCellSelectedEventArgs(position));
	}
}

public sealed class BoardCellSelectedEventArgs(CellPosition position) : EventArgs
{
	public CellPosition Position { get; } = position;
}
