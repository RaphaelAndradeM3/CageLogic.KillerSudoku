using CageLogic.Application.Progression;
using Microsoft.Maui.Storage;

namespace CageLogic.Maui.Preferences;

/// <summary>Stores the optional appearance choice in the app-private MAUI preferences store.</summary>
public sealed class MauiThemePreferenceStore : IThemePreferenceStore
{
	private const string PreferenceKey = "appearance.theme";

	public AppThemePreference? Get()
	{
		var storedValue = global::Microsoft.Maui.Storage.Preferences.Default.Get(PreferenceKey, string.Empty);
		return Enum.TryParse<AppThemePreference>(storedValue, ignoreCase: false, out var preference) && Enum.IsDefined(preference)
			? preference
			: null;
	}

	public void Set(AppThemePreference? preference)
	{
		if (preference.HasValue && !Enum.IsDefined(preference.Value))
			throw new ArgumentOutOfRangeException(nameof(preference));
		if (preference is null)
			global::Microsoft.Maui.Storage.Preferences.Default.Remove(PreferenceKey);
		else
			global::Microsoft.Maui.Storage.Preferences.Default.Set(PreferenceKey, preference.Value.ToString());
	}
}
