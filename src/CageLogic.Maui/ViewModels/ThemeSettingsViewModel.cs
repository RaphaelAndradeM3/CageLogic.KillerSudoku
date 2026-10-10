using CageLogic.Application.Progression;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;

namespace CageLogic.Maui.ViewModels;

public partial class ThemeSettingsViewModel : ObservableObject
{
	private readonly ThemePreferenceUseCase _themePreference;
	private readonly ILogger<ThemeSettingsViewModel> _logger;

	[ObservableProperty]
	public partial AppThemePreference? SelectedPreference { get; set; }

	[ObservableProperty]
	public partial string StatusMessage { get; set; } = string.Empty;

	public ThemeSettingsViewModel(ThemePreferenceUseCase themePreference, ILogger<ThemeSettingsViewModel> logger)
	{
		_themePreference = themePreference;
		_logger = logger;
	}

	public string SystemButtonText => SelectedPreference is null ? "Sistema (selecionado)" : "Sistema";
	public string LightButtonText => SelectedPreference == AppThemePreference.Light ? "Claro (selecionado)" : "Claro";
	public string DarkButtonText => SelectedPreference == AppThemePreference.Dark ? "Escuro (selecionado)" : "Escuro";
	public string CurrentThemeDescription => SelectedPreference switch
	{
		AppThemePreference.Light => "Tema claro aplicado e salvo neste dispositivo.",
		AppThemePreference.Dark => "Tema escuro aplicado e salvo neste dispositivo.",
		_ => $"Seguindo o tema do sistema ({GetSystemThemeLabel()})."
	};

	public void Initialize()
	{
		SelectedPreference = _themePreference.Get();
		ApplySelectedTheme();
		NotifySelectionChanged();
	}

	[RelayCommand]
	private void UseSystemTheme() => SavePreference(null);

	[RelayCommand]
	private void UseLightTheme() => SavePreference(AppThemePreference.Light);

	[RelayCommand]
	private void UseDarkTheme() => SavePreference(AppThemePreference.Dark);

	private void SavePreference(AppThemePreference? preference)
	{
		try
		{
			_themePreference.Set(preference);
			SelectedPreference = preference;
			ApplySelectedTheme();
			StatusMessage = "Preferência de tema salva.";
			NotifySelectionChanged();
		}
		catch (Exception exception)
		{
			var correlationId = Guid.NewGuid().ToString("N");
			_logger.LogError(exception, "Saving theme preference failed (CorrelationId {CorrelationId})", correlationId);
			StatusMessage = $"Não foi possível salvar a preferência. Código: {correlationId}";
		}
	}

	private void ApplySelectedTheme()
	{
		if (Microsoft.Maui.Controls.Application.Current is not { } application)
			return;
		application.UserAppTheme = SelectedPreference switch
		{
			AppThemePreference.Light => AppTheme.Light,
			AppThemePreference.Dark => AppTheme.Dark,
			_ => AppTheme.Unspecified
		};
	}

	private static string GetSystemThemeLabel() =>
		Microsoft.Maui.Controls.Application.Current?.PlatformAppTheme == AppTheme.Dark ? "escuro" : "claro";

	partial void OnSelectedPreferenceChanged(AppThemePreference? value) => NotifySelectionChanged();

	private void NotifySelectionChanged()
	{
		OnPropertyChanged(nameof(SystemButtonText));
		OnPropertyChanged(nameof(LightButtonText));
		OnPropertyChanged(nameof(DarkButtonText));
		OnPropertyChanged(nameof(CurrentThemeDescription));
	}
}
