using Microsoft.Maui.Controls;

#if WINDOWS
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;
#endif

namespace CageLogic.Maui.Controls;

/// <summary>Routes hardware keyboard input to the same page intents used by touch controls.</summary>
public sealed class BoardInputBehavior : Behavior<ContentPage>
{
	private static BoardInputBehavior? _activeBehavior;
	private ContentPage? _page;

#if WINDOWS
	private UIElement? _nativeRoot;
#endif

	public event EventHandler<BoardKeyInputEventArgs>? KeyPressed;

	protected override void OnAttachedTo(ContentPage bindable)
	{
		base.OnAttachedTo(bindable);
		_page = bindable;
		bindable.Appearing += OnAppearing;
		bindable.Disappearing += OnDisappearing;
		bindable.HandlerChanged += OnHandlerChanged;
	}

	protected override void OnDetachingFrom(ContentPage bindable)
	{
		bindable.Appearing -= OnAppearing;
		bindable.Disappearing -= OnDisappearing;
		bindable.HandlerChanged -= OnHandlerChanged;
		if (ReferenceEquals(_activeBehavior, this))
			_activeBehavior = null;
#if WINDOWS
		DetachWindowsKeyboard();
#endif
		_page = null;
		base.OnDetachingFrom(bindable);
	}

	public static bool TryHandleHardwareKey(string key, bool controlPressed = false)
	{
		return _activeBehavior?.ProcessKey(key, controlPressed) ?? false;
	}

	private void OnAppearing(object? sender, EventArgs eventArgs)
	{
		_activeBehavior = this;
#if WINDOWS
		AttachWindowsKeyboard();
#endif
	}

	private void OnDisappearing(object? sender, EventArgs eventArgs)
	{
		if (ReferenceEquals(_activeBehavior, this))
			_activeBehavior = null;
#if WINDOWS
		DetachWindowsKeyboard();
#endif
	}

	private void OnHandlerChanged(object? sender, EventArgs eventArgs)
	{
#if WINDOWS
		if (_page?.IsVisible == true)
			AttachWindowsKeyboard();
#endif
	}

	private bool ProcessKey(string key, bool controlPressed)
	{
		if (!IsSupportedKey(key, controlPressed))
			return false;

		KeyPressed?.Invoke(this, new BoardKeyInputEventArgs(key, controlPressed));
		return true;
	}

	private static bool IsSupportedKey(string key, bool controlPressed)
	{
		if (controlPressed)
			return key.Equals("Z", StringComparison.OrdinalIgnoreCase) || key.Equals("Y", StringComparison.OrdinalIgnoreCase);

		return TryGetDirection(key, out _, out _) ||
			IsClearKey(key) ||
			TryGetDigit(key, out _);
	}

	public static bool TryGetDirection(string key, out int rowDelta, out int columnDelta)
	{
		rowDelta = 0;
		columnDelta = 0;
		if (Matches(key, "Left", "ArrowLeft", "DpadLeft", "DPAD_LEFT", "KEYCODE_DPAD_LEFT"))
			columnDelta = -1;
		else if (Matches(key, "Right", "ArrowRight", "DpadRight", "DPAD_RIGHT", "KEYCODE_DPAD_RIGHT"))
			columnDelta = 1;
		else if (Matches(key, "Up", "ArrowUp", "DpadUp", "DPAD_UP", "KEYCODE_DPAD_UP"))
			rowDelta = -1;
		else if (Matches(key, "Down", "ArrowDown", "DpadDown", "DPAD_DOWN", "KEYCODE_DPAD_DOWN"))
			rowDelta = 1;
		else
			return false;

		return true;
	}

	public static bool IsClearKey(string key) => Matches(
		key,
		"Backspace",
		"Delete",
		"Del",
		"Back",
		"ForwardDel",
		"ForwardDelete");

	public static bool TryGetDigit(string key, out int digit)
	{
		digit = 0;
		if (key is null)
			return false;

		if (key.Length == 1 && char.IsAsciiDigit(key[0]))
		{
			digit = key[0] - '0';
			return digit is >= 1 and <= 9;
		}

		if (key.Length == 2 && (key[0] is 'D' or 'd') && key[1] is >= '1' and <= '9')
		{
			digit = key[1] - '0';
			return true;
		}

		foreach (var prefix in new[] { "Num", "Number", "NumberPad", "Numpad", "Digit" })
		{
			if (key.Length != prefix.Length + 1 ||
				!key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
				key[^1] is < '1' or > '9')
				continue;

			digit = key[^1] - '0';
			return true;
		}

		return false;
	}

	private static bool Matches(string key, params string[] names) =>
		key is not null && names.Any(name => key.Equals(name, StringComparison.OrdinalIgnoreCase));

#if WINDOWS
	private void AttachWindowsKeyboard()
	{
		if (_page?.Window?.Handler?.PlatformView is not Microsoft.UI.Xaml.Window nativeWindow || nativeWindow.Content is not UIElement root)
			return;
		if (ReferenceEquals(root, _nativeRoot))
			return;

		DetachWindowsKeyboard();
		_nativeRoot = root;
		_nativeRoot.KeyDown += OnWindowsKeyDown;
	}

	private void DetachWindowsKeyboard()
	{
		if (_nativeRoot is null)
			return;
		_nativeRoot.KeyDown -= OnWindowsKeyDown;
		_nativeRoot = null;
	}

	private void OnWindowsKeyDown(object sender, KeyRoutedEventArgs eventArgs)
	{
		var controlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
		if (ProcessKey(eventArgs.Key.ToString(), controlPressed))
			eventArgs.Handled = true;
	}
#endif
}

public sealed class BoardKeyInputEventArgs(string key, bool controlPressed) : EventArgs
{
	public string Key { get; } = key;

	public bool ControlPressed { get; } = controlPressed;
}
