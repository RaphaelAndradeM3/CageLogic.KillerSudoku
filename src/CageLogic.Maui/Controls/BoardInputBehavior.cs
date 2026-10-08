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
			return false;

		return key.Contains("Left", StringComparison.OrdinalIgnoreCase) ||
			key.Contains("Right", StringComparison.OrdinalIgnoreCase) ||
			key.Contains("Up", StringComparison.OrdinalIgnoreCase) ||
			key.Contains("Down", StringComparison.OrdinalIgnoreCase) ||
			key.Contains("Backspace", StringComparison.OrdinalIgnoreCase) ||
			key.Contains("Delete", StringComparison.OrdinalIgnoreCase) ||
			key.Equals("Del", StringComparison.OrdinalIgnoreCase) ||
			TryGetDigit(key, out _);
	}

	public static bool TryGetDigit(string key, out int digit)
	{
		digit = 0;
		if (key.Length == 1 && char.IsAsciiDigit(key[0]))
		{
			digit = key[0] - '0';
			return digit is >= 1 and <= 9;
		}

		if ((key.StartsWith("D", StringComparison.OrdinalIgnoreCase) ||
			key.StartsWith("Num", StringComparison.OrdinalIgnoreCase) ||
			key.StartsWith("Number", StringComparison.OrdinalIgnoreCase) ||
			key.StartsWith("Digit", StringComparison.OrdinalIgnoreCase)) &&
			char.IsAsciiDigit(key[^1]))
		{
			digit = key[^1] - '0';
			return digit is >= 1 and <= 9;
		}

		return false;
	}

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
